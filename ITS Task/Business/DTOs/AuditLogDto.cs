namespace ITS_Task.Business.DTOs
{
    public class AuditLogDto
    {
        public int Id { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string Action { get; set; } = string.Empty;

        public string EntityName { get; set; } = string.Empty;

        public string EntityId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }

        public string? Details { get; set; }
    }
}