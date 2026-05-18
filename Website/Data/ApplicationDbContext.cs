namespace Website.Data
{
    using Microsoft.EntityFrameworkCore;
    using Website.Models;
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Product
            builder.Entity<Product>()
                .Property(p => p.Price)
                .HasColumnType("decimal(18,2)");

            // Order
            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)");

            builder.Entity<OrderItem>()
                .Property(o => o.UnitPrice)
                .HasColumnType("decimal(18,2)");

            // Cart: mỗi user chỉ có 1 cart
            builder.Entity<Cart>()
                .HasIndex(c => c.UserId)
                .IsUnique();

            // CartItem: mỗi sản phẩm chỉ xuất hiện 1 lần trong 1 cart
            builder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.ProductId })
                .IsUnique();
        }

    }
}
