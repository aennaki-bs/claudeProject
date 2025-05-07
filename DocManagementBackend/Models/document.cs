using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class Document
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int CreatedByUserId { get; set; }
        [ForeignKey("CreatedByUserId")]
        public required User CreatedBy { get; set; }
        public int TypeId { get; set; }
        [ForeignKey("TypeId")]
        public DocumentType? DocumentType { get; set; }

        // New SubType relationship
        public int? SubTypeId { get; set; }
        [ForeignKey("SubTypeId")]
        public SubType? SubType { get; set; }

        public int? CurrentStepId { get; set; }
        [ForeignKey("CurrentStepId")]
        [JsonIgnore]
        public CircuitStep? CurrentStep { get; set; }
        public int? CircuitId { get; set; }
        [ForeignKey("CircuitId")]
        public Circuit? Circuit { get; set; }
        public int? CurrentStatusId { get; set; }
        [ForeignKey("CurrentStatusId")]
        public CircuitStatus? CurrentStatus { get; set; }
        public bool IsCircuitCompleted { get; set; } = false;
        [Required]
        public string DocumentKey { get; set; } = string.Empty;
        public string DocumentAlias { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Content { get; set; }
        public int Status { get; set; } = 0; // 0: Draft, 1: In Progress, 2: Completed, 3: Rejected
        public DateTime? DocDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int LigneCouter { get; set; } = 0;
        public bool IsDeleted { get; set; } = false;
        [JsonIgnore]
        public ICollection<Ligne> Lignes { get; set; } = new List<Ligne>();
        public ICollection<DocumentCircuitHistory> History { get; set; } = new List<DocumentCircuitHistory>();
    }

    public class DocumentType
    {
        [Key]
        public int Id { get; set; }
        public string TypeKey { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string TypeAttr { get; set; } = string.Empty;
        public int DocumentCounter { get; set; } = 0;
        public int DocCounter { get; set; } = 0;
        [JsonIgnore]
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
    public class SubType
    {
        [Key]
        public int Id { get; set; }

        public string SubTypeKey { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int DocumentTypeId { get; set; }

        [ForeignKey("DocumentTypeId")]
        [JsonIgnore]
        public DocumentType? DocumentType { get; set; }

        public bool IsActive { get; set; } = true;

        [JsonIgnore]
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
    public class TypeCounter
    {
        public int Id { get; set; }
        public int Counter { get; set; }
        public int circuitCounter { get; set; }
    }
    public class DocumentStepHistory
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        [JsonIgnore]
        public Document? Document { get; set; }
        [Required]
        public int StepId { get; set; }
        [ForeignKey("StepId")]
        [JsonIgnore]
        public Step? Step { get; set; }
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        [JsonIgnore]
        public User? User { get; set; }
        public DateTime TransitionDate { get; set; } = DateTime.UtcNow;
        public string Comments { get; set; } = string.Empty;
    }
}