namespace DocManagementBackend.Models
{
    public class CreateSubTypeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DocumentTypeId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateSubTypeDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool? IsActive { get; set; }
    }

    public class SubTypeDto
    {
        public int Id { get; set; }
        public string SubTypeKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DocumentTypeId { get; set; }
        public bool IsActive { get; set; }
        public DocumentTypeDto? DocumentType { get; set; }
    }
}