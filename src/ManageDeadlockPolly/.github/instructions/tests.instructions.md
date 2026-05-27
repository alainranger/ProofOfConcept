---
applyTo: "tests/DeadlockPolly.Tests/**/*.cs"
description: "Use when creating or modifying xUnit tests in DeadlockPolly.Tests, including Moq-based tests and SqlException deadlock scenarios (1205)."
---

# Test Instructions

## Scope

These instructions apply only to C# test files in `tests/DeadlockPolly.Tests`.

## Test Framework and Style

- Use xUnit with `[Fact]` unless parameterization is clearly needed (`[Theory]`).
- Use Moq for collaborators (`Mock<T>`) and keep tests deterministic.
- Prefer method naming pattern: `Method_Condition_ExpectedResult`.
- Keep each test focused on one behavior/assertion path.

## Isolation Rules

- Do not require live SQL Server in unit tests.
- Do not use Docker or external dependencies for unit tests.
- Avoid timing-sensitive assertions when possible.

## Deadlock-Specific Rules

- For deadlock simulation, always use `tests/DeadlockPolly.Tests/Helpers/SqlExceptionHelper.cs`.
- Simulate SQL deadlock using `SqlException` number `1205` only via the helper.
- Test both success-after-retry and max-retries-exhausted paths.

## Project Conventions

- Prefer interfaces in tests (`ITransactionalExecutor`, `IDeadlockRetryPolicy`, `IDbConnectionProvider`).
- Keep namespaces under `DeadlockPolly.Tests.*`.
- Align test data and options with defaults in `DeadlockRetryPolicyOptions` unless the test explicitly validates custom values.

## Validation

Run from repository root:

- `dotnet test ManageDeadlockPolly.slnx`

If narrowing scope while iterating:

- `dotnet test tests/DeadlockPolly.Tests/DeadlockPolly.Tests.csproj`
