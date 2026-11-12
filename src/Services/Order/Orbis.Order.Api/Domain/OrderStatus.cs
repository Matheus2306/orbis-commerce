using System;

namespace Orbis.Order.Api.Domain;

public enum OrderStatus
{
    PendingReservation = 1,
    Reserved = 2,
    Rejected = 3
}
