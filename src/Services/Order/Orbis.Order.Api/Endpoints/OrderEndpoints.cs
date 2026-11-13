using Orbis.Order.Api.Application;
using Orbis.Order.Api.Contracts;
using OrderEntity = Orbis.Order.Api.Domain.Order;

namespace Orbis.Order.Api.Endpoints;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders");

        group.MapPost("/", async (
            CreateOrderRequest request,
            OrderService service,
            CancellationToken ct) =>
        {
            if (request.ProductId == Guid.Empty ||
                request.Quantity <= 0)
            {
                return Results.BadRequest(new
                {
                    error = "Produto ou quantidade inválida."
                });
            }

            var order = await service.CreateAsync(
                request, ct);

            return Results.Accepted(
                $"/api/orders/{order.Id}",
                ToResponse(order));
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            OrderService service,
            CancellationToken ct) =>
        {
            var order = await service.GetByIdAsync(id, ct);

            return order is null
                ? Results.NotFound()
                : Results.Ok(ToResponse(order));
        });

        return app;
    }

    private static object ToResponse(OrderEntity order)
    {
        return new
        {
            order.Id,
            order.ProductId,
            order.Quantity,
            Status = order.Status.ToString(),
            order.CreatedAt,
            order.UpdatedAt
        };
    }
}
