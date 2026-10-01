using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Events;
using SmartTix.Application.DTOs.Seats;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Application.Interfaces.Services;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;

namespace SmartTix.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ISeatRepository _seatRepository;

    public EventService(
        IEventRepository eventRepository,
        ISeatRepository seatRepository)
    {
        _eventRepository = eventRepository;
        _seatRepository = seatRepository;
    }

    public async Task<PagedResult<EventListDto>> GetAllAsync(EventQueryParams queryParams)
    {
        var pagedEvents = await _eventRepository.GetAllAsync(queryParams);

        var dtos = pagedEvents.Items.Select(e => new EventListDto
        {
            Id = e.Id,
            Name = e.Name,
            ArtistName = e.ArtistName,
            VenueName = e.VenueName,
            VenueCity = e.VenueCity,
            EventDate = e.EventDate,
            PosterImageUrl = e.PosterImageUrl,
            Genre = e.Genre ?? string.Empty,
            Status = e.Status.ToString(),
            StartingPrice = e.SeatCategories.Any()
                ? e.SeatCategories.Min(sc => sc.Price)
                : 0,
            AvailableSeatsCount = e.Seats
                .Count(s => s.Status == SeatStatus.Available),
            TotalSeatsCount = e.Seats.Count
        }).ToList();

        return new PagedResult<EventListDto>
        {
            Items = dtos,
            TotalCount = pagedEvents.TotalCount,
            Page = pagedEvents.Page,
            PageSize = pagedEvents.PageSize
        };
    }

    public async Task<EventDetailDto> GetByIdAsync(Guid id)
    {
        var eventEntity = await _eventRepository.GetByIdWithCategoriesAsync(id);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event with ID {id} was not found");

        return MapToEventDetailDto(eventEntity);
    }

    public async Task<EventDetailDto> CreateAsync(CreateEventRequest request, Guid adminUserId)
    {
        // ── Validate request ─────────────────────────────────
        ValidateCreateRequest(request);

        // ── Create event entity ──────────────────────────────
        var eventEntity = new Event
        {
            Name = request.Name.Trim(),
            ArtistName = request.ArtistName.Trim(),
            Description = request.Description?.Trim(),
            Genre = request.Genre?.Trim(),
            VenueName = request.VenueName.Trim(),
            VenueCity = request.VenueCity.Trim(),
            VenueAddress = request.VenueAddress?.Trim(),
            EventDate = request.EventDate.ToUniversalTime(),
            DoorsOpenTime = request.DoorsOpenTime?.ToUniversalTime(),
            PosterImageUrl = request.PosterImageUrl?.Trim(),
            Status = EventStatus.Draft,
            CreatedByUserId = adminUserId
        };

        // ── Create seat categories ───────────────────────────
        var rowToCategory = new Dictionary<string, SeatCategory>();

        foreach (var categoryRequest in request.Categories)
        {
            var category = new SeatCategory
            {
                Name = categoryRequest.Name.Trim(),
                Price = categoryRequest.Price,
                ColorHex = categoryRequest.ColorHex?.Trim(),
                EventId = eventEntity.Id
            };

            eventEntity.SeatCategories.Add(category);

            // Map each row label to its category for seat generation below
            foreach (var row in categoryRequest.Rows)
                rowToCategory[row.ToUpper()] = category;
        }

        // ── Generate seat records ────────────────────────────
        // Convert number of rows to row labels A, B, C...
        var rowLabels = Enumerable
            .Range(0, request.NumberOfRows)
            .Select(i => ((char)('A' + i)).ToString())
            .ToList();

        var seats = new List<Seat>();

        foreach (var rowLabel in rowLabels)
        {
            // Validate every row is assigned to a category
            if (!rowToCategory.TryGetValue(rowLabel, out var category))
                throw new InvalidOperationException(
                    $"Row {rowLabel} is not assigned to any category. " +
                    $"All rows must belong to a category.");

            for (var seatNum = 1; seatNum <= request.SeatsPerRow; seatNum++)
            {
                seats.Add(new Seat
                {
                    EventId = eventEntity.Id,
                    SeatCategoryId = category.Id,
                    RowLabel = rowLabel,
                    SeatNumber = seatNum,
                    SeatCode = $"{rowLabel}-{seatNum}",
                    Status = SeatStatus.Available
                });
            }
        }

        // ── Save everything in one transaction ───────────────
        // Event + Categories + Seats — all or nothing
        await _eventRepository.AddAsync(eventEntity);
        await _seatRepository.AddRangeAsync(seats);
        await _eventRepository.SaveChangesAsync();

        // ── Return full detail ───────────────────────────────
        // Fetch fresh from DB to include generated IDs
        var created = await _eventRepository.GetByIdWithCategoriesAsync(eventEntity.Id);
        return MapToEventDetailDto(created!);
    }

    public async Task<EventDetailDto> UpdateAsync(Guid id, UpdateEventRequest request)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event with ID {id} was not found");

        // Only update fields that were actually sent (not null)
        // This is the partial update pattern
        if (request.Name is not null)
            eventEntity.Name = request.Name.Trim();

        if (request.ArtistName is not null)
            eventEntity.ArtistName = request.ArtistName.Trim();

        if (request.Description is not null)
            eventEntity.Description = request.Description.Trim();

        if (request.Genre is not null)
            eventEntity.Genre = request.Genre.Trim();

        if (request.VenueName is not null)
            eventEntity.VenueName = request.VenueName.Trim();

        if (request.VenueCity is not null)
            eventEntity.VenueCity = request.VenueCity.Trim();

        if (request.VenueAddress is not null)
            eventEntity.VenueAddress = request.VenueAddress.Trim();

        if (request.EventDate.HasValue)
            eventEntity.EventDate = request.EventDate.Value.ToUniversalTime();

        if (request.DoorsOpenTime.HasValue)
            eventEntity.DoorsOpenTime = request.DoorsOpenTime.Value.ToUniversalTime();

        if (request.PosterImageUrl is not null)
            eventEntity.PosterImageUrl = request.PosterImageUrl.Trim();

        if (request.Status.HasValue)
            eventEntity.Status = request.Status.Value;

        // Track when it was last updated
        eventEntity.UpdatedAt = DateTime.UtcNow;

        await _eventRepository.SaveChangesAsync();

        var updated = await _eventRepository.GetByIdWithCategoriesAsync(id);
        return MapToEventDetailDto(updated!);
    }

    public async Task DeleteAsync(Guid id)
    {
        var eventEntity = await _eventRepository.GetByIdAsync(id);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event with ID {id} was not found");

        // Soft delete — never hard delete events
        // Booking history must remain intact even after event is deleted
        eventEntity.IsDeleted = true;
        eventEntity.DeletedAt = DateTime.UtcNow;
        eventEntity.Status = EventStatus.Cancelled;

        await _eventRepository.SaveChangesAsync();
    }

    public async Task<SeatMapDto> GetSeatMapAsync(Guid eventId)
    {
        var eventEntity = await _eventRepository.GetByIdWithCategoriesAsync(eventId);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event with ID {eventId} was not found");

        var seats = await _seatRepository.GetByEventIdAsync(eventId);

        return new SeatMapDto
        {
            EventId = eventEntity.Id,
            EventName = eventEntity.Name,
            NumberOfRows = seats.Select(s => s.RowLabel).Distinct().Count(),
            SeatsPerRow = seats.Any()
                ? seats.GroupBy(s => s.RowLabel).Max(g => g.Count())
                : 0,
            Categories = eventEntity.SeatCategories.Select(sc => new SeatCategoryInfo
            {
                Id = sc.Id,
                Name = sc.Name,
                Price = sc.Price,
                ColorHex = sc.ColorHex
            }).ToList(),
            Seats = seats.Select(s => new SeatDto
            {
                Id = s.Id,
                RowLabel = s.RowLabel,
                SeatNumber = s.SeatNumber,
                SeatCode = s.SeatCode,
                Status = s.Status.ToString(),
                CategoryId = s.SeatCategoryId,
                CategoryName = s.SeatCategory.Name,
                Price = s.SeatCategory.Price,
                ColorHex = s.SeatCategory.ColorHex
            }).ToList()
        };
    }

    // ── Private helpers ──────────────────────────────────────

    private static void ValidateCreateRequest(CreateEventRequest request)
    {
        if (request.EventDate <= DateTime.UtcNow)
            throw new InvalidOperationException(
                "Event date must be in the future");

        if (request.DoorsOpenTime.HasValue &&
            request.DoorsOpenTime.Value >= request.EventDate)
            throw new InvalidOperationException(
                "Doors open time must be before event start time");

        // Verify all rows are covered by categories
        var rowLabels = Enumerable
            .Range(0, request.NumberOfRows)
            .Select(i => ((char)('A' + i)).ToString())
            .ToHashSet();

        var coveredRows = request.Categories
            .SelectMany(c => c.Rows.Select(r => r.ToUpper()))
            .ToHashSet();

        // Check for rows not assigned to any category
        var uncoveredRows = rowLabels.Except(coveredRows).ToList();
        if (uncoveredRows.Any())
            throw new InvalidOperationException(
                $"Rows {string.Join(", ", uncoveredRows)} are not assigned to any category");

        // Check for rows assigned to multiple categories
        var allRows = request.Categories
            .SelectMany(c => c.Rows.Select(r => r.ToUpper()))
            .ToList();

        var duplicateRows = allRows
            .GroupBy(r => r)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicateRows.Any())
            throw new InvalidOperationException(
                $"Rows {string.Join(", ", duplicateRows)} are assigned to multiple categories");
    }

    private static EventDetailDto MapToEventDetailDto(Event e)
    {
        return new EventDetailDto
        {
            Id = e.Id,
            Name = e.Name,
            ArtistName = e.ArtistName,
            Description = e.Description,
            Genre = e.Genre,
            VenueName = e.VenueName,
            VenueCity = e.VenueCity,
            VenueAddress = e.VenueAddress,
            EventDate = e.EventDate,
            DoorsOpenTime = e.DoorsOpenTime,
            PosterImageUrl = e.PosterImageUrl,
            Status = e.Status.ToString(),
            CreatedAt = e.CreatedAt,
            TotalSeatsCount = e.Seats.Count,
            AvailableSeatsCount = e.Seats
                .Count(s => s.Status == SeatStatus.Available),
            Categories = e.SeatCategories.Select(sc => new SeatCategoryDto
            {
                Id = sc.Id,
                Name = sc.Name,
                Price = sc.Price,
                ColorHex = sc.ColorHex,
                TotalSeats = e.Seats.Count(s => s.SeatCategoryId == sc.Id),
                AvailableSeats = e.Seats.Count(s =>
                    s.SeatCategoryId == sc.Id &&
                    s.Status == SeatStatus.Available)
            }).ToList()
        };
    }
}