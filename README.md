# dotnet-api-security

![CI](https://github.com/elsaldagaster-stack/dotnet-api-security/actions/workflows/ci.yml/badge.svg)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-grade security hardening layer for .NET APIs. Demonstrates 8 security patterns applied to a demo Products API.

## What this project demonstrates

| Pattern | Implementation |
|---------|---------------|
| API Key Management | Custom `IAuthenticationHandler`, SHA-256 storage, scopes |
| JWT Hardening | 15min access + 7-day refresh, zero clock skew |
| Rate Limiting | Sliding window (built-in .NET), per-IP + per-API key |
| Security Headers | 7 headers via middleware (CSP, HSTS, X-Frame-Options...) |
| CORS | Named policy, explicit origins, no wildcards |
| IP Filter | Allowlist/denylist from config, O(1) lookup |
| Audit Logging | 401/403/429 events persisted to PostgreSQL |
| Request Validation | FluentValidation pipeline, anti-injection |

## Architecture

Clean Architecture — Domain / Application / Infrastructure / API layers. Security features are cross-cutting concerns in middleware, authentication handlers, and pipeline behaviors.

```
Request → SecurityHeaders → IpFilter → AuditLog → CORS → RateLimiter → Auth → Endpoints
```

## AI-Assisted Development

This project was built using Claude Code as the primary AI coding assistant.

- [`CLAUDE.md`](CLAUDE.md) — Architecture context loaded at every AI session
- [`docs/ai-workflow/retrospective.md`](docs/ai-workflow/retrospective.md) — How AI is used, where it helps, and where human review is required
- PR descriptions include an **AI-Assisted Development Log** documenting what was generated vs. modified

Key principle: AI generates the draft. The ADRs, constraints in CLAUDE.md, and test suite define what "correct" means. The developer validates the output against those standards.

## Quick Start

**Prerequisites:** Docker Desktop

```bash
git clone https://github.com/elsaldagaster-stack/dotnet-api-security
cd dotnet-api-security
docker compose up
```

API: `http://localhost:8080`  
Docs: `http://localhost:8080/scalar/v1`  
Seq logs: `http://localhost:5341`

## Testing the security features

```bash
# 1. Login (JWT)
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}'

# 2. Create API key (use JWT from step 1)
curl -X POST http://localhost:8080/apikeys \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"name":"My Key","scopes":1}'

# 3. Use API key for Products
curl http://localhost:8080/products \
  -H "X-Api-Key: ask_<your-key>"

# 4. Trigger rate limit (11 rapid auth requests)
for i in {1..12}; do
  curl -s -o /dev/null -w "%{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done
```

## Project structure

```
src/
  ApiSecurity.Domain/        — Entities, enums (no dependencies)
  ApiSecurity.Application/   — Commands, queries, interfaces (MediatR)
  ApiSecurity.Infrastructure/— EF Core, repositories, JWT/ApiKey services
  ApiSecurity.API/           — Endpoints, middleware, authentication handlers
tests/
  ApiSecurity.UnitTests/     — ApiKeyHasher (7), JwtTokenService (5), SecurityHeaders (6)
  ApiSecurity.IntegrationTests/ — Testcontainers PostgreSQL, full stack tests (13)
docs/adr/                    — Architecture Decision Records
```

## Key design decisions

- [ADR-001: API Key hashing strategy](docs/adr/ADR-001-api-key-hashing.md)
- [ADR-002: Rate limiting — built-in vs third party](docs/adr/ADR-002-rate-limiting-strategy.md)
- [ADR-003: JWT expiry and refresh strategy](docs/adr/ADR-003-jwt-expiry-refresh.md)

## YouTube tutorial

*"Seguridad en APIs .NET — Checklist de Producción Completo"*  
🎥 [Link coming soon]

## Built With

- ASP.NET Core Minimal APIs
- Entity Framework Core + Npgsql (PostgreSQL)
- MediatR + FluentValidation
- Microsoft.AspNetCore.RateLimiting (built-in sliding window)
- Serilog + Seq, OpenTelemetry + Jaeger
- xUnit + FluentAssertions + Testcontainers
- Claude Code (AI pair programming)
