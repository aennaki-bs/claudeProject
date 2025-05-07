using DocManagementBackend.ModelsDtos;
using DocManagementBackend.Services;
using DocManagementBackend.utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DocManagementBackend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CircuitManagementController : ControllerBase
    {
        private readonly CircuitManagementService _circuitManagementService;

        public CircuitManagementController(CircuitManagementService circuitManagementService)
        {
            _circuitManagementService = circuitManagementService;
        }

        #region Status Management

        [HttpPost("status")]
        public async Task<ActionResult<CircuitStatusDto>> CreateStatus([FromBody] CreateCircuitStatusDto dto)
        {
            try
            {
                var status = await _circuitManagementService.CreateStatusAsync(dto);
                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("status/{id}")]
        public async Task<ActionResult<CircuitStatusDto>> UpdateStatus(int id, [FromBody] UpdateCircuitStatusDto dto)
        {
            try
            {
                var status = await _circuitManagementService.UpdateStatusAsync(id, dto);
                return Ok(status);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("status/{id}")]
        public async Task<ActionResult> DeleteStatus(int id)
        {
            try
            {
                await _circuitManagementService.DeleteStatusAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Step Management

        [HttpPost("step")]
        public async Task<ActionResult<CircuitStepDto>> CreateStep([FromBody] CreateStepDto dto)
        {
            try
            {
                var step = await _circuitManagementService.CreateStepAsync(dto);
                return Ok(step);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("step/{id}")]
        public async Task<ActionResult<CircuitStepDto>> UpdateStep(int id, [FromBody] UpdateStepDto dto)
        {
            try
            {
                var step = await _circuitManagementService.UpdateStepAsync(id, dto);
                return Ok(step);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("step/{id}")]
        public async Task<ActionResult> DeleteStep(int id)
        {
            try
            {
                await _circuitManagementService.DeleteStepAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        #endregion

        #region Circuit Validation

        [HttpGet("validate/{circuitId}")]
        public async Task<ActionResult<CircuitValidationDto>> ValidateCircuit(int circuitId)
        {
            try
            {
                var validation = await _circuitManagementService.ValidateCircuitAsync(circuitId);
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

        #endregion
    }
} 