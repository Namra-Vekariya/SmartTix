using Microsoft.EntityFrameworkCore;
using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Events;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;
using SmartTix.Infrastructure.Data;

namespace SmartTix.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<Event>> GetAllAsync(EventQueryParams queryParams)
    {
        // Start with base query — soft delete already filtered by global query filter
        var query = _context.Events
            .Include(e => e.SeatCategories)
            .Include(e => e.Seats)
            .AsQueryable();

        // ── Filters ─────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(queryParams.City))
            query = query.Where(e =>
                e.VenueCity.ToLower().Contains(queryParams.City.ToLower()));

        if (!string.IsNullOrWhiteSpace(queryParams.Genre))
            query = query.Where(e =>
                e.Genre != null &&
                e.Genre.ToLower() == queryParams.Genre.ToLower());

        if (!string.IsNullOrWhiteSpace(queryParams.Search))
            query = query.Where(e =>
                e.Name.ToLower().Contains(queryParams.Search.ToLower()) ||
                e.ArtistName.ToLower().Contains(queryParams.Search.ToLower()));

        if (queryParams.DateFrom.HasValue)
            query = query.Where(e => e.EventDate >= queryParams.DateFrom.Value);

        if (queryParams.DateTo.HasValue)
            query = query.Where(e => e.EventDate <= queryParams.DateTo.Value);

        if (queryParams.MinPrice.HasValue)
            query = query.Where(e =>
                e.SeatCategories.Any(sc => sc.Price >= queryParams.MinPrice.Value));

        if (queryParams.MaxPrice.HasValue)
            query = query.Where(e =>
                e.SeatCategories.Any(sc => sc.Price <= queryParams.MaxPrice.Value));

        // Only show upcoming and ongoing events on public listing
        query = query.Where(e =>
            e.Status == EventStatus.Published ||
            e.Status == EventStatus.Ongoing);

        // ── Sorting ──────────────────────────────────────────
        query = queryParams.SortBy.ToLower() switch
        {
            "price" => queryParams.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(e => e.SeatCategories.Min(sc => sc.Price))
                : query.OrderBy(e => e.SeatCategories.Min(sc => sc.Price)),

            "popularity" => queryParams.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(e => e.Bookings.Count)
                : query.OrderBy(e => e.Bookings.Count),

            // Default — sort by date
            _ => queryParams.SortDirection.ToLower() == "desc"
                ? query.OrderByDescending(e => e.EventDate)
                : query.OrderBy(e => e.EventDate)
        };

        // ── Pagination ───────────────────────────────────────
        // Get total count BEFORE applying skip/take
        // This is the count of ALL matching records not just current page
        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToListAsync();

        return new PagedResult<Event>
        {
            Items = items,
            TotalCount = totalCount,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }

    public async Task<Event?> GetByIdAsync(Guid id)
    {
        return await _context.Events
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Event?> GetByIdWithCategoriesAsync(Guid id)
    {
        return await _context.Events
            .Include(e => e.SeatCategories)
            .Include(e => e.Seats)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task AddAsync(Event eventEntity)
    {
        await _context.Events.AddAsync(eventEntity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}