using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.Utils;
using Microsoft.AspNetCore.Authorization;

namespace DocManagementBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        public AuthController(ApplicationDbContext context, IConfiguration config)
        {
            _context = context; _config = config;
        }

        [HttpPost("valide-email")]
        public async Task<IActionResult> ValideEmail([FromBody] ValideUsernameRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return Ok("False");
            return Ok("True");
        }

        [HttpPost("valide-username")]
        public async Task<IActionResult> ValideUsername([FromBody] ValideUsernameRequest request)
        {
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return Ok("False");
            return Ok("True");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            var existingUser = await _context.Users.AnyAsync(u => u.Email == user.Email);
            if (existingUser)
                return BadRequest("Email is already in use.");
            var existingUsername = await _context.Users.AnyAsync(u => u.Username == user.Username);
            if (existingUsername)
                return BadRequest("Username is already in use.");
            if (!AuthHelper.IsValidPassword(user.PasswordHash))
                return BadRequest("Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a digit, and a special character.");
            var adminSecretHeader = Request.Headers["AdminSecret"].FirstOrDefault();
            if (!string.IsNullOrEmpty(adminSecretHeader))
            {
                var expectedAdminSecret = Environment.GetEnvironmentVariable("ADMIN_SECRET");
                if (adminSecretHeader == expectedAdminSecret)
                {
                    var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
                    if (adminRole != null)
                        {user.RoleId = adminRole.Id; user.Role = adminRole;}
                }
                else
                    return Unauthorized("Invalid admin secret.");
            }
            else
            {
                var simpleUserRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SimpleUser");
                if (simpleUserRole != null)
                    {user.RoleId = simpleUserRole.Id; user.Role = simpleUserRole;}
                else
                    return BadRequest("Default role not found.");
            }
            user.EmailVerificationCode = new Random().Next(100000, 999999).ToString();
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);
            user.IsActive = false;
            user.IsEmailConfirmed = false;
            user.ProfilePicture = "/images/profile/default.png";
            string? frontDomain = Environment.GetEnvironmentVariable("FRONTEND_DOMAIN");
            var verificationLink = $"{frontDomain}/verify/{user.Email}";
            string emailBody = AuthHelper.CreateEmailBody(verificationLink, user.EmailVerificationCode);
            try
            {
                _context.Users.Add(user);
                var logEntry = new LogHistory
                {
                    UserId = user.Id,
                    User = user,
                    Timestamp = DateTime.UtcNow,
                    ActionType = 2,
                    Description = $"{user.Username} has created their profile"
                };
                _context.LogHistories.Add(logEntry);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                AuthHelper.SendEmail(user.Email, "Email Verification", emailBody);
                return Ok("Registration successful! Please check your email for the verification code.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, $"An error occurred. Please try again. {ex.Message}");
            }
        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return NotFound("User not found.");
            if (user.IsEmailConfirmed && user.EmailVerificationCode == null)
                return BadRequest("Email is already verified !");
            if (user.EmailVerificationCode != request.VerificationCode)
                return BadRequest("Invalid verification code.");
            user.IsEmailConfirmed = true;
            user.IsActive = true;
            // user.IsOnline = false;
            user.EmailVerificationCode = null;
            await _context.SaveChangesAsync();

            return Ok("Email verified successfully!");
        }

        [HttpPost("clear-users")]
        public async Task<IActionResult> ClearUsers()
        {
            var users = _context.Users.Where(u => u.Email != null).ToList();
            _context.Users.RemoveRange(users);
            await _context.SaveChangesAsync();
            return Ok("All users with emails have been deleted.");
        }

        [HttpPost("clear-users/{id}")]
        public async Task<IActionResult> ClearUser(int id)
        {
            var user = _context.Users.Where(u => u.Id == id).ToList();
            _context.Users.RemoveRange(user);
            await _context.SaveChangesAsync();
            return Ok("the user have been deleted.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest model)
        {
            if (string.IsNullOrEmpty(model.EmailOrUsername))
                return BadRequest("Email or Username is required.");
            if (string.IsNullOrEmpty(model.Password))
                return BadRequest("Password is required.");
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == model.EmailOrUsername || u.Username == model.EmailOrUsername);
            if (user == null) {return Unauthorized("Invalid email or username.");}
            if (!BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                return Unauthorized("Invalid password.");
            if (!user.IsEmailConfirmed)
                return Unauthorized("Your account is not activated yet. Please check your email for verification before logging in.");
            if (!user.IsActive)
                return Unauthorized("User Account Is Desactivated. Please contact an admin!");
            var accessToken = AuthHelper.GenerateAccessToken(user);
            var refreshToken = AuthHelper.GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            user.IsOnline = true;
            var login = new LogHistory { UserId = user.Id, User = user, ActionType = 1, Description = "login", Timestamp = DateTime.UtcNow };
            _context.LogHistories.Add(login);
            await _context.SaveChangesAsync();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddHours(3)
            };
            Response.Cookies.Append("accessToken", accessToken, cookieOptions);
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
            return Ok(new { accessToken, refreshToken });
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            var refreshToken = Request.Cookies["refresh_token"];
            if (string.IsNullOrEmpty(refreshToken))
                return Unauthorized("No refresh token provided.");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");
            var newAccessToken = AuthHelper.GenerateAccessToken(user);
            var newRefreshToken = AuthHelper.GenerateRefreshToken();
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddHours(3);
            await _context.SaveChangesAsync();
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            // Response.Cookies.Append("accessToken", accessToken, cookieOptions);
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);

            return Ok(new { accessToken = newAccessToken });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
            if (user == null)
                return Unauthorized("User Not Found!");
            var logEntry = new LogHistory { UserId = user.Id, User = user, Timestamp = DateTime.UtcNow, ActionType = 0, Description = "logout"};
            _context.LogHistories.Add(logEntry);
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            user.IsOnline = false;
            await _context.SaveChangesAsync();
            Response.Cookies.Delete("refresh_token");

            return Ok("Logged out successfully.");
        }
    }
}
