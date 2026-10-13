# Capstone Submission Summary (Personalize Before Submitting)

**Project title:** SafeVault — Full-Stack Application Security

**GitHub URL:** [Paste your public SafeVault repository URL here]

SafeVault demonstrates server-side input validation, SQL injection prevention using Entity Framework Core parameterized queries, ASP.NET Identity authentication, role-based authorization, and JWT-protected API endpoints. It also applies Razor HTML encoding for user-generated note content, anti-forgery validation for cookie-authenticated POST actions, owner-scoped data access, request rate limiting for authentication endpoints, and defensive response headers.

During my Microsoft Copilot review, I used prompts to examine input validation and database query patterns, verify Identity/JWT and RBAC configuration, and review XSS and CSRF protections. I compared Copilot's suggestions with the actual source code, applied only changes that fit the application's security requirements, and used automated tests to check key behaviors. **Before submitting, replace this paragraph with the findings, changes, and test results from your own Copilot work log; do not claim tests passed unless you ran them.**

The project includes automated tests for unauthenticated API rejection, admin authorization metadata, Identity password hashing, HTML encoding of note content, injection-like search input, and model validation. I ran `dotnet restore SafeVault.sln` and `dotnet test SafeVault.sln` locally and recorded the actual output: [fill in the date and real result here].
