# Skill: DDD + Clean Architecture + SOLID

## Objective

Design and implement maintainable, testable, and evolvable applications using Domain-Driven Design (DDD), Clean Architecture, and SOLID principles.

Prioritize:

- Business logic independence
- Clear separation of concerns
- Dependency inversion
- Stable application contracts
- Replaceable infrastructure
- Explicit domain boundaries
- Testability
- Future modular-monolith → microservice migration

---

## 1. Dependency Direction

Dependencies must point inward:

```text
Domain ← Application ← Infrastructure
```

- Domain MUST NOT depend on Application or Infrastructure.
- Application MUST NOT depend on Infrastructure implementations.
- Infrastructure may depend on Domain and Application.
- Composition root / dependency injection configuration belongs outside business logic.
- Frameworks and infrastructure technologies must not leak into Domain.

---

## 2. Domain Layer

The Domain contains the business model and business rules.

Use the Domain layer for:

- Entities
- Aggregates
- Value Objects
- Domain Services
- Domain Policies
- Domain Events
- Domain Exceptions
- Domain-specific abstractions required by domain behavior

### Entities

Entities have identity and should encapsulate behavior and invariants.

Prefer:

```csharp
appointment.Cancel();
appointment.Reschedule(time);
```

over moving entity-specific business rules into procedural services.

Avoid anemic entities when behavior naturally belongs to the entity.

### Value Objects

Use Value Objects for concepts defined by their values rather than identity.

Examples:

```text
Money
EmailAddress
TimeRange
Address
OperatingHours
```

Value Objects should validate and encapsulate their own rules where appropriate.

### Domain Services / Policies

Use a Domain Service or Policy when:

- Logic is business logic.
- Logic does not naturally belong to one entity/value object.
- Multiple domain objects participate in the rule.

Domain Services should normally be pure:

- No database access.
- No HTTP/gRPC.
- No caching.
- No message broker.
- No Application dependency.
- No infrastructure/framework dependency.

Do not create interfaces or DI for purely stateless logic unless substitution or polymorphism is required.

---

## 3. Domain Abstractions

A Domain interface is appropriate when the **Domain itself requires an abstraction** to express a business rule.

Example:

```csharp
public interface IExchangeRate
{
    Money Convert(Money amount, Currency target);
}
```

Do not place infrastructure-specific abstractions in Domain.

Avoid:

```text
IHttpClient
IDbContext
IRepositoryClient
IApiClient
IRedisService
```

A Domain abstraction should express a business concept or capability, not a technical mechanism.

---

## 4. Application Layer

The Application layer implements use cases and orchestrates business operations.

Use it for:

- Commands
- Queries
- Handlers
- Application Services
- DTOs
- Results
- Use-case-specific interfaces/ports
- Transaction orchestration
- Authorization orchestration
- Coordination of external capabilities

The Application layer answers:

> "How do I execute this use case?"

It should not contain core business invariants that belong to the Domain.

Typical flow:

```text
Command
   ↓
Application Handler
   ↓
Obtain required data/capabilities
   ↓
Invoke Domain behavior
   ↓
Persist changes
   ↓
Publish events / trigger side effects
```

---

## 5. Application Interfaces / Ports

Application interfaces represent capabilities required by a use case.

Examples:

```text
ICustomerProvider
IDealershipProvider
INotificationService
IAvailabilityChecker
IPaymentGateway
```

Name abstractions according to **what the application needs**, not how it is currently implemented.

Prefer:

```text
ICustomerProvider
```

over:

```text
ICustomerRepository
```

when the application is consuming a customer capability that may later be provided by another service.

Prefer:

```text
IPaymentGateway
```

over:

```text
IStripeService
```

when the implementation may change.

---

## 6. Repository Rule

Use a Repository when representing persistence access to an aggregate owned by the current bounded context.

Example:

```text
IOrderRepository
IAppointmentRepository
IUserRepository
```

A Repository represents persistence, not an arbitrary external service.

Do not call every external integration a Repository.

If another bounded context owns the data, prefer a capability-oriented abstraction:

```text
IProductProvider
ICustomerProvider
IInventoryAvailability
```

rather than pretending the remote service is a local repository.

---

## 7. Cross-Bounded-Context Dependencies

Do not expose another bounded context's Domain Entities directly.

Avoid:

```text
Order Domain
   ↓
Customer Domain Entity
```

Instead, consume a contract containing only the information required by the consuming context.

Example:

```csharp
public sealed record CustomerInfo(
    Guid Id,
    string Name);
```

The consuming Domain should depend on concepts it owns or understands, not another bounded context's internal model.

---

## 8. "Application Gets Facts, Domain Makes Decisions"

Use this principle when deciding where logic belongs.

Application:

```text
Get customer
Get product
Get availability
Get configuration
```

Domain:

```text
Can this order be confirmed?
Does this appointment overlap?
Can this payment transition state?
Is this operation allowed?
```

The Application gathers required information and passes it to the Domain.

The Domain makes the business decision.

---

## 9. Infrastructure Layer

Infrastructure contains technical implementations.

Use it for:

