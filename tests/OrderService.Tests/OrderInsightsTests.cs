using OrderService;
using Xunit;

namespace OrderService.Tests;

public sealed class OrderInsightsTests
{
    private static OrderRepository Seed()
    {
        var repo = new OrderRepository();
        repo.AddOrder(new Order("ORD-C", "Carol", 30.00m, "SHIPPED"));
        repo.AddOrder(new Order("ORD-A", "Ana", 10.00m, "PENDING"));
        repo.AddOrder(new Order("ORD-B", "Bruno", 20.00m, "PENDING"));
        repo.AddOrder(new Order("ORD-D", "Davi", 40.00m, "CANCELLED"));
        return repo;
    }

    [Fact]
    public void SummaryUsesAllOrdersAndStatusCounts()
    {
        var report = OrderInsights.Query(Seed());
        Assert.Equal(4, report.TotalOrders);
        Assert.Equal(100.00m, report.TotalAmount);
        Assert.Equal(2, report.CountsByStatus["PENDING"]);
        Assert.Equal(1, report.CountsByStatus["SHIPPED"]);
        Assert.Equal(1, report.CountsByStatus["CANCELLED"]);
        Assert.Equal(0, report.CountsByStatus["DELIVERED"]);
        Assert.Equal(4, report.TotalMatching);
        Assert.Equal(new[] { "ORD-A", "ORD-B", "ORD-C", "ORD-D" },
            report.Items.Select(item => item.OrderId));
    }

    [Fact]
    public void StatusFilterIsCaseInsensitiveAndWhitespaceTolerant()
    {
        var result = OrderInsights.Query(Seed(), status: " pending ");
        Assert.Equal(2, result.TotalMatching);
        Assert.All(result.Items, item => Assert.Equal("PENDING", item.Status));
        Assert.Equal(4, result.TotalOrders);
        Assert.Equal(100m, result.TotalAmount);
    }

    [Fact]
    public void PaginationReturnsSortedNonOverlappingPages()
    {
        var repo = Seed();
        var page0 = OrderInsights.Query(repo, page: 0, pageSize: 2);
        var page1 = OrderInsights.Query(repo, page: 1, pageSize: 2);
        var beyond = OrderInsights.Query(repo, page: int.MaxValue, pageSize: 100);
        Assert.Equal(new[] { "ORD-A", "ORD-B" }, page0.Items.Select(o => o.OrderId));
        Assert.Equal(new[] { "ORD-C", "ORD-D" }, page1.Items.Select(o => o.OrderId));
        Assert.Empty(beyond.Items);
        Assert.Equal(4, beyond.TotalMatching);
    }

    [Fact]
    public void ReportIsIndependentFromSubsequentRepositoryChanges()
    {
        var repo = Seed();
        var before = OrderInsights.Query(repo);
        repo.UpdateOrderStatus("ORD-A", "CONFIRMED");
        repo.AddOrder(new Order("ORD-E", "Elaine", 12m, "PENDING"));
        Assert.Equal(4, before.TotalOrders);
        Assert.Equal(2, before.CountsByStatus["PENDING"]);
        Assert.Equal("PENDING", before.Items[0].Status);
        Assert.Equal(5, OrderInsights.Query(repo).TotalOrders);
    }

    [Fact]
    public void RejectsNullRepositoryInvalidPagingAndUnknownStatus()
    {
        Assert.Throws<ArgumentNullException>(() => OrderInsights.Query(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderInsights.Query(Seed(), page: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderInsights.Query(Seed(), pageSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => OrderInsights.Query(Seed(), pageSize: 101));
        Assert.Throws<ArgumentException>(() => OrderInsights.Query(Seed(), status: "REFUNDED"));
    }

    [Fact]
    public void EmptyRepositoryHasZeroedReport()
    {
        var report = OrderInsights.Query(new OrderRepository(), status: "SHIPPED");
        Assert.Empty(report.Items);
        Assert.Equal(0, report.TotalOrders);
        Assert.Equal(0m, report.TotalAmount);
        Assert.Equal(0, report.TotalMatching);
        Assert.All(report.CountsByStatus.Values, count => Assert.Equal(0, count));
    }
}
