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
