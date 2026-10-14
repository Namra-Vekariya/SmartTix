using Microsoft.EntityFrameworkCore;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;
using SmartTix.Infrastructure.Data;

namespace SmartTix.Infrastructure.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _context;

    public SeatRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Seat>> GetByEventIdAsync(Guid eventId)
    {
        return await _context.Seats
            .Include(s => s.SeatCategory)
            .Where(s => s.EventId == eventId)
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Seat> seats)
    {
        await _context.Seats.AddRangeAsync(seats);
    }

    public async Task<int> GetAvailableCountByEventIdAsync(Guid eventId)
    {
        return await _context.Seats
            .CountAsync(s =>
                s.EventId == eventId &&
                s.Status == SeatStatus.Available);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}