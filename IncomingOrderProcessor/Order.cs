using System;
using System.Collections.Generic;

namespace IncomingOrderProcessor
{
    [Serializable]
    public class Order
    {
        public string OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Tax { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
        public string CustomerSessionId { get; set; }

        public Order()
        {
            OrderId = Guid.NewGuid().ToString();
            OrderDate = DateTime.Now;
            Items = new List<OrderItem>();
        }
    }

    [Serializable]
    public class OrderItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}
