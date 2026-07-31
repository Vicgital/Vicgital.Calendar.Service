# Vicgital.Calendar.Service — Architecture & Code Review

*Reviewed 2026-07-26. Scope: full solution (`src/`, `database/`, solution/build files). This is the first microservice in a planned Home Lab / LifeOS platform, so recommendations lean toward what will pay off as more services are added and Kubernetes enters the picture.*

*Round 2 addendum added 2026-07-28, after several Round 1 items were fixed and a shared `Vicgital.Grpc` package was extracted. See [Round 2 Review](#round-2-review--2026-07-28) at the bottom for what changed and what's new.*

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
3. ~~Decide Fortnight's design (component-mediated, like Week/Quarter) before implementing it, and remove the direct `IFortnightRepository` injection from `CalendarService`.~~ *(`CalendarService` now injects `IFortnightComponent`; the component itself is still an empty stub — see Round 2.)*
4. Stand up a Domain unit test project and cover `QuarterHelper`/`WeekHelper` across a range of years — this is your highest-value, lowest-effort testing investment. **Still not done — still the top priority.**
5. ~~Either wire `Mapper.cs` up for real or delete it.~~
6. Add a `Dockerfile` and double check the HTTP/2-only Kestrel config against how you intend to probe this in Kubernetes. **Still not done.**

---

## Round 2 Review — 2026-07-28

*Scope: `Vicgital.Calendar.Service` at commit `f347c2e` ("Code Refactor and Enhancements"), plus the new `Vicgital.Grpc` shared package it now depends on (`C:\Git\Vicgital\Vicgital.Grpc`, commit `f7de780`). This round focuses on what changed since Round 1, since most of that section still applies unchanged.*

### What Actually Got Fixed

- **Hosting model corrected.** `Program.cs` now uses `WebApplication.CreateBuilder` (via a new `VicgitalGrpcService.CreateWebApplicationBuilder` helper in `Vicgital.Grpc`) instead of the old `Host.CreateDefaultBuilder` + `ConfigureWebHostDefaults` combination that couldn't actually expose `MapGrpcService`/`MapGet`.
- **Exception→status mapping extracted for real**, into `Vicgital.Grpc.Interceptors.ExceptionHandlerInterceptor`, registered once via `AddGrpc(o => o.Interceptors.Add<...>())` instead of repeated per-method try/catch. Round 1's swallowed-`RpcException` bug is fully gone — `catch (RpcException) { throw; }` is now the first catch clause.
- **Validation unified.** The manual `Helpers/Validator.cs` + duplicated inline `Id`/`Code` checks are gone, replaced by FluentValidation validators (`Validators/*.cs`) resolved and run automatically by `Vicgital.Grpc.Interceptors.ValidationInterceptor` before any service method executes. Round 1's "duplicate validation, two mechanisms" issue is resolved at the root, not just patched.
- **`Mapper.cs` is real** — every RPC now calls `.ToProto()` instead of hand-rolling the same object initializer six times.
- **Fortnight layering fixed** — `CalendarService` now injects `IFortnightComponent`, not `IFortnightRepository` directly. (The component is still an empty stub, which is fine — that was always tracked as a separate, larger "design Fortnight" task, not a layering bug.)
- **Reflection + real gRPC health checks** are now wired up via `MapVicgitalGrpcEndpoints()`, using the standard gRPC Health Checking Protocol instead of nothing.

This is good progress, and notably it was accomplished by pulling the cross-cutting pieces (hosting, interceptors, health/reflection) out into a shared `Vicgital.Grpc` package rather than just patching them locally — the right move given more services are coming.

### 🔴 New Bug: `NotFoundException`/`BusinessRuleViolationException` no longer map to the correct gRPC status — this is a regression

`ExceptionHandlerInterceptor` in `Vicgital.Grpc` was deliberately written with **no dependency on `Vicgital.Application.Shared`** — it only special-cases `ArgumentException` → `InvalidArgument`; everything else, including `RpcException`'s catch-all sibling, falls through to `Internal`:

```csharp
private RpcException ToRpcException(Exception ex, ServerCallContext context, object? request = null)
{
    if (ex is ArgumentException)
    {
        _logger.LogWarning(ex, "{Method} - invalid argument ({Request})", context.Method, request);
        return new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
    }

    _logger.LogError(ex, "{Method} - unhandled exception ({Request})", context.Method, request);
    return new RpcException(new Status(StatusCode.Internal, "An unexpected error occurred."));
}
```

That's the correct architectural call for the generic package (a transport-plumbing library shouldn't hard-depend on your app's exception taxonomy — this is exactly the tradeoff discussed when `Vicgital.Grpc` was being designed). But **nothing replaced the mapping it removed.** `QuarterComponent`/`WeekComponent` still throw `NotFoundException` from `Vicgital.Application.Shared.Exceptions` on every not-found lookup (`GetQuarter`, `GetWeek`, `GetQuarterByDate`, `GetWeekByDate`, `GetWeeksByQuarter`). Today, a client asking for a quarter that doesn't exist gets `StatusCode.Internal` / "An unexpected error occurred" instead of `StatusCode.NotFound` — worse behavior than Round 1's original bug, not better, even though the code causing it is architecturally cleaner.

Concretely: **`request.Id = 99999` on `GetQuarter` now returns `Internal`, not `NotFound`.**

Fix: build the small adapter package/registration discussed earlier (a `Vicgital.Grpc`-consuming piece — could be as small as one extra interceptor or a pluggable exception-mapper registered from `Vicgital.Calendar.Service` itself) that maps `NotFoundException` → `NotFound`, `BusinessRuleViolationException` → `FailedPrecondition`, etc., and slot it into the interceptor pipeline alongside (before) `ExceptionHandlerInterceptor`. Until then, this is the most user-visible bug in the service — worse than "Internal" being merely unhelpful, it's actively misleading (every 404-shaped case looks like a server crash).

### 🔴 New Bug: date validation and date parsing disagree with each other

`Validators/DateRequestValidator.cs` validates with `DateTime.TryParse(date, out _)`, but the actual usage in `CalendarService` parses with `DateOnly.Parse(request.Date)`:

```csharp
// DateRequestValidator — what determines "valid"
.Must(date => DateTime.TryParse(date, out _))

// CalendarService.GetQuarterByDate / GetWeekByDate — what actually runs
DateOnly.Parse(request.Date)
```

`DateOnly.Parse` is stricter than `DateTime.TryParse` (e.g. a string with a time component parses fine as a `DateTime` but throws `FormatException` from `DateOnly.Parse`). Since `FormatException` isn't `ArgumentException`, it isn't special-cased by `ExceptionHandlerInterceptor` either — it also falls through to `Internal`. So the validator gives a false sense of safety: some inputs that pass validation still crash the request path afterward. Fix: validate with the same parser you consume with — `DateOnly.TryParse(date, out _)` in the validator — so "passed validation" and "will parse downstream" are actually the same claim.

### 🟡 Dead config: `Kestrel:EndpointDefaults:Protocols` in `appsettings.json`

Kestrel is now configured entirely in code by `VicgitalGrpcService.CreateWebApplicationBuilder` (`ConfigureKestrel` + `ListenAnyIP(port, o => o.Protocols = HttpProtocols.Http2)`), which never reads the `Kestrel` config section. `appsettings.json`'s `"Kestrel": { "EndpointDefaults": { "Protocols": "Http2" } }` block is now inert — it looks like it's controlling the protocol, but changing it would do nothing. Worth deleting so nobody loses time "fixing" it later.

### 🟡 The plaintext `/` route is now both redundant and still broken for its apparent purpose

`Program.cs` still has `app.MapGet("/", () => "Calendar Service is running!")` alongside the new `MapVicgitalGrpcEndpoints()` (which adds a real gRPC health check). The `/` route is no longer needed for basic liveness signaling now that a proper health check exists, and — as noted in Round 1 — it still won't respond to a plain HTTP/1.1 `GET` (e.g. a naive Kubernetes `httpGet` probe) since the Kestrel endpoint is HTTP/2-only. Either delete it, or if it's meant as a human-friendly smoke-test URL, that's fine, but don't rely on it for anything infrastructure-facing — use the gRPC health check for that.

### Everything Else From Round 1 — Still Open, Unchanged

These weren't touched in this pass and the Round 1 write-up still applies as-is:
- `Vicgital.Calendar.Service.Definition.csproj` still references the legacy `Grpc.Core` (2.46.6) instead of just `Grpc.Core.Api`, forcing native deps on every consumer of the published contract package.
- Package version drift (`Grpc.Tools 2.83.0` / `Grpc.AspNetCore 2.80.0` / `Grpc.Core 2.46.6`) — still no `Directory.Packages.props`/CPM.
- No `Directory.Build.props` — `TargetFramework`/`Nullable`/`ImplicitUsings` still repeated per `.csproj`.
- `GetSqlConnectionString()` is still byte-for-byte duplicated between `Vicgital.Calendar.Service` and `Vicgital.Calendar.Setup`'s `ServiceCollectionExtension.cs`.
- DTO/Domain duplication remains (`QuarterDTO`/`WeekDTO`/`FortnightDTO` mirror the Domain entities exactly except `DateTime` vs `DateOnly`), and `MapFromDTO`/`MapToDTO` naming still reads backwards.
- `TimeOnly.MinValue` (in the Components) vs `new TimeOnly()` (in the DTOs) — same value, still two spellings.
- Repositories still use `SELECT *` throughout.
- `QuarterComponent.CreateQuartersByYear` still loops inserts with no transaction.
- No test project anywhere in the solution — `QuarterHelper`/`WeekHelper` are still untested. **This is still the single highest-value gap**, and it's now compounded: `Vicgital.Grpc`'s interceptor pipeline (validation ordering, exception mapping) is shared, untested infrastructure that every future service will inherit bugs from — the `NotFoundException` regression above is exactly the kind of thing a test suite around the interceptor pipeline would have caught before it shipped.
- Still no `Dockerfile`, no `global.json`. The only CI workflow (`.github/workflows/publish_package.yml`) packs and pushes `Vicgital.Calendar.Service.Definition` on every push/PR to `main` — there is still no `dotnet build`/`dotnet test` gate for the service itself.
- `calendar.proto` still has the unreferenced `QuarterCodeRequest`/`EmptyRequest` messages (with `QuarterCodeRequest`'s field numbering starting at `2`), no `oneof` for the recurring Id-or-Code pattern, and dates are still `MM/dd/yyyy` strings.

### Updated Punch List

1. **Fix the `NotFoundException`/`BusinessRuleViolationException` → `Internal` regression** — highest priority, it's a live behavioral regression, not a style nit.
2. Fix `DateRequestValidator` to validate with `DateOnly.TryParse`, matching the parser actually used downstream.
3. Stand up a Domain unit test project (`QuarterHelper`/`WeekHelper`) — still not done, still the top structural gap. Consider adding a small test project for `Vicgital.Grpc`'s interceptor pipeline too, now that it's shared infrastructure.
4. Delete the dead `Kestrel:EndpointDefaults` config block and decide whether to keep or drop the plaintext `/` route.
5. Swap `Grpc.Core` for `Grpc.Core.Api` in `Vicgital.Calendar.Service.Definition.csproj`.
6. Add a `Dockerfile`, a `dotnet build`/`dotnet test` CI workflow, and a `global.json`.

---

## Round 3 Review — 2026-07-31

*Scope: full solution at commit `d240de7` ("Service refactor"). This round focuses on what changed since Round 2, since most of Round 1/2 still applies unchanged where noted.*

### What Actually Got Fixed

- **The `NotFoundException` → `Internal` regression is gone** — but fixed differently than Round 2 suggested, and arguably better. Rather than adding an exception-mapping interceptor, the components now return `Result<Quarter>`/`Result<Week>` (`Error.NotFound(...)` on a miss) instead of throwing, and `Helpers/ResultExtensions.Unwrap()` maps `ErrorType.NotFound → StatusCode.NotFound` directly in `CalendarService`, before `Vicgital.Grpc`'s generic interceptor ever sees it. This sidesteps the whole "should the shared gRPC package know about this service's exception taxonomy" tension Round 2 flagged — worth keeping as the pattern going forward instead of building the adapter interceptor that was proposed.
- **`Grpc.Core` → `Grpc.Core.Api`** in `Vicgital.Calendar.Service.Definition.csproj` — done, no more native deps forced on consumers of the contract package.
- **Dead `Kestrel:EndpointDefaults` config removed** — `appsettings.json` is now just `{}`.
- **The plaintext `/` route is gone** from `Program.cs` — the gRPC health check (`MapVicgitalGrpcEndpoints()`) is the only liveness signal now, which is the correct one for a gRPC-only service.
- **Dead proto messages removed** — `QuarterCodeRequest`/`EmptyRequest` are gone from `calendar.proto`.
- **`Directory.Build.props` and `Directory.Packages.props` (CPM) added** — the repeated `TargetFramework`/`Nullable`/`ImplicitUsings` block and the package-version drift Round 1/2 flagged are both resolved.
- **`SELECT *` replaced with explicit columns** in `QuarterRepository`/`WeekRepository` — but see the new bug below, this refactor wasn't clean.
- **`context.CancellationToken` now forwarded** on every `CalendarService` call site.
- **A caching layer was added and immediately promoted to a shared package** (`Vicgital.Core.Caching.Abstractions` / `Vicgital.Core.Caching.InMemory`), following the same "pull cross-cutting concerns into a shared package" pattern that worked well for `Vicgital.Grpc`. `CachedQuarterComponent`/`CachedWeekComponent` decorate the real components behind the existing `IQuarterComponent`/`IWeekComponent` interfaces (registered via factory delegates in `ServiceCollectionExtension`), with prefix-based invalidation on the rare `Create*` calls. This is the right shape and a good structural precedent for future services.
- **Dockerfile + real CI added** — multi-stage-free single-stage `Dockerfile` on `aspnet:10.0-noble-chiseled`, and `.github/workflows/main.yml` now builds, publishes the contract package, and builds/pushes the Docker image via the shared `vicgital/cicd` composite actions.

### 🔴 New Bug: `WeekRepository.GetWeekByDateAsync` is missing its `FROM` clause — `GetWeekByDate` is broken for every caller

The `SELECT *` cleanup dropped the `FROM` clause on this one query:

```csharp
// WeekRepository.cs
public async Task<WeekDTO?> GetWeekByDateAsync(DateTime date, CancellationToken cancellationToken = default)
{
    return await _dapper.QueryFirstOrDefaultAsync<WeekDTO?>(
        @"SELECT 
             [Id]
            ,[QuarterId]
            ,[Code]
            ,[StartDate]
            ,[EndDate] WHERE [EndDate] >= @Date AND [StartDate] <= @Date", new { Date = date }, cancellationToken: cancellationToken);
}
```

There's no `FROM [dbo].[Week]` — compare with the sibling `QuarterRepository.GetQuarterByDate`, which correctly kept its `FROM [dbo].[Quarter] WHERE ...`. This compiles fine (it's just a string) but every call to the `GetWeekByDate` RPC will fail at the database with a SQL syntax/invalid-column error. This isn't an edge case or an admin-only path — it's one of the six live, client-facing RPCs, and it's been broken since the most recent commit (`d240de7`). This is the highest-priority fix in this round.

**Fix:**
```csharp
@"SELECT 
     [Id]
    ,[QuarterId]
    ,[Code]
    ,[StartDate]
    ,[EndDate]
    FROM [dbo].[Week] 
    WHERE [EndDate] >= @Date AND [StartDate] <= @Date"
```
This is exactly the kind of regression a thin `WeekRepository` integration test (or even a smoke test hitting a real/local SQL instance) would have caught immediately — see the testing note below, which is now more urgent, not less.

### 🟡 Minor: leftover unused `PackageVersion` entries

`Directory.Packages.props` still declares `Microsoft.Extensions.Caching.Memory` and `Microsoft.Extensions.DependencyInjection.Abstractions` — left over from when the caching layer briefly lived in this repo as a local project. Now that it's the external `Vicgital.Core.Caching.InMemory` package, nothing in the solution references either package directly anymore. Safe to delete both lines.

### 🟡 Minor: inconsistent defensive check in `WeekRepository`

`WeekRepository.GetWeekAsync(int id)` throws `ArgumentOutOfRangeException` if `id <= 0`; no other repository method (`GetWeekAsync(string code)`, either `QuarterRepository` overload) has an equivalent guard, and `CalendarService` only ever calls the int overload when `request.Id > 0`, so this is dead code today. Not a bug, just an inconsistency — either drop it or apply the same guard pattern consistently across repositories.

### Everything Else From Round 1/2 — Still Open, Verified Unchanged

- ~~`DateRequestValidator` still validates with `DateTime.TryParse` while `CalendarService.GetQuarterByDate`/`GetWeekByDate` parse with the stricter `DateOnly.Parse`.~~ *(Fixed same day, see addendum below — the mismatch is gone because dates are no longer strings at all.)*
- **No test project anywhere** — `test/` is still an empty placeholder in the `.slnx`. This is now the most consequential gap in the repo: the `WeekRepository` bug above is precisely the failure mode a repository/integration test suite exists to catch, and it shipped straight past code review into what would've been production.
- **Fortnight is still a complete stub** (`FortnightComponent`, `IFortnightComponent`, `IFortnightRepository`, `FortnightRepository`, `Fortnight` entity are all empty; `CalendarService`'s three Fortnight RPCs still just `// TODO` + fall through to `base.*` → `Unimplemented`). No `FortnightHelper` exists yet either. Tracked correctly as a deliberate "not yet designed" gap, not a bug.
- **DTO/Domain duplication remains** — `QuarterDTO`/`WeekDTO`/`FortnightDTO` still mirror their Domain entities exactly except `DateTime` vs `DateOnly`, and `MapFromDTO`/`MapToDTO` naming still reads backwards.
- **`TimeOnly.MinValue` (Components) vs `new TimeOnly()` (DTOs)** — same value, still two spellings.
- **`ServiceCollectionExtension.cs` is still byte-for-byte duplicated** between `Vicgital.Calendar.Service` and `Vicgital.Calendar.Setup`, including `GetSqlConnectionString()`.
- ~~`QuarterComponent.CreateQuartersByYear`/`WeekComponent.CreateWeeksByQuarter` still loop `SELECT` + `INSERT` per item with no transaction.~~ *(Fixed 2026-07-31 — batched into one existence-check query and one multi-row insert each; see `TECH_DEBT.md`.)*
- **No `RuntimeIdentifier` anywhere** — already tracked in `TECH_DEBT.md` as the one remaining Docker-image-size item; still unaddressed.
- **No `global.json`.**
- ~~`calendar.proto` still has no `oneof` for the recurring Id-or-Code pattern, and dates are still culture-sensitive `MM/dd/yyyy` strings.~~ *(Fixed same day, see addendum below.)*
- **CI still has no test job** — `.github/workflows/main.yml` literally has `## TODO: Add a test job here` above the `docker-publish` job. Given the bug above, this is the gap that would have actually caught it.
- No `FluentValidation` validator exists yet for `FortnightRequest`/`MonthRequest` — harmless while those RPCs are unimplemented, but worth remembering to add (`Id > 0 || Code`, `1 <= Month <= 12`) once Fortnight gets built out, following the existing `QuarterRequestValidator`/`WeekRequestValidator`/`YearRequestValidator` pattern.

### Updated Punch List

1. **Fix the missing `FROM [dbo].[Week]` clause in `WeekRepository.GetWeekByDateAsync`** — live bug, breaks a real RPC, highest priority.
2. ~~Fix `DateRequestValidator` to validate with `DateOnly.TryParse`, matching the parser actually used downstream.~~ *(Done — see addendum.)*
3. Stand up a test project — at minimum `QuarterHelper`/`WeekHelper` unit tests and a `WeekRepository`/`QuarterRepository` integration test (Testcontainers or a local SQL instance) that would catch exactly the class of bug found this round. Wire it into the CI workflow's empty test-job placeholder.
4. ~~Delete the two unused `PackageVersion` entries (`Microsoft.Extensions.Caching.Memory`, `Microsoft.Extensions.DependencyInjection.Abstractions`) from `Directory.Packages.props`.~~ *(Done.)*
5. ~~Set `RuntimeIdentifier`/CI publish flags (tracked in `TECH_DEBT.md`) and add `global.json`.~~ *(Done — see addendum.)*
6. When Fortnight design work starts: define the business rule in `Domain` (like `QuarterHelper`/`WeekHelper`), then fill in the repository/component/DTO layers and add the matching request validators.

---

### Addendum — same day, 2026-07-31: `oneof` and `google.type.Date` adopted

Two of the Round 3 "still open" items were addressed immediately after the review:

- **`calendar.proto` now uses `oneof identifier { int32 id = 1; string code = 2; }`** on `QuarterRequest`, `WeekRequest`, and `FortnightRequest` — field numbers unchanged, so this is wire-compatible with the old two-independent-fields shape. `CalendarService` and the request validators now branch on `request.IdentifierCase` instead of `Id > 0`.
- **Dates are now `google.type.Date` instead of `MM/dd/yyyy` strings**, via the `Google.Api.CommonProtos` package (`IncludeGoogleApiCommonProtos=true` in `Vicgital.Calendar.Service.Definition.csproj`, imported as `google/type/date.proto`). This is a genuine wire-format break, so `Vicgital.Calendar.Service.Definition`'s `Version` was bumped `1.0.0 → 2.0.0`. `Helpers/Mapper.cs` gained `DateOnly.ToProtoDate()`/`Date.ToDateOnly()` extensions used at every mapping site; the Domain/Application layers are untouched since they still work in `DateOnly` throughout — only the presentation-layer (de)serialization changed.
- **This incidentally fixed Round 3 punch-list item #2** (`DateRequestValidator` vs. `DateOnly.Parse` mismatch): there's no string parsing left on this path at all. `DateRequestValidator` now validates the `Date` message's `Year`/`Month`/`Day` form a real calendar date directly (`BeAValidDate`), so "passed validation" and "will work downstream" can no longer diverge.

Not addressed by this change: the `WeekRepository.GetWeekByDateAsync` missing-`FROM`-clause bug (punch-list item #1) is independent of the wire format and still needs fixing.

### Addendum 2 — same day, 2026-07-31: `RuntimeIdentifier` and `global.json`

- **`global.json`** added, pinned to the locally-installed `10.0.302` with `rollForward: latestFeature` — strict enough to catch an accidental major/minor SDK drift, loose enough that CI's `dotnetVersion: '10.0.x'` resolution (which may land on a different feature band) still satisfies it.
- **`RuntimeIdentifier`**: turns out `vicgital/cicd`'s `dotnetBuild` action runs a bare `dotnet publish ... --no-restore` — no `-r`/`--self-contained` flags, and no inputs to add them — so passing this through CI wasn't an option without editing that separate repo. Set directly in `Vicgital.Calendar.Service.csproj` instead, guarded by `Condition="'$(CI)' == 'true'"` (GitHub Actions sets `CI=true` on every runner) so local `dotnet build`/`dotnet run` on Windows stay RID-agnostic — an unconditional `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>` would have broken `Microsoft.Data.SqlClient`'s native SNI resolution locally. Verified both directions: local build unaffected, and `CI=true dotnet publish` produces no `runtimes/` folder at all — the linux-x64 native SNI assets flatten straight into the output root instead of shipping all four platforms.
