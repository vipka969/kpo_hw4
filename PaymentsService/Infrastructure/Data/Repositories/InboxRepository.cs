using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Interfaces;
using PaymentsService.Models.Entities;
using PaymentsService.Models.Enums;

namespace PaymentsService.Infrastructure.Data.Repositories;

public class InboxRepository : IInboxRepository
{
    private readonly PaymentsDbContext _context;

    public InboxRepository(PaymentsDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(InboxMessage message)
    {
        await _context.InboxMessages.AddAsync(message);
    }

    public async Task<bool> ExistsByMessageIdAsync(Guid messageId)
    {
        return await _context.InboxMessages.AnyAsync(m => m.MessageId == messageId);
    }

    public async Task<IEnumerable<InboxMessage>> GetNewMessagesAsync(int batchSize)
    {
        return await _context.InboxMessages
            .Where(m => m.Status == InboxMesStatus.New)
            .OrderBy(m => m.CreatedDate)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task UpdateAsync(InboxMessage message)
    {
        _context.InboxMessages.Update(message);
        await Task.CompletedTask;
    }
}