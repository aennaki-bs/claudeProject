using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using DocManagementBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using DocManagementBackend.Data;
// using DocManagementBackend.Models;

namespace DocManagementBackend.Utils
{
    public static class AuthHelper
    {
        // private readonly ApplicationDbContext _context;
        // private readonly IConfiguration _config;
        // public AuthController(ApplicationDbContext context, IConfiguration config)
        // {
        //     _context = context; _config = config;
        // }
        // Password validation logic
        public static bool IsValidPassword(string password)
        {
            return password.Length >= 8 && password.Any(char.IsLower) &&
                   password.Any(char.IsUpper) && password.Any(char.IsDigit) &&
                   password.Any(ch => !char.IsLetterOrDigit(ch));
        }
        public static void SendEmail(string to, string subject, string body)
        {
            try
            {
                string? emailAddress = Environment.GetEnvironmentVariable("EMAIL_ADDRESS");
                string? emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
                if (string.IsNullOrEmpty(emailAddress) || string.IsNullOrEmpty(emailPassword))
                    throw new InvalidOperationException("Email address or password is not set in environment variables.");
                using (var smtp = new SmtpClient("smtp.gmail.com", 587))
                {
                    smtp.Credentials = new System.Net.NetworkCredential(emailAddress, emailPassword);
                    smtp.EnableSsl = true;
                    var message = new MailMessage();
                    message.To.Add(to); message.Subject = subject;
                    message.Body = body; message.IsBodyHtml = true;
                    message.From = new MailAddress(emailAddress);
                    smtp.Send(message);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Email send failed: {ex.Message}"); }
        }

        public static string GenerateAccessToken(User user)
        {
            // var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET"); ;
            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT configuration is missing.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var claims = new[] {new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Username", user.Username),
                new Claim("IsActive", user.IsActive.ToString()),
                new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "SimpleUser")};
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expMinutes = 180;
            // if (string.IsNullOrEmpty(expMinutes))
            //     throw new InvalidOperationException("ExpiryMinutes is missing.");
            var token = new JwtSecurityToken(issuer: Environment.GetEnvironmentVariable("ISSUER"),
                audience: Environment.GetEnvironmentVariable("AUDIENCE"), claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }
        public static string CreateEmailBody(string verificationLink, string verificationCode)
        {
            return $@"
                <html><head><style>
                        body {{font-family: Arial, sans-serif; line-height: 1.6; color: #fff; width: 100vw; height: 80vh; align-items: center;
                            background-color: #333333; margin: 0; padding: 0; display: flex; justify-content: center; color:white;}}
                        h2 {{font-size: 24px; color: #c3c3c7;}}
                        p {{color: #c3c3c7; margin: 0 0 20px;}}
                        .button {{display: inline-block; padding: 10px 20px; margin: 20px 0; font-size: 16px;
                            color: #fff; background-color: #007bff; text-decoration: none; border-radius: 5px;}}
                        .button:hover {{background-color: rgb(6, 75, 214);}}
                        .footer {{margin-top: 20px; font-size: 12px; color: #f8f6f6;}}
                        span {{display: inline-block; font-size: 1rem; font-weight: bold; color: #2d89ff;
                            background: #f0f6ff; padding: 10px 10px; border-radius: 8px; letter-spacing: 3px;
                            font-family: monospace; border: 2px solid #ffffff; margin: 5px;}}
                        .card {{padding: 20px; background-color: #555555; margin: auto; border-radius: 12px;
                            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); color: #c3c3c7;}}
                    </style></head>
                <body><div class='card'>
                        <h2>Email Verification</h2>
                        <p>Thank you for registering with us! To complete your registration, please
                            verify your email address, your verification code is
                            <br /><span>{verificationCode}</span> <br />
                            by clicking the button below you will be redirected to the verification
                            page:</p>
                        <a href='{verificationLink}' class='button'>Verify Email</a>
                        <p>If the button doesn't work, you can also copy and paste the following
                            link into your browser:</p>
                        <p><a href='{verificationLink}'>{verificationLink}</a></p>
                        <div class='footer'><p>If you did not request this verification, please ignore this email.</p>
                        </div></div></body></html>";
        }
        public static string createPassEmailBody(string verificationLink)
        {
            return $@"
                <html><head><style>
                        body {{font-family: Arial, sans-serif; line-height: 1.6;
                            color: #fff; width: 100vw; height: 80vh; background-color: #333333; margin: 0; padding: 0;
                            display: flex; justify-content: center; align-items: center;}}
                        h2 {{font-size: 24px; color: #c3c3c7;}}
                        p {{margin: 0 0 20px;}}
                        .button {{display: inline-block; padding: 10px 20px; margin: 20px 0; font-size: 16px; color: #fff;
                            background-color: #007bff; text-decoration: none; border-radius: 5px;}}
                        .button:hover {{background-color: rgb(6, 75, 214);}}
                        .footer {{margin-top: 20px; font-size: 12px; color: #f8f6f6;}}
                        span {{display: inline-block; font-size: 1.5rem; font-weight: bold; color: #2d89ff; background: #f0f6ff;
                            padding: 10px 15px; border-radius: 8px; letter-spacing: 3px; font-family: monospace; border: 2px solid #ffffff; margin: 5px;}}
                        .card {{padding: 20px; width: 50%; background-color: #555555; margin: auto;
                            border-radius: 12px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);}}
                    </style></head>
                <body><div class='card'><h2>Reset Password</h2>
                        <p>To reset your password click on the button bellow:</p>
                        <a href='{verificationLink}' class='button'>Reset Password</a>
                        <p>If the button doesn't work, you can also copy and paste the following
                            link into your browser:</p>
                        <p><a href='{verificationLink}'>{verificationLink}</a></p>
                        <div class='footer'><p>If you did not request this verification, please ignore this email.</p>
                        </div></div></body></html>";
        }
    }
}