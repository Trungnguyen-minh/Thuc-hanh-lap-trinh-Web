using Microsoft.AspNetCore.Identity;
namespace Website.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? FullName { get; set; }
        public string? Address { get; set; }

        public ICollection<Cart> Carts { get; set; } = [];
        public ICollection<Order> Orders { get; set; } = [];
    }
}
