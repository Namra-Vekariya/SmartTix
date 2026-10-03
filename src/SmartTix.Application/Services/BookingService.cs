using SmartTix.Application.DTOs.Bookings;
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

    public BookingService(
        IBookingRepository bookingRepository,
        ISeatRepository seatRepository,
        IEventRepository eventRepository,
        IRedisService redisService)
    {
        _bookingRepository = bookingRepository;
        _seatRepository = seatRepository;
        _eventRepository = eventRepository;
        _redisService = redisService;
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
                    // Release all locks we already acquired — partial holds not allowed
                    throw new InvalidOperationException(
                        $"Seat {seat.SeatCode} was just taken by another user. " +
                        $"Please select a different seat.");
                }

                lockedSeatIds.Add(seat.Id);
            }
        }
        catch
        {
            // Release all successfully acquired locks before re-throwing
            foreach (var seatId in lockedSeatIds)
                await _redisService.ReleaseSeatLockAsync(request.EventId, seatId);

            throw;
        }

        // ── Step 6: Create pending booking in PostgreSQL ─────
        var holdId = Guid.NewGuid().ToString();
        var totalAmount = requestedSeats.Sum(s => s.SeatCategory.Price);

        var booking = new Booking
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
            // Payment failed — release locks, mark booking failed
            await ReleaseBookingLocksAsync(booking);
            booking.Status = BookingStatus.Cancelled;
            booking.PaymentStatus = PaymentStatus.Failed;
            await _bookingRepository.SaveChangesAsync();

            throw new InvalidOperationException(
                "Payment failed. Please try again with a different card.");
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

        await _bookingRepository.SaveChangesAsync();

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

        if (booking.Status == BookingStatus.Confirmed)
            throw new InvalidOperationException(
                "Confirmed bookings cannot be cancelled directly. Contact support for refunds.");

        // Release Redis locks if still pending
        if (booking.Status == BookingStatus.Pending)
            await ReleaseBookingLocksAsync(booking);

        booking.Status = BookingStatus.Cancelled;
        booking.UpdatedAt = DateTime.UtcNow;

        // Free up seats back to available
        foreach (var bookingSeat in booking.BookingSeats)
        {
            bookingSeat.Seat.Status = SeatStatus.Available;
            bookingSeat.Seat.UpdatedAt = DateTime.UtcNow;
        }

        await _bookingRepository.SaveChangesAsync();
    }

    // ── Private helpers ──────────────────────────────────────

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