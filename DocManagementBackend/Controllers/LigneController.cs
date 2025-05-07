using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.Mappings;
using System.Security.Claims;

namespace DocManagementBackend.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LignesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public LignesController(ApplicationDbContext context) { _context = context; }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LigneDto>>> GetLignes()
        {
            var lignes = await _context.Lignes
                .Include(l => l.Document!).ThenInclude(d => d.DocumentType)
                .Include(l => l.Document!).ThenInclude(d => d.CreatedBy).ThenInclude(u => u.Role)
                .Select(LigneMappings.ToLigneDto).ToListAsync();
            return Ok(lignes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<LigneDto>> GetLigne(int id)
        {
            var ligneDto = await _context.Lignes
                .Include(l => l.Document!).ThenInclude(d => d.DocumentType)
                .Include(l => l.Document!).ThenInclude(d => d.CreatedBy).ThenInclude(u => u.Role)
                .Where(l => l.Id == id).Select(LigneMappings.ToLigneDto).FirstOrDefaultAsync();
            if (ligneDto == null)
                return NotFound("Ligne not found.");
            return Ok(ligneDto);
        }

        [HttpGet("by-document/{documentId}")]
        public async Task<ActionResult<IEnumerable<LigneDto>>> GetLignesByDocumentId(int documentId)
        {
            var lignes = await _context.Lignes
                .Where(l => l.DocumentId == documentId)
                .Include(l => l.Document!).ThenInclude(d => d.DocumentType)
                .Include(l => l.Document!).ThenInclude(d => d.CreatedBy).ThenInclude(u => u.Role)
                .Select(LigneMappings.ToLigneDto).ToListAsync();
            return Ok(lignes);
        }

        [HttpPost]
        public async Task<ActionResult<LigneDto>> CreateLigne([FromBody] Ligne ligne)
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
            if (ThisUser.Role!.RoleName != "Admin" && ThisUser.Role!.RoleName != "FullUser")
                return Unauthorized("User Not Allowed To do this action.");
            var document = await _context.Documents.FindAsync(ligne.DocumentId);
            if (document == null)
                return BadRequest("Invalid DocumentId. Document not found.");
            ligne.CreatedAt = DateTime.UtcNow;
            ligne.UpdatedAt = DateTime.UtcNow;
            ligne.LigneKey = $"{document.DocumentKey}L{document.LigneCouter++}";
            _context.Lignes.Add(ligne);
            await _context.SaveChangesAsync();
            var ligneDto = await _context.Lignes
                .Include(l => l.Document!).ThenInclude(d => d.DocumentType)
                .Include(l => l.Document!).ThenInclude(d => d.CreatedBy).ThenInclude(u => u.Role)
                .Where(l => l.Id == ligne.Id).Select(LigneMappings.ToLigneDto).FirstOrDefaultAsync();
            return CreatedAtAction(nameof(GetLigne), new { id = ligne.Id }, ligneDto);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateLigne(int id, [FromBody] Ligne updatedLigne)
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
            if (ThisUser.Role!.RoleName != "Admin" && ThisUser.Role!.RoleName != "FullUser")
                return Unauthorized("User Not Allowed To do this action.");
            var ligne = await _context.Lignes.FindAsync(id);
            if (ligne == null)
                return NotFound("Ligne not found.");
            if (!string.IsNullOrEmpty(updatedLigne.Title))
                ligne.Title = updatedLigne.Title;
            if (!string.IsNullOrEmpty(updatedLigne.Article))
                ligne.Article = updatedLigne.Article;
            if (updatedLigne.Prix >= 0)
                ligne.Prix = updatedLigne.Prix;
            ligne.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok("Ligne updated!");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLigne(int id)
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
            if (ThisUser.Role!.RoleName != "Admin" && ThisUser.Role!.RoleName != "FullUser")
                return Unauthorized("User Not Allowed To do this action.");
            var ligne = await _context.Lignes.FindAsync(id);
            if (ligne == null)
                return NotFound("Ligne not found.");
            _context.Lignes.Remove(ligne);
            await _context.SaveChangesAsync();
            return Ok("Ligne deleted!");
        }
    }
}
