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
        /// Assigns a document to a circuit workflow and sets it to the initial status
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
                    .Include(c => c.Statuses)
                    .FirstOrDefaultAsync(c => c.Id == circuitId && c.IsActive);

                if (circuit == null)
                    throw new InvalidOperationException($"Circuit ID {circuitId} not found or not active");

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException($"User ID {userId} not found");

                // Find initial status for the circuit
                var initialStatus = circuit.Statuses.FirstOrDefault(s => s.IsInitial);
                if (initialStatus == null)
                    throw new InvalidOperationException("Circuit has no initial status defined");

                // Assign document to circuit
                document.CircuitId = circuitId;
                document.Circuit = circuit;
                document.Status = 1; // In Progress
                document.CurrentStatusId = initialStatus.Id;
                document.CurrentStatus = initialStatus;
                document.IsCircuitCompleted = false;

                // Create history entry
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = 0, // No step concept in new model
                    StatusId = initialStatus.Id,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = "Document assigned to circuit",
                    IsApproved = true
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

                // Create document status record
                var documentStatus = new DocumentStatus
                {
                    DocumentId = documentId,
                    StatusId = initialStatus.Id,
                    IsComplete = false
                };
                _context.DocumentStatus.Add(documentStatus);

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
        /// Moves a document to a new status via defined steps/transitions
        /// </summary>
        public async Task<bool> MoveToNextStatusAsync(int documentId, int targetStatusId, int userId, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStatus)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStatusId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                if (document.IsCircuitCompleted)
                    throw new InvalidOperationException("Document workflow is already completed");

                var circuit = document.Circuit;
                if (circuit == null)
                    throw new InvalidOperationException("Document is not assigned to a circuit");

                var currentStatus = document.CurrentStatus;
                if (currentStatus == null)
                    throw new InvalidOperationException("Current status not found");

                var targetStatus = await _context.Status.FindAsync(targetStatusId);
                if (targetStatus == null)
                    throw new InvalidOperationException("Target status not found");

                if (targetStatus.CircuitId != circuit.Id)
                    throw new InvalidOperationException("Target status doesn't belong to document's circuit");

                // Check if there's a valid step (transition) from current to target status
                var step = await _context.Steps
                    .FirstOrDefaultAsync(s =>
                        s.CircuitId == circuit.Id &&
                        s.CurrentStatusId == document.CurrentStatusId &&
                        s.NextStatusId == targetStatusId);

                if (step == null)
                    throw new InvalidOperationException($"No valid transition found from current status to target status");

                // Update document status
                document.CurrentStatusId = targetStatusId;
                document.CurrentStatus = targetStatus;
                document.UpdatedAt = DateTime.UtcNow;

                // Create history entry for the transition
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = step.Id,
                    StatusId = targetStatusId,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = string.IsNullOrEmpty(comments) ? $"Moved from {currentStatus.Title} to {targetStatus.Title}" : comments,
                    IsApproved = true
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

                // Create or update document status record
                var documentStatus = await _context.DocumentStatus
                    .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == targetStatusId);

                if (documentStatus == null)
                {
                    documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = targetStatusId,
                        IsComplete = false
                    };
                    _context.DocumentStatus.Add(documentStatus);
                }

                // If the status is final, mark the document as completed
                if (targetStatus.IsFinal)
                {
                    document.IsCircuitCompleted = true;
                    document.Status = 2; // Completed
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
        /// Returns a document to a previous status based on available transitions
        /// </summary>
        public async Task<bool> ReturnToPreviousStatusAsync(int documentId, int targetStatusId, int userId, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .Include(d => d.CurrentStatus)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStatusId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                var circuit = document.Circuit;
                if (circuit == null)
                    throw new InvalidOperationException("Document is not assigned to a circuit");

                var currentStatus = document.CurrentStatus;
                if (currentStatus == null)
                    throw new InvalidOperationException("Current status not found");

                var targetStatus = await _context.Status.FindAsync(targetStatusId);
                if (targetStatus == null)
                    throw new InvalidOperationException("Target status not found");

                if (targetStatus.CircuitId != circuit.Id)
                    throw new InvalidOperationException("Target status doesn't belong to document's circuit");

                // Check if there is a transition from the target status to the current status
                // This verifies that we're going "backwards" in the workflow
                var backwardStep = await _context.Steps
                    .FirstOrDefaultAsync(s =>
                        s.CircuitId == circuit.Id &&
                        s.CurrentStatusId == targetStatusId &&
                        s.NextStatusId == document.CurrentStatusId);

                if (backwardStep == null && !targetStatus.IsFlexible)
                    throw new InvalidOperationException($"Cannot return to this status from current status");

                // Update document status
                document.CurrentStatusId = targetStatusId;
                document.CurrentStatus = targetStatus;
                document.UpdatedAt = DateTime.UtcNow;

                // If the document was completed and we're going back, it's no longer complete
                if (document.IsCircuitCompleted)
                {
                    document.IsCircuitCompleted = false;
                    document.Status = 1; // In Progress
                }

                // Create history entry for the transition
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = 0, // No specific step for backwards movement
                    StatusId = targetStatusId,
                    ProcessedByUserId = userId,
                    ProcessedAt = DateTime.UtcNow,
                    Comments = string.IsNullOrEmpty(comments) ? $"Returned from {currentStatus.Title} to {targetStatus.Title}" : comments,
                    IsApproved = true
                };
                _context.DocumentCircuitHistory.Add(historyEntry);

                // Create or update document status record
                var documentStatus = await _context.DocumentStatus
                    .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.StatusId == targetStatusId);

                if (documentStatus == null)
                {
                    documentStatus = new DocumentStatus
                    {
                        DocumentId = documentId,
                        StatusId = targetStatusId,
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
        /// Updates the completion state of a document's status
        /// </summary>
        public async Task<bool> CompleteDocumentStatusAsync(int documentId, int statusId, int userId, bool isComplete, string comments = "")
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CurrentStatusId == null)
                    throw new KeyNotFoundException("Document not found or not in a workflow");

                if (document.IsCircuitCompleted)
                    throw new InvalidOperationException("Document workflow is already completed");

                // Get the status and verify it belongs to the document's circuit
                var status = await _context.Status.FindAsync(statusId);
                if (status == null)
                    throw new KeyNotFoundException("Status not found");

                if (status.CircuitId != document.CircuitId)
                    throw new InvalidOperationException("Status does not belong to the document's circuit");

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
                    StepId = 0, // No specific step for status updates
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
        /// Gets available transitions from the current status
        /// </summary>
        public async Task<List<StatusDto>> GetAvailableTransitionsAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Circuit)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.CurrentStatusId == null || document.CircuitId == null)
                throw new InvalidOperationException("Document not found or not in a workflow");

            if (document.IsCircuitCompleted)
                return new List<StatusDto>(); // No transitions available if completed

            // Find all possible next statuses based on steps defined for the current status
            var steps = await _context.Steps
                .Where(s =>
                    s.CircuitId == document.CircuitId &&
                    s.CurrentStatusId == document.CurrentStatusId)
                .ToListAsync();

            // Get the statuses for the available steps
            var nextStatusIds = steps.Select(s => s.NextStatusId).Distinct().ToList();

            var statuses = await _context.Status
                .Where(s => nextStatusIds.Contains(s.Id))
                .Select(s => new StatusDto
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
                })
                .ToListAsync();

            // Also include any flexible statuses in the circuit
            var flexibleStatuses = await _context.Status
                .Where(s =>
                    s.CircuitId == document.CircuitId &&
                    s.IsFlexible &&
                    s.Id != document.CurrentStatusId && // Don't include current status
                    !nextStatusIds.Contains(s.Id)) // Don't duplicate statuses
                .Select(s => new StatusDto
                {
                    StatusId = s.Id,
                    StatusKey = s.StatusKey,
                    Title = s.Title,
                    Description = s.Description,
                    IsRequired = s.IsRequired,
                    IsInitial = s.IsInitial,
                    IsFinal = s.IsFinal,
                    IsFlexible = s.IsFlexible, // This will be true
                    CircuitId = s.CircuitId
                })
                .ToListAsync();

            statuses.AddRange(flexibleStatuses);
            return statuses;
        }

        /// <summary>
        /// Gets the document's workflow history
        /// </summary>
        public async Task<IEnumerable<DocumentCircuitHistory>> GetDocumentCircuitHistory(int documentId)
        {
            return await _context.DocumentCircuitHistory
                .Where(h => h.DocumentId == documentId)
                .Include(h => h.ProcessedBy)
                .Include(h => h.Action)
                .Include(h => h.Status)
                .OrderByDescending(h => h.ProcessedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Processes an action for a document in its current status
        /// </summary>
        public async Task<bool> ProcessActionAsync(int documentId, int actionId, int userId, string comments = "", bool isApproved = true)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var document = await _context.Documents
                    .Include(d => d.Circuit)
                    .FirstOrDefaultAsync(d => d.Id == documentId);

                if (document == null || document.CircuitId == null || document.CurrentStatusId == null)
                    throw new InvalidOperationException("Document not assigned to circuit or status");

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    throw new KeyNotFoundException($"User ID {userId} not found");

                var action = await _context.Actions.FindAsync(actionId);
                if (action == null)
                    throw new KeyNotFoundException($"Action ID {actionId} not found");

                // Create history entry
                var historyEntry = new DocumentCircuitHistory
                {
                    DocumentId = documentId,
                    StepId = 0, // No specific step for actions in new model
                    ActionId = actionId,
                    StatusId = document.CurrentStatusId,
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

                // Check if action is associated with any steps that have the current status
                var steps = await _context.StepActions
                    .Include(sa => sa.Step)
                    .Where(sa =>
                        sa.ActionId == actionId &&
                        sa.Step.CurrentStatusId == document.CurrentStatusId &&
                        sa.Step.CircuitId == document.CircuitId)
                    .Select(sa => sa.Step)
                    .ToListAsync();

                // If there are steps associated with this action and current status,
                // auto-advance to the next status if action.AutoAdvance is true
                if (steps.Any() && action.AutoAdvance)
                {
                    // Take the first available transition
                    var step = steps.First();
                    await MoveToNextStatusAsync(
                        documentId,
                        step.NextStatusId,
                        userId,
                        $"Auto-advanced by action: {action.Title}");
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

        /// <summary>
        /// Checks if a document can move to a specific status
        /// </summary>
        public async Task<bool> CanMoveToStatusAsync(int documentId, int targetStatusId)
        {
            var document = await _context.Documents
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null || document.CurrentStatusId == null || document.CircuitId == null || document.IsCircuitCompleted)
                return false;

            // Check if the target status belongs to the document's circuit
            var targetStatus = await _context.Status.FindAsync(targetStatusId);
            if (targetStatus == null || targetStatus.CircuitId != document.CircuitId)
                return false;

            // If the status is flexible, it can always be reached
            if (targetStatus.IsFlexible)
                return true;

            // Check if there's a step/transition from current status to target status
            var hasStep = await _context.Steps.AnyAsync(s =>
                s.CircuitId == document.CircuitId &&
                s.CurrentStatusId == document.CurrentStatusId &&
                s.NextStatusId == targetStatusId);

            return hasStep;
        }

        /// <summary>
        /// Gets a document's workflow status information
        /// </summary>
        public async Task<DocumentWorkflowStatusDto> GetDocumentWorkflowStatusAsync(int documentId)
        {
            var document = await _context.Documents
                .Include(d => d.Circuit)
                .Include(d => d.CurrentStatus)
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                throw new KeyNotFoundException("Document not found");

            if (document.CircuitId == null || document.CurrentStatusId == null)
                throw new InvalidOperationException("Document is not in a workflow");

            // Get available transitions
            var availableTransitions = await GetAvailableTransitionsAsync(documentId);

            // Get status of the document
            var statusText = document.Status switch
            {
                0 => "Draft",
                1 => "In Progress",
                2 => "Completed",
                3 => "Rejected",
                _ => "Unknown"
            };

            var result = new DocumentWorkflowStatusDto
            {
                DocumentId = document.Id,
                DocumentTitle = document.Title,
                CircuitId = document.CircuitId,
                CircuitTitle = document.Circuit?.Title ?? "Unknown",
                CurrentStatusId = document.CurrentStatusId,
                CurrentStatusTitle = document.CurrentStatus?.Title ?? "Unknown",
                Status = document.Status,
                StatusText = statusText,
                IsCircuitCompleted = document.IsCircuitCompleted,
                AvailableStatusTransitions = availableTransitions
            };

            return result;
        }
    }
}