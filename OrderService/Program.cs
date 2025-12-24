using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Interfaces;
using OrderService.Application.UseCases.CreateOrder;
using OrderService.Application.UseCases.GetOrders;
using OrderService.Application.UseCases.GetOrderById;
using OrderService.Application.DTOs;
using OrderService.Infrastructure.Data;
using OrderService.Infrastructure.Data.Repositories;
using OrderService.Infrastructure.Messaging.Kafka; // <- новый consumer
using IOutboxRepository = OrderService.Application.Interfaces.IOutboxRepository;
using IUnitOfWork = OrderService.Application.Interfaces.IUnitOfWork;
using OutboxRepository = OrderService.Infrastructure.Data.Repositories.OutboxRepository;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<CreateOrderHandler>();
builder.Services.AddScoped<GetOrdersHandler>();
builder.Services.AddScoped<GetOrderByIdHandler>();

builder.Services.AddSingleton<KafkaProducer>(sp =>
{
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    string bootstrapServers = configuration["Kafka:BootstrapServers"];
    return new KafkaProducer(bootstrapServers);
});
builder.Services.AddHostedService<KafkaConsumer>();

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();

app.MapPost("/orders", async (
    CreateOrderRequest request,
    [FromHeader(Name = "X-User-Id")] Guid userId,
    CreateOrderHandler handler) =>
{
    OrderResponse response = await handler.Handle(userId, request);
    return Results.Created($"/orders/{response.Id}", response);
});

app.MapGet("/orders", async (
    [FromHeader(Name = "X-User-Id")] Guid userId,
    GetOrdersHandler handler) =>
{
    IEnumerable<OrderResponse> responses = await handler.Handle(userId);
    return Results.Ok(responses);
});

app.MapGet("/orders/{id:guid}", async (
    Guid id,
    [FromHeader(Name = "X-User-Id")] Guid userId,
    GetOrderByIdHandler handler) =>
{
    OrderResponse response = await handler.Handle(id, userId);
    return response is not null ? Results.Ok(response) : Results.NotFound();
});

app.MapHealthChecks("/health");

using (IServiceScope scope = app.Services.CreateScope())
{
    OrderDbContext dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    await dbContext.Database.EnsureCreatedAsync();
    Console.WriteLine("Database tables created/verified");
}

app.Run();
