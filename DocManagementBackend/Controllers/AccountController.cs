using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.Utils;
using System.Security.Claims;

namespace DocManagementBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context) { _context = context; }

        [Authorize]
        [HttpGet("user-info")]
        public async Task<IActionResult> GetUserInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim is missing.");
            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest("Invalid user ID.");

            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");
            if (!user.IsActive)
                return Unauthorized("This account is desactivated!");
            var picture = string.Empty;
            if (!string.IsNullOrEmpty(user.ProfilePicture))
                {picture = $"{Request.Scheme}://{Request.Host}{user.ProfilePicture}";}
            var userInfo = new {userId = user.Id,
                username = user.Username, email = user.Email,
                role = user.Role?.RoleName ?? "SimpleUser",
                firstName = user.FirstName, lastName = user.LastName,
                profilePicture = picture, isActive = user.IsActive,
                address = user.Address, city = user.City, country = user.Country,
                phoneNumber = user.PhoneNumber, isOnline = user.IsOnline, //isBlocked = user.IsBlocked,
            };

            return Ok(userInfo);
        }

        [Authorize]
        [HttpGet("user-role")]
        public async Task<IActionResult> GetUserRole()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim is missing.");
            if (!int.TryParse(userIdClaim, out var userId))
                return BadRequest("Invalid user ID.");
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound("User not found.");
            if (!user.IsActive)
                return Unauthorized("This account is desactivated!");
            var userRole = new { role = user.Role?.RoleName ?? "SimpleUser" };

            return Ok(userRole);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            string? frontDomain = Environment.GetEnvironmentVariable("FRONTEND_DOMAIN");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound("No user found with that email address.");
            if (!user.IsEmailConfirmed)
                return Unauthorized("Email Not Verified!");
            if (!user.IsActive)
                return Unauthorized("User Account Desactivated!");
            var verificationLink = $"{frontDomain}/update-password/{user.Email}";
            var emailBody = AuthHelper.createPassEmailBody(verificationLink);
            AuthHelper.SendEmail(user.Email, "Password Reset", emailBody);
            return Ok("A Link Is Sent To Your Email.");
        }

        [HttpPut("update-password")]
        public async Task<IActionResult> UpdatePassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound("No user found with that email address.");
            if (!user.IsEmailConfirmed)
                return Unauthorized("Email Not Verified!");
            if (!user.IsActive)
                return Unauthorized("User Account Desactivated!");
            if (!AuthHelper.IsValidPassword(request.NewPassword))
                return BadRequest("Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a digit, and a special character.");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return Ok("Your password is updated successfully!");
        }

        [HttpPost("resend-code")]
        public async Task<IActionResult> ResendCode([FromBody] ForgotPasswordRequest request) {
            string? frontDomain = Environment.GetEnvironmentVariable("FRONTEND_DOMAIN");
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required!");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound("No user found with that email address.");
            if (user.IsEmailConfirmed)
                return BadRequest("Email already verified!");
            var verifCode = new Random().Next(100000, 999999).ToString();
            // if (string.IsNullOrEmpty(user.EmailVerificationCode))
            user.EmailVerificationCode = verifCode;
            var verificationLink = $"{frontDomain}/verify/{user.Email}";
            string emailBody = AuthHelper.CreateEmailBody(verificationLink, user.EmailVerificationCode);
            AuthHelper.SendEmail(user.Email, "Email Verification", emailBody);
            await _context.SaveChangesAsync();
            return Ok("A Verification Code Is reSent To Your Email.");
        }

        [Authorize]
        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");
            if (!string.IsNullOrEmpty(request.Username) && user.Username != request.Username)
            {
                var userName = await _context.Users.AnyAsync(u => u.Username == request.Username);
                if (userName)
                    return BadRequest("Username is already in use.");
            }
            user.Username = request.Username ?? user.Username;
            user.FirstName = request.FirstName ?? user.FirstName;
            user.Address = request.Address ?? user.Address;
            user.City = request.City ?? user.City;
            user.Country = request.Country ?? user.Country;
            user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
            user.LastName = request.LastName ?? user.LastName;
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                if (!string.IsNullOrEmpty(request.CurrentPassword))
                    {if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                        return BadRequest("Current password is incorrect.");}
                else { return BadRequest("Current password is required."); }
                if (!AuthHelper.IsValidPassword(request.NewPassword))
                    return BadRequest("New password does not meet complexity requirements.");
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }
            await _context.SaveChangesAsync();
            var logEntry = new LogHistory
            {
                UserId = userId,
                User = user,
                Timestamp = DateTime.UtcNow,
                ActionType = 3,
                Description = $"{user.Username} has updated their profile"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return Ok("Profile updated successfully.");
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            try {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("User ID claim is missing.");
                if (!int.TryParse(userIdClaim, out int userId))
                    return BadRequest("Invalid user ID format.");
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found.");
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "profile");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Invalid file type. Allowed: JPG, JPEG, PNG, GIF");
                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest("File size exceeds 5MB limit");
                if (!string.IsNullOrEmpty(user.ProfilePicture) && user.ProfilePicture != "/images/profile/default.png") {
                    var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", user.ProfilePicture.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);}
                var sanitizedUsername = string.Join("", user.Username.Split(Path.GetInvalidFileNameChars()));
                var fileName = $"{sanitizedUsername}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);
                user.ProfilePicture = $"/images/profile/{fileName}";
                await _context.SaveChangesAsync();
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(new {filePath = $"{baseUrl}{user.ProfilePicture}",
                    message = "Image uploaded successfully"});
            }
            catch (Exception ex) {return StatusCode(500, $"Internal server error: {ex.Message}");}
        }

        [HttpGet("profile-image/{userId}")]
        public async Task<IActionResult> GetProfileImage(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.ProfilePicture))
                return NotFound("Profile image not found.");
            return Ok(new { ProfilePicture = user.ProfilePicture });
        }

        [Authorize]
        [HttpPut("update-email")]
        public async Task<IActionResult> UpdateEmail([FromBody] UpdateEmailRequest request) {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID claim is missing.");
            if (!int.TryParse(userIdClaim, out int userId))
                return BadRequest("Invalid user ID format.");
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");
            if (!user.IsActive)
                return Unauthorized("User account is desactivated. Please contact an admin!");
            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required!");
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email already in use!");
            user.Email = request.Email;
            user.EmailVerificationCode = new Random().Next(100000, 999999).ToString();
            user.IsActive = false;
            user.IsEmailConfirmed = false;
            string? frontDomain = Environment.GetEnvironmentVariable("FRONTEND_DOMAIN");
            var verificationLink = $"{frontDomain}/verify/{user.Email}";
            string emailBody = AuthHelper.CreateEmailBody(verificationLink, user.EmailVerificationCode);
            await _context.SaveChangesAsync();
            AuthHelper.SendEmail(user.Email, "Email Verification", emailBody);
            var logEntry = new LogHistory
            {
                UserId = userId,
                User = user,
                Timestamp = DateTime.UtcNow,
                ActionType = 3,
                Description = $"{user.Username} has updated their profile"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return Ok("Email is updated successfully. Please check your email for confirmation!");
        }
    }
}
