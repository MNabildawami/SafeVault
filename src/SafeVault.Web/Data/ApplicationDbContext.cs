using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SafeVault.Web.Models;

namespace SafeVault.Web.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<VaultNote> VaultNotes => Set<VaultNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<VaultNote>(entity =>
        {
            entity.HasKey(note => note.Id);
            entity.Property(note => note.Title).HasMaxLength(120).IsRequired();
            entity.Property(note => note.Content).HasMaxLength(4000).IsRequired();
            entity.HasIndex(note => new { note.OwnerId, note.CreatedUtc });
            entity.HasOne(note => note.Owner)
                .WithMany()
                .HasForeignKey(note => note.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
