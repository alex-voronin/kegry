using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using Shared.Infrastructure.Persistence.Audit;

namespace SecurityModule.Infrastructure.Persistence;

public class SecurityContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>, IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } 
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public SecurityContext(DbContextOptions<SecurityContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new AuditableInterceptor());
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("security");

        modelBuilder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<IdentityRole<Guid>>()
            .Property(r => r.Id)
            .ValueGeneratedNever();

        modelBuilder.Entity<RefreshToken>(builder =>
        {
            builder.HasIndex(t => t.Token)
                .IsUnique();
            builder.HasIndex(t => t.Jti)
                .IsUnique();
            builder.Property(x => x.Token)
                .IsRequired()
                .HasMaxLength(40);
            builder.Property(x => x.Jti)
                .IsRequired()
                .HasMaxLength(40);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}