using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommercePlatform.Models
{
    [Table("Reviews")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [StringLength(50)]
        public string? UserName { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [Range(1, 5)]
        public int Rating { get; set; }

        public byte[]? ImageData { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? ReplyId { get; set; }

        public bool IsVisible { get; set; } = true;
        public DateTime? UpdatedAt { get; set; }

        // 導航屬性
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Review? ReplyTo { get; set; }
        public virtual ICollection<Review> Replies { get; set; } = new List<Review>();
        public virtual ICollection<ReviewReport> Reports { get; set; } = new List<ReviewReport>();

        // 相容性屬性 (為了不破壞現有代碼)
        [NotMapped]
        public int messageID => Id;

        [NotMapped]
        public int userID => UserId;

        [NotMapped]
        public int productID => ProductId;

        [NotMapped]
        public string userName => UserName ?? User?.Username ?? "";

        [NotMapped]
        public string main => Content;

        [NotMapped]
        public int score => Rating;

        [NotMapped]
        public DateTime date => CreatedAt;

        [NotMapped]
        public int replyID => ReplyId ?? 0;
    }
}