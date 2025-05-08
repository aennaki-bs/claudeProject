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
            // circuit.IsActive = true;

            _context.Circuits.Add(circuit);
            await _context.SaveChangesAsync();
            return circuit;
        }

        public async Task<Step> AddStepToCircuitAsync(Step step)
        {
            var circuit = await _context.Circuits.FindAsync(step.CircuitId);
            if (circuit == null)
                throw new KeyNotFoundException("Circuit not found");

            // Get the count of existing steps to determine the order index
            var stepCount = await _context.Steps.CountAsync(s => s.CircuitId == step.CircuitId);
            step.OrderIndex = stepCount + 1;

            // Generate a unique key for the step
            step.StepKey = $"STP-{circuit.CircuitKey}-{Guid.NewGuid().ToString().Substring(0, 8)}";

            _context.Steps.Add(step);
            await _context.SaveChangesAsync();

            // If the circuit has ordered flow, update the step links
            if (circuit.HasOrderedFlow)
            {
                await UpdateStepLinksAsync(circuit.Id);
            }

            return step;
        }

        public async Task<bool> UpdateStepOrderAsync(int circuitId, List<StepOrderUpdateDto> stepOrders)
        {
            var circuit = await _context.Circuits
                .Include(c => c.Steps)
                .FirstOrDefaultAsync(c => c.Id == circuitId);

            if (circuit == null)
                throw new KeyNotFoundException($"Circuit ID {circuitId} not found");

            // Start a transaction for updating all steps
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                foreach (var stepOrder in stepOrders)
                {
                    var step = await _context.Steps.FindAsync(stepOrder.StepId);
                    if (step == null || step.CircuitId != circuitId)
                        throw new InvalidOperationException($"Step ID {stepOrder.StepId} not found in circuit {circuitId}");

                    step.OrderIndex = stepOrder.OrderIndex;
                }

                // If circuit has ordered flow, update Next/Prev relationships
                if (circuit.HasOrderedFlow)
                {
                    await UpdateStepLinksAsync(circuitId);
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

        // Add this method to your CircuitManagementService class
        public async Task UpdateStepLinksAsync(int circuitId)
        {
            // Get all steps for this circuit ordered by OrderIndex
            var steps = await _context.Steps
                .Where(s => s.CircuitId == circuitId)
                .OrderBy(s => s.OrderIndex)
                .ToListAsync();

            if (steps.Count <= 0)
                return;

            // Reset all links first
            foreach (var step in steps)
            {
                step.PrevStepId = null;
                step.NextStepId = null;
            }

            // Set up the chain of steps
            for (int i = 0; i < steps.Count; i++)
            {
                // Set prev step (if not first)
                if (i > 0)
                {
                    steps[i].PrevStepId = steps[i - 1].Id;
                }

                // Set next step (if not last)
                if (i < steps.Count - 1)
                {
                    steps[i].NextStepId = steps[i + 1].Id;
                }
            }

            // Mark the last step as final
            if (steps.Count > 0)
            {
                steps[steps.Count - 1].IsFinalStep = true;
            }

            await _context.SaveChangesAsync();
        }

        // Add a method to update all circuits' step links
        public async Task UpdateAllCircuitStepLinksAsync()
        {
            var circuits = await _context.Circuits
                .Where(c => c.HasOrderedFlow)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var circuitId in circuits)
            {
                await UpdateStepLinksAsync(circuitId);
            }
        }
    }
}