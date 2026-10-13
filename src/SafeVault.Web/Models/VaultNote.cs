using System.ComponentModel.DataAnnotations;

namespace SafeVault.Web.Models;

public sealed class VaultNote
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public ApplicationUser? Owner { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; set; }
}
