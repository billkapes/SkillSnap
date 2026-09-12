using Microsoft.EntityFrameworkCore;
using SkillSnap.Models;
using Microsoft.EntityFrameworkCore.Design;

namespace SkillSnap;

public class SkillSnapContextFactory : IDesignTimeDbContextFactory<SkillSnapContext>
{
    public SkillSnapContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SkillSnapContext>();
        optionsBuilder.UseSqlite("Data Source=SkillSnap.db");
        return new SkillSnapContext(optionsBuilder.Options);
    }
}

public class SkillSnapContext : DbContext
{
    public SkillSnapContext(DbContextOptions<SkillSnapContext> options)
        : base(options)
    {
    }

    public DbSet<PortfolioUser> PortfolioUsers => Set<PortfolioUser>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Skill> Skills => Set<Skill>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PortfolioUser>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Bio).HasMaxLength(2000);
            entity.Property(p => p.ProfileImageUrl).HasMaxLength(500);

            entity.HasMany(p => p.Projects)
                .WithOne(p => p.PortfolioUser)
                .HasForeignKey(p => p.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(p => p.Skills)
                .WithOne(p => p.PortfolioUser)
                .HasForeignKey(p => p.PortfolioUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Title).IsRequired().HasMaxLength(200);
            entity.Property(p => p.Description).HasMaxLength(2000);
            entity.Property(p => p.ImageUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Name).IsRequired().HasMaxLength(100);
            entity.Property(s => s.Level).IsRequired().HasMaxLength(50);
        });
    }
}
