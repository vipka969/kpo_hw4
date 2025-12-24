using OrderService.Application.DTOs;
using OrderService.Application.Interfaces;
using OrderService.Models.Entities;

namespace OrderService.Application.UseCases.GetOrders;

public class GetOrdersHandler
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderResponse>> Handle(Guid userId)
    {
        IEnumerable<Order> orders = await _orderRepository.GetByUserIdAsync(userId);
        return orders.Select(OrderResponse.FromDomain);
    }
}