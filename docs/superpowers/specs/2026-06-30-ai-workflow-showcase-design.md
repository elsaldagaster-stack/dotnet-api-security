# AI Workflow Showcase — Design Spec

**Date:** 2026-06-30  
**Repo:** dotnet-api-security  
**Approach:** Enfoque C — Hybrid auténtico (CLAUDE.md + live feature + honest retrospective)

---

## Goal

Make AI-assisted development workflow visible and credible to recruiters and clients evaluating senior .NET candidates. Target job description explicitly requires: responsible AI tool use, ability to validate AI-generated code against standards, and non-blind dependency on AI.

**Not building AI into the product.** Building evidence that AI is used responsibly during development.

---

## Structure

```
dotnet-api-security/
├── CLAUDE.md                                  ← new: repo context for AI tools
├── docs/
│   ├── adr/                                   ← existing, unchanged
│   ├── ai-workflow/
│   │   └── retrospective.md                   ← new: honest AI workflow documentation
│   └── superpowers/
│       └── specs/
│           └── 2026-06-30-ai-workflow-showcase-design.md  ← this file
├── src/
│   ├── ApiSecurity.Domain/
│   │   └── Webhooks/
│   │       ├── WebhookSubscription.cs
│   │       ├── WebhookDelivery.cs
│   │       └── WebhookDeliveryStatus.cs
│   ├── ApiSecurity.Application/
│   │   └── Features/Webhooks/
│   │       ├── CreateSubscription/
│   │       ├── DeleteSubscription/
│   │       ├── GetDeliveries/
│   │       ├── WebhookDispatcher.cs
│   │       └── WebhookSignatureService.cs
│   ├── ApiSecurity.Infrastructure/
│   │   └── Webhooks/
│   │       └── WebhookDeliveryWorker.cs       ← BackgroundService
│   └── ApiSecurity.API/
│       └── Endpoints/
│           └── WebhookEndpoints.cs
└── tests/
    ├── ApiSecurity.UnitTests/
    │   └── Webhooks/
    │       ├── WebhookSignatureServiceTests.cs
    │       └── WebhookRetryTests.cs
    └── ApiSecurity.IntegrationTests/
        └── Webhooks/
            └── WebhookDeliveryTests.cs
```

---

## Artifact 1: CLAUDE.md

Location: `dotnet-api-security/CLAUDE.md`

Permanent context file for AI tools. Describes what must never be violated — not what the code does (README covers that).

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

## Test conventions
- Unit tests: xUnit + FluentAssertions, no I/O
- Integration tests: Testcontainers PostgreSQL, WebApplicationFactory
- Never override IpFilter:Allowlist in test fixtures (creates HashSet{""} → all 403)

## File placement
- New features: Application/Features/<FeatureName>/
- Middleware: API/Middleware/
- Auth handlers: Infrastructure/Auth/
- Background workers: Infrastructure/<Domain>/

## ADRs
- ADR-001: docs/adr/ADR-001-api-key-hashing.md
- ADR-002: docs/adr/ADR-002-rate-limiting-strategy.md
- ADR-003: docs/adr/ADR-003-jwt-expiry-refresh.md
```

---

## Artifact 2: Webhook Delivery Feature

New security-adjacent feature added using AI-first workflow. The PR itself is the showcase artifact.

### Domain

**WebhookSubscription**
- `Id` (Guid)
- `EndpointUrl` (string)
- `Secret` (string — stored plaintext, used for HMAC only, never returned in API responses)
- `EventTypes` (flags enum: `ProductCreated`, `ProductUpdated`, `ProductDeleted`)
- `IsActive` (bool)
- `CreatedAt` (DateTimeOffset)

**WebhookDelivery**
- `Id` (Guid)
- `SubscriptionId` (Guid FK)
- `Payload` (string JSON)
- `AttemptCount` (int, max 3)
- `Status` (enum: `Pending`, `Delivered`, `Failed`)
- `ResponseCode` (int?)
- `ResponseBody` (string?)
- `LastAttemptAt` (DateTimeOffset?)
- `CreatedAt` (DateTimeOffset)

### Delivery Flow

```
Domain event raised
  → WebhookDispatcher.DispatchAsync(eventType, payload)
    → query active subscriptions for eventType
    → create WebhookDelivery (status: Pending) per subscription
    → persist via EF Core

