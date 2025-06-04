using System;
using System.ComponentModel.DataAnnotations;

namespace ECommercePlatform.Models
{
    public class OperationLog
    {
        [Key]
        public int Id { get; set; }

        public Engineer? Engineer { get; set; } = null!;

        public DateTime ActionTime { get; set; }

        public string Controller { get; set; } = null!;

        public string Action { get; set; } = null!;

        public string? TargetId { get; set; }

        public string? Description { get; set; }
        public DateTime Timestamp { get; internal set; }
    }
}
