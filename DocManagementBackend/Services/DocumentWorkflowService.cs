using DocManagementBackend.Data;
using DocManagementBackend.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace DocManagementBackend.Services
{
    public class DocumentWorkflowService
    {
        private readonly ApplicationDbContext _context;

        public DocumentWorkflowService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Assigns a document to a circuit workflow and sets it to the first step
        /// </summary>
        public async Task<bool> AssignDocumentToCircuitAsync(int documentId, int circuitId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null)
                    throw new KeyNotFoundException($"Document ID {documentId} not found");

                var circuit = await _context.Circuits
                    .Include(c => c.Steps.OrderBy(s => s.OrderIndex))
                    .FirstOrDefaultAsync(c => c.Id == circuitId && c.IsActive);

                if (circuit == null || !circuit.Steps.Any())
                    throw new InvalidOperationException($"Circuit ID {circuitId} not found or has no steps");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException($"User ID {userId} not found");

                // Assign document to circuit
                document.CircuitId = circuitId;
                document.Circuit = circuit;
                document.Status = 1; // In Progress

                // Find first step (lowest OrderIndex)
                var firstStep = circuit.Steps.OrderBy(s => s.OrderIndex).First();
                document.CurrentStepId = firstStep.Id;
                document.CurrentStep = firstStep;
                document.IsCircuitCompleted = false;

                // Create history entry
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = firstStep.Id,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = "Document assigned to circuit",
                    IsApproved = true
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

                // Initialize document statuses for the first step
                var stepStatuses = await _context.Status
                    .Where(s => s.StepId == firstStep.Id)
                    .ToListAsync();

                foreach (var status in stepStatuses)
                {
                    var documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = status.Id,
                        IsComplete = false
                    };
                    _context.DocumentStatus.Add(documentStatus);
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

        /// <summary>
        /// Moves a document to the next step if all required statuses are complete
        /// </summary>
        public async Task<bool> MoveToNextStepAsync(int documentId, int currentStepId, int userId, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStep)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStepId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                if (document.CurrentStepId != currentStepId)
                    throw new InvalidOperationException("Document is not at the specified step");
                
                if (document.IsCircuitCompleted)
                    throw new InvalidOperationException("Document workflow is already completed");

                var circuit = document.Circuit;
                if (circuit == null)
                    throw new InvalidOperationException("Document is not assigned to a circuit");

                var currentStep = document.CurrentStep;
                if (currentStep == null)
                    throw new InvalidOperationException("Current step not found");

                // Check if current step is the final step
                if (currentStep.IsFinalStep)
                    throw new InvalidOperationException("Document is already at the final step");

                // Check if all required statuses for current step are complete
                var requiredStatuses = await _context.Status
                    .Where(s => s.StepId == currentStepId && s.IsRequired)
                    .ToListAsync();
                
                foreach (var status in requiredStatuses)
                {
                    var documentStatus = await _context.DocumentStatus
                        .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == status.Id);
                    
                    if (documentStatus == null || !documentStatus.IsComplete)
                        throw new InvalidOperationException($"Required status '{status.Title}' is not complete");
                }

                // Determine the next step based on OrderIndex
                Step? nextStep;
                
                if (currentStep.NextStepId.HasValue)
                {
                    // Use the explicit next step reference
                    nextStep = await _context.Steps.FindAsync(currentStep.NextStepId.Value);
                    if (nextStep == null)
                        throw new InvalidOperationException("Next step not found");
                }
                else
                {
                    // Find the next step by OrderIndex
                    nextStep = await _context.Steps
                        .Where(s => s.CircuitId == circuit.Id && s.OrderIndex > currentStep.OrderIndex)
                        .OrderBy(s => s.OrderIndex)
                        .FirstOrDefaultAsync();
                    
                    if (nextStep == null)
                        throw new InvalidOperationException("No next step found in the workflow");
                }

                // Move to the next step
                document.CurrentStepId = nextStep.Id;
                document.CurrentStep = nextStep;
                document.UpdatedAt = DateTime.UtcNow;

                // If next step is final, mark document as completed
                if (nextStep.IsFinalStep)
                {
                    document.IsCircuitCompleted = true;
                    document.Status = 2; // Completed
                }

                // Create history entry
                _context.DocumentStepHistory.Add(new DocumentStepHistory
                {
                    DocumentId = documentId,
                    StepId = nextStep.Id,
                    UserId = userId,
                    TransitionDate = DateTime.UtcNow,
                    Comments = comments
                });

                // Initialize statuses for the new step
                var nextStepStatuses = await _context.Status
                    .Where(s => s.StepId == nextStep.Id)
                    .ToListAsync();

                foreach (var status in nextStepStatuses)
                {
                    var documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = status.Id,
                        IsComplete = false
                    };
                    _context.DocumentStatus.Add(documentStatus);
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

        /// <summary>
        /// Returns a document to the previous step unconditionally
        /// </summary>
        public async Task<bool> ReturnToPreviousStepAsync(int documentId, int userId, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStep)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStepId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                var circuit = document.Circuit;
                if (circuit == null)
                    throw new InvalidOperationException("Document is not assigned to a circuit");

                if (!circuit.AllowBacktrack)
                    throw new InvalidOperationException("Backtracking is not allowed for this circuit");

                var currentStep = document.CurrentStep;
                if (currentStep == null)
                    throw new InvalidOperationException("Current step not found");

                // Determine the previous step
                Step? previousStep;
                
                if (currentStep.PrevStepId.HasValue)
                {
                    // Use the explicit previous step reference
                    previousStep = await _context.Steps.FindAsync(currentStep.PrevStepId.Value);
                    if (previousStep == null)
                        throw new InvalidOperationException("Previous step not found");
                }
                else
                {
                    // Find the previous step by OrderIndex
                    previousStep = await _context.Steps
                        .Where(s => s.CircuitId == circuit.Id && s.OrderIndex < currentStep.OrderIndex)
                        .OrderByDescending(s => s.OrderIndex)
                        .FirstOrDefaultAsync();
                    
                    if (previousStep == null)
                        throw new InvalidOperationException("This is the first step; cannot go back further");
                }

                // Remove statuses from current step
                var currentStatuses = await _context.DocumentStatus
                    .Where(ds => ds.DocumentId == documentId && _context.Status.Any(s => s.Id == ds.StatusId && s.StepId == currentStep.Id))
                    .ToListAsync();
                
                _context.DocumentStatus.RemoveRange(currentStatuses);

                // Move back to previous step
                document.CurrentStepId = previousStep.Id;
                document.CurrentStep = previousStep;
                document.UpdatedAt = DateTime.UtcNow;
                
                // If the document was completed and we're going back, it's no longer complete
                if (document.IsCircuitCompleted)
                {
                    document.IsCircuitCompleted = false;
                    document.Status = 1; // In Progress
                }

                // Create history entry
                _context.DocumentStepHistory.Add(new DocumentStepHistory
                {
                    DocumentId = documentId,
                    StepId = previousStep.Id,
                    UserId = userId,
                    TransitionDate = DateTime.UtcNow,
                    Comments = comments
                });

                // Restore the document's previous statuses for this step
                var previousStatuses = await _context.Status
                    .Where(s => s.StepId == previousStep.Id)
                    .ToListAsync();

                // Try to find previous status completion data
                var previousStatusHistory = await _context.DocumentCircuitHistory
                    .Where(h => h.DocumentId == documentId && h.StepId == previousStep.Id && h.StatusId.HasValue)
                    .OrderByDescending(h => h.ProcessedAt)
                    .ToListAsync();

                foreach (var status in previousStatuses)
                {
                    // Try to find if this status was previously completed
                    var lastStatusUpdate = previousStatusHistory
                        .FirstOrDefault(h => h.StatusId == status.Id);
                    
                    var documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = status.Id,
                        // Preserve previous completion state if available
                        IsComplete = lastStatusUpdate?.IsApproved ?? false,
                        CompletedByUserId = lastStatusUpdate?.ProcessedByUserId,
                        CompletedAt = lastStatusUpdate?.ProcessedAt
                    };
                    
                    _context.DocumentStatus.Add(documentStatus);
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

        /// <summary>
        /// Updates the completion state of a document's status
        /// </summary>
        public async Task<bool> CompleteDocumentStatusAsync(int documentId, int statusId, int userId, bool isComplete, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var document = await _context.Documents
                    .Include(d => d.CurrentStep)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStepId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                if (document.IsCircuitCompleted)
                    throw new InvalidOperationException("Document workflow is already completed");

                // Get the status and verify it belongs to the current step
                var status = await _context.Status.FindAsync(statusId);
                if (status == null)
                    throw new KeyNotFoundException("Status not found");

                if (status.StepId != document.CurrentStepId)
                    throw new InvalidOperationException("Status does not belong to the document's current step");

                // Get or create the document status
                var documentStatus = await _context.DocumentStatus
                    .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == statusId);

                if (documentStatus == null)
                {
                    documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = statusId,
                        IsComplete = isComplete,
                        CompletedByUserId = isComplete ? userId : null,
                        CompletedAt = isComplete ? DateTime.UtcNow : null
                    };
                    _context.DocumentStatus.Add(documentStatus);
                }
                else
                {
                    documentStatus.IsComplete = isComplete;
                    documentStatus.CompletedByUserId = isComplete ? userId : null;
                    documentStatus.CompletedAt = isComplete ? DateTime.UtcNow : null;
                }

                // Create history entry
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = document.CurrentStepId.Value,
                    StatusId = statusId,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = comments,
                    IsApproved = isComplete
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

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

        /// <summary>
        /// Checks if a document can be moved to the next step (all required statuses complete)
        /// </summary>
        public async Task<bool> CanMoveToNextStepAsync(int documentId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.CurrentStepId == null || document.IsCircuitCompleted)
                return false;

            // Get all required statuses for the current step
            var requiredStatuses = await _context.Status
                .Where(s => s.StepId == document.CurrentStepId && s.IsRequired)
                .ToListAsync();

            if (!requiredStatuses.Any())
                return true;

            // Check if all required statuses are complete
            foreach (var status in requiredStatuses)
            {
                var documentStatus = await _context.DocumentStatus
                    .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == status.Id);

                if (documentStatus == null || !documentStatus.IsComplete)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a document can be returned to the previous step
        /// </summary>
        public async Task<bool> CanReturnToPreviousStepAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Circuit)
                .Include(d => d.CurrentStep)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.CurrentStepId == null || document.Circuit == null)
                return false;

            // Circuit must allow backtracking
            if (!document.Circuit.AllowBacktrack)
                return false;

            var currentStep = document.CurrentStep;
            if (currentStep == null)
                return false;

            // There must be a previous step (either via PrevStepId or by OrderIndex)
            if (currentStep.PrevStepId.HasValue)
                return true;

            // Check if there's any step with a lower OrderIndex
            return await _context.Steps
                .AnyAsync(s => s.CircuitId == document.Circuit.Id && s.OrderIndex < currentStep.OrderIndex);
        }

        /// <summary>
        /// Gets the document's workflow history
        /// </summary>
        public async Task<IEnumerable<DocumentCircuitHistory>> GetDocumentCircuitHistory(int documentId)
        {
            return await _context.DocumentCircuitHistory
                .Where(h => h.DocumentId == documentId)
                .Include(h => h.Step)
                .Include(h => h.ProcessedBy)
                .Include(h => h.Action)
                .Include(h => h.Status)
                .OrderByDescending(h => h.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Processes an action for a document in its current step
        /// </summary>
        public async Task<bool> ProcessActionAsync(int documentId, int actionId, int userId, string comments = "", bool isApproved = true)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStep)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CircuitId == null || document.CurrentStepId == null)
                    throw new InvalidOperationException("Document not assigned to circuit or step");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    throw new KeyNotFoundException($"User ID {userId} not found");

                var action = await _context.Actions.FindAsync(actionId);
                if (action == null)
                    throw new KeyNotFoundException($"Action ID {actionId} not found");

                // Verify the action is valid for the current step
                var currentStep = document.CurrentStep;
                if (currentStep == null)
                    throw new InvalidOperationException("Current step not found");

                var stepAction = await _context.StepActions
                    .FirstOrDefaultAsync(sa => sa.StepId == currentStep.Id && sa.ActionId == actionId);

                if (stepAction == null)
                    throw new InvalidOperationException($"Action ID {actionId} not valid for step ID {currentStep.Id}");

                // Create history entry
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = currentStep.Id,
                    ActionId = actionId,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = comments,
                    IsApproved = isApproved
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

                // Handle rejection if the action is not approved
                if (!isApproved)
                {
                    document.Status = 3; // Rejected
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return true;
                }

                // Find which statuses this action affects
                var affectedStatuses = await _context.ActionStatusEffects
                    .Where(ase => ase.ActionId == actionId && ase.StepId == currentStep.Id)
                    .ToListAsync();

                // Update the affected statuses
                foreach (var effect in affectedStatuses)
                {
                    var documentStatus = await _context.DocumentStatus
                        .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == effect.StatusId);

                    if (documentStatus == null)
                    {
                        documentStatus = new DocumentStatus
                        {
                            DocumentId = documentId,
                            StatusId = effect.StatusId,
                            IsComplete = effect.SetsComplete,
                            CompletedByUserId = effect.SetsComplete ? userId : null,
                            CompletedAt = effect.SetsComplete ? DateTime.UtcNow : null
                        };
                        _context.DocumentStatus.Add(documentStatus);
                    }
                    else
                    {
                        documentStatus.IsComplete = effect.SetsComplete;
                        documentStatus.CompletedByUserId = effect.SetsComplete ? userId : null;
                        documentStatus.CompletedAt = effect.SetsComplete ? DateTime.UtcNow : null;
                    }
                }

                // Check if we should auto-advance to the next step
                bool canMoveNext = await CanMoveToNextStepAsync(documentId);
                if (canMoveNext && action.AutoAdvance)
                {
                    await MoveToNextStepAsync(documentId, currentStep.Id, userId, $"Auto-advanced by action: {action.Title}");
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

        /// <summary>
        /// Deletes a document and all its related data including workflow history
        /// </summary>
        public async Task<bool> DeleteDocumentAsync(int documentId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                // Delete related records in DocumentStepHistory first
                var stepHistoryRecords = await _context.DocumentStepHistory
                    .Where(dsh => dsh.DocumentId == documentId)
                    .ToListAsync();
                    
                if (stepHistoryRecords.Any())
                {
                    _context.DocumentStepHistory.RemoveRange(stepHistoryRecords);
                    await _context.SaveChangesAsync();
                }
                
                // Delete related records in DocumentCircuitHistory
                var circuitHistoryRecords = await _context.DocumentCircuitHistory
                    .Where(dch => dch.DocumentId == documentId)
                    .ToListAsync();
                    
                if (circuitHistoryRecords.Any())
                {
                    _context.DocumentCircuitHistory.RemoveRange(circuitHistoryRecords);
                    await _context.SaveChangesAsync();
                }
                
                // Delete related document statuses
                var documentStatuses = await _context.DocumentStatus
                    .Where(ds => ds.DocumentId == documentId)
                    .ToListAsync();
                    
                if (documentStatuses.Any())
                {
                    _context.DocumentStatus.RemoveRange(documentStatuses);
                    await _context.SaveChangesAsync();
                }
                
                // Get the document itself
                var document = await _context.Documents.FindAsync(documentId);
                if (document == null)
                {
                    await transaction.RollbackAsync();
                    return false;
                }
                
                // Finally delete the document
                _context.Documents.Remove(document);
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
    }
}