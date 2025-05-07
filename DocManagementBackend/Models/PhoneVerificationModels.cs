
using System.Text.Json.Serialization;

namespace DocManagementBackend.Models
{
    public class PhoneVerificationRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    public class VerifyPhoneRequest
    {
        public int UserId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string SessionInfo { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class SendVerificationResponse
    {
        [JsonPropertyName("sessionInfo")]
        public string SessionInfo { get; set; } = string.Empty;
    }

    public class VerifyPhoneResponse
    {
        [JsonPropertyName("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
    }
}