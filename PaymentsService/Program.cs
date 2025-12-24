using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Application.Interfaces;
using PaymentsService.Application.UseCases.CreateAccount;
using PaymentsService.Application.UseCases.Deposit;
using PaymentsService.Application.UseCases.GetAccount;
using PaymentsService.Application.UseCases.ProcessPayment;
using PaymentsService.Application.DTOs;
using PaymentsService.Infrastructure.Data;
using PaymentsService.Infrastructure.Data.Repositories;
using PaymentsService.Infrastructure.Messaging.Kafka;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<PaymentsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IInboxRepository, InboxRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddScoped<CreateAccountHandler>();
builder.Services.AddScoped<DepositHandler>();
builder.Services.AddScoped<GetAccountHandler>();
builder.Services.AddScoped<ProcessPaymentHandler>();

builder.Services.AddSingleton<IKafkaProducer>(sp =>
{
    IConfiguration configuration = sp.GetRequiredService<IConfiguration>();
    return new KafkaProducer(configuration);
});

builder.Services.AddHostedService<KafkaConsumer>();

builder.Services.AddHostedService<InboxProcessor>();
builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddHealthChecks();

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/accounts", async (CreateAccountRequest request, [FromHeader(Name = "X-User-Id")] Guid userId, CreateAccountHandler handler) =>
{
    AccountResponse account = await handler.Handle(userId, request);
    return Results.Created($"/accounts/{account.Id}", account);
});

app.MapPost("/accounts/deposit", async (DepositRequest request, [FromHeader(Name = "X-User-Id")] Guid userId, DepositHandler handler) =>
{
    AccountResponse account = await handler.Handle(userId, request);
    return Results.Ok(account);
});

app.MapGet("/accounts", async ([FromHeader(Name = "X-User-Id")] Guid userId, GetAccountHandler handler) =>
{
    AccountResponse account = await handler.Handle(userId);
    return account is not null ? Results.Ok(account) : Results.NotFound();
});

app.MapHealthChecks("/health");

using (IServiceScope scope = app.Services.CreateScope())
{
    PaymentsDbContext dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    
    await dbContext.Database.EnsureCreatedAsync();
    
    Console.WriteLine("Database tables created/verified");
}

app.Run();