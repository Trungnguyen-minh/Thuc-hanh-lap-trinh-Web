using Website.Dtos;

namespace Website.Services
{
    public interface IOrderService
    {
        // Bước 2: Tạo reservation từ cart
        Task<OrderConfirmationDto> CreateReservationAsync(string userId, ReservationDto dto);

        // Bước 3: Tạo URL thanh toán VNPay
        Task<PaymentResponseDto> CreatePaymentUrlAsync(string userId, int orderId, string ipAddress);

        // Bước 3: VNPay callback sau khi thanh toán
        Task<OrderConfirmationDto> HandleVnpayCallbackAsync(IQueryCollection vnpayData);

        // Bước 4: Lấy thông tin xác nhận đơn hàng
        Task<OrderConfirmationDto> GetOrderConfirmationAsync(string userId, int orderId);
    }
}
