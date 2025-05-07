// using Microsoft.AspNetCore.Mvc;
// using System.Threading.Tasks;
// using DocManagementBackend.Services;
// using DocManagementBackend.Models;
// using DocManagementBackend.Data;
// using Microsoft.EntityFrameworkCore;

// namespace DocManagementBackend.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class PhoneVerificationController : ControllerBase
//     {
//         private readonly SmsVerificationService _verificationService;
//         private readonly ApplicationDbContext _context;

//         public PhoneVerificationController(SmsVerificationService verificationService, ApplicationDbContext context)
//         {
//             _verificationService = verificationService;
//             _context = context;
//         }

//         [HttpPost("send-code")]
//         public async Task<IActionResult> SendVerificationCode([FromBody] PhoneVerificationRequest request)
//         {
//             try
//             {
//                 if (string.IsNullOrEmpty(request.PhoneNumber))
//                 {
//                     return BadRequest(new { message = "Phone number is required" });
//                 }

//                 var sessionInfo = await _verificationService.SendVerificationCode(request.PhoneNumber);
//                 return Ok(new
//                 {
//                     sessionInfo,
//                     message = "Verification code generated. Check the server console output to get the code."
//                 });
//             }
//             catch (Exception ex)
//             {
//                 return BadRequest(new { message = $"Failed to send verification code: {ex.Message}" });
//             }
//         }

//         [HttpPost("verify")]
//         public async Task<IActionResult> VerifyPhoneNumber([FromBody] VerifyPhoneRequest request)
//         {
//             if (string.IsNullOrEmpty(request.PhoneNumber) || string.IsNullOrEmpty(request.Code))
//             {
//                 return BadRequest(new { message = "Phone number and verification code are required" });
//             }

//             var isVerified = await _verificationService.VerifyPhoneNumber(
//                 request.SessionInfo, request.Code, request.PhoneNumber);

//             if (isVerified)
//             {
//                 if (request.UserId > 0)
//                 {
//                     var user = await _context.Users.FindAsync(request.UserId);
//                     if (user != null)
//                     {
//                         user.PhoneNumber = request.PhoneNumber;
//                         user.IsPhoneVerified = true; // Make sure this field exists
//                         await _context.SaveChangesAsync();
//                     }
//                 }

//                 return Ok(new { message = "Phone number verified successfully" });
//             }

//             return BadRequest(new { message = "Invalid verification code" });
//         }
//     }
// }