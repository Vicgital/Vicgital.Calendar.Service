# Vicgital.Calendar.Service

A gRPC microservice that serves calendar reference data — **Quarters**, **Weeks**, and
**Fortnights** — for the Vicgital LifeOS platform. It owns the definitions of these periods,
persists them in SQL Server, and exposes them (plus the ability to seed new years) over gRPC.

## Domain concepts

| Concept | Definition | Codes | Count/year |
|---|---|---|---|
| **Quarter** | Four 12-week (84-day) quarters starting from the first Monday of the year, plus a fifth "final" quarter (`QF`) covering the remainder of the year through the first Sunday of the following year. | `{year}.Q1`…`{year}.Q4`, `{year}.QF` | 5 |
| **Week** | Monday–Sunday chunks within a quarter. The last week of a quarter may be shorter than 7 days to land exactly on the quarter's end date. | `{quarterCode}.W{n}` (e.g. `2026.Q1.W1`) | varies by quarter |
| **Fortnight** | Two semi-monthly periods per month (around the 15th and month-end), modeling a biweekly pay period — end dates are shifted back to the nearest Friday if they'd otherwise fall on a weekend. | `{year}.{MM}.F1`/`F2` (e.g. `2026.03.F1`) | 24 |

All three are generated deterministically from pure date-math (`src/Vicgital.Calendar.Domain/Helpers/`) and persisted once created — they don't change after creation.

## Architecture

```
Vicgital.Calendar.Domain               Pure C#, zero dependencies. Entities + date-math helpers.
Vicgital.Calendar.Application          Components (business logic), caching decorators, DTOs.
Vicgital.Calendar.Infrastructure       Dapper repositories against SQL Server.
Vicgital.Calendar.Service.Definition   The .proto contract — published independently as a NuGet package.
Vicgital.Calendar.Service              The gRPC host (ASP.NET Core, Vicgital.Grpc).
```

Reads are cached in-memory (24h, per-entity, invalidated on create) since this data is
effectively immutable once seeded.

## Running locally

**Requirements**: .NET SDK version pinned in `global.json`, access to a SQL Server instance, and
the environment variables below.

```
SQLDB_SERVER=<hostname>
SQLDB_USERNAME=<username>
SQLDB_PASSWORD=<password>
```

```bash
dotnet run --project src/Vicgital.Calendar.Service
```

By default the service listens on **port `50051`**, plaintext HTTP/2 (no TLS) — override with the
`Grpc:Port`/`Grpc:Host` config keys. gRPC server reflection is **off by default**; enable it with
`Grpc:EnableReflection=true` (already on in `appsettings.dev.json`) if you want to browse the API
with Postman, `grpcurl`, or similar without a local copy of the `.proto`. A standard gRPC health
check endpoint is always available.

## Consuming the service

### 1. Add the contract package

The gRPC contract (generated client + messages) is published as a NuGet package to a private
GitHub Packages feed on every push to `main`/`develop`.

```xml
<PackageReference Include="Vicgital.Calendar.Service.Definition" Version="3.0.0" />
```

Point your `nuget.config` at the feed:

```xml
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/vicgital/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="%GH_PACKAGE_USERNAME%" />
      <add key="ClearTextPassword" value="%GH_PACKAGE_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

(`GH_PACKAGE_TOKEN` needs at least `read:packages` scope on the `vicgital` org.)

### 2. Open a channel

The service is plaintext HTTP/2 (h2c) — no TLS — so the client needs the unencrypted-support
switch set before opening the channel:

```csharp
using Grpc.Net.Client;
using Vicgital.Calendar.Service.Definition;

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

using var channel = GrpcChannel.ForAddress("http://<host>:50051");
var client = new Calendar.CalendarClient(channel);
```

### 3. Available RPCs

Every "by id or code" request (`QuarterRequest`, `WeekRequest`, `FortnightRequest`) uses a proto
`oneof identifier { int32 id = 1; string code = 2; }` — set exactly one.

| RPC | Request | Response | Notes |
|---|---|---|---|
| `GetQuarter` | `QuarterRequest` | `QuarterModel` | By id or code |
| `GetQuartersByYear` | `YearRequest` | `QuartersReply` | All 5 quarters for the year |
| `GetQuarterByDate` | `DateRequest` | `QuarterModel` | Quarter containing the date |
| `CreateQuartersByYear` | `YearRequest` | `QuartersReply` | Seeds a year; `AlreadyExists` if already seeded |
| `GetWeek` | `WeekRequest` | `WeekModel` | By id or code |
| `GetWeeksByQuarter` | `QuarterRequest` | `WeeksReply` | All weeks in a quarter |
| `GetWeekByDate` | `DateRequest` | `WeekModel` | Week containing the date |
| `CreateWeeksByQuarter` | `CreateWeeksByQuarterRequest` | `WeeksReply` | Seeds weeks for a quarter (by `quarterCode`) |
| `GetFortnight` | `FortnightRequest` | `FortnightModel` | By id or code |
| `GetFortnightsByYear` | `YearRequest` | `FortnightsReply` | All 24 fortnights for the year |
| `GetFortnightByDate` | `DateRequest` | `FortnightModel` | Fortnight containing the date |
| `CreateFortnightsByYear` | `YearRequest` | `FortnightsReply` | Seeds a year; `AlreadyExists` if already seeded |

Dates on the wire are `google.type.Date` (`{ int32 year, month, day }`), never strings.

### 4. Example calls

```csharp
using Google.Type;

// Look up by code
var quarter = await client.GetQuarterAsync(new QuarterRequest { Code = "2026.Q1" });

// Look up by id
var week = await client.GetWeekAsync(new WeekRequest { Id = 42 });

// Look up by date
var fortnight = await client.GetFortnightByDateAsync(new DateRequest
{
    Date = new Date { Year = 2026, Month = 3, Day = 15 }
});

// Seed a year's worth of quarters, then that quarter's weeks
var quarters = await client.CreateQuartersByYearAsync(new YearRequest { Year = 2027 });
foreach (var q in quarters.Quarters)
    await client.CreateWeeksByQuarterAsync(new CreateWeeksByQuarterRequest { QuarterCode = q.Code });
```

### 5. Error handling

Failures come back as standard gRPC status codes — catch `RpcException` and switch on
`ex.StatusCode`:

| Status | Meaning |
|---|---|
| `NotFound` | No entity matches the given id/code/date |
| `AlreadyExists` | `Create*` called for a year/quarter that's already seeded |
| `InvalidArgument` | Request failed validation (e.g. neither id nor code set) |

```csharp
try
{
    var quarter = await client.GetQuarterAsync(new QuarterRequest { Code = "2026.Q1" });
}
catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
{
    // no quarter with that code yet
}
```

## Testing

```bash
dotnet test test/Vicgital.Calendar.Domain.Tests -c Release
```

Domain date-math (`QuarterHelper`, `WeekHelper`, `FortnightHelper`) is covered by theory tests
across a wide year range, asserting structural invariants (no gaps/overlaps, correct code
patterns, continuity across year boundaries) rather than a handful of hand-picked dates.

## Docker

```bash
docker build -f src/Vicgital.Calendar.Service/Dockerfile -t vicgital-calendar-service .
docker run -p 50051:50051 \
  -e SQLDB_SERVER=... -e SQLDB_USERNAME=... -e SQLDB_PASSWORD=... \
  vicgital-calendar-service
```

## CI/CD

On every push to `main`: builds the solution, publishes `Vicgital.Calendar.Service.Definition` to
the private NuGet feed, and builds/pushes the Docker image — all via shared composite actions in
`vicgital/cicd`.
