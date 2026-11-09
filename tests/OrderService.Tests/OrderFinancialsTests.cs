using System;
using OrderService;
using Xunit;

namespace OrderService.Tests;

public sealed class OrderFinancialsTests
{
    [Fact]
    public void SummarizesAmountsByStatusAndSeparatesCancellation()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("A", "Cliente 1", 10m, "PENDING"));
        repository.AddOrder(new Order("B", "Cliente 2", 20m, "CONFIRMED"));
        repository.AddOrder(new Order("C", "Cliente 3", 30m, "SHIPPED"));
        repository.AddOrder(new Order("D", "Cliente 4", 40m, "DELIVERED"));
        repository.AddOrder(new Order("E", "Cliente 5", 50m, "CANCELLED"));

        var snapshot = OrderFinancials.Summarize(repository);
        Assert.Equal(5, snapshot.OrderCount);
        Assert.Equal(150m, snapshot.RecordedTotal);
        Assert.Equal(60m, snapshot.InProgressAmount);
        Assert.Equal(40m, snapshot.DeliveredAmount);
        Assert.Equal(50m, snapshot.CancelledAmount);
        Assert.Equal(30m, snapshot.AverageOrderAmount);
        Assert.Equal(20m, snapshot.AmountsByStatus["CONFIRMED"]);
        Assert.Equal(150m, snapshot.InProgressAmount + snapshot.DeliveredAmount +
            snapshot.CancelledAmount);

        repository.UpdateOrderStatus("A", "CANCELLED");
        Assert.Equal(10m, snapshot.AmountsByStatus["PENDING"]);
        Assert.Equal(0m, OrderFinancials.Summarize(repository).AmountsByStatus["PENDING"]);
        Assert.Throws<NotSupportedException>(() =>
            ((System.Collections.Generic.IDictionary<string, decimal>)snapshot.AmountsByStatus)
                .Add("OTHER", 99m));
    }

    [Fact]
    public void EmptyRepositoryProducesZeroes()
    {
        var snapshot = OrderFinancials.Summarize(new OrderRepository());
        Assert.Equal(0, snapshot.OrderCount);
        Assert.Equal(0m, snapshot.RecordedTotal);
        Assert.Equal(0m, snapshot.AverageOrderAmount);
        Assert.All(snapshot.AmountsByStatus.Values, amount => Assert.Equal(0m, amount));
    }

    [Fact]
    public void RejectsNullAndThrowsOnDecimalOverflow()
    {
        Assert.Throws<ArgumentNullException>(() => OrderFinancials.Summarize(null!));
        var repository = new OrderRepository();
        repository.AddOrder(new Order("max", "Cliente", decimal.MaxValue, "DELIVERED"));
        repository.AddOrder(new Order("another", "Cliente", 1m, "PENDING"));
        Assert.Throws<OverflowException>(() => OrderFinancials.Summarize(repository));
    }

    [Fact]
    public void ConcurrentSnapshotsDoNotExposeInconsistentTotals()
    {
        var repository = new OrderRepository();
        System.Threading.Tasks.Parallel.For(0, 50, i =>
        {
            repository.AddOrder(new Order($"ID-{i}", "Cliente", 5m, "PENDING"));
            var report = OrderFinancials.Summarize(repository);
            Assert.Equal(report.OrderCount * 5m, report.RecordedTotal);
            Assert.Equal(report.RecordedTotal, report.InProgressAmount +
                report.DeliveredAmount + report.CancelledAmount);
        });
    }
}
