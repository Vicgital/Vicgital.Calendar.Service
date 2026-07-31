# Tech Debt

Tracked improvements that are safe to defer but shouldn't be forgotten. Each item notes the concrete evidence found in the repo, the fix, and rough effort/risk.

## Docker image size

- [ ] **No `RuntimeIdentifier` is set anywhere in the repo, so publish output is portable/RID-agnostic.**
  `Microsoft.Data.SqlClient`'s native SNI binaries get published for all four platforms at once (`runtimes/win-x86`, `runtimes/win-x64`, `runtimes/win-arm64`, `runtimes/unix`), even though the service only ever runs in a Linux container.
  **Fix:** set `<RuntimeIdentifier>linux-x64</RuntimeIdentifier>` (framework-dependent, not self-contained) on the service project, or pass `-r linux-x64 --self-contained false` in the CI publish step.
  Effort: low. Risk: low (verify the `dotnetBuild`/`dotnetDockerBuildAndPush` composite actions in `vicgital/cicd` don't assume a portable/RID-less publish).

- [x] **No `SatelliteResourceLanguages` is set.**
  Every localized dependency (`Microsoft.Data.SqlClient`, MSAL, Azure Identity, etc.) ships resource DLLs for ~13 cultures (`de`, `cs`, `es`, `fr`, `ja`, `ko`, `tr`, `zh-Hant`, `pt-BR`, `ru`, `zh-Hans`, `pl`, `it`), confirmed present in the build output.
  **Fix:** add `<SatelliteResourceLanguages>en</SatelliteResourceLanguages>` to `Directory.Build.props` so it applies repo-wide.
  Effort: low. Risk: none — no localized UI strings are served by this gRPC service.

- [x] **Confirm the CI publish step builds `Release`, not `Debug`, before it gets copied into the Docker image.**
  Local `bin/Debug` output was used to inspect the payload; if the `dotnetBuild` composite action already publishes `Release` this is a non-issue, but it's worth confirming since Debug output carries extra debug/PDB weight.
  Effort: trivial (verification only).

- [x] **EF Core assemblies (`Microsoft.EntityFrameworkCore(.Relational/.SqlServer).dll`) end up in the published output, but nothing in this service uses EF Core** — all repositories (`QuarterRepository`, `WeekRepository`, `FortnightRepository`) go through `IDapperQueryExecutor` from `Vicgital.Data.Sql`.
  **Action:** find which internal package (`Vicgital.Data.Sql`, `Vicgital.Core.Configuration`, etc.) pulls in EF Core transitively and see if it can be split so services that only need Dapper don't pay for it.
  Effort: medium (requires changes in a shared internal package, out of scope for this repo alone).

- [x] Base image: already on `mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled`, which is the right call and accounts for the bulk of the size difference vs. the non-chiseled tag — no action needed here.

## Performance

- [x] **`context.CancellationToken` from the gRPC `ServerCallContext` is never forwarded to component/repository calls.**
  In `Implementation/CalendarService.cs`, every call site (e.g. `GetQuarter`, `GetWeek`, `GetQuartersByYear`, `GetWeeksByQuarter`, `GetQuarterByDate`, `GetWeekByDate`) omits `context.CancellationToken`, even though every component and repository method already accepts a `CancellationToken cancellationToken = default`. If a client cancels or times out, the SQL query keeps running server-side instead of being aborted, wasting DB connections/CPU under load.
  **Fix:** pass `context.CancellationToken` through on every call in `CalendarService`.
  Effort: low, mechanical change across ~8 call sites.

- [x] **No caching layer for reference data that is effectively immutable.**
  Quarters/weeks/fortnights are calendar structures tied to fixed date ranges — once created they don't change. Every lookup (`GetQuarter`, `GetWeek`, `GetWeekByDate`, ...) still round-trips to SQL Server on every call, including repeated lookups of the same quarter/week.
  **Fix:** add a read-through `IMemoryCache` (or hybrid cache) in front of the repositories, keyed by id/code/date, with a long or no expiration (data doesn't change) and manual invalidation on the rare `Create*` calls.
  Effort: medium. High payoff — likely turns most requests into in-memory lookups.

- [x] **All Dapper queries use `SELECT *`** (`QuarterRepository`, `WeekRepository`) instead of naming the columns actually mapped onto `QuarterDTO`/`WeekDTO`.
  Minor I/O cost today given the table width, but it also silently changes behavior if columns are ever added/reordered on the table.
  **Fix:** list explicit columns in each query.
  Effort: low.

- [ ] **Sequential per-row DB round trips in the seeding loops** (`QuarterComponent.CreateQuartersByYear`, `WeekComponent.CreateWeeksByQuarter`): for each item, one `SELECT` to check existence followed by one `INSERT`, done serially in a `foreach`.
  Not a hot path (admin/seed operation), but for a full year/quarter it's N*2 sequential round trips where one existence-check query (`WHERE Code IN (...)`) plus a single multi-row insert would do.
  **Fix:** batch the existence check and insert.
  Effort: low-medium. Low priority since this isn't on the request-serving path.
