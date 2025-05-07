using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class Circuit
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string Descriptif { get; set; } = string.Empty;

        public string CircuitKey { get; set; } = string.Empty;

        public int CrdCounter { get; set; }

        public bool IsActive { get; set; } = true;

        public bool AllowBacktrack { get; set; } = false;

        public bool HasOrderedFlow { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<CircuitStatus> Statuses { get; set; } = new List<CircuitStatus>();
        public ICollection<CircuitStep> Steps { get; set; } = new List<CircuitStep>();
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class Step
    {
        [Key]
        public int Id { get; set; }
        public string StepKey { get; set; } = string.Empty;
        [Required]
        public int CircuitId { get; set; }
        [ForeignKey("CircuitId")]
        [JsonIgnore]
        public Circuit? Circuit { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public int OrderIndex { get; set; } = 0;
        public int? ResponsibleRoleId { get; set; }
        [ForeignKey("ResponsibleRoleId")]
        [JsonIgnore]
        public Role? ResponsibleRole { get; set; }
        public int? NextStepId { get; set; }
        [ForeignKey("NextStepId")]
        [JsonIgnore]
        public Step? NextStep { get; set; }
        public int? PrevStepId { get; set; }
        [ForeignKey("PrevStepId")]
        [JsonIgnore]
        public Step? PrevStep { get; set; }
        public bool IsFinalStep { get; set; } = false;
        [JsonIgnore]
        public ICollection<Status> Statuses { get; set; } = new List<Status>();
        [JsonIgnore]
        public ICollection<StepAction> StepActions { get; set; } = new List<StepAction>();
    }
}