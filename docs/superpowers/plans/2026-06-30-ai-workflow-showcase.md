# AI Workflow Showcase Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Layer AI workflow artifacts onto `dotnet-api-security`: CLAUDE.md context file, webhook delivery feature built AI-first, and honest retrospective doc.

**Architecture:** Three independent artifacts on the existing Clean Architecture repo. Webhook feature follows established patterns: domain entity → Application interface → Infrastructure implementation → API endpoint. BackgroundService uses IServiceProvider scope to access scoped services.

**Tech Stack:** .NET 10, EF Core 9 + Npgsql, MediatR 12, xUnit + FluentAssertions + Testcontainers, IHttpClientFactory (named client "webhook"), System.Security.Cryptography.HMACSHA256

---

## File Map

**New files:**
```
CLAUDE.md
docs/ai-workflow/retrospective.md
src/ApiSecurity.Domain/Enums/WebhookDeliveryStatus.cs
src/ApiSecurity.Domain/Enums/WebhookEventType.cs
src/ApiSecurity.Domain/Entities/WebhookSubscription.cs
src/ApiSecurity.Domain/Entities/WebhookDelivery.cs
src/ApiSecurity.Application/Interfaces/IWebhookRepository.cs
src/ApiSecurity.Application/Interfaces/IWebhookSignatureService.cs
src/ApiSecurity.Application/Interfaces/IWebhookDispatcher.cs
src/ApiSecurity.Application/Interfaces/IWebhookDeliveryService.cs
src/ApiSecurity.Application/Webhooks/WebhookDispatcher.cs
src/ApiSecurity.Application/Webhooks/Commands/CreateSubscriptionCommand.cs
src/ApiSecurity.Application/Webhooks/Commands/DeleteSubscriptionCommand.cs
src/ApiSecurity.Application/Webhooks/Queries/GetSubscriptionsQuery.cs
src/ApiSecurity.Application/Webhooks/Queries/GetDeliveriesQuery.cs
src/ApiSecurity.Infrastructure/Security/WebhookSignatureService.cs
src/ApiSecurity.Infrastructure/Persistence/Configurations/WebhookSubscriptionConfiguration.cs
src/ApiSecurity.Infrastructure/Persistence/Configurations/WebhookDeliveryConfiguration.cs
src/ApiSecurity.Infrastructure/Repositories/WebhookRepository.cs
src/ApiSecurity.Infrastructure/Webhooks/WebhookDeliveryService.cs
src/ApiSecurity.Infrastructure/Webhooks/WebhookDeliveryWorker.cs
src/ApiSecurity.API/Endpoints/WebhookEndpoints.cs
tests/ApiSecurity.IntegrationTests/Fixtures/FakeWebhookHandler.cs
tests/ApiSecurity.UnitTests/Webhooks/WebhookDeliveryTests.cs
tests/ApiSecurity.UnitTests/Webhooks/WebhookSignatureServiceTests.cs
tests/ApiSecurity.IntegrationTests/Webhooks/WebhookSubscriptionTests.cs
```

**Modified files:**
```
src/ApiSecurity.Infrastructure/Persistence/AppDbContext.cs
src/ApiSecurity.Application/Products/Commands/CreateProductCommand.cs
src/ApiSecurity.API/Program.cs
tests/ApiSecurity.IntegrationTests/Fixtures/ApiTestFixture.cs
```

---

## Task 1: CLAUDE.md

**Files:**
- Create: `CLAUDE.md`

- [ ] **Step 1: Create file**

```markdown
# dotnet-api-security — Claude Context

## Architecture
Clean Architecture: Domain → Application → Infrastructure → API
Middleware pipeline (order matters): SecurityHeaders → IpFilter → AuditLog → CORS → RateLimiter → Auth → Endpoints

## Constraints (never violate)
- No AllowAnyOrigin / AllowAnyHeader in CORS
- API keys: prefix stored plaintext (indexed), full key SHA-256 hashed — never BCrypt (ADR-001)
- Rate limiter: built-in .NET Microsoft.AspNetCore.RateLimiting — no Redis, no third-party (ADR-002)
- JWT: ClockSkew = Zero, access 15min, refresh 7 days (ADR-003)
- net10.0 only — no net9.0 targets
- Webhook secrets stored plaintext — required for HMAC computation (ADR-001 does not apply here)

## Test conventions
- Unit tests: xUnit + FluentAssertions, no I/O
- Integration tests: Testcontainers PostgreSQL, WebApplicationFactory
- Never override IpFilter:Allowlist in test fixtures (creates HashSet{""} → all 403)
- Webhook handler override: use FakeWebhookHandler via ApiTestFixture.WebhookFakeHandler

## File placement
- New features: Application/<Domain>/Commands/ and Application/<Domain>/Queries/
- Background workers: Infrastructure/Webhooks/
- Auth handlers: Infrastructure/Security/
- Middleware: API/Middleware/

## ADRs
- ADR-001: docs/adr/ADR-001-api-key-hashing.md
- ADR-002: docs/adr/ADR-002-rate-limiting-strategy.md
- ADR-003: docs/adr/ADR-003-jwt-expiry-refresh.md
```

- [ ] **Step 2: Commit**

```bash
git add CLAUDE.md
git commit -m "docs: add CLAUDE.md with architecture context for AI tools"
```

---

## Task 2: Domain Enums

**Files:**
- Create: `src/ApiSecurity.Domain/Enums/WebhookDeliveryStatus.cs`
- Create: `src/ApiSecurity.Domain/Enums/WebhookEventType.cs`

- [ ] **Step 1: Create WebhookDeliveryStatus**

```csharp
namespace ApiSecurity.Domain.Enums;

public enum WebhookDeliveryStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2
}
```

- [ ] **Step 2: Create WebhookEventType**

```csharp
namespace ApiSecurity.Domain.Enums;

[Flags]
public enum WebhookEventType
{
    None = 0,
    ProductCreated = 1,
    ProductUpdated = 2,
    ProductDeleted = 4,
    All = ProductCreated | ProductUpdated | ProductDeleted
}
```

