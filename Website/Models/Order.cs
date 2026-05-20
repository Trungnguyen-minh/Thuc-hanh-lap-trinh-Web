namespace Website.Models
{
    public enum OrderStatus
    {
        Pending,        // Bước 1: Cart
        Reserved,       // Bước 2: Reservation - đã giữ hàng
        Paid,           // Bước 3: Payment - đã thanh toán
        Confirmed,      // Bước 4: Order Confirmation - hoàn tất
        Cancelled       // Bước 5: Đã hủy đơn hàng
    }
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string OrderCode { get; set; } = string.Empty;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public decimal TotalAmount { get; set; }

        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        // VNPay
        public string? VnpayTransactionId { get; set; }
        public string? VnpayResponseCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    }

    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; }

        public int ProductId { get; set; }
        public Product? Product { get; set; }

        public string ProductName { get; set; } = string.Empty; // snapshot tên lúc đặt
        public decimal UnitPrice { get; set; }                  // snapshot giá lúc đặt
        public int Quantity { get; set; }
        public decimal SubTotal => UnitPrice * Quantity;
    }
}
