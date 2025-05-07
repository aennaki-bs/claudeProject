public class DocumentDto
{
    public int Id { get; set; }
    public string DocumentKey { get; set; } = string.Empty;
    public string DocumentAlias { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime? DocDate { get; set; }
    public int Status { get; set; }
    public int? TypeId { get; set; }
    public DocumentTypeDto? DocumentType { get; set; }
    public int? SubTypeId { get; set; }
    public SubTypeDto? SubType { get; set; }
    public int CreatedByUserId { get; set; }
    public DocumentUserDto? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CircuitId { get; set; }
    public int? CurrentStepId { get; set; }
    public string CurrentStepTitle { get; set; } = string.Empty;
    public int? CurrentStatusId { get; set; }
    public string CurrentStatusTitle { get; set; } = string.Empty;
    public bool IsCircuitCompleted { get; set; }
    public int LignesCount { get; set; }
    public int SousLignesCount { get; set; }
} 