using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace ECommercePlatform.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "產品名稱是必填的。")]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Range(0.01, double.MaxValue, ErrorMessage = "價格必須大於 0。")]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        public string? ImageUrl { get; set; }
        // 修正 Reviews 的型別
        public byte[]? ImageData { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public bool IsActive { get; internal set; }
    }
}