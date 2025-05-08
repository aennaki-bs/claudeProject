using System;
using System.Linq.Expressions;
using DocManagementBackend.Models;


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
                UpdatedAt = l.Document.UpdatedAt,
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
                SousLignesCount = l.Document.Lignes.Sum(ll => ll.SousLignes.Count)
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
                Id = s.Id,
                DocumentId = s.Ligne!.DocumentId,
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
                    UpdatedAt = s.Ligne.Document.UpdatedAt,
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
                        }
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
            UpdatedAt = d.UpdatedAt,
            DocDate = d.DocDate,
            Status = d.Status,
            TypeId = d.TypeId,
            DocumentType = new DocumentTypeDto
            {
                TypeKey = d.DocumentType!.TypeKey,
                TypeName = d.DocumentType!.TypeName,
                TypeAttr = d.DocumentType.TypeAttr
            },
            SubTypeId = d.SubTypeId,
            SubType = d.SubType == null ? null : new SubTypeDto
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
            CurrentStatusId = d.CurrentStatusId,
            CurrentStatusTitle = d.CurrentStatus != null ? d.CurrentStatus.Title : string.Empty,
            IsCircuitCompleted = d.IsCircuitCompleted
        };
    }

    public static class UserMappings
    {
        public static Expression<Func<User, UserDto>> ToUserDto = d => new UserDto
        {
            Id = d.Id,
            Email = d.Email,
            Username = d.Username,
            FirstName = d.FirstName,
            LastName = d.LastName,
            City = d.City,
            WebSite = d.WebSite,
            Address = d.Address,
            PhoneNumber = d.PhoneNumber,
            Country = d.Country,
            UserType = d.UserType,
            Identity = d.Identity,
            IsEmailConfirmed = d.IsEmailConfirmed,
            EmailVerificationCode = d.EmailVerificationCode,
            IsActive = d.IsActive,
            IsOnline = d.IsOnline,
            ProfilePicture = d.ProfilePicture,
            Role = new RoleDto
            {
                RoleId = d.Role!.Id,
                RoleName = d.Role.RoleName
            }
        };
    }
}