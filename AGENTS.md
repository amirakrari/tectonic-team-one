# Hackathon Repository

This repository contains the API in `src/Tectonic.API` and the Blazor webapp in
`src/Tectonic.Web`. Apply the shared timebox below and the project-specific
guidance to its respective project. Existing authentication and validation are
implemented features; merging the projects does not authorize removing them.

## Hackathon Timeboxed Mode

This repository is a short-lived hackathon prototype with only a few hours
available. Delivery speed and a working demonstration take precedence over
production-grade assurance.

- Do not create automated tests unless explicitly requested.
- Do not run test suites during implementation.
- Do not use Red/Green/Refactor or incremental verification gates.
- Do not create implementation plans, ADRs, abstractions, migrations, or
  supporting infrastructure unless immediately necessary for the demo.
- Implement the complete requested vertical feature before verification.
- Prefer ASP.NET Core Minimal APIs, direct framework capabilities, and the
  smallest coherent implementation.
- Avoid speculative extensibility, compatibility layers, broad error taxonomies,
  provider matrices, benchmarking, and unrelated cleanup.
- After the complete feature is written, run one final build and manually exercise
  the demonstrated happy path. Fix only defects blocking the demo.
- Do not claim production readiness, security hardening, scalability, or complete
  test coverage.

## Working in This Repository

1. Read this `AGENTS.md`, `README.md`, and relevant `docs/internal/` documentation
   when that directory exists.
2. Recognize this explicit timeboxed mode and load only compatible cockpit
   guidance. This repository policy overrides conflicting cockpit requirements
   for TDD, phased test gates, elaborate plans, and production architecture.
3. Build the complete requested feature first. The API uses mock transactions,
   not a real KBC connection; the webapp is a separate project in this repository.
4. Keep the API implementation in its .NET 10 project with EF Core SQLite,
   `EnsureCreated()`, and direct framework capabilities.
5. After implementation, run `dotnet build --configuration Release` from the
   API project directory and manually exercise the README's demo happy path.
   Do not substitute automated test infrastructure for the actual demo.

Null cockpit test commands mean tests are intentionally unavailable, not passing.
The build command becomes runnable once the API project exists.

Do not embed secrets or credentials in source code. Preserve unrelated work and
keep the README truthful about what is implemented and what has been verified.
## Webapp guidance: `src/Tectonic.Web`

> **Lifecycle**: Hackathon prototype (< 4 hours total, ~2.5 hours coding).
> **Goal**: Working demo of the happy path. Speed over everything.
> **Scope**: These rules apply to the Blazor Server webapp. The API is the separate `src/Tectonic.API` project in this repository.

## Hackathon Timeboxed Mode

This repository is a short-lived hackathon prototype. Delivery speed and a working demonstration take absolute precedence over production-grade assurance, architectural purity, or code quality.

### Suspended Practices

- Do NOT create automated tests of any kind.
- Do NOT run test suites during implementation.
- Do NOT use Red/Green/Refactor or incremental verification gates.
- Do NOT create implementation plans, ADRs, context files, or task files.
- Do NOT use CQRS, MediatR, or the command/query pattern.
- Do NOT add new authentication, authorization, or validation flows unless requested.
  Preserve the existing webapp demo login and the API's Identity/JWT implementation.
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
