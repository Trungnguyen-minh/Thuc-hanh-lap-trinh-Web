namespace Website.Dtos
{
    public class DTOs
    {
        public class CategoryCreateDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class CategoryUpdateDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class CategoryResponseDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public int ProductCount { get; set; }
        }

        // ─── PRODUCT DTOs ─────────────────────────────────────────────

        public class ProductCreateDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public IFormFile? Image { get; set; }
            public int CategoryId { get; set; }
        }

        public class ProductUpdateDto
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public IFormFile? Image { get; set; }
            public int CategoryId { get; set; }
        }

        public class ProductResponseDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public string? ImagePath { get; set; }
            public string? ImageUrl { get; set; }
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = string.Empty;
        }

        // ─── COMMON ───────────────────────────────────────────────────

        public class ApiResponse<T>
        {
            public bool Success { get; set; }
            public string Message { get; set; } = string.Empty;
            public T? Data { get; set; }

            public static ApiResponse<T> Ok(T data, string message = "Thành công") =>
                new() { Success = true, Message = message, Data = data };

            public static ApiResponse<T> Fail(string message) =>
                new() { Success = false, Message = message };
        }

        public class PagedResult<T>
        {
            public List<T> Items { get; set; } = new();
            public int TotalCount { get; set; }
            public int Page { get; set; }
            public int PageSize { get; set; }
            public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        }
    }
}
