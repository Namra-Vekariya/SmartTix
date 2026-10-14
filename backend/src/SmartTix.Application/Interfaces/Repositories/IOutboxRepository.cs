using SmartTix.Domain.Entities;

namespace SmartTix.Application.Interfaces.Repositories;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task<List<OutboxMessage>> GetPendingBatchAsync(int limit, int maxRetries);
    Task SaveChangesAsync();
}
