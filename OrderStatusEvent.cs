using System;

namespace OrderService
{
    /// <summary>
    /// Immutable in-memory audit event. Sequence is scoped to an order,
    /// starting at one for its initial registration.
    /// </summary>
    public sealed record OrderStatusEvent(
        string OrderId,
        int Sequence,
        string? PreviousStatus,
        string Status,
        DateTimeOffset OccurredAtUtc);
}
