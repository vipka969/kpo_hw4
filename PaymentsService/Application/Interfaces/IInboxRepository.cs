using PaymentsService.Models.Entities;

namespace PaymentsService.Application.Interfaces;

public interface IInboxRepository
{
    Task AddAsync(InboxMessage message);
    Task<bool> ExistsByMessageIdAsync(Guid messageId);
    Task<IEnumerable<InboxMessage>> GetNewMessagesAsync(int batchSize);
    Task UpdateAsync(InboxMessage message);
}