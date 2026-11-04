using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OrderService;
using Xunit;

namespace OrderService.Tests;

public sealed class OrderAuditTests
{
    [Fact]
    public void RecordsCreationAndOnlySuccessfulTransitionsInSequence()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order(" ORD-1 ", "Cliente", 120m, " pending "));
        var created = repository.GetOrderHistory("ORD-1");
        Assert.Single(created);
        Assert.Equal(1, created[0].Sequence);
        Assert.Null(created[0].PreviousStatus);
        Assert.Equal("PENDING", created[0].Status);
        Assert.Equal("ORD-1", created[0].OrderId);

        repository.UpdateOrderStatus("ORD-1", "confirmed");
        repository.UpdateOrderStatus("ORD-1", "CONFIRMED"); // idempotent
        repository.UpdateOrderStatus("ORD-1", "SHIPPED");
        Assert.Throws<InvalidOperationException>(() => repository.UpdateOrderStatus("ORD-1", "PENDING"));
        Assert.Throws<ArgumentException>(() => repository.UpdateOrderStatus("ORD-1", "UNKNOWN"));

        var events = repository.GetOrderHistory(" ORD-1 ");
        Assert.Equal(new[] { 1, 2, 3 }, events.Select(e => e.Sequence));
        Assert.Equal(new string?[] { null, "PENDING", "CONFIRMED" },
                     events.Select(e => e.PreviousStatus));
        Assert.Equal(new[] { "PENDING", "CONFIRMED", "SHIPPED" },
                     events.Select(e => e.Status));
        Assert.Equal("SHIPPED", repository.GetOrderById("ORD-1")?.Status);
        Assert.All(events, e => Assert.Equal(TimeSpan.Zero, e.OccurredAtUtc.Offset));

        // An earlier snapshot cannot observe later append operations.
        Assert.Single(created);
        Assert.Throws<NotSupportedException>(() =>
            ((IList<OrderStatusEvent>)events).Clear());
    }

    [Fact]
    public void RejectsUnknownOrdersAndLeavesHistoryUnchanged()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-2", "Cliente", 20m, "PENDING"));
        Assert.Throws<ArgumentException>(() => repository.GetOrderHistory(" "));
        Assert.Throws<KeyNotFoundException>(() => repository.GetOrderHistory("missing"));
        Assert.Throws<InvalidOperationException>(() =>
            repository.UpdateOrderStatus("ORD-2", "DELIVERED"));
        Assert.Single(repository.GetOrderHistory("ORD-2"));
    }

    [Fact]
    public void ConcurrentTransitionsProduceOneEventPerCommittedChange()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-3", "Cliente", 20m, "CONFIRMED"));

        Parallel.For(0, 100, index =>
        {
            try
            {
                repository.UpdateOrderStatus("ORD-3", index % 2 == 0 ? "SHIPPED" : "CANCELLED");
            }
            catch (InvalidOperationException)
            {
                // Another thread selected the only allowed next status first.
            }
        });

        var events = repository.GetOrderHistory("ORD-3");
        Assert.Equal(2, events.Count);
        Assert.Equal(1, events[0].Sequence);
        Assert.Equal(2, events[1].Sequence);
        Assert.Equal("CONFIRMED", events[1].PreviousStatus);
        Assert.Equal(repository.GetOrderById("ORD-3")?.Status, events[1].Status);
    }
}
