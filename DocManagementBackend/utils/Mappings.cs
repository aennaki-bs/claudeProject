using System;
using System.Linq;
using System.Linq.Expressions;
using DocManagementBackend.Models;
using DocManagementBackend.ModelsDtos;

namespace DocManagementBackend.Mappings
{
    public static class LigneMappings
    {
        public static Expression<Func<Ligne, LigneDto>> ToLigneDto = l => new LigneDto
        {
            Id = l.Id,
            DocumentId = l.DocumentId,
            LingeKey = l.LigneKey,
            Title = l.Title,
            Article = l.Article,
            Prix = l.Prix,
            CreatedAt = l.CreatedAt,
            UpdatedAt = l.UpdatedAt,
            Document = new DocumentDto
            {
                Id = l.Document!.Id,
                DocumentKey = l.Document.DocumentKey,
                DocumentAlias = l.Document.DocumentAlias,
                Title = l.Document.Title,
                Content = l.Document.Content,
                TypeId = l.Document.TypeId,
                DocumentType = l.Document.DocumentType == null
                    ? null
                    : new DocumentTypeDto
                    {
                        TypeKey = l.Document.DocumentType.TypeKey,
                        TypeName = l.Document.DocumentType.TypeName,
                        TypeAttr = l.Document.DocumentType.TypeAttr
                    },
                CreatedAt = l.Document.CreatedAt,
                UpdatedAt = l.Document.UpdatedAt ?? DateTime.UtcNow, // Fix nullable DateTime
                Status = l.Document.Status,
                CreatedByUserId = l.Document.CreatedByUserId,
                CreatedBy = l.Document.CreatedBy == null
                    ? null
                    : new DocumentUserDto
                    {
                        Email = l.Document.CreatedBy.Email,
                        Username = l.Document.CreatedBy.Username,
                        FirstName = l.Document.CreatedBy.FirstName,
                        LastName = l.Document.CreatedBy.LastName,
                        UserType = l.Document.CreatedBy.UserType,
                        Role = l.Document.CreatedBy.Role != null
                            ? l.Document.CreatedBy.Role.RoleName
                            : "SimpleUser"
                    },
                LignesCount = l.Document.Lignes.Count,
                SousLignesCount = l.Document.Lignes.Sum(ll => ll.SousLignes.Count),
                // Add missing properties
                CurrentStatusId = l.Document.CurrentStatusId,
                CurrentStatusTitle = l.Document.CurrentStatus != null ? l.Document.CurrentStatus.Title : string.Empty
            },
            SousLignesCount = l.SousLignes.Count
        };
    }

    public static class SousLigneMappings
    {
        public static Expression<Func<SousLigne, SousLigneDto>> ToSousLigneDto = s => new SousLigneDto
        {
            Id = s.Id,
            LigneId = s.LigneId,
            Title = s.Title,
            Attribute = s.Attribute,
            Ligne = new LigneDto
            {
                Id = s.Ligne!.Id,
                DocumentId = s.Ligne.DocumentId,
                LingeKey = s.Ligne.LigneKey,
                Title = s.Ligne.Title,
                Article = s.Ligne.Article,
                Prix = s.Ligne.Prix,
                CreatedAt = s.Ligne.CreatedAt,
                UpdatedAt = s.Ligne.UpdatedAt,
                Document = new DocumentDto
                {
                    Id = s.Ligne.Document!.Id,
                    DocumentKey = s.Ligne.Document.DocumentKey,
                    DocumentAlias = s.Ligne.Document.DocumentAlias,
                    Title = s.Ligne.Document.Title,
                    Content = s.Ligne.Document.Content,
                    TypeId = s.Ligne.Document.TypeId,
                    DocumentType = s.Ligne.Document.DocumentType == null
                        ? null
                        : new DocumentTypeDto
                        {
                            TypeKey = s.Ligne.Document.DocumentType.TypeKey,
                            TypeName = s.Ligne.Document.DocumentType.TypeName,
                            TypeAttr = s.Ligne.Document.DocumentType.TypeAttr
                        },
                    CreatedAt = s.Ligne.Document.CreatedAt,
                    UpdatedAt = s.Ligne.Document.UpdatedAt ?? DateTime.UtcNow, // Fix nullable DateTime
                    Status = s.Ligne.Document.Status,
                    CreatedByUserId = s.Ligne.Document.CreatedByUserId,
                    CreatedBy = s.Ligne.Document.CreatedBy == null
                        ? null
                        : new DocumentUserDto
                        {
                            Email = s.Ligne.Document.CreatedBy.Email,
                            Username = s.Ligne.Document.CreatedBy.Username,
                            FirstName = s.Ligne.Document.CreatedBy.FirstName,
                            LastName = s.Ligne.Document.CreatedBy.LastName,
                            UserType = s.Ligne.Document.CreatedBy.UserType,
                            Role = s.Ligne.Document.CreatedBy.Role != null
                                ? s.Ligne.Document.CreatedBy.Role.RoleName
                                : "SimpleUser"
                        },
                    // Add missing properties
                    CurrentStatusId = s.Ligne.Document.CurrentStatusId,
                    CurrentStatusTitle = s.Ligne.Document.CurrentStatus != null ? s.Ligne.Document.CurrentStatus.Title : string.Empty
                }
            }
        };
    }

