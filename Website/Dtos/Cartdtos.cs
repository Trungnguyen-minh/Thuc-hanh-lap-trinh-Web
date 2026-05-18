using System.ComponentModel.DataAnnotations;

namespace Website.Dtos
{
    public class AddToCartDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
        public int ProductId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemDto
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; }
    }

    public class CartItemResponseDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
    }

    public class CartResponseDto
    {
        public int CartId { get; set; }
        public List<CartItemResponseDto> Items { get; set; } = new();
        public decimal TotalAmount { get; set; }
        public int TotalItems { get; set; }
    }
}
