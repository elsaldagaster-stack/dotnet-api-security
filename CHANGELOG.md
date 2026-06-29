# Changelog

## [Unreleased]

### Added
- API Key management with SHA-256 hashing and prefix-based lookup
- JWT authentication with 15-minute access tokens and 7-day refresh tokens
- Sliding window rate limiting (10/min for auth, 1000/min per API key)
- Security headers middleware (CSP, X-Frame-Options, Referrer-Policy, Permissions-Policy)
- IP allowlist/denylist middleware configurable from appsettings
- Audit logging for 401/403/429 events to PostgreSQL
- FluentValidation request validation pipeline
- Integration tests with Testcontainers (PostgreSQL)
- GitHub Actions CI with Docker smoke test
- Docker Compose with PostgreSQL 16 + Seq
