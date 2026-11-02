using OrderService;
using Xunit;

namespace OrderService.Tests;

public sealed class OrderRepositoryTests
{
    [Fact]
    public void AddOrderAndGetOrderByIdReturnsSavedOrder()
    {
        var repository = new OrderRepository();
        var order = new Order("ORD-1", "Cliente", 99.90m, "PENDING");

        repository.AddOrder(order);

        Assert.Equal(order, repository.GetOrderById("ORD-1"));
    }

    [Fact]
    public void GetAllOrdersReturnsSnapshotInsteadOfInternalCollection()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-1", "Cliente", 99.90m, "PENDING"));

        var snapshot = repository.GetAllOrders();

        Assert.Single(snapshot);
        Assert.IsNotType<List<Order>>(snapshot);
        repository.AddOrder(new Order("ORD-2", "Outro cliente", 20m, "PENDING"));
        Assert.Single(snapshot);
        Assert.Equal(2, repository.GetAllOrders().Count);
    }

    [Theory]
    [InlineData("", "Cliente", 10, "PENDING")]
    [InlineData("ORD-1", " ", 10, "PENDING")]
    [InlineData("ORD-1", "Cliente", 0, "PENDING")]
    [InlineData("ORD-1", "Cliente", -1, "PENDING")]
    [InlineData("ORD-1", "Cliente", 10, " ")]
    public void AddOrderRejectsInvalidFields(string id, string customer, decimal total, string status)
    {
        var repository = new OrderRepository();

        Assert.ThrowsAny<ArgumentException>(() => repository.AddOrder(new Order(id, customer, total, status)));
    }

    [Fact]
    public void AddOrderRejectsDuplicateIds()
    {
        var repository = new OrderRepository();
        var order = new Order("ORD-1", "Cliente", 99.90m, "PENDING");
        repository.AddOrder(order);

        Assert.Throws<InvalidOperationException>(() => repository.AddOrder(order));
    }

    [Fact]
    public void AddOrderTrimsFieldsAndLookupTrimsOrderId()
    {
        var repository = new OrderRepository();
        var order = new Order("  ORD-1  ", "  Cliente  ", 99.90m, "  PENDING  ");

        repository.AddOrder(order);

        Assert.Equal(new Order("ORD-1", "Cliente", 99.90m, "PENDING"), repository.GetOrderById(" ORD-1 "));
    }

    [Fact]
    public void AddOrderRejectsDuplicateIdsAfterTrimming()
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-1", "Cliente", 99.90m, "PENDING"));

        Assert.Throws<InvalidOperationException>(() =>
            repository.AddOrder(new Order(" ORD-1 ", "Outro cliente", 20m, "PENDING")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void GetOrderByIdReturnsNullForBlankIds(string id)
    {
        var repository = new OrderRepository();
        repository.AddOrder(new Order("ORD-1", "Cliente", 99.90m, "PENDING"));

        Assert.Null(repository.GetOrderById(id));
    }

    [Fact]
    public void ConcurrentInsertsForSameIdAllowExactlyOneOrder()
    {
        var repository = new OrderRepository();
        var accepted = 0;
        var duplicates = 0;

        System.Threading.Tasks.Parallel.For(0, 100, _ =>
        {
            try
            {
                repository.AddOrder(new Order("ORD-CONCURRENT", "Cliente", 12m, "PENDING"));
                System.Threading.Interlocked.Increment(ref accepted);
            }
            catch (InvalidOperationException)
            {
                System.Threading.Interlocked.Increment(ref duplicates);
            }
        });

        Assert.Equal(1, accepted);
        Assert.Equal(99, duplicates);
        Assert.Single(repository.GetAllOrders());
    }

    [Fact]
    public void ConcurrentUniqueInsertsProduceConsistentSnapshotsAndLookup()
    {
        var repository = new OrderRepository();
        System.Threading.Tasks.Parallel.For(0, 100, index =>
        {
            repository.AddOrder(new Order($"ORD-{index}", "Cliente", 12m, "PENDING"));
            _ = repository.GetAllOrders();
            _ = repository.GetOrderById($"ORD-{index}");
        });

        Assert.Equal(100, repository.GetAllOrders().Count);
        Assert.NotNull(repository.GetOrderById("ORD-99"));
    }
}
