# CLAUDE.md

Guidance for Claude Code working in this repo. This file is written to be reusable across
Vicgital gRPC microservices — copy it into new service repos and adjust the "Project-specific"
section at the bottom.

## Stack

- .NET (version pinned in `global.json`), C#, gRPC via `Grpc.AspNetCore` + the shared
  `Vicgital.Grpc` package (hosting, interceptors, health checks, reflection).
- SQL Server via Dapper, through `Vicgital.Data.Sql.Dapper`'s `IDapperQueryExecutor` — never
  raw `SqlConnection`/EF Core.
- `Vicgital.Application.Shared.Results` (`Result<T>` / `Error`) for anything that can fail in an
  expected way (not found, conflict). Don't throw exceptions for expected failure paths.
- `Vicgital.Core.Caching.Abstractions`/`.InMemory` for read-through caching.
- FluentValidation for request validation, run via `Vicgital.Grpc`'s `ValidationInterceptor`.
- xUnit v3 for tests. Central Package Management (`Directory.Packages.props`) — every
  `PackageReference` in a `.csproj` has no `Version` attribute; versions live in one place.

## Architecture: Clean Architecture, strictly one-directional

```
Domain          -> zero dependencies. Pure C#. Entities + static Helper classes for business rules.
Application     -> depends on Domain only. Components (I*Component), DTOs, cache decorators.
Infrastructure  -> depends on Application + Domain. Repositories (I*Repository) using Dapper.
*.Definition    -> the .proto contract. Its own NuGet package, versioned independently. Depends on nothing internal.
Service         -> the gRPC host. Depends on all of the above. Thin — no business logic here.
```

Never let a later layer's types leak into an earlier one (e.g. no `Grpc.Core` types in
`Application`, no proto types in `Domain`). If a gRPC service class is injecting a repository
directly, that's a layering bug — fix it, don't extend it.

## Patterns to follow

- **Components return `Result<T>`** for single-item lookups/creates that can meaningfully fail
  (`Error.NotFound(...)`, `Error.Conflict(...)`). Plain `Task<IReadOnlyList<T>>` is fine for
  list queries that can't fail in an interesting way.
- **Caching is a decorator**, not baked into the real component. `Cached{X}Component : I{X}Component`
  wraps the real `{X}Component` behind the same interface, keyed by id/code/date/etc., with a
  prefix-based `RemoveByPrefix` invalidation on `Create*` — and only invalidate on success
  (check `result.IsSuccess` first).
- **Repositories batch, never loop.** Existence checks use `WHERE Code IN @Codes`; multi-row
  inserts use one `INSERT ... OUTPUT INSERTED.*` with `Dapper.DynamicParameters`, not N separate
  round trips. Select explicit columns, never `SELECT *`.
- **The gRPC service class is a thin translator.** Branch on `request.IdentifierCase`, call the
  component, `.Unwrap()` the `Result<T>` (throws the right `RpcException`/`StatusCode` from
  `Error.Type`), map to proto via `Mapper.ToProto()` extension methods. No inline object
  initializers building proto messages field-by-field in the service class.
- **Proto conventions**: `oneof identifier { int32 id = 1; string code = 2; }` for the recurring
  "look this up by id or code" shape — never two loose optional fields. Dates are
  `google.type.Date` (via `Google.Api.CommonProtos`, `IncludeGoogleApiCommonProtos=true`), never
  formatted strings. Every request message gets a matching `FluentValidation` validator
  registered in DI, even if the RPC feels obvious — an unvalidated oneof falls through to a
  default branch silently.
- **The `.Definition` project is a real, independently-versioned NuGet package.** Bump `<Version>`
  before every push that changes the `.proto` — a breaking change (removed/renamed RPC or
  message, changed field type) is a major bump; additive is minor. CI rejects re-publishing an
  existing version, so check what's already published before assuming the current `<Version>` is
  safe to leave as-is.

## Build / test

```
dotnet build <Solution>.slnx -c Release
dotnet test  test/<Project>.Tests/<Project>.Tests.csproj -c Release
```

Domain logic (date math, business-rule helpers) gets tested with `[Theory]` + a shared year-range
`MemberData` (see `test/*/TestSupport/Years.cs` if present) asserting structural invariants — no
gaps, no overlaps, both within a period and across period boundaries — rather than a handful of
hand-picked example dates. Date-math bugs hide at specific boundary years; a broad range test
categorically catches classes of bug that spot-checks miss. When you fix a boundary bug, add a
dedicated regression test pinning the exact failing case in addition to the range test.

## House rules

- `RuntimeIdentifier`/`SelfContained` on the host project (if set) should be scoped to
  `Condition="'$(CI)' == 'true'"`, not unconditional — an unconditional RID breaks local
  `dotnet run` on a dev machine whose OS differs from the deploy target (native SNI/driver
  resolution fails silently).
- Don't add a package to a `.csproj` without a matching `PackageVersion` in
  `Directory.Packages.props` — CPM will fail the build otherwise. Remove the `PackageVersion`
  entry too when nothing references it anymore.
- Verify claims about external/shared packages (`Vicgital.Grpc`, `Vicgital.Data.Sql.*`, etc.) by
  actually decompiling or reflecting over the cached DLL when behavior is unclear — don't guess
  at what a shared package does from its name.
- Prefer fixing a real bug with a real, run test over a hand-traced explanation — if you can
  write a throwaway verification script (or a real test) to prove a fix works, do that instead of
  asserting confidence.

## Project-specific (fill in per repo)

- **Domain**: what this service's core entities/business rules represent.
- **Consumers**: who calls this service and how (see `README.md`).
- **Datastore**: connection details / required env vars.
