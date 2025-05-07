namespace DocManagementBackend.Models
{
    public class LignesRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
    }

    public class LigneDto
    {
        public int Id { get; set; }
        public int DocumentId { get; set; }
        public string LingeKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public float Prix { get; set; }
        public int SousLignesCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DocumentDto Document { get; set; } = new DocumentDto();
    }

    public class SousLigneDto
    {
        public int Id { get; set; }
        public int LigneId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public LigneDto Ligne { get; set; } = new LigneDto();
    }
}