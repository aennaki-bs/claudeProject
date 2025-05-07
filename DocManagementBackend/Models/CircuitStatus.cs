using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DocManagementBackend.Models
{
    public class CircuitStatus
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "flexible"; // "initial", "final", "flexible"

        [Required]
        public int CircuitId { get; set; }

        [ForeignKey("CircuitId")]
        public Circuit? Circuit { get; set; }

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<CircuitStep>? FromSteps { get; set; }
        public ICollection<CircuitStep>? ToSteps { get; set; }
    }
} 