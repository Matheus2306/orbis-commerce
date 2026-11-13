using MassTransit;
using Microsoft.EntityFrameworkCore;
using Orbis.Contracts.Events;
using Orbis.Order.Api.Contracts;
using Orbis.Order.Api.Data;
using OrderEntity = Orbis.Order.Api.Domain.Order;

namespace Orbis.Order.Api.Application;

public sealed class OrderService
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publisher;

    public OrderService(
        OrderDbContext db,
        IPublishEndpoint publisher)
    {
        _db = db;
        _publisher = publisher;
    }

    public async Task<OrderEntity> CreateAsync(
        CreateOrderRequest request,
        CancellationToken ct = default)
    {
        var order = new OrderEntity(
            request.ProductId,
            request.Quantity);

        _db.Orders.Add(order);

        var @event = new StockReservationRequested(
            EventId: Guid.NewGuid(),
            OrderId: order.Id,
            ProductId: order.ProductId,
            Quantity: order.Quantity,
            OccurredAt: DateTime.UtcNow
        );

        // O Bus Outbox registra a mensagem no DbContext.
        await _publisher.Publish(@event, ct);

        // Pedido e mensagem sao confirmados juntos.
        await _db.SaveChangesAsync(ct);

        return order;
    }

    public Task<OrderEntity?> GetByIdAsync(
        Guid id,
        CancellationToken ct = default)
    {
        return _db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);
    }
}