using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models {
    public class User {
        [Key]
        public int Id { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string WebSite { get; set; } = string.Empty;
        public string Identity { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public bool IsPhoneVerified { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = false;
        public bool IsOnline { get; set; } = false;
        public DateTime? LastLogin { get; set; }
        public string? ProfilePicture { get; set; }
        public string? BackgroundPicture { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiry { get; set; }
        public int RoleId { get; set; }
        [ForeignKey("RoleId")]
        [JsonIgnore]
        public Role? Role { get; set; }
        [JsonIgnore]
        public ICollection<LogHistory> LogHistories { get; set; } = new List<LogHistory>();
        [JsonIgnore]
        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }

    public class Role {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? RoleName { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public bool IsSimpleUser { get; set; } = false;
        public bool IsFullUser { get; set; } = false;
    }

    public class LogHistory {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public required User User { get; set; }
        [Required]
        public DateTime Timestamp { get; set; }
        [Required]
        public int ActionType { get; set; } //  0 for Logout, 1 for Login, 2 create his profile, 3 update his profile, 4 create doc, 5 update doc, 6 delete doc, 7 create profile, 8 update profile, 9 delete profile
        public string Description { get; set; } = string.Empty;
    }
}
