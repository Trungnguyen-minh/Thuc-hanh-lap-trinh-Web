using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Website.Dtos;
using Website.Services;
using static Website.Dtos.DTOs;

namespace Website.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService) => _orderService = orderService;

        /// <summary>
        /// Get all orders for current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;
            var orders = await _orderService.GetAllAsync(GetUserId(), page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderSummaryDto>>.Ok(orders));
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        [HttpGet("{orderId:int}")]
        public async Task<IActionResult> GetById(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderConfirmationAsync(GetUserId(), orderId);
                return Ok(ApiResponse<OrderConfirmationDto>.Ok(order));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// BƯỚC 2 — Reservation: Xác nhận thông tin giao hàng, giữ hàng
        /// </summary>
        [HttpPost("reserve")]
        public async Task<IActionResult> Reserve([FromBody] ReservationDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(GetModelErrors()));

            try
            {
                var order = await _orderService.CreateReservationAsync(GetUserId(), dto);
                return Ok(ApiResponse<OrderConfirmationDto>.Ok(order, "Đặt hàng thành công, vui lòng thanh toán"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// BƯỚC 3 — Payment: Tạo URL thanh toán VNPay
        /// </summary>
        [HttpPost("{orderId:int}/payment")]
        public async Task<IActionResult> CreatePayment(int orderId)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                var result = await _orderService.CreatePaymentUrlAsync(GetUserId(), orderId, ipAddress);
                return Ok(ApiResponse<PaymentResponseDto>.Ok(result, "Chuyển đến trang thanh toán VNPay"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// BƯỚC 3 — VNPay Callback: VNPay gọi về sau khi user thanh toán
        /// (Không cần [Authorize] vì VNPay gọi trực tiếp)
        /// </summary>
        [HttpGet("vnpay-callback")]
        [AllowAnonymous]
        public async Task<IActionResult> VnpayCallback()
        {
            try
            {
                var order = await _orderService.HandleVnpayCallbackAsync(Request.Query);
                return Ok(ApiResponse<OrderConfirmationDto>.Ok(order, "Thanh toán thành công"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>
        /// BƯỚC 4 — Order Confirmation: Lấy thông tin xác nhận đơn hàng
        /// </summary>
        [HttpGet("{orderId:int}/confirmation")]
        public async Task<IActionResult> GetConfirmation(int orderId)
        {
            try
            {
                var order = await _orderService.GetOrderConfirmationAsync(GetUserId(), orderId);
                return Ok(ApiResponse<OrderConfirmationDto>.Ok(order));
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(ApiResponse<object>.Fail(ex.Message));
            }
        }

        private string GetUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException();

        private string GetModelErrors() =>
            string.Join(", ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
    }
}
