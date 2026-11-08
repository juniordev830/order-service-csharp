using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OrderService
{
    /// <summary>A read-only, point-in-time view over orders already stored in memory.</summary>
    public sealed record OrderQueryResult(
        IReadOnlyList<Order> Items,
        int Page,
        int PageSize,
        int TotalMatching,
        int TotalOrders,
        decimal TotalAmount,
        IReadOnlyDictionary<string, int> CountsByStatus);

    /// <summary>
    /// Reporting and pagination over repository snapshots. No database or network.
    /// The summary is computed across all orders even when a status filter is used.
    /// </summary>
    public static class OrderInsights
    {
        private static readonly string[] KnownStatuses =
            { "PENDING", "CONFIRMED", "SHIPPED", "DELIVERED", "CANCELLED" };

        public static OrderQueryResult Query(
            OrderRepository repository,
            string? status = null,
            int page = 0,
            int pageSize = 20)
        {
            if (repository is null)
                throw new ArgumentNullException(nameof(repository));
            if (page < 0)
                throw new ArgumentOutOfRangeException(nameof(page),
                    "Page must be nonnegative.");
            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentOutOfRangeException(nameof(pageSize),
                    "Page size must be from 1 to 100.");
            if (status is not null && string.IsNullOrWhiteSpace(status))
                throw new ArgumentException("Status filter cannot be blank.", nameof(status));

            var normalizedStatus = status?.Trim().ToUpperInvariant();
            if (normalizedStatus != null &&
                !KnownStatuses.Contains(normalizedStatus, StringComparer.Ordinal))
                throw new ArgumentException("Unsupported status filter.", nameof(status));

            // GetAllOrders takes its snapshot under a single repository lock.
            var snapshot = repository.GetAllOrders();
            var counts = KnownStatuses.ToDictionary(
                key => key, _ => 0, StringComparer.Ordinal);
            decimal totalAmount = 0m;
            foreach (var order in snapshot)
            {
                counts[order.Status] += 1;
                totalAmount = checked(totalAmount + order.TotalAmount);
            }

            var matching = snapshot
                .Where(order => normalizedStatus == null || order.Status == normalizedStatus)
                .OrderBy(order => order.OrderId, StringComparer.Ordinal)
                .ToArray();

            // Long arithmetic avoids int overflow when requesting large pages.
            long skip = (long)page * pageSize;
            var items = skip >= matching.Length
                ? Array.Empty<Order>()
                : matching.Skip((int)skip).Take(pageSize).ToArray();

            return new OrderQueryResult(
                Array.AsReadOnly(items),
                page,
                pageSize,
                matching.Length,
                snapshot.Count,
                totalAmount,
                new ReadOnlyDictionary<string, int>(counts));
        }
    }
}
