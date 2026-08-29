# Unified Service Scheduler — Task Tracker

Tracks progress across the 4 top-level tasks defined in `Agent.md`. Update status
and add checkpoint notes as work completes so the session can resume cleanly.

Status legend: `TODO` / `IN PROGRESS` / `BLOCKED` / `DONE`

---

## Task 1 — System Design Document (`architecture.md`)

Status: **DONE** — all 17 sub-items complete

| # | Item | Status | Notes |
|---|------|--------|-------|
| 1.1 | Problem statement | DONE | |
| 1.2 | Domain assumptions | DONE | Revised: Technician/ServiceBay now external systems (validated via `ITechnicianService`/`IServiceBayService`, mocked, Refit `I*HttpClient` defined but unwired); ServiceType JSON-backed via `IServiceTypeProvider`; Vehicle is free text `"Make - Model - Trim/Variant+Year"`, no Vehicle entity |
| 1.3 | C4 L1 — System Context diagram | DONE | Two actors, two API surfaces (Customer Booking API implemented; Staff/Admin API documented, not implemented); 3 external systems (Technician, Service Bay, Notification), all mocked |
| 1.4 | C4 L2 — Container diagram | DONE | Scheduler API shows split Customer Booking API / Staff-Admin API (placeholder) internally; app/domain/infra layers moved to L3 (not duplicated here); Postgres reduced to `Appointments` only |
| 1.5 | C4 L3 — Component diagram | DONE | Rendered as Mermaid, split into 3 diagrams (Request Handling, Domain Model, Infrastructure & External Integrations) for readability. Does not reflect the Customer/Staff API split, per user. Introduces `AppointmentSchedulingPolicy` domain service name — not yet confirmed by user |
| 1.6 | C4 L4 — Code diagram | DONE | Split into L4a (Handler & MockService injection, incl. Dictionary-backed `IServiceTypeProvider`) and L4b (Domain model detail, incl. `AppointmentStatus` enum). Does not reflect the API surface split |
| 1.7 | Data model | DONE | `Appointment` + new `AppointmentSlot` ledger table (concurrency guarantee); `Status` stored as string via EF `HasConversion<string>()`; ServiceType seed catalog documented in Domain Assumptions |
| 1.8 | Data flow explanation (mermaid) | DONE | Sequence diagram for `CreateAppointmentCommand` incl. all failure branches (400/409) and the TOCTOU-race explanation for why the DB constraint, not the read-check, is the real concurrency guarantee |
| 1.9 | Observability | DONE | Serilog + OTel exporter (vendor-agnostic backend); domain metrics (booking outcome rate, 409-conflict rate, external mock-service latency, availability-check latency); tracing mapped to Data Flow stages; correlation ids; health checks |
| 1.10 | Security (authN/authZ assumptions) | DONE | Two-API-surface rationale documented (sub-scoped Customer API vs scope-claim Staff/Admin API); JWT bearer suggested mechanism; explicitly framed as suggestions per Agent.md, nothing implemented |
| 1.11 | Technology choices | DONE | One-line WHY per technology; flags `net10.0` vs Agent.md's stated ".NET 8" as a resolved-but-noted discrepancy; flags FluentValidation/OpenTelemetry/Serilog as not yet wired into any `.csproj` |
| 1.12 | Testing strategy (Unit + Integration) | DONE | §9. Unit: Domain/Application/Infrastructure, >80% target, Moq (not FluentAssertions — flagged its 2025 commercial license change), avoid re-testing branch logic at integration level. Integration: WebApplicationFactory + real SQLite, concurrency race test is the key one, operating-hours boundary via real HTTP |
| 1.13 | Future evolution — concurrency strategy | DONE | §10. 3 scenarios (retry/backoff UX, per-resource serialization for hot rows, multi-region out-of-scope note), all gated on 409-rate/lock-wait metrics, not preemptive |
| 1.14 | Future evolution — cache strategy | DONE | §5 finished: what's cached (availability + ServiceType catalog, not external-id validation), explicit invalidation + TTL backstop, safe-by-construction staleness, Redis trigger tied to horizontal scaling + §7's 409-rate metric |
| 1.15 | Future evolution — scalability strategy | DONE | §10. API horizontal scale-out (blocked on Redis per §5), DB read replicas (stale-read safety argument mirrors §5's cache-staleness argument), `DealershipId` partitioning, service-extraction path via existing `IDispatcher` boundary |
| 1.16 | Future evolution — production capacity triggers | DONE | §10 table: metric → threshold → action, referencing §7's metrics directly (409 rate, API CPU/mem, availability-check p95, external call latency, lock-wait time, per-dealership volume) |
| 1.17 | Future evolution — reliability | DONE | §10. Transactional atomicity (already in place), notification as best-effort/outbox pattern, Polly circuit breaker for future real HTTP clients, fail-closed on external validation failure, idempotency-under-retry analysis (accidental via AppointmentSlot, optional Idempotency-Key header as refinement) |

**Checkpoint / output:** —

---

## Task 2 — Implementation (Clean Architecture, per code diagram)

Status: **IN PROGRESS** (scaffold only)

### 2.0 Prerequisite fixes
| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.0.1 | Resolve `SharedKernel.*` namespace in `Scheduler.Application` (Commands/Handlers/Interfaces/Queries/Dispatcher.cs) | DONE | Renamed to `Scheduler.Application.*` in place (not a separate `SharedKernel` project — no cross-project sharing need exists yet). Also collapsed the stray `Handlers.Abstractions` sub-namespace into plain `Handlers` for consistency with `ICommandHandler` |
| 2.0.2 | Rename `Appoinment.cs` → `Appointment.cs` | DONE | Filename only; class body unchanged (still empty — populating it is 2.1.1) |
| 2.0.3 | **Discovered while fixing 2.0.1**: `Scheduler.Application.csproj` referenced `Scheduler.Infrastructure` — backwards for Clean Architecture (Infrastructure implements Application's interfaces, so the dependency must point the other way; the old direction would've become circular the moment Infrastructure needed to reference Application back) | DONE | Removed Application→Infrastructure reference; added Infrastructure→Application reference. Added `Microsoft.Extensions.DependencyInjection.Abstractions` package directly to `Scheduler.Application.csproj` since `Dispatcher.cs`'s `GetRequiredService` call had been relying on that package leaking in transitively through the wrong Infrastructure reference. Full solution build verified green after the fix |

### 2.1 Scheduler.Domain
| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.1.1 | `Appointment` entity (currently empty) | TODO | Holds `TechnicianId`/`ServiceBayId` as plain reference ids (no local FK), free-text `Vehicle` string, `ServiceType` reference, `Dealership` reference, `Customer` reference |
| 2.1.2 | ~~`Technician` entity~~ | DROPPED | Now external; see `ITechnicianService` in 2.3 |
| 2.1.3 | ~~`ServiceBay` entity~~ | DROPPED | Now external; see `IServiceBayService` in 2.3 |
| 2.1.4 | `ServiceType` value/reference (name + duration) | DONE | Created as `Scheduler.Domain/ServiceType.cs` — minimal record (`Code`, `Description`, `Duration`), no behavior yet. Sourced via `IServiceTypeProvider`, not a DB entity |
| 2.1.5 | ~~`Vehicle` entity~~ | DROPPED | Free-text field on `Appointment`: `"Make - Model - Trim/Variant+Year"` |
| 2.1.6 | `Dealership` entity | TODO | Still locally owned (operating hours) |
| 2.1.7 | `Customer` entity | TODO | |
| 2.1.8 | Value objects (e.g. time range/availability window) | IN PROGRESS | `TimeRange` created (`Scheduler.Domain/TimeRange.cs`, inherits the existing previously-unused `ValueObject` base) — `Start`/`End` only, no `Overlaps()` yet (deferred to keep this pass scoped to interfaces + their minimal supporting types) |
| 2.1.9 | Domain services / invariants for availability + double-booking rules (`AppointmentSchedulingPolicy`) | TODO | Based solely on local `Appointment` records for a given `TechnicianId`/`ServiceBayId` |
| 2.1.10 | Repository interfaces (`IAppointmentRepository`, etc.) | DONE | Created in `Scheduler.Application/Interfaces/` (not `Scheduler.Domain`) — matches architecture.md's C4 L3a, which places all repository/external-service interfaces in the Application layer, implemented by Infrastructure. This row's original placement under 2.1 (Domain) was a tracker inconsistency, not the actual design |
| 2.1.11 | `AppointmentSlot` concept (15-min slot ledger, one row per resource per slot) | TODO | Carries the real concurrency guarantee — see Data Model/Data Flow |

### 2.2 Scheduler.Application
| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.2.1 | `CreateAppointmentCommand` + handler (external validation + local availability check) | TODO | Calls `ITechnicianService`/`IServiceBayService` for existence, then local `Appointment` query for availability |
| 2.2.2 | Availability query/use case | TODO | |
| 2.2.3 | FluentValidation validators for commands | TODO | |
| 2.2.4 | DI registration for Application layer | TODO | |
| 2.2.5 | Interfaces implemented by Infrastructure: `IAppointmentRepository`, `ITechnicianService`, `IServiceBayService`, `IServiceTypeProvider`, `INotificationService`, `IAvailabilityCache` | DONE | Created in `Scheduler.Application/Interfaces/`, signatures match architecture.md C4 L4a. `CancellationToken` params added (not shown in the diagram's shorthand, but standard .NET practice). `IServiceTypeProvider.TryGet` is synchronous — Dictionary lookup, no I/O, matches the design rationale. No implementations yet — see 2.3.6–2.3.8 |

### 2.3 Scheduler.Infrastructure
| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.3.1 | `DbSet`s + entity configurations in `SchedulerDbContext` | TODO | Primarily `Appointment` (+ `Dealership`/`Customer` if kept local) |
| 2.3.2 | Repository implementations | TODO | |
| 2.3.3 | Fix `AddInfrastructureServices` (currently `private`, unreachable) + implement registrations | TODO | |
| 2.3.4 | EF Core migrations + SQLite provider for this assessment (SQL Server is the production target) | TODO | |
| 2.3.5 | Cache abstraction (`IMemoryCache` now, Redis-ready per architecture) | TODO | |
| 2.3.6 | `IServiceBayService` → `MockServiceBayService` (active); `IServiceBayHttpClient` (Refit) defined but left empty, DI registration commented out | IN PROGRESS | `IServiceBayHttpClient` stub created (`Scheduler.Infrastructure/ExternalClients/`, empty, no Refit attributes yet — no real contract to attribute against). `MockServiceBayService` implementation still TODO |
| 2.3.7 | `ITechnicianService` → `MockTechnicianService` (active); `ITechnicianHttpClient` (Refit) defined but left empty, DI registration commented out | IN PROGRESS | `ITechnicianHttpClient` stub created (`Scheduler.Infrastructure/ExternalClients/`), same shape as 2.3.6. `MockTechnicianService` implementation still TODO |
| 2.3.8 | `IServiceTypeProvider` backed by static JSON file loaded at startup | TODO | Interface exists (2.2.5); `JsonServiceTypeProvider` implementation + seed JSON file (per the catalog in Domain Assumptions) still TODO |

### 2.4 Scheduler.Api
| # | Item | Status | Notes |
|---|------|--------|-------|
| 2.4.1 | Remove template weather-forecast endpoint | TODO | |
| 2.4.2 | Appointment endpoints (create, check availability, etc.) | TODO | |
| 2.4.3 | Wire DI for Application + Infrastructure | TODO | |
| 2.4.4 | Swagger/OpenAPI config | TODO | `AddOpenApi()` present, needs proper setup |
| 2.4.5 | Global error handling / problem details | TODO | |
| 2.4.6 | Observability wiring (OpenTelemetry) | TODO | |

**Checkpoint / output:** —

---

## Task 3 — Tests

Status: **TODO** (not started)

| # | Item | Status | Notes |
|---|------|--------|-------|
| 3.1 | Create `Scheduler.IntegrationTests` project + add to `.sln` | TODO | Referenced in architecture but doesn't exist yet |
| 3.2 | Remove default `UnitTest1.cs` stub | TODO | |
| 3.3 | Unit tests — Domain (>80% coverage) | TODO | |
| 3.4 | Unit tests — Application (>80% coverage) | TODO | |
| 3.5 | Unit tests — Infrastructure (>80% coverage) | TODO | |
| 3.6 | Integration tests — booking + concurrency/double-booking edge cases | TODO | |
| 3.7 | Coverage reporting setup | TODO | |

**Checkpoint / output:** —

---

## Task 4 — `README.md`

Status: **TODO** (stub only)

| # | Item | Status | Notes |
|---|------|--------|-------|
| 4.1 | Build instructions | TODO | |
| 4.2 | Run instructions | TODO | |
| 4.3 | Deploy — artifacts (VM, App Service) | TODO | |
| 4.4 | Deploy — Docker container (+ future k8s note) | TODO | |
| 4.5 | Test instructions | TODO | |
| 4.6 | GitHub Actions — run tests on PR create/update | TODO | |
| 4.7 | Dependabot setup | TODO | |
| 4.8 | AI Collaboration Narrative section | TODO | How AI was used, how output was verified/refined, quality assurance |

**Checkpoint / output:** —

---

## Open Decisions Needing User Input

- [ ] `SharedKernel.*` namespace: rename in place vs. extract a dedicated `SharedKernel` project/package

## Resolved Decisions (for reference)

- Database provider: **SQL Server is the production/target choice; SQLite is used for this assessment** (lightweight, no Docker needed). EF Core provider abstraction makes this a connection-string + `UseSqlite()`/`UseSqlServer()` swap. Concurrency design (`AppointmentSlot` unique-constraint ledger, not a Postgres-only range-exclusion constraint) is portable across both, so no behavior changes between demo and production. Docker is still needed for Task 4's containerized API deployment — that's independent of the DB choice.
- Technician/Service Bay: external systems, validated by id via `ITechnicianService`/`IServiceBayService` (mocked); Refit `I*HttpClient` interfaces defined for the real future integration but left empty/unwired for this assessment.
- Service Type: mocked via `IServiceTypeProvider`, backed by a static JSON file (not DB, not HTTP).
- Vehicle: no entity/master data; free text `"Make - Model - Trim/Variant+Year"` stored on `Appointment`.
- Dealership Staff/Manager: real actor with its own Staff/Admin API surface (broader scope — appointments across multiple customers), but that surface is **not implemented** in this assessment, only documented at L1/L2. Customer Booking API scopes results to the requesting customer's own data via access token. This split must appear in L1/L2 and in the Security section, but **not** in L3/L4 (routing/authz concern, not a code-layer concern).
- Concurrency safety: double-booking prevented by a `UNIQUE(ResourceKind, ResourceId, SlotStart)` constraint on a new `AppointmentSlot` ledger table (15-minute slot granularity), not by the application-level read-check alone (that's a fast-fail UX optimization only — real guarantee is the DB constraint on insert).
