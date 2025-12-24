using PaymentsService.Application.Interfaces;
using PaymentsService.Infrastructure.Events;
using PaymentsService.Models.Entities;

namespace PaymentsService.Application.UseCases.ProcessPayment;

public class ProcessPaymentHandler
{
    private readonly IAccountRepository _account;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessPaymentHandler(IAccountRepository account, IOutboxRepository outbox, IUnitOfWork unitOfWork)
    {
        _account = account;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
    }

    public async Task<PaymentProcessedEvent> Handle(Guid orderId, Guid userId, decimal amount, Guid correlationId)
    {
        Account? account = await _account.GetByUserIdAsync(userId);
    
        PaymentProcessedEvent paymentEvent;
    
        if (account == null)
        {
            paymentEvent = PaymentProcessedEvent.Failed(orderId, correlationId, "Account not found", amount);
        }
        else
        {
            bool success = account.TryWithdraw(amount, out string errorMessage);
        
            if (success)
            {
                paymentEvent = PaymentProcessedEvent.Success(orderId, correlationId, amount);
                await _account.UpdateAsync(account);
            }
            else
            {
                paymentEvent = PaymentProcessedEvent.Failed(orderId, correlationId, errorMessage, amount);
            }
        }

        OutboxMessage outboxMessage = OutboxMessage.Create(paymentEvent.EventId, paymentEvent);
        await _outbox.AddAsync(outboxMessage);
    
        await _unitOfWork.SaveChangesAsync();

        return paymentEvent;
    }
}