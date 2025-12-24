using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Models.Entities;
using OrderService.Models.Enums;

namespace OrderService.Infrastructure.Data.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly OrderDbContext _context;

    public OutboxRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int batchSize)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMesStatus.Pending)
            .OrderBy(m => m.CreatedDate)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task UpdateAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Update(message);
        await Task.CompletedTask;
    }
}