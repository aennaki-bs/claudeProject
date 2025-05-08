namespace DocManagementBackend.Models
{
    public class CreateCircuitDto
    {
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        // public bool HasOrderedFlow { get; set; } = false;
        // public bool AllowBacktrack { get; set; } = true;
        public bool IsActive { get; set; } = false;
    }

    public class CircuitDto
    {
        public int Id { get; set; }
        public string CircuitKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        // public bool HasOrderedFlow { get; set; }
        // public bool AllowBacktrack { get; set; }
        public List<StatusDto> Statuses { get; set; } = new();
    }

    public class CreateStepDto
    {
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int CurrentStatusId { get ; set; } = 0;
        public int NextStatusId { get ; set; } = 0;
        // public int OrderIndex { get; set; } = 0;
        // public int? ResponsibleRoleId { get; set; }
    }
    public class UpdateStepDto
    {
        public string? Title { get; set; }
        public string? Descriptif { get; set; }
        public int CurrentStatusId { get; set; } = 0;
        public int NextStatusId { get; set; } = 0;
        // public int? OrderIndex { get; set; }
        // public int? ResponsibleRoleId { get; set; }
        // public bool? IsFinalStep { get; set; }
    }

    public class StepDto
    {
        public int Id { get; set; }
        public string StepKey { get; set; } = string.Empty;
        public int CircuitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int CurrentStatusId { get; set; } = 0;
        public string CurrentStatusTitle { get; set; } = string.Empty;
        public int NextStatusId { get; set; } = 0;
        public string NextStatusTitle { get; set; } = string.Empty;
        // public int OrderIndex { get; set; }
        // public int? ResponsibleRoleId { get; set; }
        // public bool IsFinalStep { get; set; }
    }

    // public class StepOrderUpdateDto
    // {
    //     public int StepId { get; set; }
    //     public int OrderIndex { get; set; }
    // }

    // public class CircuitValidationDto
    // {
    //     public int CircuitId { get; set; }
    //     public string CircuitTitle { get; set; } = string.Empty;
    //     public bool HasSteps { get; set; }
    //     public int TotalSteps { get; set; }
    //     public bool AllStepsHaveStatuses { get; set; }
    //     public bool IsValid { get; set; }
    //     public List<StepValidationDto> StepsWithoutStatuses { get; set; } = new List<StepValidationDto>();
    // }

    // public class StepValidationDto
    // {
    //     public int StepId { get; set; }
    //     public string StepTitle { get; set; } = string.Empty;
    //     public int Order { get; set; }
    // }
}