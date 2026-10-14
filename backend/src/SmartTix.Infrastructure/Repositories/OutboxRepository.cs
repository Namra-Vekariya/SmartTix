using Microsoft.EntityFrameworkCore;
using SmartTix.Application.Interfaces.Repositories;
using SmartTix.Domain.Entities;
using SmartTix.Domain.Enums;
using SmartTix.Infrastructure.Data;

namespace SmartTix.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    public OutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
    }

    public async Task<List<OutboxMessage>> GetPendingBatchAsync(int limit, int maxRetries)
    {
        return await _context.OutboxMessages
            .Where(m =>
                m.Status == OutboxMessageStatus.Pending &&
                m.RetryCount < maxRetries)
            .OrderBy(m => m.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
