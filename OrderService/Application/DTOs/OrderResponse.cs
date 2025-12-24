using OrderService.Models.Entities;
using OrderService.Models.Enums;

namespace OrderService.Application.DTOs;

public sealed record OrderResponse(
    Guid Id,
    Guid UserId,
    decimal Amount,
    string Description,
    string Status,
    DateTime CreatedDate)
{
    public static OrderResponse FromDomain(Order order) =>
        new(
            order.Id,
            order.UserId,
            order.Amount,
            order.Description,
            order.Status.ToString(),
            order.CreatedDate);
}