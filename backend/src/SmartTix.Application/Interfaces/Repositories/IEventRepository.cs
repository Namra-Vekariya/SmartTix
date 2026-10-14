using SmartTix.Application.Common;
using SmartTix.Application.DTOs.Events;
using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Repositories;

public interface IEventRepository
{
    Task<PagedResult<Event>> GetAllAsync(EventQueryParams queryParams);
    Task<Event?> GetByIdAsync(Guid id);
    Task<Event?> GetByIdWithCategoriesAsync(Guid id);
    Task AddAsync(Event eventEntity);
    Task SaveChangesAsync();
}