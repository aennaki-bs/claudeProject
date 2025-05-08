using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.Services;
using System.Security.Claims;

namespace DocManagementBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WorkflowController : ControllerBase
    {
        private readonly DocumentWorkflowService _workflowService;
        private readonly ApplicationDbContext _context;

        public WorkflowController(DocumentWorkflowService workflowService, ApplicationDbContext context)
        {
            _workflowService = workflowService;
            _context = context;
        }

        [HttpPost("assign-circuit")]
        public async Task<IActionResult> AssignDocumentToCircuit([FromBody] AssignCircuitDto assignCircuitDto)
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
                return Unauthorized("User not allowed to assign documents to circuits.");

            try
            {
                var success = await _workflowService.AssignDocumentToCircuitAsync(
                    assignCircuitDto.DocumentId, assignCircuitDto.CircuitId, userId);

                if (success)
                    return Ok("Document assigned to circuit successfully.");
                else
                    return BadRequest("Failed to assign document to circuit.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("perform-action")]
        public async Task<IActionResult> PerformAction([FromBody] PerformActionDto actionDto)
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
                return Unauthorized("User not allowed to do this action.");

            try
            {
                var success = await _workflowService.ProcessActionAsync(
                    actionDto.DocumentId, actionDto.ActionId, userId, actionDto.Comments, actionDto.IsApproved);

                if (success)
                    return Ok("Action performed successfully.");
                else
                    return BadRequest("Failed to perform action.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("move-to-status")]
        public async Task<IActionResult> MoveToStatus([FromBody] MoveToStatusDto moveStatusDto)
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
                return Unauthorized("User not allowed to perform this action.");

            try
            {
                var success = await _workflowService.MoveToNextStatusAsync(
                    moveStatusDto.DocumentId,
                    moveStatusDto.TargetStatusId,
                    userId,
                    moveStatusDto.Comments);

                if (success)
                    return Ok("Document moved to new status successfully.");
                else
                    return BadRequest("Failed to move document to new status.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("return-to-status")]
        public async Task<IActionResult> ReturnToStatus([FromBody] MoveToStatusDto returnDto)
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
                return Unauthorized("User not allowed to do action.");

            try
            {
                var success = await _workflowService.ReturnToPreviousStatusAsync(
                    returnDto.DocumentId,
                    returnDto.TargetStatusId,
                    userId,
                    returnDto.Comments);

                if (success)
                    return Ok("Document returned to status successfully.");
                else
                    return BadRequest("Failed to return document to status.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("complete-status")]
        public async Task<IActionResult> CompleteDocumentStatus([FromBody] CompleteStatusDto completeStatusDto)
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

            try
            {
                var success = await _workflowService.CompleteDocumentStatusAsync(
                    completeStatusDto.DocumentId,
                    completeStatusDto.StatusId,
                    userId,
                    completeStatusDto.IsComplete,
                    completeStatusDto.Comments);

                if (success)
                    return Ok("Document status updated successfully.");
                else
                    return BadRequest("Failed to update document status.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("document/{documentId}/available-transitions")]
        public async Task<ActionResult<IEnumerable<StatusDto>>> GetAvailableTransitions(int documentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            try
            {
                var transitions = await _workflowService.GetAvailableTransitionsAsync(documentId);
                return Ok(transitions);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("document/{documentId}/document-statuses")]
        public async Task<ActionResult<IEnumerable<DocumentStatusDto>>> GetDocumentStatuses(int documentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact un admin!");

            // Get the document and check if it has a current status
            var document = await _context.Documents
                .Include(d => d.Circuit)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                return NotFound("Document not found.");

            if (document.CircuitId == null)
                return NotFound("Document is not assigned to any circuit.");

            // Get statuses for the document's circuit with completion info for this document
            var statuses = await _context.Status
                .Where(s => s.CircuitId == document.CircuitId)
                .OrderBy(s => s.Id)
                .Select(s => new
                {
                    Status = s,
                    DocumentStatus = _context.DocumentStatus
                        .FirstOrDefault(ds => ds.DocumentId == documentId && ds.StatusId == s.Id)
                })
                .ToListAsync();

            var statusDtos = statuses.Select(item => new DocumentStatusDto
            {
                StatusId = item.Status.Id,
                Title = item.Status.Title,
                IsRequired = item.Status.IsRequired,
                IsComplete = item.DocumentStatus?.IsComplete ?? false,
                CompletedBy = item.DocumentStatus != null && item.DocumentStatus.CompletedByUserId.HasValue
                    ? _context.Users
                        .Where(u => u.Id == item.DocumentStatus.CompletedByUserId.Value)
                        .Select(u => u.Username)
                        .FirstOrDefault()
                    : null,
                CompletedAt = item.DocumentStatus?.CompletedAt
            }).ToList();

            return Ok(statusDtos);
        }

        [HttpGet("document/{documentId}/history")]
        public async Task<ActionResult<IEnumerable<DocumentHistoryDto>>> GetDocumentHistory(int documentId)
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

            try
            {
                var history = await _context.DocumentCircuitHistory
                    .Where(h => h.DocumentId == documentId)
                    .Include(h => h.Step)
                    .Include(h => h.ProcessedBy)
                    .Include(h => h.Action)
                    .Include(h => h.Status)
                    .OrderByDescending(h => h.ProcessedAt)
                    .ToListAsync();

                var historyDtos = history.Select(h => new DocumentHistoryDto
                {
                    Id = h.Id,
                    StepTitle = h.Step?.Title ?? "N/A",
                    ActionTitle = h.Action?.Title ?? "N/A",
                    StatusTitle = h.Status?.Title ?? "N/A",
                    ProcessedBy = h.ProcessedBy?.Username ?? "System",
                    ProcessedAt = h.ProcessedAt,
                    Comments = h.Comments,
                    IsApproved = h.IsApproved
                }).ToList();

                return Ok(historyDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("document/{documentId}/workflow-status")]
        public async Task<ActionResult<DocumentWorkflowStatusDto>> GetDocumentWorkflowStatus(int documentId)
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

            try
            {
                var workflowStatus = await _workflowService.GetDocumentWorkflowStatusAsync(documentId);
                return Ok(workflowStatus);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet("pending-documents")]
        public async Task<ActionResult<IEnumerable<PendingDocumentDto>>> GetPendingDocuments()
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

            try
            {
                // Get documents that are:
                // 1. Assigned to a circuit
                // 2. Not completed
                // 3. Current status is assigned to user's role (if role assignment is enabled)
                var pendingQuery = _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStatus)
                    .Include(d => d.CreatedBy)
                    .Where(d =>
                        d.CircuitId.HasValue &&
                        !d.IsCircuitCompleted &&
                        d.Status == 1 && // In Progress
                        d.CurrentStatusId.HasValue);

                var pendingDocuments = await pendingQuery.ToListAsync();

                var pendingDtos = pendingDocuments.Select(d => new PendingDocumentDto
                {
                    DocumentId = d.Id,
                    DocumentKey = d.DocumentKey,
                    Title = d.Title,
                    CreatedBy = d.CreatedBy?.Username ?? "Unknown",
                    CreatedAt = d.CreatedAt,
                    CircuitId = d.CircuitId!.Value,
                    CircuitTitle = d.Circuit?.Title ?? "Unknown",
                    CurrentStatusId = d.CurrentStatusId!.Value,
                    CurrentStatusTitle = d.CurrentStatus?.Title ?? "Unknown",
                    DaysInCurrentStatus = (int)(DateTime.UtcNow - d.UpdatedAt).TotalDays
                }).ToList();

                return Ok(pendingDtos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Draft",
                1 => "In Progress",
                2 => "Completed",
                3 => "Rejected",
                _ => "Unknown"
            };
        }
    }

    // Data Transfer Objects
    public class MoveToStatusDto
    {
        public int DocumentId { get; set; }
        public int TargetStatusId { get; set; }
        public string Comments { get; set; } = string.Empty;
    }
}