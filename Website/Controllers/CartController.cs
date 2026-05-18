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
    [Authorize] // Phải đăng nhập mới dùng được giỏ hàng
    [Produces("application/json")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService) => _cartService = cartService;

        /// <summary>Xem giỏ hàng</summary>
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetUserId();
            var cart = await _cartService.GetCartAsync(userId);
            return Ok(ApiResponse<CartResponseDto>.Ok(cart));
        }

        /// <summary>Thêm sản phẩm vào giỏ</summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] AddToCartDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(GetModelErrors()));

            try
            {
                var cart = await _cartService.AddItemAsync(GetUserId(), dto);
                return Ok(ApiResponse<CartResponseDto>.Ok(cart, "Đã thêm vào giỏ hàng"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Cập nhật số lượng sản phẩm trong giỏ</summary>
        [HttpPut("items/{cartItemId:int}")]
        public async Task<IActionResult> UpdateItem(int cartItemId, [FromBody] UpdateCartItemDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ApiResponse<object>.Fail(GetModelErrors()));

            try
            {
                var cart = await _cartService.UpdateItemAsync(GetUserId(), cartItemId, dto);
                return Ok(ApiResponse<CartResponseDto>.Ok(cart, "Đã cập nhật giỏ hàng"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Xóa sản phẩm khỏi giỏ</summary>
        [HttpDelete("items/{cartItemId:int}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                var cart = await _cartService.RemoveItemAsync(GetUserId(), cartItemId);
                return Ok(ApiResponse<CartResponseDto>.Ok(cart, "Đã xóa khỏi giỏ hàng"));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.Fail(ex.Message));
            }
        }

        /// <summary>Xóa toàn bộ giỏ hàng</summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync(GetUserId());
            return Ok(ApiResponse<object>.Ok(null!, "Đã xóa toàn bộ giỏ hàng"));
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
