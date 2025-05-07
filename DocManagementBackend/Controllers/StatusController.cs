using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using System.Security.Claims;

namespace DocManagementBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StatusController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("step/{stepId}")]
        public async Task<ActionResult<IEnumerable<StatusDto>>> GetStatusesForStep(int stepId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            var statuses = await _context.Status
                .Where(s => s.StepId == stepId)
                .OrderBy(s => s.Id)
                .Select(s => new StatusDto
                {
                    StatusId = s.Id,
                    StatusKey = s.StatusKey,
                    Title = s.Title,
                    IsRequired = s.IsRequired,
                    IsComplete = s.IsComplete,
                    StepId = s.StepId
                })
                .ToListAsync();

            return Ok(statuses);
        }

        [HttpPost("step/{stepId}")]
        public async Task<ActionResult<StatusDto>> AddStatusToStep(int stepId, [FromBody] CreateStatusDto createStatusDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to add statuses.");

            var step = await _context.Steps.FindAsync(stepId);
            if (step == null)
                return NotFound("Step not found.");

            var status = new Status
            {
                StepId = stepId,
                Title = createStatusDto.Title,
                IsRequired = createStatusDto.IsRequired,
                IsComplete = false,
                StatusKey = $"ST-{Guid.NewGuid().ToString().Substring(0, 3)}"
            };

            _context.Status.Add(status);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetStatusesForStep), new { stepId = stepId }, new StatusDto
            {
                StatusId = status.Id,
                StatusKey = status.StatusKey,
                Title = status.Title,
                IsRequired = status.IsRequired,
                IsComplete = status.IsComplete,
                StepId = status.StepId
            });
        }

        [HttpPut("{statusId}")]
        public async Task<IActionResult> UpdateStatus(int statusId, [FromBody] CreateStatusDto updateStatusDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to update statuses.");

            var status = await _context.Status.FindAsync(statusId);
            if (status == null)
                return NotFound("Status not found.");

            if (!string.IsNullOrEmpty(updateStatusDto.Title))
                status.Title = updateStatusDto.Title;
            status.IsRequired = updateStatusDto.IsRequired;
            // status.IsComplete = updateStatusDto.IsComplete;

            await _context.SaveChangesAsync();
            return Ok("Status updated successfully.");
        }

        [HttpDelete("{statusId}")]
        public async Task<IActionResult> DeleteStatus(int statusId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to delete statuses.");

            var status = await _context.Status.FindAsync(statusId);
            if (status == null)
                return NotFound("Status not found.");

            // Check if documents are already using this status
            var inUse = await _context.DocumentStatus.AnyAsync(ds => ds.StatusId == statusId);
            if (inUse)
                return BadRequest("Cannot delete status that is in use by documents.");

            _context.Status.Remove(status);
            await _context.SaveChangesAsync();

            return Ok("Status deleted successfully.");
        }
    }
}