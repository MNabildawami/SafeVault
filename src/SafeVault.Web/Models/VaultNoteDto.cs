namespace SafeVault.Web.Models;

public sealed record VaultNoteDto(int Id, string Title, string Content, DateTime CreatedUtc, DateTime? UpdatedUtc);