- [ ] **Step 3: Commit**

```bash
git add src/ApiSecurity.Domain/Enums/WebhookDeliveryStatus.cs src/ApiSecurity.Domain/Enums/WebhookEventType.cs
git commit -m "feat: add WebhookDeliveryStatus and WebhookEventType enums"
```

---

## Task 3: Domain Entities (TDD)

**Files:**
- Create: `tests/ApiSecurity.UnitTests/Webhooks/WebhookDeliveryTests.cs`
- Create: `src/ApiSecurity.Domain/Entities/WebhookSubscription.cs`
- Create: `src/ApiSecurity.Domain/Entities/WebhookDelivery.cs`

- [ ] **Step 1: Write failing tests**

Create `tests/ApiSecurity.UnitTests/Webhooks/WebhookDeliveryTests.cs`:

```csharp
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using FluentAssertions;

namespace ApiSecurity.UnitTests.Webhooks;

public class WebhookDeliveryTests
{
    [Fact]
    public void Create_InitialState_IsPendingWithZeroAttempts()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{\"id\":1}");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(0);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ResponseCode.Should().BeNull();
    }

    [Fact]
    public void RecordSuccess_SetsStatusDelivered()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");

        delivery.RecordSuccess(200);

        delivery.Status.Should().Be(WebhookDeliveryStatus.Delivered);
        delivery.ResponseCode.Should().Be(200);
        delivery.LastAttemptAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordFailure_FirstAttempt_RemainsPendingWithNextAttemptIn5Seconds()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        var before = DateTimeOffset.UtcNow;

        delivery.RecordFailure(500, "error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Pending);
        delivery.AttemptCount.Should().Be(1);
        delivery.NextAttemptAt.Should().BeCloseTo(before.AddSeconds(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_SecondAttempt_NextAttemptIn25Seconds()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        delivery.RecordFailure(500, null);
        var before = DateTimeOffset.UtcNow;

        delivery.RecordFailure(500, null);

        delivery.AttemptCount.Should().Be(2);
        delivery.NextAttemptAt.Should().BeCloseTo(before.AddSeconds(25), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RecordFailure_ThirdAttempt_StatusIsFailedAndNoNextAttempt()
    {
        var delivery = WebhookDelivery.Create(Guid.NewGuid(), "{}");
        delivery.RecordFailure(500, null);
        delivery.RecordFailure(500, null);

        delivery.RecordFailure(500, "final error");

        delivery.Status.Should().Be(WebhookDeliveryStatus.Failed);
        delivery.AttemptCount.Should().Be(3);
        delivery.NextAttemptAt.Should().BeNull();
        delivery.ResponseBody.Should().Be("final error");
    }
}
```

- [ ] **Step 2: Run tests — expect compile error (entities don't exist yet)**

```bash
dotnet test tests/ApiSecurity.UnitTests/ --filter "FullyQualifiedName~WebhookDeliveryTests"
```

Expected: Build error — `WebhookDelivery` not found.

- [ ] **Step 3: Create WebhookSubscription entity**

```csharp
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class WebhookSubscription
{
    public Guid Id { get; private set; }
    public string EndpointUrl { get; private set; } = string.Empty;
    public string Secret { get; private set; } = string.Empty;
    public WebhookEventType EventTypes { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookSubscription() { }

    public static WebhookSubscription Create(string endpointUrl, string secret, WebhookEventType eventTypes)
        => new()
        {
            Id = Guid.NewGuid(),
            EndpointUrl = endpointUrl,
            Secret = secret,
            EventTypes = eventTypes,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void Deactivate() => IsActive = false;
}
```

- [ ] **Step 4: Create WebhookDelivery entity**

```csharp
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Domain.Entities;

public class WebhookDelivery
{
    public Guid Id { get; private set; }
    public Guid SubscriptionId { get; private set; }
    public WebhookSubscription Subscription { get; private set; } = null!;
    public string Payload { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public WebhookDeliveryStatus Status { get; private set; }
    public int? ResponseCode { get; private set; }
    public string? ResponseBody { get; private set; }
    public DateTimeOffset? NextAttemptAt { get; private set; }
    public DateTimeOffset? LastAttemptAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private WebhookDelivery() { }

    public static WebhookDelivery Create(Guid subscriptionId, string payload)
        => new()
        {
            Id = Guid.NewGuid(),
            SubscriptionId = subscriptionId,
            Payload = payload,
            AttemptCount = 0,
            Status = WebhookDeliveryStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public void RecordSuccess(int responseCode)
    {
        Status = WebhookDeliveryStatus.Delivered;
        ResponseCode = responseCode;
        LastAttemptAt = DateTimeOffset.UtcNow;
    }

    public void RecordFailure(int? responseCode, string? responseBody)
    {
        AttemptCount++;
        ResponseCode = responseCode;
        ResponseBody = responseBody;
        LastAttemptAt = DateTimeOffset.UtcNow;

        if (AttemptCount >= 3)
        {
            Status = WebhookDeliveryStatus.Failed;
            NextAttemptAt = null;
        }
        else
        {
            var delaySeconds = Math.Pow(5, AttemptCount);
            NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(delaySeconds);
        }
    }
}
```

- [ ] **Step 5: Run tests — expect pass**

```bash
dotnet test tests/ApiSecurity.UnitTests/ --filter "FullyQualifiedName~WebhookDeliveryTests"
```

Expected: 5 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/ApiSecurity.Domain/Entities/WebhookSubscription.cs src/ApiSecurity.Domain/Entities/WebhookDelivery.cs tests/ApiSecurity.UnitTests/Webhooks/WebhookDeliveryTests.cs
git commit -m "feat: add WebhookSubscription and WebhookDelivery domain entities"
```

---

## Task 4: Application Interfaces

**Files:**
- Create: `src/ApiSecurity.Application/Interfaces/IWebhookRepository.cs`
- Create: `src/ApiSecurity.Application/Interfaces/IWebhookSignatureService.cs`
- Create: `src/ApiSecurity.Application/Interfaces/IWebhookDispatcher.cs`
- Create: `src/ApiSecurity.Application/Interfaces/IWebhookDeliveryService.cs`

- [ ] **Step 1: Create IWebhookRepository**

```csharp
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Interfaces;

public interface IWebhookRepository
{
    Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default);
    Task<WebhookSubscription?> FindSubscriptionByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(WebhookEventType eventType, CancellationToken ct = default);
    Task<List<WebhookSubscription>> GetAllSubscriptionsAsync(CancellationToken ct = default);
    Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default);
    Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(CancellationToken ct = default);
    Task<List<WebhookDelivery>> GetDeliveriesBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

