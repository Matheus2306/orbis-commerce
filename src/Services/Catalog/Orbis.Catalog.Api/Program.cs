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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/health/live", ()=> Results.Ok(new { status = "alive" }));

app.MapProductEndpoints();

app.Run();