WebhookDeliveryWorker (BackgroundService, runs every 10s)
  → fetch Pending deliveries
  → POST to EndpointUrl
    → headers: Content-Type: application/json
               X-Webhook-Signature: sha256=<HMAC-SHA256(secret, payload)>
               X-Delivery-Id: <DeliveryId>
  → 2xx response → status = Delivered
  → non-2xx or exception:
    → AttemptCount++
    → schedule next attempt: 5s * 5^(AttemptCount-1) [5s, 25s, 125s]
    → AttemptCount >= 3 → status = Failed, persist last ResponseBody
```

### HMAC Signature

```csharp
// X-Webhook-Signature: sha256=<hex>
var hash = HMACSHA256.HashData(
    key: Encoding.UTF8.GetBytes(secret),
    source: Encoding.UTF8.GetBytes(payload));
var signature = $"sha256={Convert.ToHexString(hash).ToLowerInvariant()}";
```

### Endpoints

```
POST   /webhooks/subscriptions         create subscription (requires ApiKey auth)
GET    /webhooks/subscriptions         list active subscriptions (requires ApiKey auth)
DELETE /webhooks/subscriptions/{id}    deactivate subscription (requires ApiKey auth)
GET    /webhooks/deliveries/{id}       get delivery history for subscription (requires ApiKey auth)
```

All endpoints require existing `ApiKey` authentication. No new auth scheme.

### Tests

**Unit — WebhookSignatureServiceTests**
- Correct HMAC: same secret + payload → same signature
- Different secret → different signature
- Different payload → different signature
- Signature format starts with `sha256=`

**Unit — WebhookRetryTests**
- AttemptCount 0 → delay 5s
- AttemptCount 1 → delay 25s
- AttemptCount 2 → delay 125s
- AttemptCount >= 3 → status = Failed

**Integration — WebhookDeliveryTests (Testcontainers)**
- Create subscription → raise event → delivery created (Pending)
- Successful delivery → status = Delivered, response code persisted
- Failed delivery → AttemptCount increments, status = Failed after max attempts

### PR Description Template (the visible showcase)

```markdown
## AI-Assisted Development Log

### Generated with Claude
- Entity models: WebhookSubscription, WebhookDelivery, WebhookDeliveryStatus enum
- WebhookSignatureService (HMAC-SHA256 implementation)
- BackgroundService skeleton and delivery loop
- 12 unit tests (signature correctness + retry logic)

### Modified after review
- **Retry strategy**: Claude generated fixed 2s delay between attempts →
  replaced with exponential backoff (5s, 25s, 125s).
  Reason: ADR-002 philosophy — prefer built-in .NET patterns; fixed delay
  hammers failing endpoints unnecessarily.
- **CORS on webhook endpoints**: Claude added AllowAnyHeader() →
  removed. Violates CLAUDE.md constraint and existing CORS policy.
- **Delivery status**: Claude used string constants ("pending", "failed") →
  replaced with WebhookDeliveryStatus enum. Type safety, exhaustive switch.
- **Secret in API response**: Claude included Secret in GetSubscriptions response →
  removed. Secrets are write-only.

### Validated against
- CLAUDE.md constraints: CORS policy, file placement, test conventions ✅
- ADR-002 philosophy applied to retry strategy ✅
- Existing auth: reuses ApiKey authentication scheme, no new scheme ✅
```

---

## Artifact 3: `docs/ai-workflow/retrospective.md`

Honest documentation of AI workflow. Not a fabricated session log — a credible description of how AI is used in this codebase with real examples from development.

### Structure

1. **Workflow** — step-by-step process (CLAUDE.md → generate → review → adjust → test)
2. **What AI does well here** — boilerplate, test scaffolding, doc drafts
3. **Where I always review carefully** — security defaults, retry logic, type safety, Testcontainers quirks
4. **Three real examples** from this project:
   - API key prefix pattern (AI missed query optimization need)
   - Rate limiter: Redis suggestion vs built-in (AI defaults to popular, not constrained)
   - Testcontainers IpFilter fixture bug (AI can't run tests)

---

## Success Criteria

- [ ] CLAUDE.md exists at repo root, loaded by AI tools automatically
- [ ] Webhook feature builds and all tests pass (`dotnet test`)
- [ ] PR for webhook feature has AI-Assisted Development Log section
- [ ] `docs/ai-workflow/retrospective.md` exists with real examples
- [ ] No fabricated content — every claim is verifiable or honestly framed
- [ ] GitHub repo README references ai-workflow docs

---

## Out of Scope

- Fake conversation transcripts with AI tools
- AI integration in the product itself (no LLM endpoints)
- New authentication scheme for webhooks (reuses existing ApiKey)
- Webhook secret rotation (future feature)
- Distributed delivery worker (single-instance only, documented as limitation)
