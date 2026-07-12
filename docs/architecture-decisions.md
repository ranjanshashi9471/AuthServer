# AuthServer Architecture Decisions

**Project:** AuthServer

**Architecture:** Clean Architecture + Domain-Driven Design (DDD) + Vertical Slice Architecture

---

# Purpose

This document records important architectural decisions made during the development of AuthServer.

The objective is to document **why** a decision was made, not just **what** was implemented. Future contributors should be able to understand the reasoning behind the codebase.

---

# ADR-001: Clean Architecture

## Decision

The solution follows Clean Architecture.

```
AuthServer.Api
        │
        ▼
AuthServer.Application
        │
        ▼
AuthServer.Domain

AuthServer.Infrastructure
        ▲
        │
Implements Application Interfaces
```

## Rationale

- Keeps business logic independent of frameworks.
- Infrastructure can change without affecting the domain.
- Makes testing easier.
- Improves long-term maintainability.

---

# ADR-002: Domain-First Development

## Decision

The domain model is designed before the database schema.

## Rationale

The database should support the business model, not define it.

This encourages proper Domain-Driven Design and prevents database-first thinking.

---

# ADR-003: Rich Domain Model

## Decision

Entities own their behavior.

Instead of exposing setters:

```csharp
user.Status = UserStatus.Active;
```

Entities expose business methods:

```csharp
user.Activate();
```

## Rationale

Business rules belong inside the entity that owns the state.

This prevents invalid state transitions.

---

# ADR-004: Strongly Typed Identifiers

## Decision

Every aggregate has its own identifier type.

Examples:

- UserId
- RoleId
- PermissionId
- SessionId
- RefreshTokenId

instead of using Guid everywhere.

## Rationale

Provides compile-time safety.

The compiler prevents accidentally passing a RoleId where a UserId is expected.

Although this introduces additional code and EF Core configuration, the benefits outweigh the cost for an enterprise authentication server.

---

# ADR-005: Identifier Organization

## Decision

Identifiers are organized under ValueObjects.

```
ValueObjects
│
├── Identifiers
│   ├── UserId
│   ├── RoleId
│   └── PermissionId
│
├── Email
├── Username
└── PasswordHash
```

## Rationale

Identifiers are immutable value objects but deserve their own subfolder to improve discoverability and organization.

---

# ADR-006: Factory Methods

## Decision

Entities are never instantiated using public constructors.

Instead:

```csharp
User.Create(...)
```

internally invokes a private constructor.

## Rationale

Ensures every entity is fully initialized.

Prevents invalid objects from being created.

---

# ADR-007: Private Constructors

## Decision

Constructors are private.

Static factory methods control object creation.

## Rationale

Allows enforcing invariants during construction.

Supports future extensions such as domain events or audit initialization.

---

# ADR-008: Private Setters

## Decision

Entity properties expose private setters.

Example:

```csharp
public Email Email { get; private set; }
```

instead of

```csharp
public Email Email { get; set; }
```

## Rationale

Only the entity should modify its own state.

External code must use business methods.

---

# ADR-009: Value Objects

## Decision

Primitive values that represent business concepts are modeled as Value Objects.

Current:

- Email
- Username
- PasswordHash

Future:

- PhoneNumber
- DisplayName
- ClientSecret
- TenantName

## Rationale

Value Objects encapsulate validation, normalization, equality, and business meaning.

They eliminate repeated validation throughout the application.

---

# ADR-010: Password Handling

## Decision

The domain never stores plaintext passwords.

Only PasswordHash exists within the domain.

Hashing is performed in the Infrastructure layer.

## Rationale

Keeps cryptographic implementation outside the domain while ensuring plaintext passwords never become part of domain state.

---

# ADR-011: Timestamp Strategy

## Decision

All timestamps use DateTimeOffset instead of DateTime.

## Rationale

Authentication systems operate across time zones.

DateTimeOffset avoids ambiguity and is more appropriate for distributed systems.

---

# ADR-012: Entity Base Class

## Decision

All entities inherit from a generic base class.

```csharp
Entity<TId>
```

The base class owns:

- Id
- CreatedAt
- UpdatedAt

## Rationale

Provides consistency across all aggregates and reduces duplication.

---

# ADR-013: Touch Method

## Decision

The base entity provides a protected Touch() method.

## Rationale

Every state change automatically updates UpdatedAt.

Entities should not duplicate timestamp logic.

---

# ADR-014: Vertical Slice Architecture

## Decision

The Application layer is organized by features rather than technical layers.

```
Features
│
├── Authentication
│   ├── Login
│   ├── Register
│   └── RefreshToken
│
├── Users
└── Roles
```

## Rationale

Keeps related code together.

Improves maintainability as the application grows.

---

# ADR-015: Thin API Layer

## Decision

Controllers remain thin.

Responsibilities:

- Receive requests
- Delegate work
- Return responses

