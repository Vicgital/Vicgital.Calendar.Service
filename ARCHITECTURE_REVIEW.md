# Vicgital.Calendar.Service — Architecture & Code Review

*Reviewed 2026-07-26. Scope: full solution (`src/`, `database/`, solution/build files). This is the first microservice in a planned Home Lab / LifeOS platform, so recommendations lean toward what will pay off as more services are added and Kubernetes enters the picture.*

## Overall Impression

This is a solid first microservice. The layering is genuinely correct (not just labeled correctly), the domain logic for date math is isolated and dependency-free, and the choice to ship the gRPC contract as its own versioned NuGet package (`Vicgital.Calendar.Service.Definition`) is a mature decision that will scale well across many microservices. The main gaps are the kind you'd expect from a "v1, still wiring things up" project: zero automated tests, some copy-pasted boilerplate, an unfinished feature (Fortnight) that's wired inconsistently, and a couple of real bugs in the gRPC error-handling path. None of this is a redesign — it's tightening.

---

## What You Did Right

### 1. Clean Architecture dependency direction is actually respected
`Domain` has **zero** package or project references — it's pure C#. `Application` depends only on `Domain`. `Infrastructure` depends on `Application`+`Domain`. `Service` (the gRPC host) depends on all of them. That's the textbook dependency rule, and a lot of "Clean Architecture" projects get this wrong by letting Infrastructure types leak inward. Yours doesn't.

### 2. Domain logic lives in the Domain layer
`QuarterHelper.BuildQuartersByYear` and `WeekHelper.BuildWeeksByQuarter` (`src/Vicgital.Calendar.Domain/Helpers/`) encode real business rules (how a "quarter" and "week" are defined for your calendar) as pure, dependency-free functions on Domain entities. This is exactly where that logic belongs — not in a repository, not in the gRPC service. It's also the most complex code in the solution, so it's good that it's the most isolated and testable.

### 3. The gRPC contract is decoupled into its own package
`Vicgital.Calendar.Service.Definition` is versioned and published independently (`PackageId`, `Version`, `RepositoryUrl` metadata are already filled in). This means future consumers (other microservices, a client SDK) can take a dependency on the contract without pulling in your server implementation. That's exactly the right shape for a microservice contract in a growing system.

### 4. Modern, consistent project hygiene
Every project targets `net10.0` with `Nullable` and `ImplicitUsings` enabled, and you're using primary constructors throughout (`FortnightComponent(IFortnightRepository repository)`, etc.) instead of boilerplate constructor+field assignment. Repositories use parameterized Dapper queries everywhere — no string-concatenated SQL, so no injection risk.

### 5. Sensible separation between the "server" and the "seeder"
Splitting `Vicgital.Calendar.Setup` (a console app that pre-generates quarters/weeks) from `Vicgital.Calendar.Service` (the long-running gRPC host) is a good call — it keeps one-off data-generation logic out of the service's request path and out of its container image concerns.

---

## Bugs Worth Fixing

### 🔴 `RpcException` thrown for bad input gets swallowed and rewritten as "Internal Server Error"

In `CalendarService.cs`, several methods validate `Id`/`Code` a **second** time inside the `try` block (the first check already happened via `request.Validate()`), and throw `RpcException(StatusCode.InvalidArgument, ...)` when neither is present:

```csharp
// GetQuarter, GetWeek, GetWeeksByQuarter
else
    throw new RpcException(new Status(StatusCode.InvalidArgument, "Either Quarter ID or Code must be provided."));
```

This throw happens **inside** the `try`, so it's caught by the generic `catch (Exception ex)` right below it, fails the `ex is NotFoundException` check, and gets rewritten into:

```csharp
throw new RpcException(new Status(StatusCode.Internal, "An error occurred while fetching the quarter."));
```

So a client that sends a genuinely bad request (no id, no code) gets told `Internal` / "an error occurred" instead of `InvalidArgument` with the actual reason. Two fixes, pick one:
- Since `Validator.Validate(request)` already guards against this exact case before the `try` even starts, the redundant `if/else if/else throw` inside the try blocks is dead code — delete it and just branch on `Id > 0` vs. `Code`.
- If you keep any manual `RpcException` throws inside a try/catch, add `catch (RpcException) { throw; }` before the generic `catch (Exception)` so intentional statuses pass through untouched.

### 🟡 Duplicate validation, two different mechanisms

