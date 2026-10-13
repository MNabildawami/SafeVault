using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace SafeVault.Tests;

public sealed class SafeVaultFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _keeper;

    public SafeVaultFactory()
    {
        var connectionString = $"Data Source=SafeVaultTests_{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keeper = new SqliteConnection(connectionString);
        _keeper.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // The keeper connection keeps this unique in-memory database alive while EF opens scoped connections.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "SafeVault-Integration-Test-Key-At-Least-32-Bytes-Long!",
                ["Jwt:Issuer"] = "SafeVault.Tests",
                ["Jwt:Audience"] = "SafeVault.Tests",
                ["ConnectionStrings:DefaultConnection"] = _keeper.ConnectionString
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _keeper.Dispose();
        }
    }
}
