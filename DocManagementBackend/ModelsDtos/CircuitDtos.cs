namespace DocManagementBackend.Models
{
    public class CreateCircuitDto
    {
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
    }

    public class CircuitDto
    {
        public int Id { get; set; }
        public string CircuitKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<StatusDto> Statuses { get; set; } = new();
        public List<StepDto> Steps { get; set; } = new();
    }

    public class CreateStepDto
    {
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int CurrentStatusId { get; set; }
        public int NextStatusId { get; set; }
    }

    public class UpdateStepDto
    {
        public string? Title { get; set; }
        public string? Descriptif { get; set; }
        public int? CurrentStatusId { get; set; }
        public int? NextStatusId { get; set; }
    }

    public class StepDto
    {
        public int Id { get; set; }
        public string StepKey { get; set; } = string.Empty;
        public int CircuitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int CurrentStatusId { get; set; }
        public string CurrentStatusTitle { get; set; } = string.Empty;
        public int NextStatusId { get; set; }
        public string NextStatusTitle { get; set; } = string.Empty;
    }

    public class CircuitValidationDto
    {
        public int CircuitId { get; set; }
        public string CircuitTitle { get; set; } = string.Empty;
        public bool HasStatuses { get; set; }
        public int TotalStatuses { get; set; }
        public bool HasInitialStatus { get; set; }
        public bool HasFinalStatus { get; set; }
        public bool HasSteps { get; set; }
        public int TotalSteps { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
    }
}