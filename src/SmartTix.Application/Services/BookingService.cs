using System.Text.Json;
using SmartTix.Application.DTOs.Bookings;
using SmartTix.Application.DTOs.Seats;
using SmartTix.Application.Interfaces.Infrastructure;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Application.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IRedisService _redisService;
    private readonly IUserRepository _userRepository;
    private readonly ISeatNotifier _seatNotifier;
    private readonly IOutboxRepository _outboxRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        ISeatRepository seatRepository,
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IRedisService redisService,
        IOutboxRepository outboxRepository,
        ISeatNotifier seatNotifier)
    {
        _bookingRepository = bookingRepository;
        _seatRepository = seatRepository;
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _redisService = redisService;
        _outboxRepository = outboxRepository;
        _seatNotifier = seatNotifier;
    }

    public async Task<HoldSeatsResponse> HoldSeatsAsync(
        HoldSeatsRequest request,
        Guid userId)
    {
        // ── Step 1: Rate limit check ─────────────────────────
        var allowed = await _redisService.CheckRateLimitAsync(userId);
        if (!allowed)
            throw new InvalidOperationException(
                "Too many booking attempts. Please wait a minute before trying again.");

        // ── Step 1.5: Validate user is not an admin ────────────
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
            throw new UnauthorizedAccessException("User not found");

        if (user.Role == Domain.Enums.UserRole.Admin)
            throw new InvalidOperationException(
                "Admin accounts cannot book tickets. Please use a customer account.");

        // ── Step 2: Validate event exists and is bookable ────
        var eventEntity = await _eventRepository.GetByIdWithCategoriesAsync(request.EventId);
        if (eventEntity is null)
            throw new KeyNotFoundException("Event not found");

        if (eventEntity.Status != EventStatus.Published)
            throw new InvalidOperationException(
                "This event is not available for booking");

        // ── Step 3: Validate seats belong to this event ─────
        var seats = await _seatRepository.GetByEventIdAsync(request.EventId);

        var requestedSeats = seats
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToList();

        if (requestedSeats.Count != request.SeatIds.Count)
            throw new InvalidOperationException(
                "One or more seats do not belong to this event");

        // ── Step 4: Check all seats are available ────────────
        var unavailableSeats = requestedSeats
            .Where(s => s.Status != SeatStatus.Available)
            .ToList();

        if (unavailableSeats.Any())
        {
            var codes = string.Join(", ", unavailableSeats.Select(s => s.SeatCode));
            throw new InvalidOperationException(
                $"Seats {codes} are no longer available");
        }

        // ── Step 5: Acquire Redis lock for each seat ─────────
        // Track which seats we successfully locked
        // If any seat fails, we must release all previously acquired locks
        var lockedSeatIds = new List<Guid>();
        var holdId = Guid.NewGuid().ToString();
        var totalAmount = requestedSeats.Sum(s => s.SeatCategory.Price);
        Booking booking;

        try
        {
            foreach (var seat in requestedSeats)
            {
                var lockAcquired = await _redisService.AcquireSeatLockAsync(
                    request.EventId,
                    seat.Id,
                    userId);

                if (!lockAcquired)
                {
                    // This seat was taken by someone else between our check and lock
                    throw new InvalidOperationException(
                        $"Seat {seat.SeatCode} was just taken by another user. " +
                        $"Please select a different seat.");
                }

                lockedSeatIds.Add(seat.Id);
            }

            // ── Step 6: Create pending booking in PostgreSQL ─────
            // Seats are marked Held in the same transaction as the booking so
            // the REST seat map and availability counts reflect the hold for
            // users who load the page later. Redis locks are released if the
            // database write fails — otherwise seats would stay locked for the
            // full TTL with no booking record.
            foreach (var seat in requestedSeats)
            {
                seat.Status = SeatStatus.Held;
                seat.UpdatedAt = DateTime.UtcNow;
            }

            booking = new Booking
            {
                BookingReference = GenerateBookingReference(),
                UserId = userId,
                EventId = request.EventId,
                Status = BookingStatus.Pending,
                TotalAmount = totalAmount,
                PaymentStatus = PaymentStatus.Pending,
                HoldId = holdId,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                BookingSeats = requestedSeats.Select(s => new BookingSeat
                {
                    SeatId = s.Id,
                    PriceAtBooking = s.SeatCategory.Price
                }).ToList()
            };

            await _bookingRepository.AddAsync(booking);
            await _bookingRepository.SaveChangesAsync();
        }
        catch
        {
            // Release every lock we hold before re-throwing.
            // Safe even if no locks were acquired (key deletion is idempotent).
            foreach (var seatId in lockedSeatIds)
                await _redisService.ReleaseSeatLockAsync(request.EventId, seatId);

            // The failed SaveChangesAsync rolled back the transaction, but the
            // tracked seat entities still carry Held — restore them so this
            // DbContext cannot persist a stale hold later.
            foreach (var seat in requestedSeats)
                seat.Status = SeatStatus.Available;

            throw;
        }

        // Broadcast only after the commit — live clients must never see a state
        // the database does not have. Notifier is best-effort (never throws).
        await _seatNotifier.NotifySeatsChangedAsync(BuildSeatChange(
            request.EventId,
            SeatStatus.Held,
            "held",
            requestedSeats));

        // ── Step 7: Return hold response ─────────────────────
        return new HoldSeatsResponse
        {
            HoldId = Guid.Parse(holdId),
            EventId = request.EventId,
            EventName = eventEntity.Name,
            ExpiresAt = booking.ExpiresAt,
            TotalAmount = totalAmount,
            Seats = requestedSeats.Select(s => new HeldSeatDto
            {
                SeatId = s.Id,
                SeatCode = s.SeatCode,
                CategoryName = s.SeatCategory.Name,
                Price = s.SeatCategory.Price,
                RowLabel = s.RowLabel,
                SeatNumber = s.SeatNumber
            }).ToList()
        };
    }

    public async Task<BookingDetailDto> ConfirmBookingAsync(
        ConfirmBookingRequest request,
        Guid userId)
    {
        // ── Step 1: Find the booking by holdId ───────────────
        var booking = await _bookingRepository.GetByHoldIdAsync(request.HoldId.ToString());

        if (booking is null)
            throw new KeyNotFoundException("Booking hold not found");

        // ── Step 2: Ownership check ──────────────────────────
        if (booking.UserId != userId)
            throw new UnauthorizedAccessException(
                "You are not authorized to confirm this booking");

        // ── Step 3: Check hold hasn't expired ────────────────
        if (booking.ExpiresAt < DateTime.UtcNow)
            throw new InvalidOperationException(
                "Your hold has expired. Please select seats again.");

        // ── Step 4: Check booking is still pending ───────────
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException(
                "This booking has already been processed");

        // ── Step 5: Simulate payment ─────────────────────────
        // 90% success rate — realistic mock
        // Card details from request are intentionally NOT saved anywhere
        var paymentSucceeded = SimulateMockPayment();

        if (!paymentSucceeded)
        {
            booking.PaymentStatus = PaymentStatus.Failed;
            booking.UpdatedAt = DateTime.UtcNow;

            await _bookingRepository.SaveChangesAsync();

            throw new InvalidOperationException(
                "Payment failed. Your seats are still held — please try again.");
        }

        // ── Step 6: Confirm booking ──────────────────────────
        booking.Status = BookingStatus.Confirmed;
        booking.PaymentStatus = PaymentStatus.Paid;
        booking.PaymentReference = $"MOCK-{Guid.NewGuid().ToString()[..8].ToUpper()}";
        booking.UpdatedAt = DateTime.UtcNow;

        // Update seat status to permanently booked in PostgreSQL
        foreach (var bookingSeat in booking.BookingSeats)
        {
            bookingSeat.Seat.Status = SeatStatus.Booked;
            bookingSeat.Seat.UpdatedAt = DateTime.UtcNow;
        }

        // Save booker details snapshot
        // User may change their profile later — booking keeps what was true now
        booking.BookerName = request.FullName;
        booking.BookerEmail = request.Email;
        booking.BookerPhone = request.Phone;

        // Transactional outbox: the confirmation email is enqueued in the same
        // SaveChanges as the booking itself (both repositories share the scoped
        // DbContext), so a crash can never lose the email after a successful
        // confirm. OutboxEmailWorker delivers it asynchronously with retries.
        await _outboxRepository.AddAsync(new OutboxMessage
        {
            EventType = OutboxMessage.BookingConfirmedEvent,
            Payload = JsonSerializer.Serialize(new { BookingId = booking.Id }),
            Status = OutboxMessageStatus.Pending
        });

        await _bookingRepository.SaveChangesAsync();

        // Seats are Booked in PostgreSQL now — the Redis hold keys have served
        // their purpose. Delete them instead of waiting for the TTL so the
        // seats are immediately re-holdable if this booking is later refunded.
        await ReleaseBookingLocksAsync(booking);

        await _seatNotifier.NotifySeatsChangedAsync(BuildSeatChange(
            booking.EventId,
            SeatStatus.Booked,
            "confirmed",
            booking.BookingSeats.Select(bs => bs.Seat)));

        return MapToBookingDetailDto(booking);
    }

    public async Task<BookingDetailDto> GetBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId);

        if (booking is null)
            throw new KeyNotFoundException("Booking not found");

        // Security check — user can only see their own bookings
        if (booking.UserId != userId)
            throw new UnauthorizedAccessException(
                "You are not authorized to view this booking");

        return MapToBookingDetailDto(booking);
    }

    public async Task<List<BookingDetailDto>> GetUserBookingsAsync(Guid userId)
    {
        var bookings = await _bookingRepository.GetByUserIdAsync(userId);
        return bookings.Select(MapToBookingDetailDto).ToList();
    }

    public async Task CancelBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await _bookingRepository.GetByIdWithDetailsAsync(bookingId);

        if (booking is null)
            throw new KeyNotFoundException("Booking not found");

        if (booking.UserId != userId)
            throw new UnauthorizedAccessException(
                "You are not authorized to cancel this booking");

        if (booking.Status == BookingStatus.Cancelled)
            throw new InvalidOperationException("Booking is already cancelled");

        if (booking.Status == BookingStatus.Expired)
            throw new InvalidOperationException(
                "This hold has already expired. The seats have been released.");

        if (booking.Status == BookingStatus.Confirmed)
            throw new InvalidOperationException(
                "Confirmed bookings cannot be cancelled directly. Contact support for refunds.");

        // Release Redis locks if still pending
        if (booking.Status == BookingStatus.Pending)
            await ReleaseBookingLocksAsync(booking);

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        // Free up seats back to available
        ReleaseHeldSeats(booking);

        await _bookingRepository.SaveChangesAsync();

        await _seatNotifier.NotifySeatsChangedAsync(BuildSeatChange(
            booking.EventId,
            SeatStatus.Available,
            "cancelled",
            booking.BookingSeats.Select(bs => bs.Seat)));
    }

    // ── Private helpers ──────────────────────────────────────

    private static SeatsChangedEvent BuildSeatChange(
        Guid eventId,
        SeatStatus newStatus,
        string reason,
        IEnumerable<Seat> seats)
    {
        return new SeatsChangedEvent
        {
            EventId = eventId,
            Status = newStatus.ToString(),
            Reason = reason,
            Seats = seats.Select(s => new ChangedSeatDto
            {
                SeatId = s.Id,
                SeatCode = s.SeatCode
            }).ToList()
        };
    }

    private static void ReleaseHeldSeats(Booking booking)
    {
        // Only seats this booking is actually holding may be freed —
        // a Blocked seat (admin) or an already-Booked seat must survive.
        foreach (var bookingSeat in booking.BookingSeats)
        {
            if (bookingSeat.Seat.Status != SeatStatus.Held)
                continue;

            bookingSeat.Seat.Status = SeatStatus.Available;
            bookingSeat.Seat.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task ReleaseBookingLocksAsync(Booking booking)
    {
        foreach (var bookingSeat in booking.BookingSeats)
            await _redisService.ReleaseSeatLockAsync(
                booking.EventId,
                bookingSeat.SeatId);
    }

    private static bool SimulateMockPayment()
    {
        // 90% success rate — realistic simulation
        return Random.Shared.Next(1, 11) <= 9;
    }

    private static string GenerateBookingReference()
    {
        // Format: SMX-2026-XXXXX
        // Year makes references sortable and tells you when booking was made
        var year = DateTime.UtcNow.Year;
        var random = Random.Shared.Next(10000, 99999);
        return $"SMX-{year}-{random}";
    }

    private static BookingDetailDto MapToBookingDetailDto(Booking booking)
    {
        return new BookingDetailDto
        {
            Id = booking.Id,
            BookingReference = booking.BookingReference,
            Status = booking.Status.ToString(),
            PaymentStatus = booking.PaymentStatus.ToString(),
            TotalAmount = booking.TotalAmount,
            BookedAt = booking.CreatedAt,
            ExpiresAt = booking.ExpiresAt,
            BookerName = booking.BookerName ?? string.Empty,
            BookerEmail = booking.BookerEmail ?? string.Empty,
            BookerPhone = booking.BookerPhone ?? string.Empty,
            Event = new BookingEventDto
            {
                Id = booking.Event.Id,
                Name = booking.Event.Name,
                ArtistName = booking.Event.ArtistName,
                VenueName = booking.Event.VenueName,
                VenueCity = booking.Event.VenueCity,
                EventDate = booking.Event.EventDate,
                PosterImageUrl = booking.Event.PosterImageUrl
            },
            Seats = booking.BookingSeats.Select(bs => new BookingSeatDetailDto
            {
                SeatId = bs.SeatId,
                SeatCode = bs.Seat.SeatCode,
                RowLabel = bs.Seat.RowLabel,
                SeatNumber = bs.Seat.SeatNumber,
                CategoryName = bs.Seat.SeatCategory.Name,
                PriceAtBooking = bs.PriceAtBooking
            }).ToList()
        };
    }

}
