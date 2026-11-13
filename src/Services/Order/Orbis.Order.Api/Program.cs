using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Order.Api.Application;
using Orbis.Order.Api.Data;
using Orbis.Order.Api.Endpoints;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
    .GetConnectionString("OrderDb")
    ?? throw new InvalidOperationException(
        "Connection string OrderDb não configurada.");

builder.Services.AddDbContext<OrderDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<OrderService>();

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<OrderDbContext>(options =>
    {
        options.UsePostgres();
        options.UseBusOutbox();
    });

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(
            builder.Configuration["RabbitMq:Host"]
                ?? "localhost",
            "/",
            h =>
            {
                h.Username(
                    builder.Configuration["RabbitMq:Username"]
                    ?? throw new InvalidOperationException(
                        "Usuário RabbitMQ não configurado."));

                h.Password(
                    builder.Configuration["RabbitMq:Password"]
                    ?? throw new InvalidOperationException(
                        "Senha RabbitMQ não configurada."));
            });
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

app.MapOrderEndpoints();

app.Run();