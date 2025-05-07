using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.ModelsDtos;
using DocManagementBackend.Services;
using DocManagementBackend.utils;
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
                .Include(c => c.Steps.OrderBy(cd => cd.OrderIndex))
                .Include(c => c.Statuses)
                .ToListAsync();

            var circuitDtos = circuits.Select(c => c.ToDto()).ToList();
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
                .Include(c => c.Steps.OrderBy(cd => cd.OrderIndex))
                .Include(c => c.Statuses)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (circuit == null)
                return NotFound("Circuit not found.");

            return Ok(circuit.ToDto());
        }

        [HttpGet("{circuitId}/validation")]
        public async Task<ActionResult<CircuitValidationDto>> ValidateCircuitStructure(int circuitId)
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
                return StatusCode(500, $"An error occurred: {ex.Message}");
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
                Description = createCircuitDto.Description,
                IsActive = createCircuitDto.IsActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                var createdCircuit = await _circuitService.CreateCircuitAsync(circuit);
                return CreatedAtAction(nameof(GetCircuit), new { id = createdCircuit.Id }, createdCircuit.ToDto());
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating circuit: {ex.Message}");
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to modify steps.");

            try
            {
                var updatedStep = await _circuitService.UpdateStepAsync(stepId, updateStepDto);
                return Ok(updatedStep.ToDto());
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Step not found.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating step: {ex.Message}");
            }
        }

        [HttpPost("{circuitId}/steps")]
        public async Task<ActionResult<CircuitStepDto>> AddStepToCircuit(int circuitId, [FromBody] CreateStepDto createStepDto)
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
                return Unauthorized("User not allowed to add steps.");

            try
            {
                createStepDto.CircuitId = circuitId;
                var createdStep = await _circuitService.CreateStepAsync(createStepDto);
                return CreatedAtAction(nameof(GetCircuit), new { id = circuitId }, createdStep);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating step: {ex.Message}");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CircuitDto>> UpdateCircuit(int id, [FromBody] UpdateCircuitDto updateCircuitDto)
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
                return Unauthorized("User not allowed to update circuits.");

            var circuit = await _context.Circuits.FindAsync(id);
            if (circuit == null)
                return NotFound("Circuit not found.");

            circuit.Title = updateCircuitDto.Title ?? circuit.Title;
            circuit.Description = updateCircuitDto.Description ?? circuit.Description;
            circuit.IsActive = updateCircuitDto.IsActive ?? circuit.IsActive;
            circuit.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return Ok(circuit.ToDto());
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to delete steps.");

            try
            {
                await _circuitService.DeleteStepAsync(stepId);
                return Ok("Step deleted successfully.");
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Step not found.");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error deleting step: {ex.Message}");
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

            var circuit = await _context.Circuits.FindAsync(id);
            if (circuit == null)
                return NotFound("Circuit not found.");

            // Check if circuit is in use by documents
            var inUse = await _context.Documents.AnyAsync(d => d.CircuitId == id);
            if (inUse)
                return BadRequest("Cannot delete circuit that is in use by documents.");

            _context.Circuits.Remove(circuit);
            await _context.SaveChangesAsync();

            return Ok("Circuit deleted successfully.");
        }

        private bool CircuitExists(int id)
        {
            return _context.Circuits.Any(e => e.Id == id);
        }
    }
}