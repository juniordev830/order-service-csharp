using System;
using System.Collections.Generic;
using System.Linq;

namespace OrderService
{
    /// <summary>
    /// Thread-safe, in-memory repository. Status transitions update immutable Order records.
    /// No persistence or distributed coordination is provided.
    /// </summary>
    public class OrderRepository
    {
        private readonly List<Order> _orders = new();
        private readonly Dictionary<string, List<OrderStatusEvent>> _history =
            new(StringComparer.Ordinal);
        private readonly object _sync = new();

        // The graph deliberately excludes backward transitions and terminal-state changes.
        private static readonly IReadOnlyDictionary<string, string[]> AllowedTransitions =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["PENDING"] = new[] { "CONFIRMED", "CANCELLED" },
                ["CONFIRMED"] = new[] { "SHIPPED", "CANCELLED" },
                ["SHIPPED"] = new[] { "DELIVERED" },
                ["DELIVERED"] = Array.Empty<string>(),
                ["CANCELLED"] = Array.Empty<string>()
            };

        private static string NormalizeStatus(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Order status cannot be empty", nameof(status));

            var normalized = status.Trim().ToUpperInvariant();
            if (!AllowedTransitions.ContainsKey(normalized))
                throw new ArgumentException("Unsupported order status", nameof(status));

            return normalized;
        }

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

            var normalizedOrder = order with
            {
                OrderId = order.OrderId.Trim(),
                CustomerName = order.CustomerName.Trim(),
                Status = NormalizeStatus(order.Status)
            };

            lock (_sync)
            {
                if (_orders.Any(existing => string.Equals(existing.OrderId, normalizedOrder.OrderId, StringComparison.Ordinal)))
                    throw new InvalidOperationException($"An order with ID '{normalizedOrder.OrderId}' already exists.");

                _orders.Add(normalizedOrder);
                _history.Add(normalizedOrder.OrderId, new List<OrderStatusEvent>
                {
                    new(normalizedOrder.OrderId, 1, null, normalizedOrder.Status, DateTimeOffset.UtcNow)
                });
            }
        }

        /// <summary>
        /// Atomically moves an existing order to an allowed state. Repeating the current
        /// status is an idempotent no-op. Unknown IDs and invalid transitions fail.
        /// </summary>
        public Order UpdateOrderStatus(string orderId, string nextStatus)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

            var normalizedId = orderId.Trim();
            var normalizedNextStatus = NormalizeStatus(nextStatus);

            lock (_sync)
            {
                var index = _orders.FindIndex(o => string.Equals(o.OrderId, normalizedId, StringComparison.Ordinal));
                if (index < 0)
                    throw new KeyNotFoundException($"Order '{normalizedId}' not found.");

                var current = _orders[index];
                if (string.Equals(current.Status, normalizedNextStatus, StringComparison.Ordinal))
                    return current;

                if (!AllowedTransitions[current.Status].Contains(normalizedNextStatus, StringComparer.Ordinal))
                    throw new InvalidOperationException($"Cannot transition order from {current.Status} to {normalizedNextStatus}.");

                var updated = current with { Status = normalizedNextStatus };
                _orders[index] = updated;
                var events = _history[normalizedId];
                events.Add(new OrderStatusEvent(normalizedId, events.Count + 1,
                    current.Status, updated.Status, DateTimeOffset.UtcNow));
                return updated;
            }
        }

        /// <summary>
        /// Returns an immutable snapshot of committed events in registration order.
        /// Idempotent status updates do not create duplicate audit events.
        /// </summary>
        public IReadOnlyList<OrderStatusEvent> GetOrderHistory(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                throw new ArgumentException("Order ID cannot be empty", nameof(orderId));

            lock (_sync)
            {
                if (!_history.TryGetValue(orderId.Trim(), out var events))
                    throw new KeyNotFoundException($"Order '{orderId.Trim()}' not found.");

                return Array.AsReadOnly(events.ToArray());
            }
        }

        public Order? GetOrderById(string orderId)
        {
            if (string.IsNullOrWhiteSpace(orderId))
                return null;

            lock (_sync)
            {
                return _orders.FirstOrDefault(o => o.OrderId == orderId.Trim());
            }
        }

        // Return a snapshot so callers cannot mutate the repository's internal list.
        public IReadOnlyList<Order> GetAllOrders()
        {
            lock (_sync)
            {
                return _orders.ToArray();
            }
        }
    }
}
