namespace IncomingOrderProcessor;

public class Order
{
    public string OrderId { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public List<OrderItem> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Tax { get; set; }
    public decimal Shipping { get; set; }
    public decimal Total { get; set; }
    public string CustomerSessionId { get; set; } = string.Empty;

    public Order()
    {
        OrderId = Guid.NewGuid().ToString();
        OrderDate = DateTime.Now;
    }
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public decimal Subtotal { get; set; }
}
