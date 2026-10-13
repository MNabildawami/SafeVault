# Microsoft Copilot Review Prompts for SafeVault

Use these prompts in Microsoft Copilot while reviewing the actual code. Do not accept generated changes blindly; run tests after each change and record what you really used.

## 1. Input validation and SQL injection

> Review the SafeVault MVC and API controllers for SQL injection and input-validation issues. Trace the search parameter from the HTTP request through Entity Framework Core. Identify any raw SQL concatenation, missing length limits, or whitespace-only values. Suggest the smallest secure fix and explain how to test it. Do not invent files or claim a vulnerability exists unless it is visible in the provided code.

## 2. Authentication and authorization / RBAC

> Review SafeVault's ASP.NET Identity configuration, JWT bearer validation, and Admin policy. Check password policy, lockout, token signing key handling, issuer/audience/lifetime validation, role claim mapping, and whether notes are scoped to the authenticated owner. Report specific findings with file and method names. Do not weaken authorization to make a test pass.

## 3. XSS and CSRF debugging

> Review every view and state-changing MVC action in SafeVault for stored/reflected XSS and CSRF. Confirm whether Razor output is encoded and whether POST actions require anti-forgery validation. Check that no user-controlled value reaches Html.Raw or executable script. Suggest a regression test for any issue you find.

## 4. Security test review

> Review the SafeVault.Tests test suite. Identify missing security cases for unauthorized API access, role restrictions, SQL injection-like search strings, oversized input, and stored XSS. Propose test cases that run against this project and explain what each test proves. Do not report tests as passing unless they have actually been executed.

## 5. Performance and maintainability

> Review SafeVault's EF Core queries and authentication code for unnecessary database round trips, unbounded result sets, and accidental disclosure of internal fields. Recommend safe changes that preserve owner filtering, validation, and authorization. Include a verification step for each recommendation.
