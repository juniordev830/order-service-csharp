namespace OrderService
{
    public record Order(string OrderId, string CustomerName, decimal TotalAmount, string Status);
}
