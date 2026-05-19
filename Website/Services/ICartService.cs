using Website.Dtos;

namespace Website.Services
{
    public interface ICartService
    {
        Task<CartResponseDto> GetCartAsync(string userId);
        Task<CartResponseDto> AddItemAsync(string userId, AddToCartDto dto);
        Task<CartResponseDto> UpdateItemAsync(string userId, int cartItemId, UpdateCartItemDto dto);
        Task<CartResponseDto> RemoveItemAsync(string userId, int cartItemId);
        Task ClearCartAsync(string userId);
    }
}
