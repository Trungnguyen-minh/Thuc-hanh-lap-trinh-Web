using Microsoft.EntityFrameworkCore;
using Website.Data;
using Website.Dtos;
using Website.Models;
using static Website.Dtos.DTOs;

namespace Website.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _db;
        private readonly ICartService _cartService;
        private readonly VnpayService _vnpay;

        public OrderService(ApplicationDbContext db, ICartService cartService, VnpayService vnpay)
        {
            _db = db;
            _cartService = cartService;
            _vnpay = vnpay;
        }

        public async Task<PagedResult<OrderSummaryDto>> GetAllAsync(string userId, int page, int pageSize)
        {
            var query = _db.Orders.Where(o => o.UserId == userId);
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new OrderSummaryDto
                {
                    OrderId = o.Id,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return new PagedResult<OrderSummaryDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        // ─── BƯỚC 2: RESERVATION ─────────────────────────────

        public async Task<OrderConfirmationDto> CreateReservationAsync(string userId, ReservationDto dto)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null || !cart.Items.Any())
                throw new InvalidOperationException("Giỏ hàng trống, không thể đặt hàng.");

            // Kiểm tra tồn kho
            foreach (var item in cart.Items)
            {
                if (item.Product!.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Sản phẩm '{item.Product.Name}' chỉ còn {item.Product.Stock} trong kho.");
            }

            // Tạo order ở trạng thái Reserved
            var order = new Order
            {
                UserId = userId,
                Status = OrderStatus.Reserved,
                Address = dto.ShippingAddress.Trim(),
                Phone = dto.PhoneNumber.Trim(),
                TotalAmount = cart.Items.Sum(i => i.Product!.Price * i.Quantity),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product!.Name,    // snapshot
                    UnitPrice = i.Product.Price,       // snapshot
                    Quantity = i.Quantity
                }).ToList()
            };

            // Trừ tồn kho
            foreach (var item in cart.Items)
                item.Product!.Stock -= item.Quantity;

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Xóa cart sau khi đã tạo order
            await _cartService.ClearCartAsync(userId);

            return MapToConfirmation(order);
        }

        // ─── BƯỚC 3: PAYMENT ─────────────────────────────────

        public async Task<PaymentResponseDto> CreatePaymentUrlAsync(string userId, int orderId, string ipAddress)
        {
            var order = await GetUserOrderAsync(userId, orderId);

            if (order.Status != OrderStatus.Reserved)
                throw new InvalidOperationException("Đơn hàng không ở trạng thái chờ thanh toán.");

            // VnpayService exposes SimulatePayment(orderId, amount) per available signatures.
            var (transactionId, success) = _vnpay.SimulatePayment(order.Id, order.TotalAmount);

            if (!success)
                throw new InvalidOperationException("Không thể tạo yêu cầu thanh toán (simulation failed).");

            // Return the transaction id as the payment reference (or build a URL from it if your VNPAY integration requires a URL).
            return new PaymentResponseDto
            {
                PaymentUrl = transactionId,
                OrderId = order.Id
            };
        }

        // ─── BƯỚC 3: VNPAY CALLBACK ──────────────────────────

        public async Task<OrderConfirmationDto> HandleVnpayCallbackAsync(IQueryCollection vnpayData)
        {
            var isValid = _vnpay.ValidateCallback(vnpayData, out var responseCode, out var txnRef);

            // txnRef dạng: "orderId_timestamp"
            var orderId = int.Parse(txnRef.Split('_')[0]);

            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");

            if (!isValid || responseCode != "00")
            {
                // Thanh toán thất bại → hoàn tồn kho
                await RestoreStockAsync(order);
                order.Status = OrderStatus.Pending;
                order.VnpayResponseCode = responseCode;
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                throw new InvalidOperationException($"Thanh toán thất bại (mã: {responseCode}).");
            }

            // Thanh toán thành công → Confirmed
            order.Status = OrderStatus.Confirmed;
            order.VnpayTransactionId = vnpayData["vnp_TransactionNo"].ToString();
            order.VnpayResponseCode = responseCode;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return MapToConfirmation(order);
        }

        // ─── BƯỚC 4: ORDER CONFIRMATION ──────────────────────

        public async Task<OrderConfirmationDto> GetOrderConfirmationAsync(string userId, int orderId)
        {
            var order = await GetUserOrderAsync(userId, orderId);
            return MapToConfirmation(order);
        }

        // ─── BƯỚC 5: ADMIN MANAGEMENT ────────────────────────
        
        public async Task<PagedResult<AdminOrderDto>> GetAllOrdersAdminAsync(int page, int pageSize)
        {
            var query = _db.Orders.AsQueryable();
            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderDto
                {
                    OrderId = o.Id,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,
                    CreatedAt = o.CreatedAt,
                    CustomerEmail = _db.Users.Where(u => u.Id == o.UserId).Select(u => u.Email).FirstOrDefault() ?? "N/A",
                    PhoneNumber = o.Phone ?? "",
                    VnpayTransactionId = o.VnpayTransactionId
                })
                .ToListAsync();

            return new PagedResult<AdminOrderDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        public async Task<bool> UpdateOrderStatusAsync(int orderId, string status)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) return false;

            if (Enum.TryParse<OrderStatus>(status, true, out var newStatus))
            {
                // Nếu đổi trạng thái thành Hủy và trước đó chưa bị hủy
                if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
                {
                    await RestoreStockAsync(order);
                }

                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> CancelOrderAsync(string userId, int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.UserId == userId && o.Id == orderId);
            if (order == null) return false;

            // Chỉ cho phép hủy khi đơn hàng ở trạng thái Chờ xử lý hoặc Giữ hàng
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Reserved)
            {
                throw new InvalidOperationException("Chỉ có thể hủy đơn hàng khi chưa thanh toán hoặc chưa hoàn tất.");
            }

            // Hoàn lại tồn kho
            await RestoreStockAsync(order);

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return true;
        }

        // ─── HELPER ──────────────────────────────────────────
        private async Task<Order> GetUserOrderAsync(string userId, int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId)
                ?? throw new InvalidOperationException("Không tìm thấy đơn hàng.");
            return order;
        }

        private async Task RestoreStockAsync(Order order)
        {
            foreach (var item in order.Items)
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product is not null)
                    product.Stock += item.Quantity;
            }
        }

        private static OrderConfirmationDto MapToConfirmation(Order o) => new()
        {
            OrderId = o.Id,
            Status = o.Status.ToString(),
            ShippingAddress = o.Address ?? string.Empty,
            PhoneNumber = o.Phone ?? string.Empty,
            Note = null,
            TotalAmount = o.TotalAmount,
            VnpayTransactionId = o.VnpayTransactionId ?? string.Empty,
            CreatedAt = o.CreatedAt,
            Items = o.Items.Select(i => new OrderItemDto
            {
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                SubTotal = i.SubTotal
            }).ToList()
        };
    }
}
