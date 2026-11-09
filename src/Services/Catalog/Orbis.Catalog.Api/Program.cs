using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Catalog.Api.Application;
using Orbis.Catalog.Api.Data;
using Orbis.Catalog.Api.Endpoints;
using Orbis.Catalog.Api.Infrastructure.Repositories;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CatalogDb") ?? throw new InvalidOperationException("A connection string CatalogDb Não foi configurada.");

builder.Services.AddDbContext<CatalogDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddOpenApi();

// Registros de Repositórios
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Registros de serviços
builder.Services.AddScoped<IProductService, ProductService>();

// Serviço de cache Distribuidos
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration
        .GetConnectionString("Redis")
        ?? throw new InvalidOperationException(
            "Redis não configurado.");

    options.InstanceName = "orbis:catalog:";
});

// Registro de serviço de cache
builder.Services.AddScoped<CachedProductReader>();

builder.Services.AddMassTransit(x =>
{
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
    });
});

// MassTransit config
builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<CatalogDbContext>(options =>
    {
        options.UsePostgres();
        options.UseBusOutbox();
    });

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
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));

app.MapProductEndpoints();

app.Run();