    public static class DocumentMappings
    {
        public static Expression<Func<Document, DocumentDto>> ToDocumentDto = d => new DocumentDto
        {
            Id = d.Id,
            DocumentKey = d.DocumentKey,
            DocumentAlias = d.DocumentAlias,
            Title = d.Title,
            Content = d.Content,
            CreatedAt = d.CreatedAt,
            UpdatedAt = d.UpdatedAt ?? DateTime.UtcNow, // Fix nullable DateTime
            DocDate = d.DocDate ?? DateTime.UtcNow, // Fix nullable DateTime
            Status = d.Status,
            TypeId = d.TypeId,
            DocumentType = new DocumentTypeDto
            {
                TypeKey = d.DocumentType!.TypeKey,
                TypeName = d.DocumentType!.TypeName,
                TypeAttr = d.DocumentType.TypeAttr
            },
            SubTypeId = d.SubTypeId,
            SubType = d.SubType == null ? null : new Models.SubTypeDto // Specify namespace to resolve ambiguity
            {
                Id = d.SubType.Id,
                SubTypeKey = d.SubType.SubTypeKey,
                Name = d.SubType.Name,
                Description = d.SubType.Description,
                StartDate = d.SubType.StartDate,
                EndDate = d.SubType.EndDate,
                DocumentTypeId = d.SubType.DocumentTypeId,
                IsActive = d.SubType.IsActive
            },
            CreatedByUserId = d.CreatedByUserId,
            CreatedBy = new DocumentUserDto
            {
                Email = d.CreatedBy.Email,
                Username = d.CreatedBy.Username,
                FirstName = d.CreatedBy.FirstName,
                LastName = d.CreatedBy.LastName,
                UserType = d.CreatedBy.UserType,
                Role = d.CreatedBy.Role != null ? d.CreatedBy.Role.RoleName : string.Empty
            },
            LignesCount = d.Lignes.Count,
            SousLignesCount = d.Lignes.Sum(l => l.SousLignes.Count),
            CircuitId = d.CircuitId,
            CurrentStepId = d.CurrentStepId,
            CurrentStepTitle = d.CurrentStep != null ? d.CurrentStep.Title : string.Empty,
            CurrentStatusId = d.CurrentStatusId,
            CurrentStatusTitle = d.CurrentStatus != null ? d.CurrentStatus.Title : string.Empty,
            IsCircuitCompleted = d.IsCircuitCompleted
        };
    }

