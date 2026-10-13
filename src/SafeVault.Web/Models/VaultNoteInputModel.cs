using System.ComponentModel.DataAnnotations;

namespace SafeVault.Web.Models;

public sealed class VaultNoteInputModel
{
    [Required, StringLength(120, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 1)]
    public string Content { get; set; } = string.Empty;
}
