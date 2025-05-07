namespace DocManagementBackend.Models
{
    public class AdminCreateUserRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string RoleName { get; set; } = string.Empty;
    }
    public class AdminUpdateUserRequest
    {
        public string? Email { get; set; } = string.Empty;

        public string? Username { get; set; } = string.Empty;

        public string? PasswordHash { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? IsEmailConfirmed { get; set; }
        public string? RoleName { get; set; }
        public bool? IsActive { get; set; }
    }

}
