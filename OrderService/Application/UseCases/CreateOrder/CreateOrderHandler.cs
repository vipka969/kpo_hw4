using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.Events;
using OrderService.Models.Entities;

namespace OrderService.Application.UseCases.CreateOrder;

public class CreateOrderHandler
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderResponse> Handle(Guid userId, CreateOrderRequest request)
    {
        Order order = Order.Create(userId, request.Amount, request.Description);

        OrderCreatedEvent orderCreatedEvent = new OrderCreatedEvent
        {
            OrderId = order.Id,
            UserId = order.UserId,
            Amount = order.Amount,
            Description = order.Description
        };

        OutboxMessage outboxMessage = OutboxMessage.Create(
            orderCreatedEvent.EventId,
            orderCreatedEvent);

        await _orderRepository.AddAsync(order);
        await _outboxRepository.AddAsync(outboxMessage);
        await _unitOfWork.SaveChangesAsync();

        return OrderResponse.FromDomain(order);
    }
}