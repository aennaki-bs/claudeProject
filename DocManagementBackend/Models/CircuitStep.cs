using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocManagementBackend.Models
{
    public class CircuitStep
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StepKey { get; set; } = string.Empty;

        [Required]
        public int CircuitId { get; set; }

        [ForeignKey("CircuitId")]
        public Circuit? Circuit { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string Descriptif { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        [Required]
        public int FromStatusId { get; set; }

        [ForeignKey("FromStatusId")]
        public CircuitStatus? FromStatus { get; set; }

        [Required]
        public int ToStatusId { get; set; }

        [ForeignKey("ToStatusId")]
        public CircuitStatus? ToStatus { get; set; }

        public bool IsFinalStep { get; set; }

        public int? NextStepId { get; set; }

        [ForeignKey("NextStepId")]
        public CircuitStep? NextStep { get; set; }

        public int? PrevStepId { get; set; }

        [ForeignKey("PrevStepId")]
        public CircuitStep? PrevStep { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Optional: Add conditions or rules for this transition
        public string? Conditions { get; set; }

        // Optional: Add actions that should be performed during this transition
        public string? Actions { get; set; }
    }
} 