- [ ] **Step 2: Create IWebhookSignatureService**

```csharp
namespace ApiSecurity.Application.Interfaces;

public interface IWebhookSignatureService
{
    string ComputeSignature(string secret, string payload);
}
```

- [ ] **Step 3: Create IWebhookDispatcher**

```csharp
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Interfaces;

public interface IWebhookDispatcher
{
    Task DispatchAsync(WebhookEventType eventType, string payload, CancellationToken ct = default);
}
```

- [ ] **Step 4: Create IWebhookDeliveryService**

```csharp
namespace ApiSecurity.Application.Interfaces;

public interface IWebhookDeliveryService
{
    Task ProcessPendingAsync(CancellationToken ct = default);
}
```

- [ ] **Step 5: Commit**

```bash
git add src/ApiSecurity.Application/Interfaces/IWebhookRepository.cs src/ApiSecurity.Application/Interfaces/IWebhookSignatureService.cs src/ApiSecurity.Application/Interfaces/IWebhookDispatcher.cs src/ApiSecurity.Application/Interfaces/IWebhookDeliveryService.cs
git commit -m "feat: add webhook application interfaces"
```

---

## Task 5: WebhookSignatureService (TDD)

**Files:**
- Create: `tests/ApiSecurity.UnitTests/Webhooks/WebhookSignatureServiceTests.cs`
- Create: `src/ApiSecurity.Infrastructure/Security/WebhookSignatureService.cs`

- [ ] **Step 1: Write failing tests**

```csharp
using ApiSecurity.Infrastructure.Security;
using FluentAssertions;

namespace ApiSecurity.UnitTests.Webhooks;

public class WebhookSignatureServiceTests
{
    private readonly WebhookSignatureService _service = new();

    [Fact]
    public void ComputeSignature_SameInputs_ReturnsSameSignature()
    {
        var sig1 = _service.ComputeSignature("secret", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret", "{\"id\":1}");

        sig1.Should().Be(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentSecret_ReturnsDifferentSignature()
    {
        var sig1 = _service.ComputeSignature("secret1", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret2", "{\"id\":1}");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentPayload_ReturnsDifferentSignature()
    {
        var sig1 = _service.ComputeSignature("secret", "{\"id\":1}");
        var sig2 = _service.ComputeSignature("secret", "{\"id\":2}");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeSignature_StartsWithSha256Prefix()
    {
        var sig = _service.ComputeSignature("secret", "payload");

        sig.Should().StartWith("sha256=");
    }
}
```

- [ ] **Step 2: Run — expect compile error**

```bash
dotnet test tests/ApiSecurity.UnitTests/ --filter "FullyQualifiedName~WebhookSignatureServiceTests"
```

Expected: Build error — `WebhookSignatureService` not found.

- [ ] **Step 3: Implement WebhookSignatureService**

```csharp
using System.Security.Cryptography;
using System.Text;
using ApiSecurity.Application.Interfaces;

namespace ApiSecurity.Infrastructure.Security;

public class WebhookSignatureService : IWebhookSignatureService
{
    public string ComputeSignature(string secret, string payload)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var hashBytes = HMACSHA256.HashData(keyBytes, payloadBytes);
        return $"sha256={Convert.ToHexString(hashBytes).ToLowerInvariant()}";
    }
}
```

- [ ] **Step 4: Run — expect 4 passing**

```bash
dotnet test tests/ApiSecurity.UnitTests/ --filter "FullyQualifiedName~WebhookSignatureServiceTests"
```

Expected: 4 tests pass.

- [ ] **Step 5: Commit**

```bash
git add tests/ApiSecurity.UnitTests/Webhooks/WebhookSignatureServiceTests.cs src/ApiSecurity.Infrastructure/Security/WebhookSignatureService.cs
git commit -m "feat: add WebhookSignatureService with HMAC-SHA256"
```

---

## Task 6: EF Core Configurations + DbContext + Migration

**Files:**
- Create: `src/ApiSecurity.Infrastructure/Persistence/Configurations/WebhookSubscriptionConfiguration.cs`
- Create: `src/ApiSecurity.Infrastructure/Persistence/Configurations/WebhookDeliveryConfiguration.cs`
- Modify: `src/ApiSecurity.Infrastructure/Persistence/AppDbContext.cs`

- [ ] **Step 1: Create WebhookSubscriptionConfiguration**

```csharp
using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EndpointUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Secret).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EventTypes).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.IsActive);
    }
}
```

- [ ] **Step 2: Create WebhookDeliveryConfiguration**

```csharp
using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApiSecurity.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Payload).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ResponseBody).HasMaxLength(4000);
        builder.HasOne(x => x.Subscription)
            .WithMany()
            .HasForeignKey(x => x.SubscriptionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
```

- [ ] **Step 3: Modify AppDbContext — add DbSets**

In `src/ApiSecurity.Infrastructure/Persistence/AppDbContext.cs`, add after the existing DbSets:

```csharp
public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
```

The using directive `using ApiSecurity.Domain.Entities;` is already present.

Full file after edit:

```csharp
using ApiSecurity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurity.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
```

- [ ] **Step 4: Add EF migration**

Run from repo root (requires `dotnet ef` tools and Docker with Postgres running):

```bash
dotnet ef migrations add AddWebhooks --project src/ApiSecurity.Infrastructure --startup-project src/ApiSecurity.API
```

