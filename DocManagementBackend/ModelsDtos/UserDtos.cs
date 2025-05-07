namespace DocManagementBackend.Models {
    public class UserDto {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        // public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string WebSite { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string Identity { get; set; } = string.Empty;
        public bool IsEmailConfirmed { get; set; } = false;
        public string? EmailVerificationCode { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = false;
        public bool IsOnline { get; set; } = false;
        // public bool IsBlocked { get; set; } = false;
        // public DateTime? LastLogin { get; set; }
        public string? ProfilePicture { get; set; }
        public RoleDto? Role { get; set; } = new RoleDto();

    }

    public class RoleDto {
        public int RoleId { get; set; }
        public string? RoleName { get; set; } = string.Empty;
    }
}