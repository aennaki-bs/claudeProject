using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class Action
    {
        [Key]
        public int Id { get; set; }
        public string ActionKey { get; set; } = string.Empty;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        // Whether this action should automatically advance to the next step if all required statuses are complete
        public bool AutoAdvance { get; set; } = false;
        [JsonIgnore]
        public ICollection<StepAction> StepActions { get; set; } = new List<StepAction>();
    }

    public class StepAction
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int StepId { get; set; }
        [ForeignKey("StepId")]
        [JsonIgnore]
        public Step? Step { get; set; }
        [Required]
        public int ActionId { get; set; }
        [ForeignKey("ActionId")]
        [JsonIgnore]
        public Action? Action { get; set; }
    }

    public class ActionStatusEffect
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int ActionId { get; set; }
        [ForeignKey("ActionId")]
        [JsonIgnore]
        public Action? Action { get; set; }
        [Required]
        public int StepId { get; set; }
        [ForeignKey("StepId")]
        [JsonIgnore]
        public Step? Step { get; set; }
        [Required]
        public int StatusId { get; set; }
        [ForeignKey("StatusId")]
        [JsonIgnore]
        public Status? Status { get; set; }
        public bool SetsComplete { get; set; } = true;
    }
}