Expected: New file created at `src/ApiSecurity.Infrastructure/Persistence/Migrations/<timestamp>_AddWebhooks.cs` containing `CreateTable` calls for `WebhookSubscriptions` and `WebhookDeliveries`.

Verify the migration file exists and contains both table names before continuing.

- [ ] **Step 5: Commit**

```bash
git add src/ApiSecurity.Infrastructure/Persistence/Configurations/ src/ApiSecurity.Infrastructure/Persistence/AppDbContext.cs src/ApiSecurity.Infrastructure/Persistence/Migrations/
git commit -m "feat: add EF Core configurations and migration for webhook entities"
```

---

## Task 7: WebhookRepository

**Files:**
- Create: `src/ApiSecurity.Infrastructure/Repositories/WebhookRepository.cs`

- [ ] **Step 1: Implement**

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using ApiSecurity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ApiSecurity.Infrastructure.Repositories;

public class WebhookRepository(AppDbContext db) : IWebhookRepository
{
    public async Task AddSubscriptionAsync(WebhookSubscription subscription, CancellationToken ct = default)
        => await db.WebhookSubscriptions.AddAsync(subscription, ct);

    public Task<WebhookSubscription?> FindSubscriptionByIdAsync(Guid id, CancellationToken ct = default)
        => db.WebhookSubscriptions.FindAsync([id], ct).AsTask();

    public Task<List<WebhookSubscription>> GetActiveSubscriptionsForEventAsync(WebhookEventType eventType, CancellationToken ct = default)
        => db.WebhookSubscriptions
            .Where(s => s.IsActive && (s.EventTypes & eventType) != 0)
            .ToListAsync(ct);

    public Task<List<WebhookSubscription>> GetAllSubscriptionsAsync(CancellationToken ct = default)
        => db.WebhookSubscriptions.ToListAsync(ct);

    public async Task AddDeliveryAsync(WebhookDelivery delivery, CancellationToken ct = default)
        => await db.WebhookDeliveries.AddAsync(delivery, ct);

    public Task<List<WebhookDelivery>> GetPendingDeliveriesAsync(CancellationToken ct = default)
        => db.WebhookDeliveries
            .Include(d => d.Subscription)
            .Where(d => d.Status == WebhookDeliveryStatus.Pending
                && (d.NextAttemptAt == null || d.NextAttemptAt <= DateTimeOffset.UtcNow))
            .ToListAsync(ct);

    public Task<List<WebhookDelivery>> GetDeliveriesBySubscriptionAsync(Guid subscriptionId, CancellationToken ct = default)
        => db.WebhookDeliveries
            .Where(d => d.SubscriptionId == subscriptionId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default)
        => db.SaveChangesAsync(ct);
}
```

- [ ] **Step 2: Build to verify no errors**

```bash
dotnet build src/ApiSecurity.Infrastructure/
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add src/ApiSecurity.Infrastructure/Repositories/WebhookRepository.cs
git commit -m "feat: add WebhookRepository"
```

---

## Task 8: Application Commands, Queries, WebhookDispatcher

**Files:**
- Create: `src/ApiSecurity.Application/Webhooks/Commands/CreateSubscriptionCommand.cs`
- Create: `src/ApiSecurity.Application/Webhooks/Commands/DeleteSubscriptionCommand.cs`
- Create: `src/ApiSecurity.Application/Webhooks/Queries/GetSubscriptionsQuery.cs`
- Create: `src/ApiSecurity.Application/Webhooks/Queries/GetDeliveriesQuery.cs`
- Create: `src/ApiSecurity.Application/Webhooks/WebhookDispatcher.cs`
- Modify: `src/ApiSecurity.Application/Products/Commands/CreateProductCommand.cs`

- [ ] **Step 1: Create CreateSubscriptionCommand**

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Commands;

public record CreateSubscriptionCommand(string EndpointUrl, string Secret, WebhookEventType EventTypes)
    : IRequest<CreateSubscriptionResult>;

public record CreateSubscriptionResult(Guid Id, string EndpointUrl, WebhookEventType EventTypes);

public class CreateSubscriptionCommandHandler(IWebhookRepository repository)
    : IRequestHandler<CreateSubscriptionCommand, CreateSubscriptionResult>
{
    public async Task<CreateSubscriptionResult> Handle(CreateSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = WebhookSubscription.Create(request.EndpointUrl, request.Secret, request.EventTypes);
        await repository.AddSubscriptionAsync(subscription, ct);
        await repository.SaveChangesAsync(ct);
        return new CreateSubscriptionResult(subscription.Id, subscription.EndpointUrl, subscription.EventTypes);
    }
}

public class CreateSubscriptionCommandValidator : AbstractValidator<CreateSubscriptionCommand>
{
    public CreateSubscriptionCommandValidator()
    {
        RuleFor(x => x.EndpointUrl).NotEmpty().MaximumLength(500).Must(url =>
            Uri.TryCreate(url, UriKind.Absolute, out var result)
            && (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps))
            .WithMessage("EndpointUrl must be a valid HTTP or HTTPS URL.");
        RuleFor(x => x.Secret).NotEmpty().MinimumLength(16).MaximumLength(256);
        RuleFor(x => x.EventTypes).NotEqual(WebhookEventType.None);
    }
}
```

- [ ] **Step 2: Create DeleteSubscriptionCommand**

