using System;

namespace OrderService
{
    public record Order(string OrderId, string CustomerName, decimal TotalAmount, string Status);

    class Program
    {
        static void Main(string[] args)
        {
            var order = new Order("ORD-2026-001", "Alice Smith", 299.99m, "PENDING");
            Console.WriteLine($"Order Service C# - Processing Order ID: {order.OrderId} for {order.CustomerName} (Total: ${order.TotalAmount})");
        }
    }
}
