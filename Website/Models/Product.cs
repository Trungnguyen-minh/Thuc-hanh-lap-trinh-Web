using System.ComponentModel;

namespace Website.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        //Foreign key to Category
        public int CategoryId { get; set; }

        //Navigation category
        public Category? Category { get; set; }


    }
}