```csharp
using ApiSecurity.Application.Interfaces;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Commands;

public record DeleteSubscriptionCommand(Guid Id) : IRequest<bool>;

public class DeleteSubscriptionCommandHandler(IWebhookRepository repository)
    : IRequestHandler<DeleteSubscriptionCommand, bool>
{
    public async Task<bool> Handle(DeleteSubscriptionCommand request, CancellationToken ct)
    {
        var subscription = await repository.FindSubscriptionByIdAsync(request.Id, ct);
        if (subscription is null) return false;

        subscription.Deactivate();
        await repository.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 3: Create GetSubscriptionsQuery**

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Queries;

public record GetSubscriptionsQuery : IRequest<List<WebhookSubscriptionDto>>;

public record WebhookSubscriptionDto(
    Guid Id,
    string EndpointUrl,
    WebhookEventType EventTypes,
    bool IsActive,
    DateTimeOffset CreatedAt);

public class GetSubscriptionsQueryHandler(IWebhookRepository repository)
    : IRequestHandler<GetSubscriptionsQuery, List<WebhookSubscriptionDto>>
{
    public async Task<List<WebhookSubscriptionDto>> Handle(GetSubscriptionsQuery request, CancellationToken ct)
    {
        var subscriptions = await repository.GetAllSubscriptionsAsync(ct);
        return subscriptions.Select(s => new WebhookSubscriptionDto(
            s.Id, s.EndpointUrl, s.EventTypes, s.IsActive, s.CreatedAt)).ToList();
    }
}
```

- [ ] **Step 4: Create GetDeliveriesQuery**

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.Application.Webhooks.Queries;

public record GetDeliveriesQuery(Guid SubscriptionId) : IRequest<List<WebhookDeliveryDto>>;

public record WebhookDeliveryDto(
    Guid Id,
    WebhookDeliveryStatus Status,
    int AttemptCount,
    int? ResponseCode,
    DateTimeOffset? LastAttemptAt,
    DateTimeOffset CreatedAt);

public class GetDeliveriesQueryHandler(IWebhookRepository repository)
    : IRequestHandler<GetDeliveriesQuery, List<WebhookDeliveryDto>>
{
    public async Task<List<WebhookDeliveryDto>> Handle(GetDeliveriesQuery request, CancellationToken ct)
    {
        var deliveries = await repository.GetDeliveriesBySubscriptionAsync(request.SubscriptionId, ct);
        return deliveries.Select(d => new WebhookDeliveryDto(
            d.Id, d.Status, d.AttemptCount, d.ResponseCode, d.LastAttemptAt, d.CreatedAt)).ToList();
    }
}
```

- [ ] **Step 5: Create WebhookDispatcher**

```csharp
using System.Text.Json;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;

namespace ApiSecurity.Application.Webhooks;

public class WebhookDispatcher(IWebhookRepository repository) : IWebhookDispatcher
{
    public async Task DispatchAsync(WebhookEventType eventType, string payload, CancellationToken ct = default)
    {
        var subscriptions = await repository.GetActiveSubscriptionsForEventAsync(eventType, ct);

        foreach (var subscription in subscriptions)
        {
            var delivery = WebhookDelivery.Create(subscription.Id, payload);
            await repository.AddDeliveryAsync(delivery, ct);
        }

        if (subscriptions.Count > 0)
            await repository.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 6: Modify CreateProductCommand to dispatch webhook**

Replace the existing `CreateProductCommandHandler` in `src/ApiSecurity.Application/Products/Commands/CreateProductCommand.cs`:

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using ApiSecurity.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ApiSecurity.Application.Products.Commands;

public record CreateProductCommand(string Name, decimal Price, int Stock) : IRequest<Guid>;

public class CreateProductCommandHandler(IProductRepository repository, IWebhookDispatcher dispatcher)
    : IRequestHandler<CreateProductCommand, Guid>
{
    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken ct)
    {
        var product = Product.Create(request.Name, request.Price, request.Stock);
        await repository.AddAsync(product, ct);
        await repository.SaveChangesAsync(ct);

        var payload = System.Text.Json.JsonSerializer.Serialize(new
        {
            product.Id,
            product.Name,
            product.Price,
            product.Stock
        });
        await dispatcher.DispatchAsync(WebhookEventType.ProductCreated, payload, ct);

        return product.Id;
    }
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
    }
}
```

- [ ] **Step 7: Build application layer**

```bash
dotnet build src/ApiSecurity.Application/
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 8: Commit**

```bash
git add src/ApiSecurity.Application/
git commit -m "feat: add webhook commands, queries, dispatcher, and ProductCreated dispatch"
```

---

## Task 9: WebhookDeliveryService + Worker

**Files:**
- Create: `src/ApiSecurity.Infrastructure/Webhooks/WebhookDeliveryService.cs`
- Create: `src/ApiSecurity.Infrastructure/Webhooks/WebhookDeliveryWorker.cs`

- [ ] **Step 1: Create WebhookDeliveryService**

```csharp
using System.Text;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace ApiSecurity.Infrastructure.Webhooks;

public class WebhookDeliveryService(
    IWebhookRepository repository,
    IHttpClientFactory httpClientFactory,
    IWebhookSignatureService signatureService,
    ILogger<WebhookDeliveryService> logger) : IWebhookDeliveryService
{
    public async Task ProcessPendingAsync(CancellationToken ct = default)
    {
        var deliveries = await repository.GetPendingDeliveriesAsync(ct);

        foreach (var delivery in deliveries)
            await ProcessDeliveryAsync(delivery, ct);

        if (deliveries.Count > 0)
            await repository.SaveChangesAsync(ct);
    }

    private async Task ProcessDeliveryAsync(WebhookDelivery delivery, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("webhook");
        var subscription = delivery.Subscription;
        var signature = signatureService.ComputeSignature(subscription.Secret, delivery.Payload);

        var request = new HttpRequestMessage(HttpMethod.Post, subscription.EndpointUrl)
        {
            Content = new StringContent(delivery.Payload, Encoding.UTF8, "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Webhook-Signature", signature);
        request.Headers.TryAddWithoutValidation("X-Delivery-Id", delivery.Id.ToString());

        try
        {
            var response = await client.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                delivery.RecordSuccess((int)response.StatusCode);
                logger.LogInformation("Webhook delivered: {DeliveryId}", delivery.Id);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                delivery.RecordFailure((int)response.StatusCode, body);
                logger.LogWarning("Webhook delivery failed: {DeliveryId}, status {Status}", delivery.Id, response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            delivery.RecordFailure(null, ex.Message);
            logger.LogWarning(ex, "Webhook delivery exception: {DeliveryId}", delivery.Id);
        }
    }
}
```

