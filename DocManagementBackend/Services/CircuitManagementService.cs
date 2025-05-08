using DocManagementBackend.Data;
using DocManagementBackend.Models;
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

        public async Task<Circuit> CreateCircuitAsync(Circuit circuit)
        {
            // Generate circuit key and set defaults
            var counter = await _context.TypeCounter.FirstOrDefaultAsync();
            if (counter == null)
            {
                counter = new TypeCounter { circuitCounter = 1 };
                _context.TypeCounter.Add(counter);
            }
            else
            {
                counter.circuitCounter++;
            }

            string paddedCounter = counter.circuitCounter.ToString("D2");
            circuit.CircuitKey = $"CR{paddedCounter}";

            _context.Circuits.Add(circuit);
            await _context.SaveChangesAsync();
            return circuit;
        }

        public async Task<Status> AddStatusToCircuitAsync(Status status)
        {
            var circuit = await _context.Circuits.FindAsync(status.CircuitId);
            if (circuit == null)
                throw new KeyNotFoundException("Circuit not found");

            // Generate a unique key for the status
            status.StatusKey = $"ST-{circuit.CircuitKey}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            // Validate status types
            if (status.IsInitial)
            {
                // Check if there's already an initial status
                var existingInitial = await _context.Status
                    .AnyAsync(s => s.CircuitId == status.CircuitId && s.IsInitial);

                if (existingInitial)
                    throw new InvalidOperationException("This circuit already has an initial status. Only one initial status is allowed.");
            }

            if (status.IsFinal)
            {
                // Check if there's already a final status
                var existingFinal = await _context.Status
                    .AnyAsync(s => s.CircuitId == status.CircuitId && s.IsFinal);

                if (existingFinal)
                    throw new InvalidOperationException("This circuit already has a final status. Only one final status is allowed.");
            }

            _context.Status.Add(status);
            await _context.SaveChangesAsync();
            return status;
        }

        public async Task<Step> AddStepToCircuitAsync(Step step)
        {
            var circuit = await _context.Circuits.FindAsync(step.CircuitId);
            if (circuit == null)
                throw new KeyNotFoundException("Circuit not found");

            // Validate statuses exist and belong to the same circuit
            var currentStatus = await _context.Status.FindAsync(step.CurrentStatusId);
            if (currentStatus == null || currentStatus.CircuitId != step.CircuitId)
                throw new InvalidOperationException("Current status not found or doesn't belong to this circuit");

            var nextStatus = await _context.Status.FindAsync(step.NextStatusId);
            if (nextStatus == null || nextStatus.CircuitId != step.CircuitId)
                throw new InvalidOperationException("Next status not found or doesn't belong to this circuit");

            // Check if the step already exists (same current & next status combination)
            var existingStep = await _context.Steps
                .AnyAsync(s => s.CircuitId == step.CircuitId &&
                           s.CurrentStatusId == step.CurrentStatusId &&
                           s.NextStatusId == step.NextStatusId);

            if (existingStep)
                throw new InvalidOperationException("A step with this current and next status combination already exists");

            // Generate a unique key for the step
            step.StepKey = $"STP-{circuit.CircuitKey}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            _context.Steps.Add(step);
            await _context.SaveChangesAsync();
            return step;
        }

        public async Task<bool> UpdateStatusAsync(int statusId, UpdateStatusDto updateDto)
        {
            var status = await _context.Status.FindAsync(statusId);
            if (status == null)
                throw new KeyNotFoundException("Status not found");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (updateDto.Title != null)
                    status.Title = updateDto.Title;

                if (updateDto.Description != null)
                    status.Description = updateDto.Description;

                if (updateDto.IsRequired.HasValue)
                    status.IsRequired = updateDto.IsRequired.Value;

                // Handle IsInitial flag change
                if (updateDto.IsInitial.HasValue && updateDto.IsInitial.Value != status.IsInitial)
                {
                    if (updateDto.IsInitial.Value)
                    {
                        // If setting to Initial, check no other Initial exists
                        var existingInitial = await _context.Status
                            .AnyAsync(s => s.Id != statusId && s.CircuitId == status.CircuitId && s.IsInitial);

                        if (existingInitial)
                            throw new InvalidOperationException("This circuit already has an initial status. Only one initial status is allowed.");
                    }

                    status.IsInitial = updateDto.IsInitial.Value;
                }

                // Handle IsFinal flag change
                if (updateDto.IsFinal.HasValue && updateDto.IsFinal.Value != status.IsFinal)
                {
                    if (updateDto.IsFinal.Value)
                    {
                        // If setting to Final, check no other Final exists
                        var existingFinal = await _context.Status
                            .AnyAsync(s => s.Id != statusId && s.CircuitId == status.CircuitId && s.IsFinal);

                        if (existingFinal)
                            throw new InvalidOperationException("This circuit already has a final status. Only one final status is allowed.");
                    }

                    status.IsFinal = updateDto.IsFinal.Value;
                }

                if (updateDto.IsFlexible.HasValue)
                    status.IsFlexible = updateDto.IsFlexible.Value;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> UpdateStepAsync(int stepId, UpdateStepDto updateDto)
        {
            var step = await _context.Steps.FindAsync(stepId);
            if (step == null)
                throw new KeyNotFoundException("Step not found");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                if (updateDto.Title != null)
                    step.Title = updateDto.Title;

                if (updateDto.Descriptif != null)
                    step.Descriptif = updateDto.Descriptif;

                // Check if current status is being updated
                if (updateDto.CurrentStatusId.HasValue && updateDto.CurrentStatusId.Value != step.CurrentStatusId)
                {
                    var currentStatus = await _context.Status.FindAsync(updateDto.CurrentStatusId.Value);
                    if (currentStatus == null || currentStatus.CircuitId != step.CircuitId)
                        throw new InvalidOperationException("Current status not found or doesn't belong to this circuit");

                    step.CurrentStatusId = updateDto.CurrentStatusId.Value;
                }

                // Check if next status is being updated
                if (updateDto.NextStatusId.HasValue && updateDto.NextStatusId.Value != step.NextStatusId)
                {
                    var nextStatus = await _context.Status.FindAsync(updateDto.NextStatusId.Value);
                    if (nextStatus == null || nextStatus.CircuitId != step.CircuitId)
                        throw new InvalidOperationException("Next status not found or doesn't belong to this circuit");

                    step.NextStatusId = updateDto.NextStatusId.Value;
                }

                // Check if the updated step already exists with the same status combination
                if ((updateDto.CurrentStatusId.HasValue || updateDto.NextStatusId.HasValue) &&
                    await _context.Steps.AnyAsync(s =>
                        s.Id != stepId &&
                        s.CircuitId == step.CircuitId &&
                        s.CurrentStatusId == (updateDto.CurrentStatusId ?? step.CurrentStatusId) &&
                        s.NextStatusId == (updateDto.NextStatusId ?? step.NextStatusId)))
                {
                    throw new InvalidOperationException("A step with this current and next status combination already exists");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> DeleteStatusAsync(int statusId)
        {
            var status = await _context.Status.FindAsync(statusId);
            if (status == null)
                throw new KeyNotFoundException("Status not found");

            // Check if status is used in any step
            var isUsedInSteps = await _context.Steps.AnyAsync(s =>
                s.CurrentStatusId == statusId || s.NextStatusId == statusId);

            if (isUsedInSteps)
                throw new InvalidOperationException("Cannot delete status that is used in steps. Remove related steps first.");

            // Check if any documents are using this status
            var isUsedByDocuments = await _context.Documents.AnyAsync(d => d.CurrentStatusId == statusId);
            if (isUsedByDocuments)
                throw new InvalidOperationException("Cannot delete status that is used by documents.");

            _context.Status.Remove(status);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteStepAsync(int stepId)
        {
            var step = await _context.Steps.FindAsync(stepId);
            if (step == null)
                throw new KeyNotFoundException("Step not found");

            _context.Steps.Remove(step);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<CircuitValidationDto> ValidateCircuitAsync(int circuitId)
        {
            var circuit = await _context.Circuits
                .Include(c => c.Statuses)
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == circuitId);

            if (circuit == null)
                throw new KeyNotFoundException("Circuit not found");

            var validation = new CircuitValidationDto
            {
                CircuitId = circuitId,
                CircuitTitle = circuit.Title,
                HasStatuses = circuit.Statuses.Any(),
                TotalStatuses = circuit.Statuses.Count,
                HasInitialStatus = circuit.Statuses.Any(s => s.IsInitial),
                HasFinalStatus = circuit.Statuses.Any(s => s.IsFinal),
                HasSteps = circuit.Steps.Any(),
                TotalSteps = circuit.Steps.Count,
                IsValid = false // Default to false until validated
            };

            // Perform validation checks
            if (!validation.HasStatuses)
            {
                validation.ValidationMessages.Add("Circuit has no statuses defined");
                return validation;
            }

            if (!validation.HasInitialStatus)
            {
                validation.ValidationMessages.Add("Circuit has no initial status defined");
            }

            if (!validation.HasFinalStatus)
            {
                validation.ValidationMessages.Add("Circuit has no final status defined");
            }

            if (!validation.HasSteps)
            {
                validation.ValidationMessages.Add("Circuit has no steps defined");
                return validation;
            }

            // Check if initial status is used in any step as current status
            var initialStatus = circuit.Statuses.FirstOrDefault(s => s.IsInitial);
            if (initialStatus != null)
            {
                var initialStatusUsed = circuit.Steps.Any(s => s.CurrentStatusId == initialStatus.Id);
                if (!initialStatusUsed)
                {
                    validation.ValidationMessages.Add("Initial status is not used as a current status in any step");
                }
            }

            // Check if all statuses are reachable from the initial status
            if (initialStatus != null)
            {
                var reachableStatuses = new HashSet<int>();
                reachableStatuses.Add(initialStatus.Id);

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var step in circuit.Steps)
                    {
                        if (reachableStatuses.Contains(step.CurrentStatusId) && !reachableStatuses.Contains(step.NextStatusId))
                        {
                            reachableStatuses.Add(step.NextStatusId);
                            changed = true;
                        }
                    }
                }

                var unreachableStatuses = circuit.Statuses
                    .Where(s => !s.IsFlexible && !reachableStatuses.Contains(s.Id))
                    .ToList();

                if (unreachableStatuses.Any())
                {
                    validation.ValidationMessages.Add($"The following statuses are not reachable from the initial status: {string.Join(", ", unreachableStatuses.Select(s => s.Title))}");
                }
            }

            // Check if final status is reachable
            var finalStatus = circuit.Statuses.FirstOrDefault(s => s.IsFinal);
            if (finalStatus != null && initialStatus != null)
            {
                var reachableStatuses = new HashSet<int>();
                reachableStatuses.Add(initialStatus.Id);

                bool changed = true;
                while (changed)
                {
                    changed = false;
                    foreach (var step in circuit.Steps)
                    {
                        if (reachableStatuses.Contains(step.CurrentStatusId) && !reachableStatuses.Contains(step.NextStatusId))
                        {
                            reachableStatuses.Add(step.NextStatusId);
                            changed = true;
                        }
                    }
                }

                if (!reachableStatuses.Contains(finalStatus.Id))
                {
                    validation.ValidationMessages.Add("Final status is not reachable from the initial status");
                }
            }

            // If no validation messages, the circuit is valid
            validation.IsValid = !validation.ValidationMessages.Any();

            return validation;
        }
    }
}