namespace DocManagementBackend.Models {
    public class LoginRequest
    {
        public string EmailOrUsername { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
    public class LogoutRequest { public int UserId { get; set; } }

    public class VerifyEmailRequest
    {
        public string? Email { get; set; }
        public string? VerificationCode { get; set; }
    }

    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryMinutes { get; set; } = 180;
    }
}