Business logic belongs in the Application layer.

## Rationale

Improves separation of concerns and testability.

---

# ADR-016: Infrastructure Isolation

## Decision

The Domain layer has no dependency on:

- ASP.NET Core
- EF Core
- HTTP
- Logging
- JSON
- Databases

Infrastructure depends on the Application layer.

The Domain depends on nothing outside itself.

## Rationale

Keeps business logic independent of technology choices.

---

# ADR-017: User lifecycle is represented by a single UserStatus enum rather than separate status and EmailVerified flags.

---

# ADR-018: Domain Exception Hierarchy

## Decision

The Domain layer throws only exceptions derived from DomainException.

## Rationale

Separates business errors from framework/programming errors.
Allows the API layer to translate domain failures into appropriate HTTP responses (for example, 400 Bad Request or 409 Conflict).
Provides a consistent mechanism for communicating business rule violations across the application.

---

# 📘 ADR-019: Dependency Inversion

# Decision

The Application layer depends only on abstractions for external services such as persistence, hashing, and time.

Examples

IUserRepository
IPasswordHasher
IClock

# Rationale

Keeps the Application layer independent of implementation details.
Makes unit testing straightforward by mocking interfaces.
Allows swapping technologies (EF Core, Dapper, different password hashers) without changing business logic.

---

# 📘 ADR-020: Vertical Slice Architecture

## Decision

The Application layer is organized by feature rather than by technical type.

Example

Features/
Authentication/
Register/
Login/
RefreshToken/

instead of

Commands/
Handlers/
Validators/

## Rationale

High cohesion
Easier navigation
Better scalability as features grow
All code for a use case lives in one place

---

# 📘 ADR-021: Handlers are Orchestrators

## Decision

Application handlers coordinate work between the domain and infrastructure but do not contain business rules.

## Responsibilities

Create value objects
Call repositories
Coordinate services
Create aggregates
Return results

Not responsible for

Email validation
Password validation
State transitions
Business invariants

---

# 📘 ADR-022: Persistence Ignorance

## Decision

The Domain layer remains completely unaware of EF Core.

Implications

No [Key]
No [Column]
No [Required]
No [ForeignKey]
No persistence attributes of any kind

All database configuration lives in the Infrastructure project.

## Rationale

The domain model should represent the business, not the storage technology. This keeps it portable, testable, and aligned with Clean Architecture.

---

# ADR-023: Infrastructure Organization

## Decision

Infrastructure components are organized by responsibility rather than by entity.

## Structure

Configurations contains EF Core entity mappings.
Converters contains reusable type conversion logic.
Repositories contains persistence implementations.
Migrations contains database schema history.

## Rationale

This separation keeps responsibilities clear, avoids duplication, and makes converters reusable across multiple entity configurations as the model grows.

---

# 📘 ADR-024: Aggregate Creation

## Decision

Aggregates are created through factory methods (Create) rather than exposing public constructors.

## Rationale

Centralizes business invariants.
Makes the intent of object creation explicit.
Separates business creation from persistence materialization.
Prevents partially initialized aggregates from being created by application code.

---

# 📘 ADR-025: Generic Identifier Converters

## Decision

All strongly typed identifier converters inherit from a generic IdentifierValueConverter<TId> base class.

## Rationale

Eliminates duplicated conversion logic.
Keeps converters explicit and easy to discover.
Avoids reflection and hidden behavior.
Scales naturally as new aggregate roots are added.

---

# Deferred Decisions

The following topics have intentionally been postponed until they provide value.

## 📌 Deferred Decision (ADR): Introduce an application clock abstraction instead of calling DateTimeOffset.UtcNow directly.

## Domain Exceptions

Current:

- Value Objects throw ArgumentException.

Future:

- Introduce a DomainException hierarchy or a Result<T> pattern.

Reason:

Avoid unnecessary complexity while building the core domain.

---

## MediatR

Decision deferred.

We will evaluate whether MediatR provides enough value over a lightweight in-house dispatcher after the Application layer is established.

---

## Source Generators

Decision deferred.

Strongly Typed IDs are implemented manually to understand the pattern first.

Once multiple identifiers exist, we will evaluate source generators to eliminate boilerplate.

---

## Generic Repository

Decision deferred.

Current expectation is to avoid a generic repository.

EF Core's DbContext already provides Repository and Unit of Work capabilities.

We will introduce repositories only where they add meaningful domain behavior.

---

# Guiding Principles

1. Business rules belong in the Domain.
2. Infrastructure implements interfaces, never defines business rules.
3. Invalid domain objects should be impossible to create.
4. Prefer explicit code over clever abstractions.
5. Introduce complexity only when it provides measurable value.
6. Optimize for maintainability rather than minimizing lines of code.
7. Document every significant architectural decision before implementing it.

---

**Status:** Living document. Updated continuously as the project evolves.
