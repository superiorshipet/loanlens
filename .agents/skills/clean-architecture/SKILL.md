---
name: clean-architecture
description: Industry-standard Clean Architecture patterns for enterprise systems, decoupling business logic from frameworks and database implementations.
---

# Clean Architecture

## Core Rules
1. **Dependency Rule**: Inner layers have zero knowledge of outer layers. Dependencies point inwards.
   - `Presentation` → `Application` → `Domain`
   - `Infrastructure` → `Application` & `Domain`

## Layers Definition

### 1. Domain (Core)
- **Entities**: Business models with internal state encapsulation & invariants.
- **Value Objects**: Immutable objects compared by value, not identity.
- **Domain Events**: Dispatched when domain state changes (`IDomainEvent`).
- **Interfaces**: Repository contracts, specifications, domain services.
- **Rules**: Zero external dependencies (no EF Core, no ASP.NET, no third-party libraries).

### 2. Application
- **Use Cases / CQRS**: Commands (mutations) and Queries (reads).
- **DTOs / Contracts**: Request/Response models decoupled from domain entities.
- **Behaviors / Pipeline**: Validation (FluentValidation), Logging, Performance, Transaction handling.
- **Interfaces**: DB context abstraction, external service interfaces.

### 3. Infrastructure
- **Persistence**: EF Core DbContext, Entity Configurations (`IEntityTypeConfiguration<T>`), Migrations, Dapper queries.
- **External Services**: Email, Payment, SMS, Message Brokers (RabbitMQ/Kafka).
- **Implementations**: Concrete repository implementations.

### 4. Presentation (API)
- **Endpoints**: Minimal APIs / Controllers.
- **Middlewares**: Exception handling (RFC 7807 ProblemDetails), Authentication, Rate Limiting.
- **Mapping**: Fast mapping between API contracts and Application DTOs.
