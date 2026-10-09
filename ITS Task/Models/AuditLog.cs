using System.ComponentModel.DataAnnotations;

namespace ITS_Task.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string EntityName { get; set; } = string.Empty;

        [Required]
        public string EntityId { get; set; } = string.Empty;

        [Required]
        public DateTime Timestamp { get; set; }

        [StringLength(1000)]
        public string? Details { get; set; }
    }
}