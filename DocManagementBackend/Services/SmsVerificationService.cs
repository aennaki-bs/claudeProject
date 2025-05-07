// using System;
// using System.Collections.Concurrent;
// using System.Text.Json;
// using System.Threading.Tasks;
// using RestSharp;
// using FirebaseAdmin;
// using FirebaseAdmin.Auth;

// namespace DocManagementBackend.Services
// {
//     public class SmsVerificationService
//     {
//         private readonly string _apiKey;
//         private readonly RestClient _client;
//         private static readonly ConcurrentDictionary<string, (string Code, DateTime Expiry)> _verificationCodes = new();

//         public SmsVerificationService(IConfiguration configuration)
//         {
//             _apiKey = configuration["Firebase:WebApiKey"]
//                 ?? throw new InvalidOperationException("Firebase Web API Key is not configured");
//             _client = new RestClient("https://identitytoolkit.googleapis.com/v1");
//         }

//         public async Task<string> SendVerificationCode(string phoneNumber)
//         {
//             try
//             {
//                 // Generate a random 6-digit code
//                 var random = new Random();
//                 var code = random.Next(100000, 999999).ToString();

//                 // Store the code with a 10-minute expiration
//                 _verificationCodes[phoneNumber] = (code, DateTime.UtcNow.AddMinutes(10));

//                 // For debugging
//                 Console.WriteLine($"Generated verification code for {phoneNumber}: {code}");

//                 // In a real implementation, we would use Firebase to send SMS
//                 // For testing, we'll use our own session ID
//                 string sessionId = Guid.NewGuid().ToString();

//                 return sessionId;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"Error generating verification code: {ex.Message}");
//                 throw new Exception($"Failed to send verification code: {ex.Message}", ex);
//             }
//         }

//         public async Task<bool> VerifyPhoneNumber(string sessionInfo, string code, string phoneNumber)
//         {
//             // For testing, we'll verify against our locally stored codes
//             if (_verificationCodes.TryGetValue(phoneNumber, out var storedData))
//             {
//                 var (storedCode, expiry) = storedData;

//                 // Check if the code matches and hasn't expired
//                 if (storedCode == code && expiry > DateTime.UtcNow)
//                 {
//                     // Remove the code after successful verification
//                     _verificationCodes.TryRemove(phoneNumber, out _);
//                     return true;
//                 }
//             }

//             return false;
//         }

//         // Response classes
//         private class SendVerificationResponse
//         {
//             public string SessionInfo { get; set; } = string.Empty;
//         }

//         private class VerifyPhoneResponse
//         {
//             public string IdToken { get; set; } = string.Empty;
//             public string RefreshToken { get; set; } = string.Empty;
//         }
//     }
// }