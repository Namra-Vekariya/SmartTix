using Microsoft.EntityFrameworkCore;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;
using SmartTix.Infrastructure.Data;

namespace SmartTix.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(Guid id)
    {
        return await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> GetByIdWithDetailsAsync(Guid id)
    {
        return await _context.Bookings
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
                    .ThenInclude(s => s.SeatCategory)
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<Booking?> GetByHoldIdAsync(string holdId)
    {
        return await _context.Bookings
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
                    .ThenInclude(s => s.SeatCategory)
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b => b.HoldId == holdId);
    }

    public async Task<List<Booking>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Bookings
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
            .Include(b => b.Event)
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Booking>> GetExpiredHoldsAsync(int limit)
    {
        return await _context.Bookings
            .Include(b => b.BookingSeats)
                .ThenInclude(bs => bs.Seat)
            .Where(b =>
                b.Status == BookingStatus.Pending &&
                b.ExpiresAt < DateTime.UtcNow)
            .OrderBy(b => b.ExpiresAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> TryExpireBookingAsync(Guid bookingId, DateTime utcNow)
    {
        // Atomic compare-and-swap inside one transaction:
        // 1. Flip Pending → Expired only if still Pending and past ExpiresAt.
        //    If confirm/cancel won the race, rows affected = 0 → nothing changes.
        // 2. Free only this booking's seats, and only while they are still Held —
        //    a seat that got re-held by another user is left alone.
        await using var transaction = await _context.Database.BeginTransactionAsync();

        var claimed = await _context.Bookings
            .Where(b =>
                b.Id == bookingId &&
                b.Status == BookingStatus.Pending &&
                b.ExpiresAt < utcNow)
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, BookingStatus.Expired)
                .SetProperty(b => b.UpdatedAt, utcNow));

        if (claimed == 0)
        {
            await transaction.RollbackAsync();
            return false;
        }

        await _context.Seats
            .Where(s =>
                _context.BookingSeats
                    .Any(bs => bs.BookingId == bookingId && bs.SeatId == s.Id) &&
                s.Status == SeatStatus.Held)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, SeatStatus.Available)
                .SetProperty(x => x.UpdatedAt, utcNow));

        await transaction.CommitAsync();
        return true;
    }

    public async Task AddAsync(Booking booking)
    {
        await _context.Bookings.AddAsync(booking);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}