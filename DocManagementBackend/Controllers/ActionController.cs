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
    public class ActionController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ActionController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ActionDto>>> GetActions()
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

            var actions = await _context.Actions
                .OrderBy(a => a.Title)
                .Select(a => new ActionDto
                {
                    ActionId = a.Id,
                    ActionKey = a.ActionKey,
                    Title = a.Title,
                    Description = a.Description ?? string.Empty
                })
                .ToListAsync();

            return Ok(actions);
        }

        [HttpPost]
        public async Task<ActionResult<ActionDto>> CreateAction([FromBody] CreateActionDto createActionDto)
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
                return Unauthorized("User not allowed to create actions.");

            var action = new Models.Action
            {
                Title = createActionDto.Title,
                Description = createActionDto.Description,
                ActionKey = $"ACT-{Guid.NewGuid().ToString().Substring(0, 8)}"
            };

            _context.Actions.Add(action);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAction), new { id = action.Id }, new ActionDto
            {
                ActionId = action.Id,
                ActionKey = action.ActionKey,
                Title = action.Title,
                Description = action.Description
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAction(int id, [FromBody] CreateActionDto updateActionDto)
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
                return Unauthorized("User not allowed to update actions.");

            var action = await _context.Actions.FindAsync(id);
            if (action == null)
                return NotFound("Action not found.");

            // Update the properties
            action.Title = updateActionDto.Title;
            action.Description = updateActionDto.Description;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Action updated successfully.");
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Actions.AnyAsync(a => a.Id == id))
                    return NotFound("Action no longer exists.");
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAction(int id)
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
                return Unauthorized("User not allowed to delete actions.");

            var action = await _context.Actions
                .Include(a => a.StepActions)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (action == null)
                return NotFound("Action not found.");

            // Check if action is being used in any StepAction relationships
            if (action.StepActions.Any())
                return BadRequest("Cannot delete an action that is assigned to steps. Remove the action from all steps first.");

            // Check if action is referenced in any ActionStatusEffects
            var isReferenced = await _context.ActionStatusEffects.AnyAsync(ase => ase.ActionId == id);
            if (isReferenced)
            {
                // Delete the related ActionStatusEffects
                var relatedEffects = await _context.ActionStatusEffects.Where(ase => ase.ActionId == id).ToListAsync();
                _context.ActionStatusEffects.RemoveRange(relatedEffects);
            }

            // Check if action is referenced in DocumentCircuitHistory
            var isInHistory = await _context.DocumentCircuitHistory.AnyAsync(dch => dch.ActionId == id);
            if (isInHistory)
                return BadRequest("Cannot delete an action that has been used in document history.");

            // Remove the action
            _context.Actions.Remove(action);
            await _context.SaveChangesAsync();

            return Ok("Action deleted successfully.");
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ActionDto>> GetAction(int id)
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

            var action = await _context.Actions.FindAsync(id);
            if (action == null)
                return NotFound("Action not found.");

            return Ok(new ActionDto
            {
                ActionId = action.Id,
                ActionKey = action.ActionKey,
                Title = action.Title,
                Description = action.Description ?? string.Empty // Provide default if null
            });
        }

        [HttpPost("assign-to-step")]
        public async Task<IActionResult> AssignActionToStep([FromBody] AssignActionToStepDto assignDto)
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
                return Unauthorized("User not allowed to assign actions to steps.");

            var step = await _context.Steps.FindAsync(assignDto.StepId);
            if (step == null)
                return NotFound("Step not found.");

            var action = await _context.Actions.FindAsync(assignDto.ActionId);
            if (action == null)
                return NotFound("Action not found.");

            // Check if already assigned
            var existing = await _context.StepActions
                .FirstOrDefaultAsync(sa => sa.StepId == assignDto.StepId && sa.ActionId == assignDto.ActionId);

            if (existing != null)
                return BadRequest("Action already assigned to this step.");

            var stepAction = new StepAction
            {
                StepId = assignDto.StepId,
                ActionId = assignDto.ActionId
            };

            _context.StepActions.Add(stepAction);
            await _context.SaveChangesAsync();

            // Now add status effects for this action if provided
            if (assignDto.StatusEffects != null && assignDto.StatusEffects.Any())
            {
                foreach (var effect in assignDto.StatusEffects)
                {
                    var status = await _context.Status.FindAsync(effect.StatusId);
                    if (status != null && status.StepId == assignDto.StepId)
                    {
                        var actionStatusEffect = new ActionStatusEffect
                        {
                            ActionId = assignDto.ActionId,
                            StepId = assignDto.StepId,
                            StatusId = effect.StatusId,
                            SetsComplete = effect.SetsComplete
                        };
                        _context.ActionStatusEffects.Add(actionStatusEffect);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return Ok("Action assigned to step successfully.");
        }

        [HttpGet("by-step/{stepId}")]
        public async Task<ActionResult<IEnumerable<ActionDto>>> GetActionsByStep(int stepId)
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

            // First verify if the step exists
            var stepExists = await _context.Steps.AnyAsync(s => s.Id == stepId);
            if (!stepExists)
                return NotFound("Step not found.");

            // Get actions assigned to this step
            var actions = await _context.StepActions
                .Where(sa => sa.StepId == stepId)
                .Include(sa => sa.Action)
                .Select(sa => new ActionDto
                {
                    ActionId = sa.Action.Id,
                    ActionKey = sa.Action.ActionKey,
                    Title = sa.Action.Title,
                    Description = sa.Action.Description ?? string.Empty
                })
                .ToListAsync();

            return Ok(actions);
        }
    }
}