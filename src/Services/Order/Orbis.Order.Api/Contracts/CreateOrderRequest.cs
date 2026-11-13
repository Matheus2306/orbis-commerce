using System;

namespace Orbis.Order.Api.Contracts;

public sealed record CreateOrderRequest(Guid ProductId, int Quantity);