using HonestTimeTracker.Domain;
using Microsoft.EntityFrameworkCore;

namespace HonestTimeTracker.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<WorkTask> Tasks => Set<WorkTask>();
    public DbSet<WorkRecord> Records => Set<WorkRecord>();
    public DbSet<TfsCollection> TfsCollections => Set<TfsCollection>();
    public DbSet<AppSettings> Settings => Set<AppSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WorkTask>().ToTable("Tasks");
        modelBuilder.Entity<WorkRecord>().ToTable("Records");

        modelBuilder.Entity<Project>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkTask>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<WorkRecord>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TfsCollection>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<WorkRecord>()
            .HasOne(r => r.Task)
            .WithMany(t => t.Records)
            .HasForeignKey(r => r.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.TfsCollection)
            .WithMany(c => c.Projects)
            .HasForeignKey(p => p.TfsCollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppSettings>().HasData(
            new AppSettings { Id = 1, DbFilePath = string.Empty, DailyWorkHours = 8.0 });
    }
}
