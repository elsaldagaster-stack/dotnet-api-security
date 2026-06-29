# ADR-002: Rate Limiting — Built-in vs Third Party

**Status:** Accepted  
**Date:** 2026-06-27

## Context
Rate limiting is required to prevent brute-force attacks and abuse. Options: Microsoft.AspNetCore.RateLimiting (built-in since .NET 7), AspNetCoreRateLimit (third party), Redis-backed distributed limiter.

## Decision
Use `Microsoft.AspNetCore.RateLimiting` (built-in) with `SlidingWindowRateLimiter`. Two named policies:
- `ip-sliding` (10 req/min per IP): applied to `/auth/*` endpoints to prevent brute force.
- `apikey-sliding` (1000 req/min per API key, 100 per IP for unauthenticated): applied to `/products/*`.

## Consequences
- No extra dependency, ships with .NET.
- Not distributed — if app scales to N instances, each instance has its own counter. Acceptable for portfolio; production would use Redis-backed limiter.
- SlidingWindow prevents burst at window boundary (vs fixed window).
