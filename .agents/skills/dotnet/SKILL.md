---
name: dotnet
description: Modern .NET (C# 12/13, .NET 8/9/10) high-performance engineering standards, EF Core tuning, memory efficiency, and resiliency patterns.
---

# Modern .NET Standards & Performance

## Core Principles
1. **Low/Zero Allocation**:
   - Use `ValueTask<T>` on hot paths where results are frequently synchronous or cached.
   - Use `ReadOnlySpan<T>`, `Memory<T>`, and `ArrayPool<T>` for heavy parsing or buffer manipulation.
   - Use C# Primary Constructors, Collection Expressions `[]`, and `record` types for DTOs.

2. **Data Access (EF Core & Dapper)**:
   - Always `AsNoTracking()` for read queries.
   - Project explicitly (`Select`) — never fetch full entities for list displays.
   - Use Dapper for high-throughput analytical or bulk read queries.
   - Keep DbContext scoped per request; avoid long-lived contexts.
   - Use Split Queries (`AsSplitQuery()`) when loading collections to avoid cartesian explosions.

3. **Concurrency & Async**:
   - True async all the way down (`await`, never `.Result` or `.Wait()`).
   - Pass `CancellationToken` through every asynchronous method.
   - Use `System.Threading.Channels` or background workers (`IHostedService` / `BackgroundService`) for non-blocking offloading.

4. **Resiliency & API Standards**:
   - Polly v8 (`Microsoft.Extensions.Resilience`) pipelines for retry, circuit breaker, timeout.
   - Standard RFC 7807 `ProblemDetails` for errors.
   - HybridCache / MemoryCache / DistributedCache with stampede protection.
   - System.Text.Json source generation for high-throughput serialization.
