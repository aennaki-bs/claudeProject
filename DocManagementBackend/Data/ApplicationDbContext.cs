using Microsoft.EntityFrameworkCore;
using DocManagementBackend.Models;

namespace DocManagementBackend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }
        public ApplicationDbContext() { }

        // Existing entities
        public DbSet<User> Users { get; set; }
        public DbSet<LogHistory> LogHistories { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Ligne> Lignes { get; set; }
        public DbSet<SousLigne> SousLignes { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<DocumentType> DocumentTypes { get; set; }
        // New SubType entity
        public DbSet<SubType> SubTypes { get; set; }
        public DbSet<TypeCounter> TypeCounter { get; set; }
        public DbSet<Circuit> Circuits { get; set; }
        public DbSet<CircuitStatus> CircuitStatuses { get; set; }
        public DbSet<CircuitStep> CircuitSteps { get; set; }
        public DbSet<DocumentCircuitHistory> DocumentCircuitHistory { get; set; }
        public DbSet<DocumentStepHistory> DocumentStepHistory { get; set; }

        // Workflow entities
        public DbSet<Status> Status { get; set; }
        public DbSet<Step> Steps { get; set; }
        public DbSet<Models.Action> Actions { get; set; }
        public DbSet<StepAction> StepActions { get; set; }
        public DbSet<ActionStatusEffect> ActionStatusEffects { get; set; }
        public DbSet<DocumentStatus> DocumentStatus { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Circuit Status configuration
            modelBuilder.Entity<CircuitStatus>(entity =>
            {
                entity.HasOne(s => s.Circuit)
                    .WithMany(c => c.Statuses)
                    .HasForeignKey(s => s.CircuitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(s => new { s.CircuitId, s.Type })
                    .HasFilter("[Type] IN ('initial', 'final')")
                    .IsUnique();
            });

            // Circuit Step configuration
            modelBuilder.Entity<CircuitStep>(entity =>
            {
                entity.HasOne(s => s.Circuit)
                    .WithMany(c => c.Steps)
                    .HasForeignKey(s => s.CircuitId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(s => s.FromStatus)
                    .WithMany(s => s.FromSteps)
                    .HasForeignKey(s => s.FromStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.ToStatus)
                    .WithMany(s => s.ToSteps)
                    .HasForeignKey(s => s.ToStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => new { s.CircuitId, s.FromStatusId, s.ToStatusId })
                    .IsUnique();
            });

            // Document Circuit History configuration
            modelBuilder.Entity<DocumentCircuitHistory>(entity =>
            {
                entity.HasOne(h => h.Document)
                    .WithMany(d => d.History)
                    .HasForeignKey(h => h.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(h => h.FromStatus)
                    .WithMany()
                    .HasForeignKey(h => h.FromStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.ToStatus)
                    .WithMany()
                    .HasForeignKey(h => h.ToStatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Step)
                    .WithMany()
                    .HasForeignKey(h => h.StepId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Status)
                    .WithMany()
                    .HasForeignKey(h => h.StatusId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.Action)
                    .WithMany()
                    .HasForeignKey(h => h.ActionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(h => h.ProcessedBy)
                    .WithMany()
                    .HasForeignKey(h => h.ProcessedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // SubType relationship with Document
            modelBuilder.Entity<Document>()
                .HasOne(d => d.SubType)
                .WithMany(st => st.Documents)
                .HasForeignKey(d => d.SubTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // SubType relationship with DocumentType
            modelBuilder.Entity<SubType>()
                .HasOne(st => st.DocumentType)
                .WithMany()
                .HasForeignKey(st => st.DocumentTypeId)
                .OnDelete(DeleteBehavior.Cascade);

            // DocumentStepHistory relationships
            modelBuilder.Entity<DocumentStepHistory>()
                .HasOne(d => d.Document)
                .WithMany()
                .HasForeignKey(d => d.DocumentId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DocumentStepHistory>()
                .HasOne(d => d.Step)
                .WithMany()
                .HasForeignKey(d => d.StepId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DocumentStepHistory>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Status relationships
            modelBuilder.Entity<Status>()
                .HasOne(s => s.Step)
                .WithMany(s => s.Statuses)
                .HasForeignKey(s => s.StepId);

            // StepAction relationships
            modelBuilder.Entity<StepAction>()
                .HasOne(sa => sa.Step)
                .WithMany(s => s.StepActions)
                .HasForeignKey(sa => sa.StepId);

            modelBuilder.Entity<StepAction>()
                .HasOne(sa => sa.Action)
                .WithMany(a => a.StepActions)
                .HasForeignKey(sa => sa.ActionId);

            // ActionStatusEffect relationships
            modelBuilder.Entity<ActionStatusEffect>()
                .HasOne(ase => ase.Action)
                .WithMany()
                .HasForeignKey(ase => ase.ActionId);

            modelBuilder.Entity<ActionStatusEffect>()
                .HasOne(ase => ase.Status)
                .WithMany()
                .HasForeignKey(ase => ase.StatusId);

            modelBuilder.Entity<ActionStatusEffect>()
                .HasOne(ase => ase.Step)
                .WithMany()
                .HasForeignKey(ase => ase.StepId)
                .OnDelete(DeleteBehavior.NoAction);

            // DocumentStatus relationships
            modelBuilder.Entity<DocumentStatus>()
                .HasOne(ds => ds.Document)
                .WithMany()
                .HasForeignKey(ds => ds.DocumentId);

            modelBuilder.Entity<DocumentStatus>()
                .HasOne(ds => ds.Status)
                .WithMany()
                .HasForeignKey(ds => ds.StatusId);

            // Seed data
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, RoleName = "Admin", IsAdmin = true },
                new Role { Id = 2, RoleName = "SimpleUser", IsSimpleUser = true },
                new Role { Id = 3, RoleName = "FullUser", IsFullUser = true }
            );
        }
    }
}