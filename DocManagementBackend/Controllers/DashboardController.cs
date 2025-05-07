using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace DocManagementBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                // Get total documents count
                var totalDocuments = await _context.Documents.CountAsync();

                // Get active circuits count (circuits with status = active)
                var activeCircuits = await _context.Circuits
                    .Where(c => c.IsActive)
                    .CountAsync();

                // Get pending approvals count (documents with status = pending)
                var pendingApprovals = await _context.Documents
                    .Where(d => d.Status == 2) // Assuming 2 is pending status
                    .CountAsync();

                // Get team members count (active users)
                var teamMembers = await _context.Users
                    .Where(u => u.IsActive)
                    .CountAsync();

                // Calculate completion rate (approved documents / total documents)
                var approvedDocuments = await _context.Documents
                    .Where(d => d.Status == 1) // Assuming 1 is approved status
                    .CountAsync();
                var completionRate = totalDocuments > 0 
                    ? (approvedDocuments * 100.0 / totalDocuments)
                    : 0;

                // Get document activity (documents created per month)
                var now = DateTime.UtcNow;
                var startDate = now.AddMonths(-11);
                
                // Fetch all documents within the date range
                var documents = await _context.Documents
                    .Where(d => d.CreatedAt >= startDate)
                    .Select(d => new { d.CreatedAt })
                    .ToListAsync();

                // Group documents in memory
                var documentActivity = documents
                    .GroupBy(d => new { Year = d.CreatedAt.Year, Month = d.CreatedAt.Month })
                    .Select(g => new
                    {
                        Month = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Month)
                    .ToList();

                // Get weekly stats (documents created in the last 12 weeks)
                var weekStart = now.AddDays(-83); // 12 weeks * 7 days - 1 day
                var recentDocuments = await _context.Documents
                    .Where(d => d.CreatedAt >= weekStart)
                    .Select(d => new { d.CreatedAt })
                    .ToListAsync();

                // Calculate weekly stats in memory
                var weeklyStats = recentDocuments
                    .GroupBy(d => ((int)(now - d.CreatedAt).TotalDays / 7) + 1)
                    .Select(g => new
                    {
                        Name = g.Key.ToString(),
                        Value = g.Count()
                    })
                    .OrderBy(x => x.Name)
                    .ToList();

                // Calculate activity score
                var activeUsers = await _context.Users
                    .Where(u => u.IsActive && u.IsOnline)
                    .CountAsync();

                // Activity score is based on:
                // - Ratio of active users (30%)
                // - Document completion rate (40%)
                // - Circuit activity (30%)
                var totalUsers = await _context.Users.CountAsync();
                var activeUserRatio = totalUsers > 0 ? (activeUsers * 100.0 / totalUsers) : 0;
                
                // Calculate circuit activity based on active circuits ratio
                var totalCircuits = await _context.Circuits.CountAsync();
                var circuitActivityRatio = totalCircuits > 0 
                    ? (activeCircuits * 100.0 / totalCircuits) 
                    : 0;

                var activityScore = (
                    (activeUserRatio * 0.3) +
                    (completionRate * 0.4) +
                    (circuitActivityRatio * 0.3)
                ) / 10; // Scale to 0-10

                return Ok(new
                {
                    TotalDocuments = totalDocuments,
                    ActiveCircuits = activeCircuits,
                    PendingApprovals = pendingApprovals,
                    TeamMembers = teamMembers,
                    DocumentActivity = documentActivity,
                    WeeklyStats = weeklyStats,
                    CompletionRate = Math.Round(completionRate, 1),
                    ActivityScore = new
                    {
                        ActiveUsers = activeUsers,
                        Score = Math.Round(activityScore, 1)
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
} 