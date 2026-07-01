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