- [ ] **Step 2: Create WebhookDeliveryWorker**

```csharp
using ApiSecurity.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApiSecurity.Infrastructure.Webhooks;

public class WebhookDeliveryWorker(
    IServiceProvider services,
    ILogger<WebhookDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                using var scope = services.CreateScope();
                var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();
                await deliveryService.ProcessPendingAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled error in webhook delivery worker");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), ct);
        }
    }
}
```

- [ ] **Step 3: Build infrastructure**

```bash
dotnet build src/ApiSecurity.Infrastructure/
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add src/ApiSecurity.Infrastructure/Webhooks/
git commit -m "feat: add WebhookDeliveryService and BackgroundService worker"
```

---

## Task 10: API Endpoints + DI Registration

**Files:**
- Create: `src/ApiSecurity.API/Endpoints/WebhookEndpoints.cs`
- Modify: `src/ApiSecurity.API/Program.cs`

- [ ] **Step 1: Create WebhookEndpoints**

```csharp
using ApiSecurity.Application.Webhooks.Commands;
using ApiSecurity.Application.Webhooks.Queries;
using ApiSecurity.Domain.Enums;
using MediatR;

namespace ApiSecurity.API.Endpoints;

public static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/webhooks")
            .WithTags("Webhooks")
            .RequireRateLimiting("apikey-sliding")
            .RequireAuthorization();

        group.MapPost("/subscriptions", async (CreateSubscriptionRequest req, IMediator mediator) =>
        {
            var result = await mediator.Send(
                new CreateSubscriptionCommand(req.EndpointUrl, req.Secret, req.EventTypes));
            return Results.Created($"/webhooks/subscriptions/{result.Id}", result);
        })
        .WithSummary("Create webhook subscription — requires JWT Bearer");

        group.MapGet("/subscriptions", async (IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetSubscriptionsQuery())))
        .WithSummary("List webhook subscriptions — requires JWT Bearer");

        group.MapDelete("/subscriptions/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var deleted = await mediator.Send(new DeleteSubscriptionCommand(id));
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .WithSummary("Deactivate webhook subscription — requires JWT Bearer");

        group.MapGet("/deliveries/{subscriptionId:guid}", async (Guid subscriptionId, IMediator mediator) =>
            Results.Ok(await mediator.Send(new GetDeliveriesQuery(subscriptionId))))
        .WithSummary("Get delivery history for subscription — requires JWT Bearer");

        return app;
    }

    private record CreateSubscriptionRequest(string EndpointUrl, string Secret, WebhookEventType EventTypes);
}
```

- [ ] **Step 2: Modify Program.cs — add registrations**

Add the following lines to `Program.cs` after the existing `services.AddSingleton<ITokenService, JwtTokenService>();` line:

```csharp
builder.Services.AddScoped<IWebhookRepository, WebhookRepository>();
builder.Services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
builder.Services.AddScoped<IWebhookSignatureService, WebhookSignatureService>();
builder.Services.AddScoped<IWebhookDeliveryService, WebhookDeliveryService>();
builder.Services.AddHostedService<WebhookDeliveryWorker>();
builder.Services.AddHttpClient("webhook").ConfigureHttpClient(c =>
    c.Timeout = TimeSpan.FromSeconds(30));
```

Add the following using directives at the top of `Program.cs`:

```csharp
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Application.Webhooks;
using ApiSecurity.Infrastructure.Repositories;
using ApiSecurity.Infrastructure.Security;
using ApiSecurity.Infrastructure.Webhooks;
```

Add `app.MapWebhookEndpoints();` after `app.MapProductEndpoints();`.

- [ ] **Step 3: Build full solution**

```bash
dotnet build
```

Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run all existing unit tests — must pass**

```bash
dotnet test tests/ApiSecurity.UnitTests/
```

Expected: All previous tests plus new webhook unit tests pass (9+ tests).

- [ ] **Step 5: Commit**

```bash
git add src/ApiSecurity.API/
git commit -m "feat: add webhook endpoints and wire DI for webhook services"
```

---

## Task 11: Integration Tests

**Files:**
- Create: `tests/ApiSecurity.IntegrationTests/Fixtures/FakeWebhookHandler.cs`
- Modify: `tests/ApiSecurity.IntegrationTests/Fixtures/ApiTestFixture.cs`
- Create: `tests/ApiSecurity.IntegrationTests/Webhooks/WebhookSubscriptionTests.cs`

- [ ] **Step 1: Create FakeWebhookHandler**

```csharp
using System.Net;

namespace ApiSecurity.IntegrationTests.Fixtures;

public class FakeWebhookHandler : HttpMessageHandler
{
    public HttpStatusCode ResponseStatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => Task.FromResult(new HttpResponseMessage(ResponseStatusCode));
}
```

- [ ] **Step 2: Modify ApiTestFixture to expose FakeWebhookHandler**

Add `public FakeWebhookHandler WebhookFakeHandler { get; } = new();` as a property.

Add to `ConfigureWebHost` inside the `builder.ConfigureServices` block:

```csharp
services.AddHttpClient("webhook")
    .ConfigurePrimaryHttpMessageHandler(() => WebhookFakeHandler);
```

Full `ApiTestFixture.cs` after edits:

```csharp
using ApiSecurity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ApiSecurity.IntegrationTests.Fixtures;

public class ApiTestFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("apisecurity_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    public FakeWebhookHandler WebhookFakeHandler { get; } = new();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseNpgsql(_postgres.GetConnectionString()));

            services.AddHttpClient("webhook")
                .ConfigurePrimaryHttpMessageHandler(() => WebhookFakeHandler);
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = "test-signing-key-minimum-32-characters-long!!",
                ["Jwt:Issuer"] = "api-security",
                ["Jwt:Audience"] = "api-security",
                ["Jwt:AccessTokenExpiryMinutes"] = "15",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
                ["ConnectionStrings:Default"] = _postgres.GetConnectionString()
            });
        });
    }

    public async Task ApplyMigrationsAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }
}
```

