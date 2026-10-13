namespace SafeVault.Web.Models;

public sealed class VaultIndexViewModel
{
    public string? SearchTerm { get; init; }
    public IReadOnlyList<VaultNote> Notes { get; init; } = Array.Empty<VaultNote>();
}
