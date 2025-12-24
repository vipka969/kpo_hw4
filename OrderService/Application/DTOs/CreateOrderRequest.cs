namespace OrderService.Application.DTOs;

public sealed record CreateOrderRequest(
    decimal Amount,
    string Description
    );