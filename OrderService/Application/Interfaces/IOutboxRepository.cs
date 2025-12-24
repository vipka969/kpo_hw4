using OrderService.Models.Entities;

namespace OrderService.Application.Interfaces;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task<IEnumerable<OutboxMessage>> GetPendingMessagesAsync(int size);
    Task UpdateAsync(OutboxMessage message);
}