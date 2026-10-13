# SafeVault: Vulnerability Review and Fixes

This document maps common vulnerabilities to the defensive code paths in this sample. It is not a claim that a third-party penetration test has been completed.

| Risk / insecure pattern | Defensive implementation | How to verify |
|---|---|---|
| SQL injection from concatenating search text into SQL | Searches use EF Core LINQ predicates with parameterized values; no raw SQL string concatenation is used | Run `dotnet test SafeVault.sln`; inspect `VaultController` and `VaultNotesApiController` |
| Stored/reflected XSS from rendering user content as HTML | Razor renders note text with its default HTML encoder. No `Html.Raw` is used for stored note content | `RazorViewsEncodeUserSuppliedHtmlInsteadOfExecutingIt` |
| Invalid or oversized input | Data annotations, server-side trimming, length limits, and whitespace checks | `VaultNoteValidationRejectsOversizedAndBlankValues` |
| Unauthorized note access / insecure direct object reference | Queries include the current user's `OwnerId` condition on list, read, update, and delete operations | Review `VaultController` and API controller; try accessing another user's note ID |
| Unauthenticated API access | Vault API controller requires the JWT bearer scheme and rejects anonymous requests | `JwtApiRejectsAnonymousRequests` |
| Privileged operations available to all users | Admin dashboard uses the `CanViewAdminDashboard` authorization policy, which requires the `Admin` role | `AdminControllerRequiresAdminPolicy`; test as User and Admin |
| Plaintext password storage | ASP.NET Identity's password hasher is used; the application does not store or compare plaintext password columns | `IdentityStoresAHashRatherThanTheSubmittedPassword` |
| CSRF on cookie-authenticated state changes | MVC POST actions are decorated with `ValidateAntiForgeryToken`; form tag helpers emit form tokens | Remove the token from a POST and verify the request is rejected |
| JWT tampering / wrong issuer / wrong audience / expired token | JwtBearer `TokenValidationParameters` validates the signing key, issuer, audience, and lifetime | Send missing, altered, expired, or wrong-issuer tokens to `/api/vaultnotes` |
| Brute-force attempts on login/registration/token endpoint | Identity lockout and an in-process fixed-window request limiter | Repeatedly send invalid requests and check lockout / HTTP 429 behavior |
| Browser-side content risks | CSP and defensive HTTP response headers are set in middleware | Inspect response headers in browser dev tools or `curl -I` |
| Secrets accidentally committed | JWT key and optional bootstrap admin values are read from environment/configuration, not from tracked files; `.gitignore` excludes common secret/db files | Review Git history and repository settings before pushing |

## Remaining production considerations

- This sample uses a development-friendly `EnsureCreated` initialization flow. Use EF Core migrations and a reviewed deployment strategy for a real application.
- Rotate signing keys using a planned key-management process; do not copy the development key to production.
- Configure persistent data protection keys, HTTPS-only cookies, trusted proxy settings where needed, centralized logs, backup restoration drills, alerting, and dependency scanning before production.
- If real sensitive data is stored, add a documented retention/deletion policy, appropriate encryption at rest, and a privacy/security review.
