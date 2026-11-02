using System;

namespace OrderService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var repository = new OrderRepository();
            repository.AddOrder(new Order("ORD-2026-001", "Alice Smith", 299.99m, "PENDING"));

            var confirmed = repository.UpdateOrderStatus("ORD-2026-001", "CONFIRMED");
            Console.WriteLine($"Order {confirmed.OrderId} ({confirmed.CustomerName}): {confirmed.Status}");

            var shipped = repository.UpdateOrderStatus(confirmed.OrderId, "SHIPPED");
            Console.WriteLine($"Order {shipped.OrderId}: {shipped.Status}");

            var delivered = repository.UpdateOrderStatus(shipped.OrderId, "DELIVERED");
            Console.WriteLine($"Order {delivered.OrderId}: {delivered.Status} (total: {delivered.TotalAmount:C})");
        }
    }
}