- [ ] **Step 3: Create webhook integration tests**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ApiSecurity.Application.Webhooks.Queries;
using ApiSecurity.Application.Interfaces;
using ApiSecurity.Domain.Enums;
using ApiSecurity.Infrastructure.Persistence;
using ApiSecurity.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ApiSecurity.IntegrationTests.Webhooks;

public class WebhookSubscriptionTests(ApiTestFixture fixture) : IClassFixture<ApiTestFixture>, IAsyncLifetime
{
    private HttpClient _client = null!;
    private string _jwtToken = null!;

    public async Task InitializeAsync()
    {
        await fixture.ApplyMigrationsAsync();
        _client = fixture.CreateClient();

        var loginResp = await _client.PostAsJsonAsync("/auth/login",
            new { Email = "admin@example.com", Password = "Admin123!" });
        var tokens = await loginResp.Content.ReadFromJsonAsync<TokenResponse>();
        _jwtToken = tokens!.AccessToken;
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _jwtToken);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateSubscription_ValidRequest_Returns201()
    {
        var response = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/webhook",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateSubscriptionResult>();
        body!.Id.Should().NotBeEmpty();
        body.EndpointUrl.Should().Be("https://example.com/webhook");
    }

    [Fact]
    public async Task DeleteSubscription_ExistingId_Returns204()
    {
        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/to-delete",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var created = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        var deleteResp = await _client.DeleteAsync($"/webhooks/subscriptions/{created!.Id}");

        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateProduct_WithActiveSubscription_CreatesDelivery()
    {
        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/dispatch-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Dispatch Test Product", Price = 9.99m, Stock = 5 });

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub!.Id}");
        deliveriesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d => d.Status == WebhookDeliveryStatus.Pending);
    }

    [Fact]
    public async Task ProcessPending_SuccessfulDelivery_StatusIsDelivered()
    {
        fixture.WebhookFakeHandler.ResponseStatusCode = HttpStatusCode.OK;

        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/success-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Success Product", Price = 1.00m, Stock = 1 });

        using var scope = fixture.Services.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();
        await deliveryService.ProcessPendingAsync();

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub!.Id}");
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d => d.Status == WebhookDeliveryStatus.Delivered);
    }

    [Fact]
    public async Task ProcessPending_AllAttemptsFail_StatusIsFailed()
    {
        fixture.WebhookFakeHandler.ResponseStatusCode = HttpStatusCode.InternalServerError;

        var createResp = await _client.PostAsJsonAsync("/webhooks/subscriptions", new
        {
            EndpointUrl = "https://example.com/fail-test",
            Secret = "super-secret-minimum-16-chars",
            EventTypes = WebhookEventType.ProductCreated
        });
        var sub = await createResp.Content.ReadFromJsonAsync<CreateSubscriptionResult>();

        await _client.PostAsJsonAsync("/products", new { Name = "Fail Product", Price = 1.00m, Stock = 1 });

        using var scope = fixture.Services.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryService>();

        await deliveryService.ProcessPendingAsync();

        // Reset NextAttemptAt so second and third attempts are picked up immediately
        using var updateScope = fixture.Services.CreateScope();
        var db = updateScope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE \"WebhookDeliveries\" SET \"NextAttemptAt\" = NOW() - INTERVAL '1 second' WHERE \"Status\" = 0 AND \"SubscriptionId\" = {0}",
            sub!.Id);

        await deliveryService.ProcessPendingAsync();

        using var updateScope2 = fixture.Services.CreateScope();
        var db2 = updateScope2.ServiceProvider.GetRequiredService<AppDbContext>();
        await db2.Database.ExecuteSqlRawAsync(
            "UPDATE \"WebhookDeliveries\" SET \"NextAttemptAt\" = NOW() - INTERVAL '1 second' WHERE \"Status\" = 0 AND \"SubscriptionId\" = {0}",
            sub.Id);

        await deliveryService.ProcessPendingAsync();

        var deliveriesResp = await _client.GetAsync($"/webhooks/deliveries/{sub.Id}");
        var deliveries = await deliveriesResp.Content.ReadFromJsonAsync<List<WebhookDeliveryDto>>();
        deliveries.Should().ContainSingle(d =>
            d.Status == WebhookDeliveryStatus.Failed && d.AttemptCount == 3);
    }

    private record TokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
    private record CreateSubscriptionResult(Guid Id, string EndpointUrl, WebhookEventType EventTypes);
}
```

- [ ] **Step 4: Run integration tests**

```bash
dotnet test tests/ApiSecurity.IntegrationTests/ --filter "FullyQualifiedName~WebhookSubscriptionTests"
```

Expected: 5 tests pass. If any fail, check migration ran (ApplyMigrationsAsync in fixture), check FakeWebhookHandler is registered before the production `AddHttpClient("webhook")`.

- [ ] **Step 5: Run full test suite**

```bash
dotnet test
```

Expected: All 31 existing tests + 9 new tests = 40+ tests passing, 0 failed.

- [ ] **Step 6: Commit**

```bash
git add tests/
git commit -m "feat: add webhook integration tests with FakeWebhookHandler"
```

---

## Task 12: AI Workflow Retrospective

**Files:**
- Create: `docs/ai-workflow/retrospective.md`

- [ ] **Step 1: Create retrospective**

```markdown
# AI-Assisted Development — How I Work

This document describes my AI-assisted development workflow using Claude Code.
It is not a session log — it is a description of process and concrete examples of where AI helps and where human judgment is required.

## Workflow

1. **Load context** — CLAUDE.md is loaded at session start. AI tools receive architecture constraints, ADR references, and test conventions before generating any code.
2. **Generate draft** — entity models, handlers, tests, and service skeletons are generated as a starting point.
3. **Review against constraints** — every generated file is checked against CLAUDE.md constraints and relevant ADRs before accepting.
4. **Adjust what fails review** — document what was changed and why (see PR descriptions for webhook delivery feature).
5. **Run full test suite** — AI-generated tests are included. Tests that don't cover real invariants are rewritten.

