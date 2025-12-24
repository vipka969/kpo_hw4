using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;
using PaymentsService.Models.Enums;

namespace PaymentsService.Infrastructure.Data.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly PaymentsDbContext _context;

    public OutboxRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
    }

    public async Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int size)
    {
        return await _context.OutboxMessages
            .Where(m => m.Status == OutboxMesStatus.Pending)
            .OrderBy(m => m.CreatedDate)
            .Take(size)
            .ToListAsync();
    }

    public async Task UpdateAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Update(message);
        await Task.CompletedTask;
    }
}