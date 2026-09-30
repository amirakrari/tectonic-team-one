# AGENTS.md: Hackathon Repository (webapp)

> **Lifecycle**: Hackathon prototype (< 4 hours total, ~2.5 hours coding).
> **Goal**: Working demo of the happy path. Speed over everything.
> **Scope**: This repo is the Blazor Server webapp only. The API (Minimal API + SQLite, rules, emails) is a separate project.

## Hackathon Timeboxed Mode

This repository is a short-lived hackathon prototype. Delivery speed and a working demonstration take absolute precedence over production-grade assurance, architectural purity, or code quality.

### Suspended Practices

- Do NOT create automated tests of any kind.
- Do NOT run test suites during implementation.
- Do NOT use Red/Green/Refactor or incremental verification gates.
- Do NOT create implementation plans, ADRs, context files, or task files.
- Do NOT use CQRS, MediatR, or the command/query pattern.
- Do NOT implement authentication or authorization.
- Do NOT implement input validation.
- Do NOT add observability, structured logging, or telemetry.
- Do NOT create abstractions, interfaces, or extension points unless immediately needed for the feature to work. (`IApiClient` is the one exception. It exists so the UI can run on `MockApiClient` while the API is being built.)
- Do NOT spend time on error handling beyond what crashes the demo.
- Do NOT refactor, clean up, or rename unless it unblocks progress.

### Required Practices

- Use Blazor Server (global InteractiveServer, prerender off) with MudBlazor defaults.
- Keep everything in a single project: `src/Tectonic.Web`.
- Do NOT put business rules in the UI. The API decides when notifications fire.
- Call the API only through `IApiClient`. When you add a method, implement it in both `ApiClient` and `MockApiClient`.
- Keep `Models/` in sync with the API contract in `docs/TECHNICAL.md` section 2.
- Implement the complete requested feature before any verification.
- After the complete feature is written, run ONE final `dotnet build`.
- Manually exercise the demo happy path. Fix ONLY defects that block the demo.

### Still Applies

- Do NOT embed secrets or credentials in source code.
- Do NOT claim production readiness.
- Keep the README accurate about what the project does and how to run it.