    public static class UserMappings
    {
        public static Expression<Func<User, UserDto>> ToUserDto = u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            Username = u.Username,
            FirstName = u.FirstName,
            LastName = u.LastName,
            City = u.City,
            WebSite = u.WebSite,
            Address = u.Address,
            PhoneNumber = u.PhoneNumber,
            Country = u.Country,
            UserType = u.UserType,
            Identity = u.Identity,
            IsEmailConfirmed = u.IsEmailConfirmed,
            EmailVerificationCode = u.EmailVerificationCode,
            IsActive = u.IsActive,
            IsOnline = u.IsOnline,
            ProfilePicture = u.ProfilePicture,
            Role = new RoleDto
            {
                RoleId = u.Role!.Id,
                RoleName = u.Role.RoleName
            }
        };
    }

    public static class Mappings
    {
        public static void ConfigureMappings(this IMapperConfigurationExpression config)
        {
            config.CreateMap<Ligne, LigneDto>();
            config.CreateMap<SousLigne, SousLigneDto>();
            config.CreateMap<Document, DocumentDto>();
            config.CreateMap<DocumentType, DocumentTypeDto>();
            config.CreateMap<User, UserDto>();
            config.CreateMap<Circuit, CircuitDto>();
            config.CreateMap<CircuitStep, CircuitStepDto>();
            config.CreateMap<CircuitStatus, CircuitStatusDto>();
        }

        // Add extension methods for ToDto()
        public static DocumentDto ToDto(this Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                Title = document.Title,
                DocumentKey = document.DocumentKey,
                DocumentAlias = document.DocumentAlias,
                Status = document.Status,
                DocDate = document.DocDate ?? DateTime.UtcNow, // Fix nullable DateTime
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt ?? DateTime.UtcNow, // Fix nullable DateTime
                CircuitId = document.CircuitId,
                CurrentStepId = document.CurrentStepId,
                CurrentStepTitle = document.CurrentStep?.Title ?? string.Empty,
                CurrentStatusId = document.CurrentStatusId,
                CurrentStatusTitle = document.CurrentStatus?.Title ?? string.Empty,
                IsCircuitCompleted = document.IsCircuitCompleted
            };
        }

        public static CircuitDto ToDto(this Circuit circuit)
        {
            return new CircuitDto
            {
                Id = circuit.Id,
                CircuitKey = circuit.CircuitKey,
                Title = circuit.Title,
                Description = circuit.Description ?? string.Empty,
                IsActive = circuit.IsActive,
                HasOrderedFlow = circuit.HasOrderedFlow,
                AllowBacktrack = circuit.AllowBacktrack,
                CreatedAt = circuit.CreatedAt,
                UpdatedAt = circuit.UpdatedAt ?? DateTime.UtcNow, // Fix nullable DateTime
                Steps = circuit.Steps?.Select(s => s.ToDto()).ToList() ?? new List<CircuitStepDto>(),
                Statuses = circuit.Statuses?.Select(s => s.ToDto()).ToList() ?? new List<CircuitStatusDto>()
            };
        }

        public static CircuitStepDto ToDto(this CircuitStep step)
        {
            return new CircuitStepDto
            {
                Id = step.Id,
                StepKey = step.StepKey,
                CircuitId = step.CircuitId,
                Title = step.Title,
                Descriptif = step.Descriptif,
                OrderIndex = step.OrderIndex,
                FromStatusId = step.FromStatusId,
                ToStatusId = step.ToStatusId,
                IsFinalStep = step.IsFinalStep,
                CreatedAt = step.CreatedAt,
                UpdatedAt = step.UpdatedAt ?? DateTime.UtcNow // Fix nullable DateTime
            };
        }

        public static CircuitStatusDto ToDto(this CircuitStatus status)
        {
            return new CircuitStatusDto
            {
                Id = status.Id,
                CircuitId = status.CircuitId,
                Title = status.Title,
                Type = status.Type,
                IsActive = status.IsActive,
                OrderIndex = status.OrderIndex,
                CreatedAt = status.CreatedAt,
                UpdatedAt = status.UpdatedAt ?? DateTime.UtcNow // Fix nullable DateTime
            };
        }
    }
}