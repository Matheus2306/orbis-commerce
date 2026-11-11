using System;

namespace Orbis.Inventory.Api.Contracts;

public sealed record ReleaseStockRequest(Guid OrderId);
