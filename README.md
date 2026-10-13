# SafeVault — Full-Stack Application Security Capstone

SafeVault is a learning project built with **ASP.NET Core MVC, ASP.NET Core Identity, Entity Framework Core, SQLite, and JWT bearer authentication**. It demonstrates input validation, safe database access, authentication, role-based authorization (RBAC), XSS/CSRF defenses, and security verification tests.

> **Important:** This is an educational starter project, not a production security certification. Review and test it in your own environment before publishing or using real sensitive data.

## Project structure

```text
SafeVault/
├── SafeVault.sln
├── src/SafeVault.Web/
│   ├── Controllers/          # MVC, admin, and token-protected API endpoints
│   ├── Data/                 # EF Core Identity DbContext and model configuration
│   ├── Models/               # Data entities, DTOs, and validated request models
│   ├── Services/              # JWT token service
│   ├── Views/                 # Razor UI; normal output is HTML-encoded
│   ├── wwwroot/css/           # Local styles; no third-party runtime script required
│   ├── Program.cs             # Authentication, authorization, headers, rate limits
│   └── appsettings.json       # Non-secret configuration only
├── tests/SafeVault.Tests/     # Integration and validation tests
└── docs/                      # Threat notes, Copilot prompts, and work-log template
```

## Security controls implemented

- **Input validation:** data annotations, server-side validation, length limits, normalization, and rejection of whitespace-only notes.
- **SQL injection prevention:** queries use EF Core LINQ and parameterized values; user input is never concatenated into raw SQL.
- **Authentication:** ASP.NET Identity handles password hashing, registration, login, lockout after repeated failed attempts, and cookie authentication.
- **RBAC:** the `User` and `Admin` roles are created on startup; the admin dashboard requires the `CanViewAdminDashboard` policy.
- **JWT API authentication:** `/api/auth/token` issues short-lived bearer tokens. The API validates the signature, issuer, audience, and token lifetime.
- **Authorization and ownership:** vault operations return only notes belonging to the current user.
- **XSS defense:** Razor's normal text rendering HTML-encodes saved note content. Do not change this to `Html.Raw` for user-submitted content.
- **CSRF defense:** state-changing MVC forms use anti-forgery tokens and POST requests.
- **Defense in depth:** security response headers, HTTPS redirection/HSTS in non-development environments, and a fixed-window rate limit for registration/login/token requests.
- **Secret handling:** JWT and optional administrator bootstrap values are read from environment variables or user-secrets, never hard-coded in source control.
- **CI:** a GitHub Actions workflow restores the solution and runs the security tests for pushes and pull requests.

## Requirements

- .NET 8 SDK
- A browser

## Run locally (PowerShell)

From the project root, generate a development-only JWT signing key. It is not stored in the repository:

```powershell
$env:Jwt__Key = [Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet restore SafeVault.sln
dotnet run --project src/SafeVault.Web
```

Open `https://localhost:7044` (accept/trust the local ASP.NET Core development certificate if prompted). The SQLite database `safevault.db` is created on first run. Register a user in the UI to explore the protected vault.

### Optional administrator account

To bootstrap an administrator account on the first run, set a unique email and strong password **before starting the application**. The account is created only if that email does not already exist; existing accounts are not automatically promoted.

```powershell
$env:BootstrapAdmin__Email = "your-admin-email@example.test"
$securePassword = Read-Host "Choose a strong administrator password" -AsSecureString
$env:BootstrapAdmin__Password = [System.Net.NetworkCredential]::new("", $securePassword).Password
dotnet run --project src/SafeVault.Web
```

Keep these values private. Do not put them in `appsettings.json`, source code, screenshots, or Git commits. Clear them from the current PowerShell session after startup if no longer needed:

```powershell
Remove-Item Env:BootstrapAdmin__Email -ErrorAction SilentlyContinue
Remove-Item Env:BootstrapAdmin__Password -ErrorAction SilentlyContinue
Remove-Item Env:Jwt__Key -ErrorAction SilentlyContinue
```

## Run tests

```powershell
dotnet test SafeVault.sln
```

The automated tests cover anonymous API rejection, admin policy metadata, Identity password hashing, Razor HTML encoding, injection-like search input, and model validation. Run the suite locally and review any failures before reporting the results in your submission.

## API overview

### Obtain a JWT

`POST /api/auth/token` with JSON:

```json
{
  "email": "registered-user@example.test",
  "password": "YourStrongPassword!123"
}
```

A successful response contains `accessToken`, `tokenType`, and `expiresInSeconds`. Keep the token private and use it only over HTTPS.

### Read the authenticated user's notes

`GET /api/vaultnotes`

Optional search: `GET /api/vaultnotes?q=meeting`

Send `Authorization: Bearer <accessToken>`. The API returns only notes that belong to the authenticated user. Search input is passed through EF Core parameters rather than interpreted as SQL. Invalid or missing JWTs receive an unauthorized response.

### Create a note

`POST /api/vaultnotes` with `Authorization: Bearer <accessToken>` and JSON:

```json
{
  "title": "Project notes",
  "content": "Remember to review the security test results."
}
```

The API enforces model validation and ownership server-side. In MVC responses and API responses, note content is treated as plain text; it is not trusted HTML.

## GitHub submission checklist

1. Create a **public** repository named `SafeVault` on GitHub. Leave the option to initialize with a README unchecked because this project already includes one.
2. Extract/copy this folder and push its contents (including `SafeVault.sln`, `src`, `tests`, and `docs`). Never include the SQLite `.db` file, JWT signing secret, admin password, or user-secrets file.
3. Run `dotnet restore SafeVault.sln` and `dotnet test SafeVault.sln` on your computer; fix any local errors before submission.
4. Open the public repository in a private/incognito browser window to make sure the files are visible.
5. Submit the URL and a truthful reflection on how you actually used Microsoft Copilot. Suggested prompts and a work-log template are included under `docs/`.

## Development note

For quick local demonstration, the sample creates tables on first run with `EnsureCreated`. Before a production deployment, use reviewed EF Core migrations, enforce HTTPS at the hosting layer, configure a proper secret manager, use a production database, add monitoring/backup/incident-response controls, and conduct a security review.
