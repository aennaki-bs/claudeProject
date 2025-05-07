namespace DocManagementBackend.Models
{
    public class CreateStatusDto
    {
        public string Title { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        // public bool IsComplete { get; set; } = true;
    }

    public class StatusDto
    {
        public int StatusId { get; set; }
        public string StatusKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsComplete { get; set; }
        public int StepId { get; set; }
    }

    public class DocumentStatusDto
    {
        public int StatusId { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsRequired { get; set; }
        public bool IsComplete { get; set; }
        public string? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CompleteStatusDto
    {
        public int DocumentId { get; set; }
        public int StatusId { get; set; }
        public bool IsComplete { get; set; } = true;
        public string Comments { get; set; } = string.Empty;
    }

}