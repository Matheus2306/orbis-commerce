using Microsoft.EntityFrameworkCore;
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
