using Clean.Architecture.Mediator.Data.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Diagnostics.CodeAnalysis;

namespace Clean.Architecture.Mediator.Data {
    public class ApplicationDbContext : DbContext {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {
            ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        }

        // DbSets here

        public override int SaveChanges() {
            SaveChangesInternal();
            return base.SaveChanges();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
            base.OnModelCreating(modelBuilder);
        }

        private void SaveChangesInternal() {
            ApplyDeletionConfiguration(ChangeTracker);
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
        private void ApplyDeletionConfiguration(ChangeTracker changeTracker) {
            var changeTrackerEntries = changeTracker
                .Entries()
                .Where(entry =>
                    entry.State == EntityState.Deleted
                        && (entry.Metadata.BaseType != null
                            ? typeof(ISoftDelete).IsAssignableFrom(entry.Metadata.BaseType.ClrType)
                            : entry.Entity is ISoftDelete)
                        && !entry.Metadata.IsOwned()
                        && entry.Metadata is not EntityType { IsImplicitlyCreatedJoinEntityType: true });

            foreach (var scopedEntry in changeTrackerEntries) {
                scopedEntry.Property(nameof(ISoftDelete.DateDeleted)).CurrentValue = DateTime.UtcNow;
                scopedEntry.State = EntityState.Modified;
            }
        }
    }
}
