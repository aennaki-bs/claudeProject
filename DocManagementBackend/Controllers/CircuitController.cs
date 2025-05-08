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
    public class CircuitController : ControllerBase
    {
        private readonly CircuitManagementService _circuitService;
        private readonly ApplicationDbContext _context;

        public CircuitController(CircuitManagementService circuitService, ApplicationDbContext context)
        {
            _circuitService = circuitService;
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CircuitDto>>> GetCircuits()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            var circuits = await _context.Circuits
                .Include(c => c.Statuses)
                .Include(c => c.Steps)
                .ToListAsync();

            var circuitDtos = circuits.Select(c => new CircuitDto
            {
                Id = c.Id,
                CircuitKey = c.CircuitKey,
                Title = c.Title,
                Descriptif = c.Descriptif,
                IsActive = c.IsActive,
                Statuses = c.Statuses.Select(s => new StatusDto
                {
                    StatusId = s.Id,
                    StatusKey = s.StatusKey,
                    Title = s.Title,
                    Description = s.Description,
                    IsRequired = s.IsRequired,
                    IsInitial = s.IsInitial,
                    IsFinal = s.IsFinal,
                    IsFlexible = s.IsFlexible,
                    CircuitId = s.CircuitId
                }).ToList(),
                Steps = c.Steps.Select(s => new StepDto
                {
                    Id = s.Id,
                    StepKey = s.StepKey,
                    CircuitId = s.CircuitId,
                    Title = s.Title,
                    Descriptif = s.Descriptif,
                    CurrentStatusId = s.CurrentStatusId,
                    CurrentStatusTitle = c.Statuses.FirstOrDefault(st => st.Id == s.CurrentStatusId)?.Title ?? "",
                    NextStatusId = s.NextStatusId,
                    NextStatusTitle = c.Statuses.FirstOrDefault(st => st.Id == s.NextStatusId)?.Title ?? ""
                }).ToList()
            }).ToList();

            return Ok(circuitDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CircuitDto>> GetCircuit(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            var circuit = await _context.Circuits
                .Include(c => c.Statuses)
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (circuit == null)
                return NotFound("Circuit not found.");

            var circuitDto = new CircuitDto
            {
                Id = circuit.Id,
                CircuitKey = circuit.CircuitKey,
                Title = circuit.Title,
                Descriptif = circuit.Descriptif,
                IsActive = circuit.IsActive,
                Statuses = circuit.Statuses.Select(s => new StatusDto
                {
                    StatusId = s.Id,
                    StatusKey = s.StatusKey,
                    Title = s.Title,
                    Description = s.Description,
                    IsRequired = s.IsRequired,
                    IsInitial = s.IsInitial,
                    IsFinal = s.IsFinal,
                    IsFlexible = s.IsFlexible,
                    CircuitId = s.CircuitId
                }).ToList(),
                Steps = circuit.Steps.Select(s => new StepDto
                {
                    Id = s.Id,
                    StepKey = s.StepKey,
                    CircuitId = s.CircuitId,
                    Title = s.Title,
                    Descriptif = s.Descriptif,
                    CurrentStatusId = s.CurrentStatusId,
                    CurrentStatusTitle = circuit.Statuses.FirstOrDefault(st => st.Id == s.CurrentStatusId)?.Title ?? "",
                    NextStatusId = s.NextStatusId,
                    NextStatusTitle = circuit.Statuses.FirstOrDefault(st => st.Id == s.NextStatusId)?.Title ?? ""
                }).ToList()
            };

            return Ok(circuitDto);
        }

        [HttpGet("{circuitId}/validate")]
        public async Task<ActionResult<CircuitValidationDto>> ValidateCircuit(int circuitId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            try
            {
                var validation = await _circuitService.ValidateCircuitAsync(circuitId);
                return Ok(validation);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error validating circuit: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<ActionResult<CircuitDto>> CreateCircuit([FromBody] CreateCircuitDto createCircuitDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to create circuits.");

            var circuit = new Circuit
            {
                Title = createCircuitDto.Title,
                Descriptif = createCircuitDto.Descriptif,
                IsActive = createCircuitDto.IsActive
            };

            try
            {
                var createdCircuit = await _circuitService.CreateCircuitAsync(circuit);

                return CreatedAtAction(nameof(GetCircuit), new { id = createdCircuit.Id }, new CircuitDto
                {
                    Id = createdCircuit.Id,
                    CircuitKey = createdCircuit.CircuitKey,
                    Title = createdCircuit.Title,
                    Descriptif = createdCircuit.Descriptif,
                    IsActive = createdCircuit.IsActive,
                    Statuses = new List<StatusDto>(),
                    Steps = new List<StepDto>()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating circuit: {ex.Message}");
            }
        }

        [HttpPost("{circuitId}/steps")]
        public async Task<ActionResult<StepDto>> AddStep(int circuitId, [FromBody] CreateStepDto createStepDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to modify circuits.");

            var step = new Step
            {
                CircuitId = circuitId,
                Title = createStepDto.Title,
                Descriptif = createStepDto.Descriptif,
                CurrentStatusId = createStepDto.CurrentStatusId,
                NextStatusId = createStepDto.NextStatusId
            };

            try
            {
                var createdStep = await _circuitService.AddStepToCircuitAsync(step);

                // Get status titles for response
                var currentStatus = await _context.Status.FindAsync(step.CurrentStatusId);
                var nextStatus = await _context.Status.FindAsync(step.NextStatusId);

                return CreatedAtAction(nameof(GetCircuit), new { id = circuitId }, new StepDto
                {
                    Id = createdStep.Id,
                    StepKey = createdStep.StepKey,
                    CircuitId = createdStep.CircuitId,
                    Title = createdStep.Title,
                    Descriptif = createdStep.Descriptif,
                    CurrentStatusId = createdStep.CurrentStatusId,
                    CurrentStatusTitle = currentStatus?.Title ?? "",
                    NextStatusId = createdStep.NextStatusId,
                    NextStatusTitle = nextStatus?.Title ?? ""
                });
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
                return BadRequest($"Error adding step: {ex.Message}");
            }
        }

        [HttpPut("steps/{stepId}")]
        public async Task<IActionResult> UpdateStep(int stepId, [FromBody] UpdateStepDto updateStepDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to modify steps.");

            try
            {
                var success = await _circuitService.UpdateStepAsync(stepId, updateStepDto);
                return Ok("Step updated successfully.");
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
                return StatusCode(500, $"Error updating step: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCircuit(int id, [FromBody] CreateCircuitDto updateCircuitDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to modify circuits.");

            var circuit = await _context.Circuits.FindAsync(id);
            if (circuit == null)
                return NotFound("Circuit not found.");

            circuit.Title = updateCircuitDto.Title;
            circuit.Descriptif = updateCircuitDto.Descriptif;
            circuit.IsActive = updateCircuitDto.IsActive;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Circuit updated successfully.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating circuit: {ex.Message}");
            }
        }

        [HttpDelete("steps/{stepId}")]
        public async Task<IActionResult> DeleteStep(int stepId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to delete steps.");

            try
            {
                var success = await _circuitService.DeleteStepAsync(stepId);
                return Ok("Step deleted successfully.");
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting step: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCircuit(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized("User ID claim is missing.");

            int userId = int.Parse(userIdClaim);
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return BadRequest("User not found.");

            if (!user.IsActive)
                return Unauthorized("User account is deactivated. Please contact an admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to delete circuits.");

            var circuit = await _context.Circuits
                .Include(c => c.Statuses)
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (circuit == null)
                return NotFound("Circuit not found.");

            // Check if circuit is in use by documents
            var inUse = await _context.Documents.AnyAsync(d => d.CircuitId == id);
            if (inUse)
                return BadRequest("Cannot delete circuit that is in use by documents.");

            // Delete all steps first
            _context.Steps.RemoveRange(circuit.Steps);

            // Delete all statuses next
            _context.Status.RemoveRange(circuit.Statuses);

            // Finally delete the circuit
            _context.Circuits.Remove(circuit);

            await _context.SaveChangesAsync();

            return Ok("Circuit deleted successfully.");
        }
    }
}