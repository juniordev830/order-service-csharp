using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderService
{
    public record Order(string OrderId, string CustomerName, decimal TotalAmount, string Status);

    public class OrderRepository
    {
        private readonly List<Order> _orders = new();

        public void AddOrder(Order order)
        {
            if (order is null)
                throw new ArgumentNullException(nameof(order));
            if (string.IsNullOrWhiteSpace(order.OrderId))
                throw new ArgumentException("Order ID cannot be empty", nameof(order));
            if (string.IsNullOrWhiteSpace(order.CustomerName))
                throw new ArgumentException("Customer name cannot be empty", nameof(order));
            if (order.TotalAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(order), "Order total must be greater than zero.");
            if (string.IsNullOrWhiteSpace(order.Status))
                throw new ArgumentException("Order status cannot be empty", nameof(order));
            if (_orders.Any(existing => string.Equals(existing.OrderId, order.OrderId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"An order with ID '{order.OrderId}' already exists.");

            _orders.Add(order);
        }

        public Order? GetOrderById(string orderId)
        {
            return _orders.FirstOrDefault(o => o.OrderId == orderId);
        }

        public IEnumerable<Order> GetAllOrders() => _orders;
    }

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
