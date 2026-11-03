using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OrderService;
using Xunit;

namespace OrderService.Tests;

public sealed class OrderLifecycleTests
{
    private static OrderRepository WithPendingOrder()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-1", "Cliente", 99.90m, "PENDING"));
        return repository;
    }

    [Fact]
    public void SupportsFulfillmentLifecycleAndIdempotentStatusUpdates()
    {
        var repository = WithPendingOrder();

        var confirmed = repository.UpdateOrderStatus(" ORD-1 ", " confirmed ");
        Assert.Equal("CONFIRMED", confirmed.Status);
        Assert.Same(confirmed, repository.UpdateOrderStatus("ORD-1", "CONFIRMED"));

        Assert.Equal("SHIPPED", repository.UpdateOrderStatus("ORD-1", "SHIPPED").Status);
        Assert.Equal("DELIVERED", repository.UpdateOrderStatus("ORD-1", "DELIVERED").Status);
        Assert.Equal("DELIVERED", repository.GetOrderById("ORD-1")?.Status);

        Assert.Throws<InvalidOperationException>(() =>
            repository.UpdateOrderStatus("ORD-1", "CANCELLED"));
    }

    [Fact]
    public void AllowsCancellationBeforeShippingButNeverAfterShipping()
    {
        var repository = WithPendingOrder();
        Assert.Equal("CANCELLED", repository.UpdateOrderStatus("ORD-1", "cancelled").Status);
        Assert.Throws<InvalidOperationException>(() =>
            repository.UpdateOrderStatus("ORD-1", "CONFIRMED"));

        var second = new OrderRepository();
        second.AddOrder(new Order("ORD-2", "Cliente", 50m, "CONFIRMED"));
        Assert.Equal("SHIPPED", second.UpdateOrderStatus("ORD-2", "SHIPPED").Status);
        Assert.Throws<InvalidOperationException>(() =>
            second.UpdateOrderStatus("ORD-2", "CANCELLED"));
    }

    [Fact]
    public void RejectsUnknownStatusesAndMissingOrdersWithoutChangingData()
    {
        var repository = WithPendingOrder();
        Assert.Throws<ArgumentException>(() =>
            repository.AddOrder(new Order("ORD-2", "Cliente", 10m, "PROCESSING")));
        Assert.Throws<ArgumentException>(() =>
            repository.UpdateOrderStatus("ORD-1", "REFUNDED"));
        Assert.Throws<ArgumentException>(() =>
            repository.UpdateOrderStatus(" ", "CONFIRMED"));
        Assert.Throws<KeyNotFoundException>(() =>
            repository.UpdateOrderStatus("MISSING", "CONFIRMED"));
        Assert.Throws<InvalidOperationException>(() =>
            repository.UpdateOrderStatus("ORD-1", "DELIVERED"));

        Assert.Equal("PENDING", repository.GetOrderById("ORD-1")?.Status);
        Assert.Single(repository.GetAllOrders());
    }

    [Fact]
    public void SnapshotStillContainsPriorImmutableOrderAfterTransition()
    {
        var repository = WithPendingOrder();
        var snapshot = repository.GetAllOrders();

        repository.UpdateOrderStatus("ORD-1", "CONFIRMED");

        Assert.Equal("PENDING", snapshot[0].Status);
        Assert.Equal("CONFIRMED", repository.GetOrderById("ORD-1")?.Status);
    }

    [Fact]
    public void CompetingTerminalAndShippingTransitionsRemainAtomic()
    {
        var repository = WithPendingOrder();
        repository.UpdateOrderStatus("ORD-1", "CONFIRMED");

        Parallel.For(0, 100, index =>
        {
            var nextStatus = index % 2 == 0 ? "SHIPPED" : "CANCELLED";
            try
            {
                Assert.Equal(nextStatus, repository.UpdateOrderStatus("ORD-1", nextStatus).Status);
            }
            catch (InvalidOperationException)
            {
                // A competing operation chose the other valid transition first.
            }
        });

        var finalStatus = repository.GetOrderById("ORD-1")?.Status;
        Assert.Contains(finalStatus, new[] { "SHIPPED", "CANCELLED" });
        Assert.Single(repository.GetAllOrders());

        Assert.Throws<InvalidOperationException>(() =>
            repository.UpdateOrderStatus("ORD-1", finalStatus == "SHIPPED" ? "CANCELLED" : "SHIPPED"));
    }
}