## What AI does well in this codebase

- **Boilerplate**: entity models, EF configurations, DI registration, endpoint routing
- **Test scaffolding**: happy path unit tests, edge case suggestions for obvious failure modes
- **Documentation drafts**: ADR structure, README sections, commit messages
- **Repetitive patterns**: each new command/query/handler follows the same structure — AI generates it accurately after seeing the first example

## Where I always review carefully

- **Security defaults**: AI defaults to permissive CORS (`AllowAnyHeader`, `AllowAnyOrigin`). Every CORS configuration is manually verified.
- **Retry and timing logic**: AI generates fixed delays. I apply exponential backoff (5s, 25s, 125s) — per the philosophy in ADR-002: prefer built-in .NET behavior and well-understood patterns over ad-hoc solutions.
- **Type safety**: AI sometimes uses string constants where a typed enum belongs. `WebhookDeliveryStatus` started as string literals.
- **Secret handling**: AI included `Secret` in the `GetSubscriptionsQuery` response DTO. Secrets are write-only — never returned in responses.
- **Testcontainers fixture quirks**: AI is unaware of runtime behavior. The `IpFilter:Allowlist:0 = ""` override bug (creates `HashSet{""}` ≠ empty → all 403) is documented in CLAUDE.md and required human debugging.

## Three real examples from this project

### 1. API key format: `ask_<prefix8><secret>`

**AI-generated approach:** Store the full key SHA-256 hashed, no prefix.

**What I changed:** Store the first 8 chars of the key body as a plaintext prefix, indexed. Store only the hash.

**Why:** To look up a key without scanning the full table, you need a fast discriminator. A full-scan hash comparison over thousands of keys is O(n). A prefix lookup is O(log n) with an index. AI doesn't know your query patterns — you do. Documented in ADR-001.

### 2. Rate limiter: built-in vs Redis

**AI-generated approach:** Redis-backed sliding window using StackExchange.Redis.

**What I changed:** Built-in `Microsoft.AspNetCore.RateLimiting` (no external dependency).

**Why:** Redis adds operational complexity (connection management, failover, cost). For a single-instance API, the built-in limiter is zero-dependency and correct. The limitation (not distributed) is explicitly documented as a known constraint in ADR-002. AI defaults to "what's commonly demonstrated online," not "what fits your constraints."

### 3. Testcontainers IpFilter fixture bug

**AI-generated test fixture:** Added `["IpFilter:Allowlist:0"] = ""` to the in-memory config overrides.

**What broke:** `IpFilterMiddleware` builds `HashSet<string>` from config. An override of `""` creates `HashSet{""}` — not empty. Every request was blocked with 403.

**Why AI missed it:** The bug only manifests at runtime. AI cannot run the tests. Systematic debugging (binary elimination of config overrides) found the culprit. The fix (`do not override IpFilter:Allowlist keys`) and the rule are now in CLAUDE.md.
```

- [ ] **Step 2: Commit**

```bash
git add docs/ai-workflow/retrospective.md
git commit -m "docs: add AI workflow retrospective with real examples"
```

---

## Task 13: README Update + Final Verification

**Files:**
- Modify: `README.md`

- [ ] **Step 1: Add AI Workflow section to README**

Add after the existing "Architecture" section in `README.md`:

```markdown
## AI-Assisted Development

This project was built using Claude Code as the primary AI coding assistant.

- [`CLAUDE.md`](CLAUDE.md) — Architecture context loaded at every AI session
- [`docs/ai-workflow/retrospective.md`](docs/ai-workflow/retrospective.md) — How AI is used, where it helps, and where human review is required
- PR descriptions include an **AI-Assisted Development Log** documenting what was generated vs. modified

Key principle: AI generates the draft. The ADRs, constraints in CLAUDE.md, and test suite define what "correct" means. The developer validates the output against those standards.
```

- [ ] **Step 2: Final test run**

```bash
dotnet test
```

Expected: All tests pass (40+). 0 failures.

- [ ] **Step 3: Commit**

```bash
git add README.md
git commit -m "docs: add AI workflow section to README"
```

- [ ] **Step 4: Verify git log**

```bash
git log --oneline -15
```

Verify all tasks are committed with meaningful messages.

---

## PR Description Template (for the webhook feature branch, if using branches)

When creating the PR for tasks 2-11, use this description:

```markdown
## AI-Assisted Development Log

### Generated with Claude
- Entity models: WebhookSubscription, WebhookDelivery, status/event enums
- WebhookSignatureService (HMAC-SHA256 implementation)
- BackgroundService skeleton and delivery loop structure
- EF Core configurations for both entities
- Initial unit tests for signature service and entity behavior
- Command/query handler scaffolding

### Modified after review
- **Retry strategy**: Claude generated fixed 2s delay between attempts →
  replaced with exponential backoff (5s, 25s, 125s per attempt).
  Reason: ADR-002 philosophy — prefer well-understood .NET patterns over ad-hoc solutions.
- **Secret in API response**: Claude included `Secret` in `GetSubscriptionsQuery` DTO →
  removed. Secrets are write-only.
- **Delivery status type**: Claude used string constants ("pending", "failed") →
  replaced with `WebhookDeliveryStatus` enum for type safety and exhaustive switching.
- **CORS on webhook endpoints**: Claude added `AllowAnyHeader()` to endpoint group →
  removed. Violates CLAUDE.md constraint and existing named CORS policy.
- **Worker scope**: Claude created `WebhookDeliveryService` as singleton →
  changed to scoped + worker uses `IServiceProvider` to create scope per tick.
  Reason: `AppDbContext` is scoped; a singleton cannot hold a scoped service.

### Validated against
- CLAUDE.md constraints: CORS policy, file placement, test conventions ✅
- ADR-002 philosophy applied to retry strategy ✅
- Existing auth scheme reused (JWT Bearer, no new scheme) ✅
- All 31 existing tests still pass ✅
```
