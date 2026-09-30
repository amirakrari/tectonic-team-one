# Hackathon Repository

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
3. Build the complete requested feature first. The current scope is a backend
   API using mock transactions, not a UI or a real KBC connection.
4. Keep the implementation in one .NET 10 project with EF Core SQLite,
   `EnsureCreated()`, and direct framework capabilities.
5. After implementation, run `dotnet build --configuration Release` from the
   API project directory and manually exercise the README's demo happy path.
   Do not substitute automated test infrastructure for the actual demo.

Null cockpit test commands mean tests are intentionally unavailable, not passing.
The build command becomes runnable once the API project exists.

Do not embed secrets or credentials in source code. Preserve unrelated work and
keep the README truthful about what is implemented and what has been verified.
