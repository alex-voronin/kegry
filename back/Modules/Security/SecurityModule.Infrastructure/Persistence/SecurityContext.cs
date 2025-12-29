using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates.RefreshToken;
using SecurityModule.Domain.Aggregates.User;

namespace SecurityModule.Infrastructure.Persistence;

public class SecurityContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
{
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    
    public SecurityContext(DbContextOptions<SecurityContext> options) : base(options)
    {
    }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("security");
        
        builder.Entity<User>()
            .Property(u => u.Id)
            .ValueGeneratedNever();
        
        builder.Entity<IdentityRole<Guid>>()
            .Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Entity<RefreshToken>()
            .HasIndex(t => t.Token)
            .IsUnique();
        builder.Entity<RefreshToken>()
            .HasIndex(t => t.Jti)
            .IsUnique();
        
        builder.Entity<RefreshToken>()
        .HasOne<User>()             
        .WithMany()
        .HasForeignKey(rt => rt.UserId) 
        .OnDelete(DeleteBehavior.Cascade);
        
        base.OnModelCreating(builder);
    }
}