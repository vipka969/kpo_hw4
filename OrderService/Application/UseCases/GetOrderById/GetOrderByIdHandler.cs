using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Models.Entities;

namespace OrderService.Application.UseCases.GetOrderById;

public class GetOrderByIdHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> Handle(Guid orderId, Guid userId)
    {
        Order? order = await _orderRepository.GetByIdAsync(orderId);

        if (order == null || order.UserId != userId)
        {
            return null;
        }

        return OrderResponse.FromDomain(order);
    }
}