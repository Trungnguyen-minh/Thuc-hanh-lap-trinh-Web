using Microsoft.EntityFrameworkCore;
using Website.Data;
using Website.Dtos;
using Website.Models;

namespace Website.Services
{
    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CartResponseDto> GetCartAsync(string userId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            return MapToDto(cart);
        }

        public async Task<CartResponseDto> AddItemAsync(string userId, AddToCartDto dto)
        {
            var product = await _db.Products.FindAsync(dto.ProductId)
                ?? throw new InvalidOperationException("Sản phẩm không tồn tại.");

            if (product.Stock < dto.Quantity)
                throw new InvalidOperationException($"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho.");

            var cart = await GetOrCreateCartAsync(userId);

            var existing = cart.Items.FirstOrDefault(i => i.ProductId == dto.ProductId);
            if (existing is not null)
            {
                var newQty = existing.Quantity + dto.Quantity;
                if (newQty > product.Stock)
                    throw new InvalidOperationException($"Tổng số lượng vượt quá tồn kho ({product.Stock}).");
                existing.Quantity = newQty;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    CartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity
                });
            }

            await _db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartResponseDto> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm trong giỏ hàng.");

            var product = await _db.Products.FindAsync(item.ProductId)!;
            if (dto.Quantity > product!.Stock)
                throw new InvalidOperationException($"Sản phẩm '{product.Name}' chỉ còn {product.Stock} trong kho.");

            item.Quantity = dto.Quantity;
            await _db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task<CartResponseDto> RemoveItemAsync(string userId, int cartItemId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId)
                ?? throw new InvalidOperationException("Không tìm thấy sản phẩm trong giỏ hàng.");

            _db.CartItems.Remove(item);
            await _db.SaveChangesAsync();
            return await GetCartAsync(userId);
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await GetOrCreateCartAsync(userId);
            _db.CartItems.RemoveRange(cart.Items);
            await _db.SaveChangesAsync();
        }

        // ─── HELPER ──────────────────────────────────────────

        private async Task<Cart> GetOrCreateCartAsync(string userId)
        {
            var cart = await _db.Carts
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null)
            {
                cart = new Cart { UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }

            return cart;
        }

        private CartResponseDto MapToDto(Cart cart)
        {
            var items = cart.Items.Select(i =>
            {
                return new CartItemResponseDto
                {
                    CartItemId = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    ImageUrl = i.Product?.ImageUrl,
                    UnitPrice = i.Product?.Price ?? 0,
                    Quantity = i.Quantity,
                    SubTotal = (i.Product?.Price ?? 0) * i.Quantity
                };
            }).ToList();

            return new CartResponseDto
            {
                CartId = cart.Id,
                Items = items,
                TotalAmount = items.Sum(i => i.SubTotal),
                TotalItems = items.Sum(i => i.Quantity)
            };
        }
    }
}
