# ADR-001: API Key Hashing Strategy

**Status:** Accepted  
**Date:** 2026-06-27

## Context
API keys must be stored securely. If the database is compromised, plaintext keys would give attackers immediate access to all tenants' integrations.

## Decision
Store API keys as SHA-256 hashes. The first 8 characters of the key's random part are stored in plaintext as a "prefix" to enable lookup without scanning all hashes. The full plaintext key is returned only once at creation time.

**Key format:** `ask_<prefix8><secret_rest>` where `prefix8` is stored plaintext and the full key is hashed.

## Consequences
- Plaintext key is never stored — users must save it at creation time.
- Lookup: find by prefix (indexed), then compare full hash.
- SHA-256 is acceptable here because API keys are 32 bytes of random entropy (not passwords). BCrypt adds unnecessary latency without security benefit for random secrets.
