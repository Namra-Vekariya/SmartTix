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

    public async Task<List<Booking>> GetExpiredHoldsAsync()
    {
        return await _context.Bookings
            .Include(b => b.BookingSeats)
            .Where(b =>
                b.Status == BookingStatus.Pending &&
                b.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();
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