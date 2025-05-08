using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class Circuit
    {
        [Key]
        public int Id { get; set; }
        public string CircuitKey { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string Descriptif { get; set; } = string.Empty;
        public bool IsActive { get; set; } = false;
        public int CrdCounter { get; set; } = 0;

        [JsonIgnore]
        public ICollection<Status> Statuses { get; set; } = new List<Status>();

        [JsonIgnore]
        public ICollection<Step> Steps { get; set; } = new List<Step>();
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

        [Required]
        public int CurrentStatusId { get; set; }
        [ForeignKey("CurrentStatusId")]
        [JsonIgnore]
        public Status? CurrentStatus { get; set; }

        [Required]
        public int NextStatusId { get; set; }
        [ForeignKey("NextStatusId")]
        [JsonIgnore]
        public Status? NextStatus { get; set; }

        [JsonIgnore]
        public ICollection<StepAction> StepActions { get; set; } = new List<StepAction>();
    }
}