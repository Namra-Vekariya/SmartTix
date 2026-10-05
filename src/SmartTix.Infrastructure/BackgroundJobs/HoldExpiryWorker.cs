using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartTix.Application.DTOs.Seats;
using SmartTix.Application.Interfaces.Infrastructure;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Infrastructure.BackgroundJobs;

/// <summary>
/// Sweeps stale booking holds. Every tick it expires Pending bookings whose
/// ExpiresAt has passed: booking → Expired, Held seats → Available, Redis
/// seat locks released. Runs as a hosted service for the lifetime of the API.
/// </summary>
public class HoldExpiryWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(10);
    private const int BatchSize = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryWorker> _logger;

    public HoldExpiryWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hold expiry worker started (interval: {Interval}s, batch: {Batch})",
            PollingInterval.TotalSeconds, BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpireStaleHoldsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown — fall through to exit the loop.
            }
            catch (Exception ex)
            {
                // Never let a transient failure (DB blip, Redis outage) kill the
                // host — log it and try again on the next tick.
                _logger.LogError(ex, "Hold expiry sweep failed");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Hold expiry worker stopped");
    }

    private async Task ExpireStaleHoldsAsync(CancellationToken cancellationToken)
    {
        // Repositories are DbContext-bound and scoped, so each tick resolves its
        // own scope — same unit of work as an HTTP request.
        using var scope = _scopeFactory.CreateScope();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();
        var seatNotifier = scope.ServiceProvider.GetRequiredService<ISeatNotifier>();

        var now = DateTime.UtcNow;
        var candidates = await bookingRepository.GetExpiredHoldsAsync(BatchSize);

        var expiredCount = 0;

        foreach (var booking in candidates)
        {
            // Atomic in PostgreSQL: Pending → Expired + free Held seats, only if
            // the hold is still ours to expire. If confirm/cancel won the race,
            // TryExpireBookingAsync returns false and touches nothing — that flow
            // is responsible for its own Redis cleanup.
            var expired = await bookingRepository.TryExpireBookingAsync(booking.Id, now);
            if (!expired)
                continue;

            expiredCount++;

            // Best-effort Redis cleanup. Keys carry their own 600s TTL set at hold
            // time, so they expire regardless — a Redis outage here must never
            // abort the sweep; the DB is already consistent at this point.
            try
            {
                foreach (var bookingSeat in booking.BookingSeats)
                    await redisService.ReleaseSeatLockAsync(
                        booking.EventId,
                        bookingSeat.SeatId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to release Redis locks for booking {Reference}",
                    booking.BookingReference);
            }

            await seatNotifier.NotifySeatsChangedAsync(new SeatsChangedEvent
            {
                EventId = booking.EventId,
                Status = SeatStatus.Available.ToString(),
                Reason = "expired",
                Seats = booking.BookingSeats.Select(bs => new ChangedSeatDto
                {
                    SeatId = bs.Seat.Id,
                    SeatCode = bs.Seat.SeatCode
                }).ToList()
            });
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Hold expiry sweep expired {Count} hold(s)", expiredCount);
        }
    }
}
