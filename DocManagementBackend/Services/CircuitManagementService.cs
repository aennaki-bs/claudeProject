using DocManagementBackend.Data;
using DocManagementBackend.Models;
using DocManagementBackend.ModelsDtos;
using DocManagementBackend.utils;
using Microsoft.EntityFrameworkCore;

namespace DocManagementBackend.Services
{
    public class CircuitManagementService
    {
        private readonly ApplicationDbContext _context;

        public CircuitManagementService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Circuit Management

        public async Task<Circuit> CreateCircuitAsync(Circuit circuit)
        {
            circuit.CreatedAt = DateTime.UtcNow;
            circuit.UpdatedAt = DateTime.UtcNow;
            circuit.CircuitKey = GenerateCircuitKey();

            _context.Circuits.Add(circuit);
            await _context.SaveChangesAsync();
            return circuit;
        }

        private string GenerateCircuitKey()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"CIRC-{timestamp}-{random}";
        }

        #endregion

        #region Status Management

        public async Task<CircuitStatusDto> CreateStatusAsync(CreateCircuitStatusDto dto)
        {
            // Validate circuit exists
            var circuit = await _context.Circuits.FindAsync(dto.CircuitId);
            if (circuit == null)
                throw new KeyNotFoundException($"Circuit with ID {dto.CircuitId} not found");

            // Validate type constraints
            if (dto.Type == "initial" || dto.Type == "final")
            {
                var existingType = await _context.CircuitStatuses
                    .AnyAsync(s => s.CircuitId == dto.CircuitId && s.Type == dto.Type);
                if (existingType)
                    throw new InvalidOperationException($"Circuit already has a {dto.Type} status");
            }

            var status = new CircuitStatus
            {
                Title = dto.Title,
                Type = dto.Type,
                CircuitId = dto.CircuitId,
                IsActive = dto.IsActive,
                OrderIndex = dto.OrderIndex,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CircuitStatuses.Add(status);
            await _context.SaveChangesAsync();

            return status.ToDto();
        }

        public async Task<CircuitStatusDto> UpdateStatusAsync(int id, UpdateCircuitStatusDto dto)
        {
            var status = await _context.CircuitStatuses.FindAsync(id);
            if (status == null)
                throw new KeyNotFoundException($"Status with ID {id} not found");

            // Validate type constraints if type is being changed
            if (dto.Type != null && dto.Type != status.Type)
            {
                if (dto.Type == "initial" || dto.Type == "final")
                {
                    var existingType = await _context.CircuitStatuses
                        .AnyAsync(s => s.CircuitId == status.CircuitId && s.Type == dto.Type && s.Id != id);
                    if (existingType)
                        throw new InvalidOperationException($"Circuit already has a {dto.Type} status");
                }
            }

            status.Title = dto.Title ?? status.Title;
            status.Type = dto.Type ?? status.Type;
            status.IsActive = dto.IsActive ?? status.IsActive;
            status.OrderIndex = dto.OrderIndex ?? status.OrderIndex;
            status.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return status.ToDto();
        }

        public async Task DeleteStatusAsync(int id)
        {
            var status = await _context.CircuitStatuses
                .Include(s => s.FromSteps)
                .Include(s => s.ToSteps)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null)
                throw new KeyNotFoundException($"Status with ID {id} not found");

            // Check if status is referenced in any steps
            if (status.FromSteps?.Any() == true || status.ToSteps?.Any() == true)
                throw new InvalidOperationException("Cannot delete status that is referenced in steps");

            _context.CircuitStatuses.Remove(status);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Step Management

        public async Task<CircuitStepDto> CreateStepAsync(CreateStepDto dto)
        {
            // Validate circuit exists
            var circuit = await _context.Circuits.FindAsync(dto.CircuitId);
            if (circuit == null)
                throw new KeyNotFoundException($"Circuit with ID {dto.CircuitId} not found");

            // Validate statuses exist
            var fromStatus = await _context.CircuitStatuses.FindAsync(dto.FromStatusId);
            var toStatus = await _context.CircuitStatuses.FindAsync(dto.ToStatusId);

            if (fromStatus == null)
                throw new KeyNotFoundException($"From status with ID {dto.FromStatusId} not found");
            if (toStatus == null)
                throw new KeyNotFoundException($"To status with ID {dto.ToStatusId} not found");

            // Validate statuses belong to the same circuit
            if (fromStatus.CircuitId != dto.CircuitId || toStatus.CircuitId != dto.CircuitId)
                throw new InvalidOperationException("Statuses must belong to the same circuit");

            // Check for duplicate step
            var existingStep = await _context.CircuitSteps
                .AnyAsync(s => s.CircuitId == dto.CircuitId && 
                             s.FromStatusId == dto.FromStatusId && 
                             s.ToStatusId == dto.ToStatusId);
            if (existingStep)
                throw new InvalidOperationException("A step with these statuses already exists");

            var step = new CircuitStep
            {
                CircuitId = dto.CircuitId,
                Title = dto.Title,
                Descriptif = dto.Descriptif,
                OrderIndex = dto.OrderIndex,
                FromStatusId = dto.FromStatusId,
                ToStatusId = dto.ToStatusId,
                IsFinalStep = dto.IsFinalStep,
                StepKey = GenerateStepKey(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CircuitSteps.Add(step);
            await _context.SaveChangesAsync();

            return step.ToDto();
        }

        private string GenerateStepKey()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var random = new Random().Next(1000, 9999);
            return $"STEP-{timestamp}-{random}";
        }

        public async Task<CircuitStepDto> UpdateStepAsync(int id, UpdateStepDto dto)
        {
            var step = await _context.CircuitSteps.FindAsync(id);
            if (step == null)
                throw new KeyNotFoundException($"Step with ID {id} not found");

            // Validate statuses if they're being updated
            if (dto.FromStatusId.HasValue || dto.ToStatusId.HasValue)
            {
                var fromStatusId = dto.FromStatusId ?? step.FromStatusId;
                var toStatusId = dto.ToStatusId ?? step.ToStatusId;

                var fromStatus = await _context.CircuitStatuses.FindAsync(fromStatusId);
                var toStatus = await _context.CircuitStatuses.FindAsync(toStatusId);

                if (fromStatus == null)
                    throw new KeyNotFoundException($"From status with ID {fromStatusId} not found");
                if (toStatus == null)
                    throw new KeyNotFoundException($"To status with ID {toStatusId} not found");

                // Validate statuses belong to the same circuit
                if (fromStatus.CircuitId != step.CircuitId || toStatus.CircuitId != step.CircuitId)
                    throw new InvalidOperationException("Statuses must belong to the same circuit");

                // Check for duplicate step
                var existingStep = await _context.CircuitSteps
                    .AnyAsync(s => s.CircuitId == step.CircuitId && 
                                 s.FromStatusId == fromStatusId && 
                                 s.ToStatusId == toStatusId &&
                                 s.Id != id);
                if (existingStep)
                    throw new InvalidOperationException("A step with these statuses already exists");
            }

            step.Title = dto.Title ?? step.Title;
            step.Descriptif = dto.Descriptif ?? step.Descriptif;
            step.OrderIndex = dto.OrderIndex ?? step.OrderIndex;
            step.FromStatusId = dto.FromStatusId ?? step.FromStatusId;
            step.ToStatusId = dto.ToStatusId ?? step.ToStatusId;
            step.IsFinalStep = dto.IsFinalStep ?? step.IsFinalStep;
            step.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return step.ToDto();
        }

        public async Task DeleteStepAsync(int id)
        {
            var step = await _context.CircuitSteps.FindAsync(id);
            if (step == null)
                throw new KeyNotFoundException($"Step with ID {id} not found");

            _context.CircuitSteps.Remove(step);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Circuit Validation

        public async Task<CircuitValidationDto> ValidateCircuitAsync(int circuitId)
        {
            var circuit = await _context.Circuits
                .Include(c => c.Statuses)
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == circuitId);

            if (circuit == null)
                throw new KeyNotFoundException($"Circuit with ID {circuitId} not found");

            var validation = new CircuitValidationDto
            {
                CircuitId = circuit.Id,
                CircuitTitle = circuit.Title,
                HasInitialStatus = circuit.Statuses.Any(s => s.Type == "initial" && s.IsActive),
                HasFinalStatus = circuit.Statuses.Any(s => s.Type == "final" && s.IsActive),
                HasValidSteps = circuit.Steps.Any(),
                ValidationErrors = new List<string>()
            };

            if (!validation.HasInitialStatus)
                validation.ValidationErrors.Add("Circuit has no active initial status");

            if (!validation.HasFinalStatus)
                validation.ValidationErrors.Add("Circuit has no active final status");

            if (!validation.HasValidSteps)
                validation.ValidationErrors.Add("Circuit has no steps");

            // Validate step connections
            var statusIds = circuit.Statuses.Select(s => s.Id).ToHashSet();
            foreach (var step in circuit.Steps)
            {
                if (!statusIds.Contains(step.FromStatusId))
                    validation.ValidationErrors.Add($"Step {step.Title} references non-existent from status");
                if (!statusIds.Contains(step.ToStatusId))
                    validation.ValidationErrors.Add($"Step {step.Title} references non-existent to status");
            }

            validation.IsValid = !validation.ValidationErrors.Any();

            return validation;
        }

        #endregion
    }
}