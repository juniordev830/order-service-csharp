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
}
