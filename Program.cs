using System;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var repository = new OrderRepository();
            var order = new Order("ORD-2026-001", "Alice Smith", 299.99m, "PENDING");

            repository.AddOrder(order);
            var fetched = repository.GetOrderById("ORD-2026-001");

            Console.WriteLine($"Order Service C# - Processing Order ID: {fetched?.OrderId} for {fetched?.CustomerName} (Total: ${fetched?.TotalAmount})");
        }
    }
}
