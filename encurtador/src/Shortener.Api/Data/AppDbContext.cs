using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Shortener.Api.Data.Entities;

namespace Shortener.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<Link> Links => Set<Link>();
    public DbSet<AccountToken> AccountTokens => Set<AccountToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<Link>(entity =>
        {
            entity.HasKey(link => link.Id);
            entity.Property(link => link.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(link => link.Code).IsUnique();
            entity.Property(link => link.TargetUrl).IsRequired();
            entity.HasOne(link => link.Owner).WithMany().HasForeignKey(link => link.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(link => link.OwnerId);
        });
        builder.Entity<AccountToken>(entity =>
        {
            entity.HasKey(token => token.TokenHash);
            entity.Property(token => token.Type).HasMaxLength(16).IsRequired();
            entity.HasOne(token => token.User).WithMany().HasForeignKey(token => token.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(token => new { token.UserId, token.Type });
        });
        builder.Entity<AppUser>().Property(user => user.Email).IsRequired();
    }
}
