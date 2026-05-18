namespace Website.Services
{
    public interface IImageService
    {
        /// <summary>Lưu file ảnh vào wwwroot/uploads/products/, trả về đường dẫn tương đối</summary>
        Task<string> SaveAsync(IFormFile file);

        /// <summary>Xóa file ảnh theo đường dẫn tương đối đã lưu</summary>
        void Delete(string? relativePath);
    }

    public class ImageService : IImageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Chỉ chấp nhận các định dạng ảnh phổ biến
        private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

        // Giới hạn 5MB
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public ImageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> SaveAsync(IFormFile file)
        {
            // Validate kích thước
            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("Ảnh không được vượt quá 5MB.");

            // Validate extension
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException($"Định dạng ảnh không hợp lệ. Chỉ chấp nhận: {string.Join(", ", AllowedExtensions)}");

            // Tạo thư mục lưu ảnh nếu chưa có
            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "products");
            Directory.CreateDirectory(uploadsFolder);

            // Tạo tên file unique để tránh trùng lặp
            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Lưu file
            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            // Trả về đường dẫn tương đối (dùng để lưu DB và trả về client)
            return $"/uploads/products/{uniqueFileName}";
        }

        public void Delete(string? relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;

            // Chuyển relative path thành absolute path
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
    }
}
