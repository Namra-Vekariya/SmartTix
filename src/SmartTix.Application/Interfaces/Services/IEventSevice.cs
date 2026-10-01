using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Events;
using SmartTix.Application.DTOs.Seats;

namespace SmartTix.Application.Interfaces.Services;

public interface IEventService
{
    Task<PagedResult<EventListDto>> GetAllAsync(EventQueryParams queryParams);
    Task<EventDetailDto> GetByIdAsync(Guid id);
    Task<EventDetailDto> CreateAsync(CreateEventRequest request, Guid adminUserId);
    Task<EventDetailDto> UpdateAsync(Guid id, UpdateEventRequest request);
    Task DeleteAsync(Guid id);
    Task<SeatMapDto> GetSeatMapAsync(Guid eventId);
}