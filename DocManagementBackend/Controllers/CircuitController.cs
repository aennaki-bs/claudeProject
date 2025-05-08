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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            var circuits = await _context.Circuits
                .Include(c => c.Steps.OrderBy(cd => cd.OrderIndex))
                .ToListAsync();

            var circuitDtos = circuits.Select(c => new CircuitDto
            {
                Id = c.Id,
                CircuitKey = c.CircuitKey,
                Title = c.Title,
                Descriptif = c.Descriptif,
                IsActive = c.IsActive,
                HasOrderedFlow = c.HasOrderedFlow,
                AllowBacktrack = c.AllowBacktrack,
                Steps = c.Steps.Select(cd => new StepDto
                {
                    Id = cd.Id,
                    StepKey = cd.StepKey,
                    CircuitId = cd.CircuitId,
                    Title = cd.Title,
                    Descriptif = cd.Descriptif,
                    OrderIndex = cd.OrderIndex,
                    ResponsibleRoleId = cd.ResponsibleRoleId,
                    IsFinalStep = cd.IsFinalStep
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            var circuit = await _context.Circuits
                .Include(c => c.Steps.OrderBy(cd => cd.OrderIndex))
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
                HasOrderedFlow = circuit.HasOrderedFlow,
                AllowBacktrack = circuit.AllowBacktrack,
                Steps = circuit.Steps.Select(cd => new StepDto
                {
                    Id = cd.Id,
                    StepKey = cd.StepKey,
                    CircuitId = cd.CircuitId,
                    Title = cd.Title,
                    Descriptif = cd.Descriptif,
                    OrderIndex = cd.OrderIndex,
                    ResponsibleRoleId = cd.ResponsibleRoleId,
                    IsFinalStep = cd.IsFinalStep
                }).ToList()
            };

            return Ok(circuitDto);
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

            // Get the circuit with its steps
            var circuit = await _context.Circuits
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == circuitId);

            if (circuit == null)
                return NotFound("Circuit not found.");

            // Prepare validation response
            var validation = new CircuitValidationDto
            {
                CircuitId = circuit.Id,
                CircuitTitle = circuit.Title,
                HasSteps = circuit.Steps.Any(),
                TotalSteps = circuit.Steps.Count,
                StepsWithoutStatuses = new List<StepValidationDto>()
            };

            if (validation.HasSteps)
            {
                // For each step, check if it has statuses
                foreach (var step in circuit.Steps)
                {
                    var statusCount = await _context.Status.CountAsync(s => s.StepId == step.Id);

                    if (statusCount == 0)
                    {
                        validation.StepsWithoutStatuses.Add(new StepValidationDto
                        {
                            StepId = step.Id,
                            StepTitle = step.Title,
                            Order = step.OrderIndex
                        });
                    }
                }

                validation.AllStepsHaveStatuses = validation.StepsWithoutStatuses.Count == 0;
                validation.IsValid = validation.HasSteps && validation.AllStepsHaveStatuses;
            }
            else
            {
                validation.AllStepsHaveStatuses = false;
                validation.IsValid = false;
            }

            return Ok(validation);
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to create circuits.");

            Console.WriteLine($"is active value ========> {createCircuitDto.IsActive}");

            var circuit = new Circuit
            {
                Title = createCircuitDto.Title,
                Descriptif = createCircuitDto.Descriptif,
                HasOrderedFlow = createCircuitDto.HasOrderedFlow,
                AllowBacktrack = createCircuitDto.AllowBacktrack,
                IsActive = createCircuitDto.IsActive
            };

            try
            {
                Console.WriteLine($"is active value circuit ========> {circuit.IsActive}");
                var createdCircuit = await _circuitService.CreateCircuitAsync(circuit);
                Console.WriteLine($"is active value circuit ========> {createdCircuit.IsActive}");

                return CreatedAtAction(nameof(GetCircuit), new { id = createdCircuit.Id }, new CircuitDto
                {
                    Id = createdCircuit.Id,
                    CircuitKey = createdCircuit.CircuitKey,
                    Title = createdCircuit.Title,
                    Descriptif = createdCircuit.Descriptif,
                    IsActive = createdCircuit.IsActive,
                    HasOrderedFlow = createdCircuit.HasOrderedFlow,
                    AllowBacktrack = createdCircuit.AllowBacktrack,
                    Steps = new List<StepDto>()
                });
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

            // Find the step to update
            var step = await _context.Steps.Include(s => s.Circuit).FirstOrDefaultAsync(s => s.Id == stepId);
            if (step == null)
                return NotFound("Step not found.");

            // Check if the step belongs to an active circuit
            if (step.Circuit != null && step.Circuit.IsActive)
                return BadRequest("Cannot update a step that belongs to an active circuit.");

            // Update step properties
            if (!string.IsNullOrEmpty(updateStepDto.Title))
                step.Title = updateStepDto.Title;

            if (!string.IsNullOrEmpty(updateStepDto.Descriptif))
                step.Descriptif = updateStepDto.Descriptif;

            // if (updateStepDto.OrderIndex > 0)
            //     step.OrderIndex = updateStepDto.OrderIndex;

            // if (updateStepDto.ResponsibleRoleId.HasValue)
            // {
            //     // Validate role ID if provided
            //     var role = await _context.Roles.FindAsync(updateStepDto.ResponsibleRoleId.Value);
            //     if (role == null)
            //         return BadRequest("Invalid role ID.");

            //     step.ResponsibleRoleId = updateStepDto.ResponsibleRoleId;
            // }

            if (updateStepDto.IsFinalStep.HasValue)
                step.IsFinalStep = updateStepDto.IsFinalStep.Value;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Step updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Steps.AnyAsync(s => s.Id == stepId))
                    return NotFound("Step no longer exists.");
                throw;
            }
        }

        [HttpPost("{circuitId}/steps")]
        public async Task<ActionResult<StepDto>> AddStepToCircuit(int circuitId, [FromBody] CreateStepDto createStepDto)
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
                return Unauthorized("User not allowed to modify circuits.");

            var step = new Step
            {
                CircuitId = circuitId,
                Title = createStepDto.Title,
                Descriptif = createStepDto.Descriptif,
                ResponsibleRoleId = createStepDto.ResponsibleRoleId,
                OrderIndex = 0
            };

            try
            {
                var createdStep = await _circuitService.AddStepToCircuitAsync(step);

                return CreatedAtAction(nameof(GetCircuit), new { id = circuitId }, new StepDto
                {
                    Id = createdStep.Id,
                    StepKey = createdStep.StepKey,
                    CircuitId = createdStep.CircuitId,
                    Title = createdStep.Title,
                    Descriptif = createdStep.Descriptif,
                    OrderIndex = createdStep.OrderIndex,
                    ResponsibleRoleId = createdStep.ResponsibleRoleId,
                    IsFinalStep = createdStep.IsFinalStep
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error adding step: {ex.Message}");
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to modify circuits.");

            var circuit = await _context.Circuits.FindAsync(id);
            if (circuit == null)
                return NotFound("Circuit not found.");

            circuit.Title = updateCircuitDto.Title;
            circuit.Descriptif = updateCircuitDto.Descriptif;
            circuit.HasOrderedFlow = updateCircuitDto.HasOrderedFlow;
            circuit.AllowBacktrack = updateCircuitDto.AllowBacktrack;
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

            if (user.Role!.RoleName != "Admin" && user.Role!.RoleName != "FullUser")
                return Unauthorized("User not allowed to delete steps.");

            // Find the step to delete
            var step = await _context.Steps
                .Include(s => s.Circuit)
                .Include(s => s.Statuses)
                .Include(s => s.StepActions)
                .FirstOrDefaultAsync(s => s.Id == stepId);

            if (step == null)
                return NotFound("Step not found.");

            // Check if the step belongs to an active circuit
            if (step.Circuit != null && step.Circuit.IsActive)
                return BadRequest("Cannot delete a step that belongs to an active circuit.");

            // Check if the step is referenced by any document
            var isReferenced = await _context.Documents.AnyAsync(d => d.CurrentStepId == stepId);
            if (isReferenced)
                return BadRequest("Cannot delete a step that is currently in use by documents.");

            // Check if the step has previous/next references from other steps
            var hasReferences = await _context.Steps.AnyAsync(s => s.NextStepId == stepId || s.PrevStepId == stepId);
            if (hasReferences)
            {
                // Remove references from other steps
                var referencingSteps = await _context.Steps
                    .Where(s => s.NextStepId == stepId || s.PrevStepId == stepId)
                    .ToListAsync();

                foreach (var refStep in referencingSteps)
                {
                    if (refStep.NextStepId == stepId)
                        refStep.NextStepId = null;
                    if (refStep.PrevStepId == stepId)
                        refStep.PrevStepId = null;
                }
            }

            // Delete related entities first
            if (step.Statuses.Any())
            {
                // Check if statuses are used in DocumentStatus
                var statusIds = step.Statuses.Select(s => s.Id).ToList();
                var statusesInUse = await _context.DocumentStatus.AnyAsync(ds => statusIds.Contains(ds.StatusId));

                if (statusesInUse)
                    return BadRequest("Cannot delete step with statuses that are in use by documents.");

                _context.Status.RemoveRange(step.Statuses);
            }

            if (step.StepActions.Any())
            {
                _context.StepActions.RemoveRange(step.StepActions);
            }

            // Delete any ActionStatusEffects related to this step
            var actionEffects = await _context.ActionStatusEffects.Where(ase => ase.StepId == stepId).ToListAsync();
            if (actionEffects.Any())
            {
                _context.ActionStatusEffects.RemoveRange(actionEffects);
            }

            // Delete the step
            _context.Steps.Remove(step);

            // Update OrderIndex values for remaining steps in the circuit
            if (step.CircuitId > 0)
            {
                var remainingSteps = await _context.Steps
                    .Where(s => s.CircuitId == step.CircuitId && s.Id != stepId)
                    .OrderBy(s => s.OrderIndex)
                    .ToListAsync();

                for (int i = 0; i < remainingSteps.Count; i++)
                {
                    remainingSteps[i].OrderIndex = i + 1;
                }
                
                // Update step links to maintain proper navigation
                if (step.Circuit != null && step.Circuit.HasOrderedFlow)
                {
                    await _circuitService.UpdateStepLinksAsync(step.CircuitId);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Step deleted successfully.");
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
                return Unauthorized("User account is deactivated. Please contact un admin!");

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

        [HttpPost("update-all-step-links")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateAllStepLinks()
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
                await _circuitService.UpdateAllCircuitStepLinksAsync();
                return Ok("All circuit step links have been updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating step links: {ex.Message}");
            }
        }
        
        [HttpPost("{circuitId}/update-step-links")]
        public async Task<IActionResult> UpdateCircuitStepLinks(int circuitId)
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

            // Check if circuit exists
            var circuit = await _context.Circuits.FindAsync(circuitId);
            if (circuit == null)
                return NotFound("Circuit not found.");
                
            if (!circuit.HasOrderedFlow)
                return BadRequest("This circuit does not have ordered flow, so step links are not used.");

            try
            {
                await _circuitService.UpdateStepLinksAsync(circuitId);
                return Ok($"Step links for circuit {circuitId} have been updated successfully.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating step links: {ex.Message}");
            }
        }
    }
}