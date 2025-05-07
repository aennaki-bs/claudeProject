using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using System.Security.Claims;
using DocManagementBackend.Mappings;
using DocManagementBackend.Utils;

namespace DocManagementBackend.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context) { _context = context; }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var users = await _context.Users
                .Include(u => u.Role).Where(u => u.Id != userId).Select(UserMappings.ToUserDto).ToListAsync();
            return Ok(users);
        }

        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound("User not found.");
            return Ok(user);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var thisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (thisUser == null)
                return BadRequest("User not found.");
            if (!thisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");
            if (thisUser.Role!.RoleName != "Admin")
                return Unauthorized("User not allowed to view roles.");

            var roles = await _context.Roles.ToListAsync();
            return Ok(roles);
        }

        [HttpPost("users")]
        public async Task<IActionResult> CreateUser([FromBody] AdminCreateUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
                return BadRequest("Email is already in use.");
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
                return BadRequest("Username is already in use.");
            int roleId = 0;
            if (request.RoleName == "Admin") { roleId = 1; }
            if (request.RoleName == "SimpleUser") { roleId = 2; }
            if (request.RoleName == "FullUser") { roleId = 3; }
            var role = await _context.Roles.FindAsync(roleId);
            if (role == null)
                return BadRequest("Invalid RoleName.");
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash);
            var emailVerificationCode = new Random().Next(100000, 999999).ToString();
            var newUser = new User
            {
                Email = request.Email,
                Username = request.Username,
                PasswordHash = hashedPassword,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsEmailConfirmed = false,
                IsActive = false,
                CreatedAt = DateTime.UtcNow,
                RoleId = roleId,
                EmailVerificationCode = emailVerificationCode,
                ProfilePicture = "/images/profile/default.png"
            };
            string? frontDomain = Environment.GetEnvironmentVariable("FRONTEND_DOMAIN");
            var verificationLink = $"{frontDomain}/verify/{newUser.Email}";
            string emailBody = AuthHelper.CreateEmailBody(verificationLink, newUser.EmailVerificationCode);
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            AuthHelper.SendEmail(newUser.Email, "Email Verification", emailBody);
            var logEntry = new LogHistory
            {
                UserId = userId,
                User = ThisUser,
                Timestamp = DateTime.UtcNow,
                ActionType = 7,
                Description = $"{ThisUser.Username} has created a profile for {newUser.Username}"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetUser), new { id = newUser.Id }, new
            {
                newUser.Id,
                newUser.Username,
                newUser.Email,
                newUser.FirstName,
                newUser.LastName,
                Role = role.RoleName,
                newUser.IsActive,
                newUser.CreatedAt
            });
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");
            if (!string.IsNullOrEmpty(request.Username) && await _context.Users.AnyAsync(u => u.Username == request.Username) && user.Username != request.Username)
                return BadRequest("Username is already in use.");
            if (!string.IsNullOrEmpty(request.Username))
                user.Username = request.Username;
            if (!string.IsNullOrEmpty(request.PasswordHash))
            {
                if (!AuthHelper.IsValidPassword(request.PasswordHash))
                    return BadRequest("Password must be at least 8 characters long and include an uppercase letter, a lowercase letter, a digit, and a special character.");
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.PasswordHash);
            }
            if (!string.IsNullOrEmpty(request.FirstName))
                user.FirstName = request.FirstName;
            if (!string.IsNullOrEmpty(request.LastName))
                user.LastName = request.LastName;
            if (request.IsEmailConfirmed.HasValue)
                user.IsEmailConfirmed = request.IsEmailConfirmed.Value;
            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;
            if (!string.IsNullOrEmpty(request.RoleName))
            {
                int roleId = 0;
                if (request.RoleName == "Admin") { roleId = 1; }
                if (request.RoleName == "SimpleUser") { roleId = 2; }
                if (request.RoleName == "FullUser") { roleId = 3; }
                var role = await _context.Roles.FindAsync(roleId);
                if (role == null)
                    return BadRequest("Invalid RoleName.");
                user.RoleId = role.Id; user.Role = role;
            }
            await _context.SaveChangesAsync();
            var logEntry = new LogHistory
            {
                UserId = userId,
                User = ThisUser,
                Timestamp = DateTime.UtcNow,
                ActionType = 8,
                Description = $"{ThisUser.Username} has updated {user.Username}'s profile"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return Ok("User updated successfully.");
        }

        [HttpPut("users/email/{id}")]
        public async Task<IActionResult> UpdateEmailUser(int id, [FromBody] AdminUpdateUserRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");
            if (!string.IsNullOrEmpty(request.Email) && await _context.Users.AnyAsync(u => u.Email == request.Email) && user.Email != request.Email)
                return BadRequest("Email is already in use.");
            if (!string.IsNullOrEmpty(request.Email))
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
                User = ThisUser,
                Timestamp = DateTime.UtcNow,
                ActionType = 8,
                Description = $"{ThisUser.Username} has updated {user.Username}'s profile"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return Ok($"{user.Username}'s email is updated successfully. He need to check his email for confirmation!");
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound("User not found.");
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            var logEntry = new LogHistory
            {
                UserId = userId,
                User = ThisUser,
                Timestamp = DateTime.UtcNow,
                ActionType = 9,
                Description = $"{ThisUser.Username} has deleted {user.Username}'s profile"
            };
            _context.LogHistories.Add(logEntry);
            await _context.SaveChangesAsync();
            return Ok("User deleted successfully.");
        }

        [HttpDelete("delete-users")]
        public async Task<IActionResult> DeleteUsers([FromBody] List<int> userIds)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            if (userIds == null || !userIds.Any())
                return BadRequest("No user IDs provided.");
            var usersToDelete = await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync();
            if (!usersToDelete.Any())
                return NotFound("No users found with the provided IDs.");
            int currentUserId = int.Parse(userIdClaim);
            usersToDelete.RemoveAll(u => u.Id == currentUserId);
            _context.Users.RemoveRange(usersToDelete);
            await _context.SaveChangesAsync();
            return Ok($"{usersToDelete.Count} Users Deleted Successfully.");
        }

        [HttpGet("logs/{id}")]
        public async Task<IActionResult> GetUserLogHistory(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");
            int userId = int.Parse(userIdClaim);
            var ThisUser = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);
            if (ThisUser == null)
                return BadRequest("User not found.");
            if (!ThisUser.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");
            if (ThisUser.Role!.RoleName != "Admin")
                return Unauthorized("User Not Allowed To do this action.");
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return NotFound("User not found!");
            var logsDto = await _context.LogHistories.Where(l => l.UserId == id).Include(l => l.User)
                .ThenInclude(u => u.Role)
            .Select(l => new LogHistoryDto
            {
                Id = l.Id,
                ActionType = l.ActionType,
                Timestamp = l.Timestamp,
                Description = l.Description,
                User = new UserLogDto
                {
                    Username = l.User.Username,
                    Role = l.User.Role != null ? l.User.Role.RoleName : string.Empty
                }
            }).OrderByDescending(l => l.Timestamp).ToListAsync();
            if (logsDto == null)
                return NotFound("User logs not found!");
            return Ok(logsDto);
        }
    }
}
// Console.ForegroundColor = ConsoleColor.Green;
// Console.WriteLine($"=== request Users === {request.RoleName}");
// Console.ResetColor();
