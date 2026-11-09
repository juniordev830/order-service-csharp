using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OrderService
{
    /// <summary>Values of recorded orders, not recognized revenue or actual payments.</summary>
    public sealed record OrderFinancialSnapshot(
        int OrderCount,
        decimal RecordedTotal,
        decimal InProgressAmount,
        decimal DeliveredAmount,
        decimal CancelledAmount,
        decimal AverageOrderAmount,
        IReadOnlyDictionary<string, decimal> AmountsByStatus);

    public static class OrderFinancials
    {
        private static readonly string[] Statuses =
            { "PENDING", "CONFIRMED", "SHIPPED", "DELIVERED", "CANCELLED" };

        /// <summary>
        /// Compute status-grouped financial indicators from one consistent repository snapshot.
        /// The returned read-only dictionary is detached from future repository changes.
        /// </summary>
        public static OrderFinancialSnapshot Summarize(OrderRepository repository)
        {
            ArgumentNullException.ThrowIfNull(repository);
            var orders = repository.GetAllOrders();
            var amounts = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var status in Statuses) amounts.Add(status, 0m);

            decimal total = 0m;
            foreach (var order in orders)
            {
                amounts[order.Status] = checked(amounts[order.Status] + order.TotalAmount);
                total = checked(total + order.TotalAmount);
            }

            var inProgress = checked(checked(amounts["PENDING"] + amounts["CONFIRMED"])
                + amounts["SHIPPED"]);
            var average = orders.Count == 0 ? 0m : total / orders.Count;
            return new OrderFinancialSnapshot(
                orders.Count,
                total,
                inProgress,
                amounts["DELIVERED"],
                amounts["CANCELLED"],
                average,
                new ReadOnlyDictionary<string, decimal>(amounts));
        }
    }
}