- EF Core
- DbContext
- Repository implementations
- HTTP clients
- REST clients
- gRPC clients
- Redis
- Message brokers
- Email providers
- Cloud SDKs
- File systems
- External APIs
- Infrastructure-specific observability
- Infrastructure-specific caching

Infrastructure implements Domain/Application abstractions.

Example:

```text
Application
    ICustomerProvider
          ↑
          │
Infrastructure
    EfCustomerProvider
```

Later:

```text
Application
    ICustomerProvider
          ↑
          │
Infrastructure
    CustomerApiProvider
          ↓
       REST/gRPC
```

The Application contract should remain stable when the implementation changes.

---

## 10. Monolith → Microservice Migration

Design boundaries so infrastructure implementations can be replaced without changing business logic.

Initial:

```text
Application
    ↓
ICustomerProvider
    ↓
LocalCustomerProvider
    ↓
Database
```

Future:

```text
Application
    ↓
ICustomerProvider
    ↓
RemoteCustomerProvider
    ↓
Customer Microservice
```

The following should ideally remain unchanged:

- Domain entities
- Value Objects
- Domain rules
- Application use cases
- Command handlers
- Query handlers

Only the adapter and dependency registration should change.

---

## 11. SOLID

### Single Responsibility

A class should have one primary reason to change.

Avoid God Services such as:

```text
OrderService
 ├── database
 ├── validation
 ├── payment
 ├── notification
 ├── HTTP
 └── business rules
```

Prefer focused components.

### Open/Closed

Support variation through abstractions and composition rather than modifying business logic.

```text
IPaymentGateway
 ├── StripePaymentGateway
 └── AdyenPaymentGateway
```

### Liskov Substitution

Implementations must honor the contract of their abstraction and remain substitutable without special handling by callers.

### Interface Segregation

Prefer small, capability-focused interfaces.

Avoid large interfaces such as:

```text
IOrderService
Create
Update
Delete
Search
Notify
Pay
Validate
Calculate
```

Prefer focused contracts.

### Dependency Inversion

High-level business logic depends on abstractions.

```text
Application
    ↓
IPaymentGateway
    ↑
Infrastructure
```

Never:

```text
Application
    ↓
StripePaymentGateway
```

---

## 12. CQRS

When using CQRS:

### Commands

Commands change state.

```text
CreateOrder
CancelOrder
ConfirmAppointment
```

Commands should invoke Domain behavior and persist changes.

### Queries

Queries read state.

```text
GetOrder
SearchOrders
CheckAvailability
```

Queries should not modify state.

Do not force every query through an aggregate/repository if a dedicated read model or query abstraction is more appropriate.

---

## 13. Persistence

Persistence concerns belong in Infrastructure.

EF Core configuration belongs in Infrastructure.

Domain entities may be persisted directly with EF Core if this does not compromise the domain model.

Do not create separate persistence entities automatically. Introduce them only when persistence concerns genuinely require separation.

Database constraints should enforce critical consistency guarantees.

A read-before-write check is not a substitute for a database concurrency constraint.

---

## 14. Events and Side Effects

Keep external side effects outside core Domain logic.

Examples:

```text
Email
Push notification
Message publishing
Cache invalidation
External API calls
```

For reliable state-change events, consider:

```text
Domain Event
     ↓
Outbox
     ↓
Message Publisher
     ↓
External consumers
```

Do not assume that saving an aggregate and sending an external notification are automatically one atomic operation.

---

## 15. Decision Rules

When deciding where code belongs, ask:

### Is it a business rule or invariant?

→ Domain

### Does it naturally belong to an Entity or Value Object?

→ Entity / Value Object

### Is it pure business logic involving multiple domain concepts?

→ Domain Service / Policy

### Does it orchestrate a use case?

→ Application

### Does it obtain data or invoke an external capability?

→ Application abstraction + Infrastructure adapter

### Does it access technology?

→ Infrastructure

### Does the abstraction represent persistence of an aggregate owned by this context?

→ Repository

### Does the abstraction represent a capability consumed from another context?

→ Provider / Gateway / Client Port / capability-specific interface

---

## 16. Naming Principles

Names should describe **intent and responsibility**, not implementation.

Prefer:

```text
IOrderRepository
ICustomerProvider
IPaymentGateway
INotificationSender
IInventoryAvailability
```

Avoid vague or technology-specific abstractions:

```text
IService
IManager
IHelper
IProcessor
IHttpService
IDbService
```

unless the name has a clear, well-defined responsibility.

Do not change an abstraction merely because its implementation changes from:

```text
EF → REST
REST → gRPC
local → remote
SQL → cache
```

The abstraction should represent the stable business capability required by the consumer.

---

## 17. Architecture Goal

The architecture should make this possible:

```text
                     DOMAIN
                        ↑
                   APPLICATION
                        ↑
                  INFRASTRUCTURE
```

with replaceable adapters:

```text
             Stable Application Port
                       │
              ┌────────┴────────┐
              ↓                 ↓
        Local Adapter      Remote Adapter
              ↓                 ↓
           EF/DB            REST/gRPC
```

The primary design principle is:

> **Business rules belong to the Domain. Use-case orchestration belongs to Application. Technical implementation belongs to Infrastructure. Abstractions should represent stable capabilities and business relationships, not temporary implementation mechanisms.**
