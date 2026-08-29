# The Unified Service Scheduler

A dealership vehicle-service appointment scheduler: customers book a service appointment for
a vehicle, service type, dealership, and time. The system validates the requested Technician
and Service Bay, checks availability against existing bookings, and confirms the appointment —
safely, even when two people try to book the same slot at the same time.

## Table of Contents

- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Building](#building)
  - [Running the Application](#running-the-application)
  - [Database Migrations](#database-migrations)
  - [Testing](#testing)
- [Deployment](#deployment)
  - [A note on the database](#a-note-on-the-database)
  - [Secrets and connection strings](#secrets-and-connection-strings)
  - [As build artifacts](#as-build-artifacts)
  - [As a Docker container](#as-a-docker-container)
- [CI/CD](#cicd)
  - [GitHub Actions](#github-actions)
  - [Dependabot](#dependabot)
- [AI Collaboration Narrative](#ai-collaboration-narrative)
  - [How I use AI](#how-i-use-ai)
  - [How I structured the requirement file](#how-i-structured-the-requirement-file)
  - [Using the C4 diagrams and data flow to structure and check the logic](#using-the-c4-diagrams-and-data-flow-to-structure-and-check-the-logic)
  - [How I verified and refined AI output](#how-i-verified-and-refined-ai-output)
  - [How I ensured final quality](#how-i-ensured-final-quality)

## Architecture

See [architecture.md](./architecture.md) for the full System Design Document — C4 diagrams,
data model, data flow, security, observability, technology choices, testing strategy, and
future evolution. [TASKS.md](./TASKS.md) tracks implementation progress task by task, if
you want to see how the project actually got built.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Docker, if you want to try the containerized deployment path (optional)
- The `dotnet-ef` global tool, only if you're adding a new migration:
  `dotnet tool install --global dotnet-ef`

You don't need to install a database server. This assessment uses SQLite — just a local file,
created automatically the first time you run the app. See
[A note on the database](#a-note-on-the-database) before deploying anywhere real.

### Building

```bash
dotnet restore UnifiedSeviceScheduler.sln
dotnet build UnifiedSeviceScheduler.sln --configuration Release
```

### Running the Application

```bash
dotnet run --project src/Scheduler.Api
```

By default this listens on `http://localhost:5207` and `https://localhost:7048` (see
`src/Scheduler.Api/Properties/launchSettings.json`). The first time it runs, it applies the
EF Core `InitialCreate` migration automatically — creating `scheduler.db` next to the running
executable — and seeds one dealership so there's something to book against:

| Field | Value |
|---|---|
| Id | `11111111-1111-1111-1111-111111111111` |
| Name | Downtown Dealership |
| Operating hours | Mon–Sat, 08:00–17:00 (closed Sunday) |

**Endpoints:**

| Endpoint | Purpose |
|---|---|
| `POST /appointments` | Book an appointment (guest checkout — see Domain Assumptions in architecture.md) |
| `GET /appointments/availability` | Check whether a Technician/Service Bay/time slot is free, without booking |
| `GET /health` | Liveness check |
| `GET /scalar/v1` | Interactive API documentation (Development only) |
| `GET /openapi/v1.json` | Raw OpenAPI document (Development only) |

`src/Scheduler.Api/Scheduler.Api.http` has ready-to-run requests covering the happy path and
every documented failure branch (400/409) — usable straight from the IDE's REST client. Treat
it as a manual/exploratory reference, not the automated test suite (see [Testing](#testing)).

**Service types you can book** (see `src/Scheduler.Infrastructure/Data/servicetypes.json`):

| Code | Description | Duration |
|---|---|---|
| `OIL_CHANGE` | Oil Change | 30 min |
| `TIRE_CHANGE` | Tire Change / Replacement | 60 min |
| `BRAKE_INSPECTION` | Brake Inspection | 45 min |
| `INTERIOR_CLEANING` | Interior Cleaning | 90 min |
| `BATTERY_REPLACEMENT` | Battery Replacement | 30 min |
| `WHEEL_ALIGNMENT` | Wheel Alignment | 60 min |

`technicianId`/`serviceBayId` are validated against mocked external services for this
assessment, so any non-empty GUID is accepted. See architecture.md's Domain Assumptions for
why, and the plan for swapping in the real integration later.

**Configuration:**

| Setting | Location | Purpose |
|---|---|---|
| `ConnectionStrings:SchedulerDb` | `appsettings.json` | SQLite by default — see [A note on the database](#a-note-on-the-database) |
| `Serilog:*` | `serilog.json` | Logging sinks, output template, level overrides — kept out of `Program.cs` deliberately |
| `OpenTelemetry` OTLP endpoint | `serilog.json` (`WriteTo:OpenTelemetry:Args:endpoint`) | Traces/metrics/logs export target; defaults to `http://localhost:4317` and fails quietly if nothing's listening — see architecture.md §7 |

### Database Migrations

EF Core migrations live under `src/Scheduler.Infrastructure/DataAccess/Migrations/`, not the
default project-root `Migrations/` folder. When you add a new migration, pass `--output-dir`
explicitly so it lands in the same place as the existing ones:

```bash
dotnet ef migrations add <MigrationName> \
  --project src/Scheduler.Infrastructure/Scheduler.Infrastructure.csproj \
  --startup-project src/Scheduler.Api/Scheduler.Api.csproj \
  --output-dir DataAccess/Migrations
```

Leave off `--output-dir` and you'll get a brand new top-level `Migrations/` folder sitting
next to the correct one — `dotnet ef` doesn't infer the location from existing migrations.

### Testing

```bash
dotnet test UnifiedSeviceScheduler.sln
```

77 tests: 64 unit tests (`tests/Scheduler.UnitTests`) covering Domain, Application, and
Infrastructure in isolation with Moq, and 13 integration tests
(`tests/Scheduler.IntegrationTests`) exercising the real HTTP pipeline against an isolated
temp SQLite database per test class, via `WebApplicationFactory`.

The one that matters most is `CreateAppointment_ConcurrentRequestsForSameSlot_ExactlyOneSucceeds`
— it fires 8 genuinely parallel booking requests at the same Technician/Service Bay/time and
checks that exactly one succeeds. That's the actual proof of this project's core requirement
(concurrency safety), not just a claim about it. See architecture.md §9 (Testing Strategy) and
the Data Model / Data Flow sections for why it's the database constraint doing the work here,
not the application-level check.

Coverage collection is wired up (`coverlet.collector`) and runs in CI, but there's no
coverage-threshold gate or HTML report yet — that's tracked as TASKS.md item 3.7.

## Deployment

### A note on the database

SQLite is used here strictly because it's convenient for this assessment — no database
server to stand up, the file just appears the first time you run the app. **It is not meant
for a real deployment.** For anything beyond local dev, use **Azure SQL Database**:

1. Update `ConnectionStrings:SchedulerDb` to your Azure SQL connection string (via app
   config/environment variable — never commit it).
2. In `src/Scheduler.Infrastructure/ServiceCollectionExtensions.cs`, swap the provider call
   from `options.UseSqlite(connectionString)` to `options.UseSqlServer(connectionString)`
   (the line's already there, commented out, right next to it).

That's the whole migration — same EF Core model, same migrations, same `AppointmentSlot`
concurrency design (it's a plain `UNIQUE` constraint, not a SQLite-specific trick). See
architecture.md's Data Model section for why that portability was a deliberate design choice.

### Secrets and connection strings

Don't put the production connection string in a GitHub Actions secret, or type it into App
Service's Configuration blade by hand — use **Azure Key Vault** instead, so the actual value
never has to exist inside the CI/CD pipeline at all:

- **Azure App Service**: grant the app's Managed Identity a `Key Vault Secrets User` role on
  the vault, then set `ConnectionStrings__SchedulerDb` to a [Key Vault
  reference](https://learn.microsoft.com/azure/app-service/app-service-key-vault-references)
  (`@Microsoft.KeyVault(SecretUri=...)`). That reference is just a pointer, not a secret — safe
  to check into Infrastructure-as-Code. Azure resolves the real value at runtime using the
  Managed Identity; the deployment pipeline never touches it.
- **VM / container**: pull secrets into `IConfiguration` at startup with
  `Azure.Extensions.AspNetCore.Configuration.Secrets` + `DefaultAzureCredential`, using a
  Managed Identity (or a federated GitHub OIDC identity, if the workload runs outside Azure) —
  same idea, no stored secret value flowing through CI/CD.
- If GitHub Actions needs secrets at all, they should be **deployment** credentials (e.g. an
  OIDC identity for `az login`), never the application's own connection string. If a DB
  connection string ever ends up in a GitHub Actions secret, that's the exact thing this setup
  is meant to avoid.

See architecture.md §6 (Security) for the full write-up. This is documented as a
recommendation per Agent.md's scope, not implemented here — there's no real Azure environment
to point it at in this assessment.

### As build artifacts

**To a VM** (Linux, systemd example):

```bash
dotnet publish src/Scheduler.Api --configuration Release --output /opt/scheduler-api
```

Run it under a process manager rather than a bare `dotnet` process. Example systemd unit
(`/etc/systemd/system/scheduler-api.service`):

```ini
[Unit]
Description=Unified Service Scheduler API
After=network.target

[Service]
WorkingDirectory=/opt/scheduler-api
ExecStart=/usr/bin/dotnet /opt/scheduler-api/Scheduler.Api.dll
Restart=always
RestartSec=10
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://+:8080

[Install]
WantedBy=multi-user.target
```

The connection string is deliberately left out of this unit file — see
[Secrets and connection strings](#secrets-and-connection-strings) for why, and how the app
should pull it from Azure Key Vault at startup instead.

Put a reverse proxy (nginx/Caddy) in front for TLS termination — architecture.md §6
(Security) already assumes this for the production HTTPS story.

**To Azure App Service:**

```bash
az webapp up \
  --name <app-name> \
  --resource-group <resource-group> \
  --runtime "DOTNETCORE:10.0" \
  --sku B1
```

Set any `serilog.json`-related settings through App Service's own configuration (Application
Settings) — not by committing them. For `ConnectionStrings__SchedulerDb`, use a Key Vault
reference there instead of the raw value; see
[Secrets and connection strings](#secrets-and-connection-strings).

### As a Docker container

A multi-stage `Dockerfile` and `docker-compose.yml` live at the repo root.

```bash
docker build -t scheduler-api .
docker run -p 8080:8080 -v scheduler-data:/app/data scheduler-api
```

or, with Compose:

```bash
docker compose up --build
```

The container runs as a non-root user, listens on `:8080`, and stores the SQLite file under
`/app/data` — mount a volume there, as shown, so it survives container restarts. Same rule
as above: for anything beyond a quick demo, override `ConnectionStrings__SchedulerDb` with
an Azure SQL connection string via environment variable (no image rebuild needed) rather than
relying on the SQLite file long-term.

**Future: Kubernetes.** Not implemented here, but the container is already stateless aside
from the SQLite file. The natural path is: move to Azure SQL Database (removing the
local-file dependency entirely), then a standard `Deployment` + `Service` +
`HorizontalPodAutoscaler` applies with no further changes to the image. This lines up with
architecture.md §10's Scalability Strategy — the API layer is stateless once the availability
cache also moves off in-process `IMemoryCache` to Redis.

## CI/CD

### GitHub Actions

`.github/workflows/ci.yml` builds and runs the full test suite on every pull request against
`main` (when it's opened, and on every push to the PR branch after that) and on every push to
`main` itself. Test results, including coverage data, are uploaded as a workflow artifact.

### Dependabot

`.github/dependabot.yml` checks weekly for updates across three ecosystems: NuGet packages,
the GitHub Actions versions used in the workflow, and the Dockerfile's base images. Each opens
its own PR when there's an update available, and the CI workflow above validates it like any
other change.

## AI Collaboration Narrative

I built this project working closely with Claude (Anthropic) across one long session, and I'd
rather describe how I actually worked than just assert "AI was used responsibly." So this is
written the way I'd explain it to another engineer, not as a generated summary of the AI's own
activity log.

### How I use AI

My working rule is simple: nothing lands in this codebase because the AI decided it should —
it lands because I decided it should, after seeing the AI's reasoning. I don't ask for the
whole system and review it at the end; I work through it doc-first and section-by-section, and
I confirm or correct each piece before the next one gets built on top of it. That's a slower
way to work than "generate everything," but it's the only way I'll actually trust the result,
and it's why almost nothing in this repo is a first draft I just accepted — most of it went
through at least one round of me pushing back.

### How I structured the requirement file

Before any code existed, I wrote `Agent.md` as the brief I'd hold the AI to, and I was
deliberate about its shape. It's not just a feature description — it gives the AI a Role
(Senior Solution Architect/Engineer), the assessment scenario with its domain ambiguity spelled
out explicitly rather than left implicit, and a **prioritized** list of ten engineering goals —
Correctness first, then Domain clarity, Concurrency safety, Maintainability, Testability,
Observability, Scalability, Performance, Reliability, Simplicity, in that order — so that when
two goals pulled in different directions, there was no ambiguity about which one wins. I also
wrote explicit workflow rules into it: don't touch a file without confirming first, document
every assumption, explain the trade-off for anything proposed outside scope, and keep a
task-tracking file so the work is resumable instead of something I'd have to reconstruct from a
diff later. That last rule is why `TASKS.md` exists.

### Using the C4 diagrams and data flow to structure and check the logic

The C4 diagrams in `architecture.md` weren't decoration — they're how I checked the AI's
reasoning about the system before a single class existed. Going in order, L1 (System Context)
down to L4 (Code), meant the AI couldn't jump straight to interfaces and classes without first
agreeing with me on who the actors are, what the container boundaries are, and how the pieces
inside actually talk to each other. When something didn't hold together — like the
Customer-vs-Staff API split needing to stay a routing/authorization concern rather than
leaking into every component below it — I caught it at the diagram stage, where it costs
nothing to fix, instead of after it was already baked into a controller.

The data flow and sequence diagrams did the same job for the request-handling logic
specifically. I had the AI draw out the full `CreateAppointmentCommand` sequence — every
failure branch, not just the happy path — before it wrote
`CreateAppointmentCommandHandler`. That diagram then became the thing I checked the real
implementation against: does the handler validate Technician/ServiceBay before checking
availability, in that order, with those exact 400/409 branches? Does the pre-insert read-check
actually behave as the fast-fail optimization the diagram says it is, rather than silently
becoming the real correctness mechanism? I read the code against the diagram myself — I didn't
take "there are tests for it" as proof the implementation matched the design.

### How I verified and refined AI output

Every output — a paragraph in `architecture.md`, a class, a test — went through me before I
counted it as done. That's not a formality; a few concrete examples of what that meant in
practice:

- I insisted on real evidence over a description of evidence. For the concurrency guarantee
  specifically, I wasn't willing to accept "the logic looks right" — we fired genuinely
  concurrent HTTP requests at the same slot and I confirmed with my own eyes that exactly one
  succeeded and the rest came back `409`, before I'd call that requirement met.
- I caught things the AI's own narrative would have missed. An early README draft claimed
  switching from SQLite to SQL Server needed "no code change" — I checked
  `ServiceCollectionExtensions.cs` myself and found `UseSqlServer` sitting there commented out
  next to the active `UseSqlite` call. That's a one-line code change plus a rebuild, not a pure
  config swap, and I had it corrected everywhere the claim had been repeated, not just where I
  first noticed it.
- When the AI hit an intermittent `"table already exists"` failure after moving the EF Core
  migrations, I didn't accept the first fix it offered. I had it isolate the actual cause step
  by step — confirm the app worked fine standalone, prove the entry point ran exactly once,
  apply the migration directly to rule out a bad migration file — until it landed on the real
  cause: `WebApplicationFactory`'s own test-host startup machinery, not the migration itself.
  That's recorded in `TASKS.md` as a finding, because a fix I don't understand isn't a fix I'm
  willing to keep.
- Anything hard to reverse or architecturally significant — the database provider, the
  concurrency mechanism, the guest-checkout customer model, the Customer-Booking-vs-Staff/Admin
  API split, the Minimal-API-to-Controllers move — I made the AI raise explicitly as a decision
  rather than fold in quietly. Several of those got redirected mid-implementation because I
  disagreed with the first version it proposed.

### How I ensured final quality

I held this project to the same bar I'd hold my own code to. A green `dotnet build` across the
whole solution and a green `dotnet test` (77/77 — 64 unit, 13 integration) were both required
before I'd call any piece of work done — that's now enforced automatically on every PR via
GitHub Actions (see [CI/CD](#cicd)) instead of relying on me remembering to check it by hand. I
didn't take the Docker deployment path on faith either: once I had a Docker daemon available, I
had the image actually built, run, hit with real requests, and restarted, to confirm the data
volume genuinely persists rather than just trusting that the Dockerfile looked correct.
Documentation gets the same treatment as code — `architecture.md`, `TASKS.md`, and this README
get corrected when an earlier assumption turns out wrong, for example the Security section was
rewritten once guest checkout replaced the originally assumed authenticated booking flow,
instead of being left to quietly go stale. And where the AI made a judgment call I hadn't
explicitly specified — like classifying `ServiceType` as a Value Object during the Domain
folder reorganization — I had it write that down in `TASKS.md` rather than decide it silently,
so I could actually see the call being made and push back if I disagreed with it.
