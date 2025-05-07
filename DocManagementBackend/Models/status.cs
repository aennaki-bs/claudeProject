using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class Status
    {
        [Key]
        public int Id { get; set; }
        public string StatusKey { get; set; } = string.Empty;
        [Required]
        public int StepId { get; set; }
        [ForeignKey("StepId")]
        [JsonIgnore]
        public Step? Step { get; set; }
        [Required]
        public string Title { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public bool IsComplete { get; set; } = false;
    }

    public class DocumentStatus
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        [JsonIgnore]
        public Document? Document { get; set; }
        [Required]
        public int StatusId { get; set; }
        [ForeignKey("StatusId")]
        [JsonIgnore]
        public Status? Status { get; set; }
        public bool IsComplete { get; set; } = false;
        public int? CompletedByUserId { get; set; }
        [ForeignKey("CompletedByUserId")]
        [JsonIgnore]
        public User? CompletedBy { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}