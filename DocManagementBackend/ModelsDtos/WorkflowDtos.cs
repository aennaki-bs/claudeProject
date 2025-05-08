namespace DocManagementBackend.Models
{
    public class AssignCircuitDto
    {
        public int DocumentId { get; set; }
        public int CircuitId { get; set; }
    }

    public class PerformActionDto
    {
        public int DocumentId { get; set; }
        public int ActionId { get; set; }
        public string Comments { get; set; } = string.Empty;
        public bool IsApproved { get; set; } = true;
    }

    public class ReturnToPreviousDto
    {
        public int DocumentId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

    public class DocumentHistoryDto
    {
        public int Id { get; set; }
        public string StepTitle { get; set; } = string.Empty;
        public string ActionTitle { get; set; } = string.Empty;
        public string StatusTitle { get; set; } = string.Empty;
        public string ProcessedBy { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; }
        public string Comments { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
    }

    public class DocumentWorkflowStatusDto
    {
        public int DocumentId { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public int? CircuitId { get; set; }
        public string CircuitTitle { get; set; } = string.Empty;
        public int? CurrentStepId { get; set; }
        public string CurrentStepTitle { get; set; } = string.Empty;
        public int Status { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public bool IsCircuitCompleted { get; set; }
        public List<DocumentStatusDto> Statuses { get; set; } = new();
        public List<ActionDto> AvailableActions { get; set; } = new();
        public bool CanAdvanceToNextStep { get; set; }
        public bool CanReturnToPreviousStep { get; set; }
    }

    public class PendingDocumentDto
    {
        public int DocumentId { get; set; }
        public string DocumentKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int CircuitId { get; set; }
        public string CircuitTitle { get; set; } = string.Empty;
        public int CurrentStepId { get; set; }
        public string CurrentStepTitle { get; set; } = string.Empty;
        public int DaysInCurrentStep { get; set; }
    }
    public class MoveNextDto
    {
        public int DocumentId { get; set; }
        public int CurrentStepId { get; set; }
        public int NextStepId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
    public class MoveToDocumentDto
    {
        public int DocumentId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }

}