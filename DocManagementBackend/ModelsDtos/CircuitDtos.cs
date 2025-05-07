using System.ComponentModel.DataAnnotations;

namespace DocManagementBackend.ModelsDtos
{
    public class CreateCircuitDto
    {
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool HasOrderedFlow { get; set; } = true;
        public bool AllowBacktrack { get; set; } = false;
    }

    public class UpdateCircuitDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public bool? HasOrderedFlow { get; set; }
        public bool? AllowBacktrack { get; set; }
    }

    public class CircuitDto
    {
        public int Id { get; set; }
        public string CircuitKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool HasOrderedFlow { get; set; }
        public bool AllowBacktrack { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CircuitStepDto> Steps { get; set; } = new();
        public List<CircuitStatusDto> Statuses { get; set; } = new();
    }

    public class CreateStepDto
    {
        [Required]
        public int CircuitId { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        [Required]
        public int FromStatusId { get; set; }
        [Required]
        public int ToStatusId { get; set; }
        public bool IsFinalStep { get; set; }
    }

    public class UpdateStepDto
    {
        public string? Title { get; set; }
        public string? Descriptif { get; set; }
        public int? OrderIndex { get; set; }
        public int? FromStatusId { get; set; }
        public int? ToStatusId { get; set; }
        public bool? IsFinalStep { get; set; }
    }

    public class CircuitStepDto
    {
        public int Id { get; set; }
        public string StepKey { get; set; } = string.Empty;
        public int CircuitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int FromStatusId { get; set; }
        public int ToStatusId { get; set; }
        public bool IsFinalStep { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreateCircuitStatusDto
    {
        [Required]
        public int CircuitId { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        [Required]
        public string Type { get; set; } = "flexible";
        public bool IsActive { get; set; } = true;
        public int OrderIndex { get; set; }
    }

    public class UpdateCircuitStatusDto
    {
        public string? Title { get; set; }
        public string? Type { get; set; }
        public bool? IsActive { get; set; }
        public int? OrderIndex { get; set; }
    }

    public class CircuitStatusDto
    {
        public int Id { get; set; }
        public int CircuitId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "flexible";
        public bool IsActive { get; set; }
        public int OrderIndex { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CircuitValidationDto
    {
        public int CircuitId { get; set; }
        public string CircuitTitle { get; set; } = string.Empty;
        public bool HasInitialStatus { get; set; }
        public bool HasFinalStatus { get; set; }
        public bool HasValidSteps { get; set; }
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }
}