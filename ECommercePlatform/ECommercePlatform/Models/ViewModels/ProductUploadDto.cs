using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace ECommercePlatform.Models.ViewModels
{
    public class ProductUploadDto
    {
        [Required(ErrorMessage = "產品名稱是必填的")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "價格必須大於 0")]
        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }

        public IFormFile? ImageFile { get; set; }
    }
}
