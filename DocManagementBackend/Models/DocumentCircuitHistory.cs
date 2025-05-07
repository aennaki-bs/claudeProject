using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocManagementBackend.Models
{
    public class DocumentCircuitHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DocumentId { get; set; }

        [ForeignKey("DocumentId")]
        public Document? Document { get; set; }

        public int? FromStatusId { get; set; }

        [ForeignKey("FromStatusId")]
        public CircuitStatus? FromStatus { get; set; }

        public int? ToStatusId { get; set; }

        [ForeignKey("ToStatusId")]
        public CircuitStatus? ToStatus { get; set; }

        public int? StepId { get; set; }

        [ForeignKey("StepId")]
        public CircuitStep? Step { get; set; }

        public int? StatusId { get; set; }

        [ForeignKey("StatusId")]
        public CircuitStatus? Status { get; set; }

        public int? ActionId { get; set; }

        [ForeignKey("ActionId")]
        public Models.Action? Action { get; set; }

        [Required]
        public int ProcessedByUserId { get; set; }

        [ForeignKey("ProcessedByUserId")]
        public User? ProcessedBy { get; set; }

        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;

        public string Comments { get; set; } = string.Empty;

        public bool IsApproved { get; set; }
    }
} 