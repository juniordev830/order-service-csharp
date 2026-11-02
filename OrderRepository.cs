using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderService
{
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
            var normalizedOrder = order with
            {
                OrderId = order.OrderId.Trim(),
                CustomerName = order.CustomerName.Trim(),
                Status = order.Status.Trim()
            };

            if (_orders.Any(existing => string.Equals(existing.OrderId, normalizedOrder.OrderId, StringComparison.Ordinal)))
                throw new InvalidOperationException($"An order with ID '{normalizedOrder.OrderId}' already exists.");

            _orders.Add(normalizedOrder);
        }

        public Order? GetOrderById(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return null;

            return _orders.FirstOrDefault(o => o.OrderId == orderId.Trim());
        }

        // Return a snapshot so callers cannot mutate the repository's internal list.
        public IReadOnlyList<Order> GetAllOrders() => _orders.ToArray();
    }
}
