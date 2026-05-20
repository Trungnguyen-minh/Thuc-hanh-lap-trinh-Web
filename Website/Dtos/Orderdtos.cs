using System.ComponentModel.DataAnnotations;
using Website.Models;

namespace Website.Dtos
{

    // Bước 2: Reservation — user xác nhận thông tin giao hàng
    public class ReservationDto
    {
        [Required(ErrorMessage = "Địa chỉ giao hàng không được để trống")]
        [StringLength(300, MinimumLength = 10, ErrorMessage = "Địa chỉ từ 10-300 ký tự")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại không được để trống")]
        [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại không hợp lệ")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? Note { get; set; }
    }

    // Bước 3: Payment — trả về URL redirect sang VNPay
    public class PaymentResponseDto
    {
        public string PaymentUrl { get; set; } = string.Empty;
        public int OrderId { get; set; }
    }

    // Bước 4: Order Confirmation — thông tin đơn hàng hoàn tất
    public class OrderConfirmationDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Note { get; set; }
        public decimal TotalAmount { get; set; }
        public string? VnpayTransactionId { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOrderDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? VnpayTransactionId { get; set; }
    }

    public class UpdateOrderStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
