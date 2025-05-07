namespace DocManagementBackend.Models
{
    public class UserLogDto
    {
        public string Username { get; set; } = string.Empty;
        public string? Role { get; set; } = string.Empty;
    }

    public class LogHistoryDto
    {
        public int Id { get; set; }
        public int ActionType { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = string.Empty;
        public UserLogDto User { get; set; } = new UserLogDto();
    }
}