# Project Guidelines

## Scope

This workspace uses a 3-project .NET 9 solution for SQL Server deadlock handling with Polly + Dapper.

- Core library: src/DeadlockPolly.Core
- Demo app: src/DeadlockPolly.Demo
- Tests: tests/DeadlockPolly.Tests

Use this file as the single workspace instruction source (do not add AGENTS.md unless explicitly requested).

## Build and Test

Run commands from repository root:

- `dotnet build ManageDeadlockPolly.slnx`
- `dotnet test ManageDeadlockPolly.slnx`
- `dotnet run --project src/DeadlockPolly.Demo` (requires SQL Server)
- `docker-compose up --build` (recommended full demo flow)
- `docker-compose down -v` (clean reset)

Prefer solution-level build/test commands over individual project commands unless debugging a specific project.

## Architecture

Respect project boundaries:

- `src/DeadlockPolly.Core/RetryPolicies`: deadlock retry abstractions and Polly implementation
- `src/DeadlockPolly.Core/DataAccess`: connection provider + transactional executor
- `src/DeadlockPolly.Core/Extensions`: DI registration (`AddDeadlockRetryStack`)
- `src/DeadlockPolly.Core/Repositories`: demo repository and model
- `src/DeadlockPolly.Demo`: runtime demonstration scenarios
- `tests/DeadlockPolly.Tests`: unit tests (no live SQL dependency)

When adding features, keep deadlock policy logic and transaction orchestration separated.

## Conventions

- Use namespace root `DeadlockPolly.Core.*` in Core code.
- Inject interfaces (`ITransactionalExecutor`, `IDeadlockRetryPolicy`) instead of concrete types.
- Register dependencies via `AddDeadlockRetryStack(...)` unless there is a clear reason for granular registration.
- Keep tests deterministic and isolated (Moq + in-memory patterns); do not require SQL Server in unit tests.
- For SqlException deadlock tests, reuse helper in `tests/DeadlockPolly.Tests/Helpers/SqlExceptionHelper.cs`.

## Pitfalls and Guardrails

- Some legacy root-level files remain from an earlier monolithic layout; prefer `src/` and `tests/` structure for new work.
- README may lag behind current architecture; rely on docs/ and current code structure for implementation decisions.
- Demo/Docker connection strings include development-friendly settings (for example `TrustServerCertificate=true`); keep production hardening decisions explicit.

## Link, Don’t Embed

Do not duplicate long process docs inside code comments or new instruction files. Link to existing documentation:

- docs/00-START-HERE.md
- docs/QUICKSTART.md
- docs/ARCHITECTURE_SUMMARY.md
- docs/INTEGRATION_GUIDE.md
- docs/INTEGRATION_CHECKLIST.md
- docs/MANUAL_TEST_GUIDE.md
- docs/INDEX.md
