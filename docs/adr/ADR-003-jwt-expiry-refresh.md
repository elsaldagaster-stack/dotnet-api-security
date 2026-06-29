# ADR-003: JWT Expiry and Refresh Token Strategy

**Status:** Accepted  
**Date:** 2026-06-27

## Context
JWT access tokens must balance security (short-lived) with usability (not forcing users to log in every 15 minutes).

## Decision
- **Access token:** 15-minute expiry, `ClockSkew = TimeSpan.Zero` (enforced strictly).
- **Refresh token:** Also a signed JWT, 7-day expiry, different audience (`api-security-refresh`).
- **No token blacklist:** For portfolio scope. Production would use Redis to invalidate refresh tokens on logout.

## Consequences
- Access token compromised: attacker has ≤15 minutes.
- Refresh token allows silent renewal without re-login.
- True revocation requires Redis (documented limitation — see Proyecto 6 IdentityServer for full implementation).
