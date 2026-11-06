using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Inventory.Api.Consumers;
using Orbis.Inventory.Api.Data;
using Orbis.Inventory.Api.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("InventoryDb")
    ?? throw new InvalidOperationException(
        "Connection string InventoryDb não configurada.");

builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<ProductCreatedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"]
                ?? "rabbitmq",
            "/",
            h =>
            {
                h.Username(
                    builder.Configuration["RabbitMq:Username"]
                        ?? throw new InvalidOperationException(
                            "RabbitMQ Username não configurado."));

                h.Password(
                    builder.Configuration["RabbitMq:Password"]
                        ?? throw new InvalidOperationException(
                            "RabbitMQ Password não configurado."));
            });

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

}

app.MapGet("/health/live", () =>
    Results.Ok(new { status = "alive" }));

app.MapStockEndpoints();

app.Run();