`Validator.Validate(request)` (in `Helpers/Validator.cs`) already enforces "must have Id or Code" for `QuarterRequest`/`WeekRequest`. Then `CalendarService` re-implements the identical check inline in the method body. Beyond causing the bug above, it means the rule is defined in two places that can drift. Keep the check in `Validator` only, and let the method bodies just branch on which one was supplied.

---

## Design Inconsistencies

### The Fortnight feature bypasses your own layering

For Quarter and Week, the call chain is `CalendarService → I*Component → I*Repository`, exactly as intended. For Fortnight, `CalendarService` injects `IFortnightRepository` **directly** (`Implementation/CalendarService.cs:16`) and skips the Application/Component layer entirely:

```csharp
public class CalendarService(
    ILogger<CalendarService> logger,
    IWeekComponent weekComponent,
    IQuarterComponent quarterComponent,
    IFortnightRepository fortnightRepository   // ← presentation layer talking straight to persistence
    ) : Definition.Calendar.CalendarBase
```

Right now this is harmless because `IFortnightComponent`, `IFortnightRepository`, `FortnightComponent`, and `FortnightRepository` are all completely empty stubs, and the three `GetFortnight*` RPCs just fall through to `base.GetFortnight(...)` (which returns gRPC `Unimplemented` — a fine placeholder). But the injected-and-unused `fortnightRepository` field is a trap for future-you: when someone implements Fortnight, the path of least resistance is to keep using the repository directly from the gRPC layer, quietly breaking the pattern everywhere else. Swap it for `IFortnightComponent` now (even though it's empty) so the layering stays consistent, and delete the unused repository injection.

There's also no `FortnightHelper` — Quarter and Week both have their date-math encoded in the Domain layer; Fortnight has no equivalent yet, and the `Setup` console app doesn't seed fortnights either. Worth deciding: is a "fortnight" derived from weeks (e.g., pairs of weeks) or independently defined? That business rule needs to live in Domain like the other two before the repository/component layers can be filled in meaningfully.

### `Mapper.cs` is an empty, unused stub

`Service/Helpers/Mapper.cs` is a completely empty static class. Nothing calls it. Meanwhile, every RPC method in `CalendarService` hand-rolls the same Domain→proto mapping inline, 6 times:

```csharp
return new QuarterModel
{
    Id = quarter.Id,
    Code = quarter.Code,
    StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
    EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
};
```

This is clearly where `Mapper.cs` was *meant* to live. Turning these into extension methods would cut real duplication:

```csharp
public static class Mapper
{
    public static QuarterModel ToProto(this Quarter quarter) => new()
    {
        Id = quarter.Id,
        Code = quarter.Code,
        StartDate = quarter.StartDate.ToString("MM/dd/yyyy"),
        EndDate = quarter.EndDate.ToString("MM/dd/yyyy")
    };
}
```
then every call site becomes `return quarter.ToProto();`.

### DTOs mirror Domain entities exactly, with no behavioral difference

`CalendarBaseDTO` (Application) and `CalendarEntityBase` (Domain) have identical shapes — `Id`, `Code`, `StartDate`, `EndDate` — except the DTO uses `DateTime` where the Domain entity uses `DateOnly`. Every `*DTO` class then exists solely to convert `DateOnly ↔ DateTime` on the way in and out of the repository (`fortnight.StartDate.ToDateTime(new TimeOnly())` / `DateOnly.FromDateTime(...)`).

Two ways to simplify:
1. **Preferred:** Since the SQL columns are already `DATE` and Dapper/`Microsoft.Data.SqlClient` support `DateOnly` mapping directly (recent Dapper + a small `SqlMapper.TypeHandler<DateOnly>`, or ADO.NET's native `DateOnly` support in current drivers), have the repositories work with `DateOnly` throughout and drop the DTOs entirely — return Domain entities straight from the repository. That removes an entire parallel class hierarchy and ~6 mapping methods for zero loss of information.
2. If you want to keep a persistence-model/domain-model split on principle (reasonable if you expect the DB shape to diverge from the domain shape later), that's a legitimate choice — but then the naming should say so. `MapToDTO`/`MapFromDTO` on the DTO type reads backwards (`MapFromDTO` actually returns the *domain* entity, not the DTO). Consider `ToEntity()`/`FromEntity()` naming instead so the direction is unambiguous at the call site.

Also worth noting: `QuarterDTO`/`FortnightDTO` use `new TimeOnly()` while `QuarterComponent` uses `TimeOnly.MinValue` for the same "midnight" concept — same value, two spellings. Pick one.

---

## Code Duplication to DRY Up

- `ServiceCollectionExtension.cs` in `Vicgital.Calendar.Service` and `Vicgital.Calendar.Setup` are **byte-for-byte identical**, including the private `GetSqlConnectionString()` method and its `Environment.GetEnvironmentVariable("SQLDB_...")` calls with duplicated null-check error messages. Since you already have `Vicgital.Core.Configuration` as a shared internal package, this is the natural place to move `SetupServices`/`GetSqlConnectionString` into a small shared class (or a `Vicgital.Calendar.*` bootstrapping package) that both the service host and the setup console reference, rather than maintaining two copies by hand every time a dependency is added.
- The try/catch → gRPC status-code translation block is repeated in every single RPC method in `CalendarService.cs` (6 times, identical shape: log, map `NotFoundException` → `NotFound`, else → `Internal`). This is the textbook use case for a gRPC **server interceptor**: implement `Grpc.Core.Interceptors.Interceptor`, override `UnaryServerHandler`, catch exceptions once, map `NotFoundException` → `NotFound`, `ArgumentException`/`BusinessRuleViolationException` → `InvalidArgument`/`FailedPrecondition`, everything else → `Internal`. Register with `builder.Services.AddGrpc(o => o.Interceptors.Add<ExceptionTranslationInterceptor>())`. This deletes ~40 lines of repeated boilerplate and — importantly — fixes the swallowed-`RpcException` bug above in one place instead of six.
- Every `.csproj` repeats the identical `<TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable>` block. A root `Directory.Build.props` would set these once for every project in the solution.
- Package versions are pinned independently per project and have already drifted: `Grpc.Tools 2.83.0` vs `Grpc.AspNetCore 2.80.0` vs `Grpc.Core 2.46.6`. A root `Directory.Packages.props` with Central Package Management (`<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`) gives you one place to bump versions across every project as this turns into a multi-service solution.

---

## Testing: the biggest gap

The solution file has an empty `/test/` folder placeholder, but there is **no test project at all** — zero unit tests, zero integration tests. For most of this codebase that's a minor gap, but `QuarterHelper.BuildQuartersByYear` / `WeekHelper.BuildWeeksByQuarter` are exactly the kind of date-math code that quietly breaks on edge cases (leap years, the "final quarter" boundary logic that only runs when `startDate.Month == 12`, year boundaries). Right now the only way you'd find out `DetermineFinalQuarterEndDate` throws for some future year is by running the `Setup` console app against that year and seeing it crash.

Recommend, in priority order:
1. Add a `test/Vicgital.Calendar.Domain.Tests` project (xUnit) and write theory-based tests for `QuarterHelper`/`WeekHelper` across a wide range of years (2020–2100), asserting invariants like "quarters cover the full year with no gaps/overlaps" and "every quarter's weeks sum to exactly the quarter's date range."
2. A thin test project for `CalendarService` using `Grpc.Core.Testing`/mocked components to lock in the status-code-mapping behavior (this would have caught the swallowed-exception bug immediately).
3. Repository tests are lower priority until there's a real integration-test story (e.g., Testcontainers for SQL Server) — not worth blocking on for a homelab project, but worth knowing it's the next tier once this grows.

---

## Kubernetes / Production Readiness

Since this is explicitly headed for Kubernetes, a few things will matter sooner rather than later:

- **No Dockerfile.** There's nothing in the repo yet to containerize the service. Worth adding a multi-stage `Dockerfile` (`mcr.microsoft.com/dotnet/sdk:10.0` build stage → `mcr.microsoft.com/dotnet/aspnet:10.0` runtime stage) before this becomes urgent across multiple services.
- **`Kestrel:EndpointDefaults:Protocols` is `Http2` only** (`appsettings.json`). This forces *every* endpoint — including the plain-text `app.MapGet("/", ...)` "is it running" route in `Program.cs` — onto HTTP/2. A plain HTTP/1.1 `GET` (which is what most naive tools, and Kubernetes' `httpGet` probe type, send) will not work against an HTTP/2-only Kestrel endpoint without prior knowledge/ALPN negotiation. If that `/` route is meant to be a liveness/readiness check, it likely won't behave the way you expect once deployed.
- **No health checks in the gRPC sense either.** Kubernetes 1.24+ supports a native `grpc` probe type that speaks the standard gRPC Health Checking Protocol. Adding `Grpc.HealthCheck` (and optionally `Grpc.AspNetCore.Server.Reflection` for `grpcurl`/Postman introspection during development) would give you a real liveness/readiness signal instead of the HTTP `/` route, and is the idiomatic way to health-check a gRPC-only service in k8s.
- **No CI workflow yet** — `.github/workflows/` exists but is empty. Given the multi-service direction, even a minimal `dotnet build`/`dotnet test` workflow now will save pain once there are several of these services to keep green.
- **No `global.json`** pinning the SDK version. Minor, but worth doing once you have more than one machine (or a CI runner) building this — avoids drift between whatever SDK happens to be installed locally vs. in CI/containers.

---

## Smaller Notes

- **`calendar.proto`**: `StartDate`/`EndDate` are transmitted as `string` formatted `MM/dd/yyyy` — a US-centric, ambiguous format on the wire (and `DateRequest.Date` is parsed with `DateOnly.Parse`/culture-sensitive `DateTime.TryParse` on the receiving ends, using two *different* parsing calls for what should be the same format). For a contract meant to be consumed by other services, prefer an unambiguous representation: either ISO-8601 strings (`yyyy-MM-dd`) parsed with `DateOnly.ParseExact(..., CultureInfo.InvariantCulture)`, or the well-known `google.type.Date` proto message, which removes string-formatting ambiguity entirely.
- **`calendar.proto`** also defines `QuarterCodeRequest` and `EmptyRequest` messages that are never referenced by any RPC — dead contract surface. `QuarterCodeRequest` additionally starts its field numbering at `2` instead of `1`, which is either a typo or a leftover from a previous edit.
- Consider a proto `oneof` for the recurring "either an int Id or a string Code" pattern (`QuarterRequest`, `WeekRequest`, `FortnightRequest`) instead of two loosely-related fields plus runtime `if (Id > 0) ... else if (!string.IsNullOrWhiteSpace(Code))` checks in three places. `oneof identifier { int32 id = 1; string code = 2; }` makes "exactly one of these" part of the contract instead of a convention enforced by hand in every handler.
- `Vicgital.Calendar.Service.Definition.csproj` references `Grpc.Core` (2.46.6) — the older, native-dependent gRPC C-core implementation that Google has put into maintenance mode in favor of grpc-dotnet (`Grpc.Net.Client`/`Grpc.AspNetCore`, which the `Service` project already correctly uses). Since `Service.Definition` is shipped as a standalone NuGet package that other services/clients will depend on, pulling in `Grpc.Core` means every consumer also pulls in its native platform binaries for no benefit — the generated code only actually needs `Grpc.Core.Api` (lightweight, no native deps) plus `Google.Protobuf`.
- Repositories use `SELECT *` throughout. Dapper maps by column name so this won't break today (extra columns like `DateCreated` are simply ignored), but explicit column lists are more resilient to future schema changes and self-document what the query actually needs.
- ~~`WeekComponent.CreateWeeksByQuarter` throws `InvalidOperationException` for "already exists," while `QuarterComponent.CreateQuartersByYear` throws `BusinessRuleViolationException` for the same kind of condition. Worth standardizing on one exception type for "this business rule was violated" so a future gRPC interceptor (see above) can map it consistently.~~
- `QuarterComponent.CreateQuartersByYear` inserts quarters one at a time in a loop with no transaction — if insert *N* fails, quarters *1..N-1* are already committed with no rollback. Low risk today since it's only invoked from the single-threaded `Setup` console tool, but worth wrapping in a transaction if this ever becomes reachable from a live request path.

---

## If You Want a Short Punch List

1. ~~Fix the swallowed-`RpcException` bug in `CalendarService` (delete the redundant inline validation, or re-throw `RpcException` before the generic catch).~~
2. ~~Extract the exception→gRPC-status mapping into a single `Interceptor` instead of repeating it per method.~~
3. Decide Fortnight's design (component-mediated, like Week/Quarter) before implementing it, and remove the direct `IFortnightRepository` injection from `CalendarService`.
4. Stand up a Domain unit test project and cover `QuarterHelper`/`WeekHelper` across a range of years — this is your highest-value, lowest-effort testing investment.
5. ~~Either wire `Mapper.cs` up for real or delete it.~~
6. Add a `Dockerfile` and double check the HTTP/2-only Kestrel config against how you intend to probe this in Kubernetes.
