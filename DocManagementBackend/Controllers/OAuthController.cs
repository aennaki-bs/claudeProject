using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DocManagementBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        public OAuthController(ApplicationDbContext context, IConfiguration config){ _context = context; _config = config;}

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request) {
            try {
                var payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken);
                var user = await _context.Users.Include(u => u.Role)
                                  .FirstOrDefaultAsync(u => u.Email == payload.Email);
                if (user == null) {
                    user = new User {
                        Email = payload.Email,
                        Username = payload.Email.Split('@')[0],
                        FirstName = payload.GivenName, LastName = payload.FamilyName,
                        IsEmailConfirmed = true, IsActive = true, CreatedAt = DateTime.UtcNow,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString())};
                    var simpleUserRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SimpleUser");
                    if (simpleUserRole != null) {
                        user.RoleId = simpleUserRole.Id;
                        user.Role = simpleUserRole;}

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }
                var accessToken = GenerateAccessToken(user);
                return Ok(new { accessToken });
            }
            catch (Exception ex) {
                return Unauthorized($"Invalid Google token: {ex.Message}");}
        }

        private string GenerateAccessToken(User user) {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET");;
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT configuration is missing.");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var claims = new[] {new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Username", user.Username),
                new Claim("IsActive", user.IsActive.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "SimpleUser")};

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expMinutes = Environment.GetEnvironmentVariable("ExpiryMinutes");
            if (string.IsNullOrEmpty(expMinutes))
                throw new InvalidOperationException("ExpiryMinutes is missing.");

            var token = new JwtSecurityToken(issuer: Environment.GetEnvironmentVariable("ISSUER"),
                audience: Environment.GetEnvironmentVariable("AUDIENCE"), claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(expMinutes)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}