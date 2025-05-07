using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models {
    public class Ligne
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int DocumentId { get; set; }
        [ForeignKey("DocumentId")]
        [JsonIgnore]
        public Document? Document { get; set; }
        public string LigneKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Article { get; set; } = string.Empty;
        public float Prix { get; set; }
        public int SousLigneCounter { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore]
        public ICollection<SousLigne> SousLignes { get; set; } = new List<SousLigne>();
    }

    public class SousLigne
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int LigneId { get; set; }
        [ForeignKey("LigneId")]
        [JsonIgnore]
        public Ligne? Ligne { get; set; }
        public string SousLigneKey { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Attribute { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}