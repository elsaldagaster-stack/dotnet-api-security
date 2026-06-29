# YouTube Mini-Serie: dotnet-api-security

**Fecha:** 2026-06-29  
**Proyecto base:** dotnet-api-security (portafolio .NET 10)  
**Canal:** elsaldagaster — objetivo inbound freelance senior  
**Repo:** https://github.com/elsaldagaster-stack/dotnet-api-security

---

## Decisiones de formato

| Variable | Decisión |
|----------|----------|
| Formato | Mini-serie: 3 episodios consecutivos |
| Estilo | Walk-through del repo ya construido |
| Duración | 25-35 min por episodio |
| Ángulo | "Las decisiones que los tutoriales omiten" |
| Estructura | Split por capa de responsabilidad |
| Idioma | Español |

**Ángulo central:** No es un tutorial de cómo escribir el código — es un análisis de por qué se tomó cada decisión de diseño. Los ADRs son el hilo conductor. El código ilustra las decisiones, no al revés.

---

## Estructura general

```
Ep1: Identidad        — JWT hardening + API keys + CORS
Ep2: Protección       — Rate limiting + IP filter + Security headers
Ep3: Observabilidad   — Audit logging + FluentValidation + Testcontainers
```

Cada episodio sigue la misma plantilla:
1. Hook con problema concreto (90 seg)
2. 2-3 segmentos técnicos con ADR → código → razonamiento
3. Demo en vivo (5-7 min)
4. Recap de decisiones + CTA al siguiente episodio

---

## Episodio 1: Identidad

**Título:** *"JWT + API Keys en .NET: Las decisiones que tu tutorial no te explicó"*

### Hook (0:00–1:30)
Terminal con curl mostrando tres tipos distintos de 401. Setup: "Hoy no escribimos código — revisamos decisiones reales con trade-offs reales."

### Segmento 1 — API Keys (1:30–8:00)

**Apertura:** ADR-001 en pantalla.

**Decisión central:** SHA-256 vs BCrypt.
- BCrypt es lento a propósito para passwords — protege contra brute force en caso de robo de DB
- API key tiene 32 bytes de entropía aleatoria (`RandomNumberGenerator.GetBytes(32)`) → espacio `2^256`
- BCrypt en API keys: 200-400ms de latencia por request sin beneficio de seguridad
- SHA-256: microsegundos, correcto para secretos con alta entropía

**Código a mostrar:**
- `src/ApiSecurity.Infrastructure/Security/ApiKeyHasher.cs`
  - `RandomNumberGenerator.GetBytes(32)` — por qué no `Random.Shared`
  - Formato `ask_<prefix8><secret>` — prefix en plaintext indexado para O(1) lookup
  - Hash del secreto completo con SHA-256
- `src/ApiSecurity.API/Authentication/ApiKeyAuthenticationHandler.cs`
  - Flujo: extrae prefix → `WHERE prefix = @prefix` → compara hash → construye claims
- `src/ApiSecurity.Domain/Enums/ApiKeyScope.cs`
  - `[Flags]` enum, OR bit a bit para combinar scopes, AND para verificar

### Segmento 2 — JWT Hardening (8:00–15:00)

**Apertura:** ADR-003 en pantalla.

**Decisión central:** Expiración de 15 minutos + ClockSkew=Zero.
- Token comprometido = ventana de daño hasta expiración
- Sin revocación distribuida, el tiempo es la única defensa
- 15 min limita daño a 15 min máximo
- ClockSkew por defecto en ASP.NET Core: 5 minutos de tolerancia → token "de 15 min" dura hasta 20 min
- `TimeSpan.Zero` hace que expire exactamente cuando dice

**Decisión secundaria:** Refresh token como JWT con audience separada.
- Audience `api-security` → access token, endpoints de negocio
- Audience `api-security-refresh` → refresh token, solo `/auth/refresh`
- Si access token es interceptado, no sirve para refresh (audience mismatch → 401)
- `Jti` claim único por token para tracing

**Código a mostrar:**
- `src/ApiSecurity.Infrastructure/Security/JwtTokenService.cs`
  - `BuildAccessToken` — claims, expiración, audience
  - `BuildRefreshToken` — audience diferente, Jti único
- `src/ApiSecurity.API/Extensions/AuthenticationExtensions.cs`
  - `ClockSkew = TimeSpan.Zero`
  - Dos esquemas registrados: `Bearer` default + `ApiKey`
- `src/ApiSecurity.API/appsettings.json` — sección `Jwt`, todas las keys configurables

### Segmento 3 — CORS (15:00–22:00)

**Apertura:** Código anti-pattern en pantalla: `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`.

**Decisión central:** Origins explícitos desde configuración, sin wildcards.
- `AllowAnyOrigin()` es incompatible con `AllowCredentials()` — runtime exception en ASP.NET Core
- Wildcards = cualquier dominio puede hacer requests cross-origin con credenciales del usuario
- Named policy: origins desde `appsettings.json`, fallback a localhost:3000/5173 para dev
- Orden en pipeline: después de SecurityHeaders, antes de Auth

**Código a mostrar:**
- `src/ApiSecurity.API/Extensions/CorsExtensions.cs`
- `src/ApiSecurity.API/Program.cs` — posición de `UseCors()` en pipeline

### Demo (22:00–28:00)

```bash
# Login → bearer token
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq

# Usar bearer en products
curl http://localhost:8080/products \
  -H "Authorization: Bearer <ACCESS_TOKEN>" | jq

# Crear API key
curl -X POST http://localhost:8080/apikeys \
  -H "Authorization: Bearer <ACCESS_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo Key","scopes":1}' | jq

# Usar API key
curl http://localhost:8080/products \
  -H "X-Api-Key: ask_<key>" | jq

# Demostrar audience separation: access token NO sirve como refresh token
curl -X POST http://localhost:8080/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<ACCESS_TOKEN>"}' | jq
# → 401 (audience mismatch)
```

### Cierre (28:00–30:00)

Recap de 4 decisiones con una línea cada una. CTA a Ep2.

**CTA:** "Ep2 cubre cómo protegemos cada request en runtime — rate limiting, IP filter, security headers."

---

## Episodio 2: Protección Runtime

**Título:** *"Rate Limiting e IP Filter en .NET: Sin Redis, Sin Dependencias Externas"*

### Hook (0:00–1:30)
GitHub star history de AspNetCoreRateLimit con nota "last significant commit: 2022". Contraste con `Microsoft.AspNetCore.RateLimiting` built-in desde .NET 7.

### Segmento 1 — Rate Limiting (1:30–9:00)

**Apertura:** ADR-002 en pantalla.

**Decisión central:** Built-in vs Redis vs terceros.
- Built-in: cero dependencias, ships con .NET, sliding window incluido
- Limitación honesta: single-instance. N instancias → N contadores independientes. Documentado en ADR, no ocultado.
- Cuándo necesitas Redis: escala horizontal con 2+ instancias, SLA estricto en rate limiting

**Sliding window vs fixed window:**
- Fixed window: reset al minuto. Atacante hace 10 req a :59 + 10 req a :00 = 20 req en 2 seg
- Sliding window: ventana se mueve. En cualquier período de 60s, máximo 10 req. Sin burst en boundary.

**Tres políticas:**
- Global: 200 req/min, cualquier IP. Protección base del servidor.
- `ip-sliding`: 10 req/min por IP. Aplicada a `/auth/*`. Hace brute force impracticable.
- `apikey-sliding`: 1000 req/min si tiene API key (partition por prefix), 100/min si no.

**Partition key de `apikey-sliding`:** Primeros 8 caracteres del header `X-Api-Key` (el prefix). No el header completo — principio de mínima exposición en memoria.

**Código a mostrar:**
- `src/ApiSecurity.API/Extensions/RateLimitingExtensions.cs` — las 3 políticas
- `src/ApiSecurity.API/Endpoints/` — `[EnableRateLimiting("nombre")]` por endpoint
- `src/ApiSecurity.API/Program.cs` — posición de `UseRateLimiter()` en pipeline

### Segmento 2 — IP Filter (9:00–15:00)

**Decisión central:** HashSet para O(1) lookup, config-driven, early en pipeline.
- Allowlist habilitada → solo IPs en lista pasan. Vacía = deshabilitada (no = "nadie pasa").
- Denylist → IPs explícitamente bloqueadas, 403.
- Allowlist tiene prioridad sobre denylist.
- `HashSet<string>`: lookup O(1) sin importar tamaño de lista.

**Bug real documentado:**  
En tests de integración: `IpFilter:Allowlist:0 = ""` crea `HashSet{""}`. Allowlist activa con un elemento vacío. Ninguna IP matchea con `""` → todos los tests devuelven 403. Con mocks: bug invisible. Con stack real: aparece inmediatamente.

**Orden en pipeline:** Segundo, después de SecurityHeaders. Si IP bloqueada, el request muere aquí — no gasta recursos en CORS, auth, rate limit.

**Código a mostrar:**
- `src/ApiSecurity.API/Middleware/IpFilterMiddleware.cs`
  - Construcción de HashSets desde IConfiguration
  - Lógica allowlist (si habilitada y no está → 403) antes que denylist
- `src/ApiSecurity.API/Program.cs` — posición en pipeline

### Segmento 3 — Security Headers (15:00–21:00)

**Apertura:** Browser devtools mostrando `Server: Kestrel` en respuesta sin middleware.

**Los 7 headers + razonamiento:**

| Header | Valor | Por qué |
|--------|-------|---------|
| `X-Content-Type-Options` | `nosniff` | Evita content-sniffing del browser |
| `X-Frame-Options` | `DENY` | Previene clickjacking vía iframe |
| `X-XSS-Protection` | `1; mode=block` | Filtro XSS para browsers legacy |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Evita filtrar paths con tokens en `Referer` |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Deshabilita features de browser no usados |
| `Content-Security-Policy` | `default-src 'self'` | Solo recursos del mismo origen |
| `Strict-Transport-Security` | `max-age=15552000` | HSTS: 6 meses, solo HTTPS |

**Header removido:** `Server` — Kestrel no se anuncia.

**Detalle técnico:** Headers en `OnStarting` callback, no directamente en `Invoke`. Headers de respuesta no pueden modificarse después de iniciada la respuesta — `OnStarting` ejecuta justo antes del envío.

**Código a mostrar:**
- `src/ApiSecurity.API/Middleware/SecurityHeadersMiddleware.cs`
  - `context.Response.OnStarting(...)` — por qué callback y no directo
  - `context.Response.Headers.Remove("Server")`

### Middleware Pipeline Deep-Dive (27:00–30:00)

```
SecurityHeaders → IpFilter → AuditLog → CORS → RateLimiter → Auth → Endpoints
```

Razón de cada posición:
- SecurityHeaders primero → headers en todas las respuestas, incluidos errores tempranos
- IpFilter segundo → muerte temprana si IP bloqueada, sin procesar nada más
- AuditLog tercero → observación post-pipeline via `OnCompleted` callback
- CORS cuarto → preflights antes de Auth
- RateLimiter quinto → sin gastar recursos de auth si sobre límite
- Auth sexto → solo si pasó todo lo anterior

### Demo (21:00–27:00)

```bash
# Rate limit en auth (11 requests rápidos)
for i in {1..12}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done
# Requests 1-10: 401. Request 11+: 429.

# Security headers en browser devtools
# Agregar IP a denylist en appsettings.json + restart
curl -v http://localhost:8080/products -H "Authorization: Bearer <TOKEN>"
# → 403 (IP filter, antes de que auth corra)
```

Seq UI en 5341: filtrar eventos por IP bloqueada.

### Cierre (30:00–32:00)

Recap + CTA. "Ep3 cierra con audit logging, FluentValidation en pipeline MediatR, y Testcontainers — incluyendo el bug real que mencioné sobre el IP filter."

---

## Episodio 3: Observabilidad

**Título:** *"Audit Logging + Testcontainers en .NET: Tests que Sí Agarran Bugs Reales"*

### Hook (0:00–1:30)
GitHub Actions CI verde con 31 tests. "Esto no pasó a la primera. Tuve un bug donde todos los tests de integración devolvían 403. Con mocks no habría aparecido."

### Segmento 1 — Audit Logging (1:30–9:00)

**Decisión central:** Loguear 401/403/429 a PostgreSQL, post-pipeline.

**Por qué esos tres códigos:**
- 401: puede ser token expirado legítimo O escaneo con tokens robados
- 403: scope incorrecto O IP bloqueada — semántica diferente, mismo código
- 429: potencialmente brute force, necesita visibilidad
- 200/404/etc: ruido, no eventos de seguridad

**Por qué post-pipeline:**
- Necesito el status code final
- Un request aparentemente válido puede terminar en 401 después de auth
- `OnCompleted` callback: ejecuta después de que la respuesta se envía

**Tipos de eventos:** `LoginSucceeded`, `LoginFailed`, `TokenRefreshed`, `ApiKeyUsed`, `ApiKeyInvalid`, `ApiKeyRevoked`, `RateLimitExceeded`, `IpBlocked`, `UnauthorizedAccess` — semántica específica, no solo "algo falló".

**Código a mostrar:**
- `src/ApiSecurity.Domain/Entities/AuditLog.cs` — entidad con factory method
- `src/ApiSecurity.Domain/Enums/AuditEventType.cs` — tipos de eventos
- `src/ApiSecurity.API/Middleware/AuditLogMiddleware.cs`
  - `context.Response.OnCompleted(...)` — post-pipeline
  - Filtro por status code: `401 || 403 || 429`
  - `IAuditLogRepository` — depende de interfaz, no implementación
- Seq UI: filtrar `EventType = RateLimitExceeded` para ver patrones de ataque

### Segmento 2 — FluentValidation Pipeline (9:00–15:00)

**Decisión central:** Validación como `IPipelineBehavior`, no en endpoints.

**Por qué pipeline behavior y no endpoint filters:**
- DRY: un behavior registrado valida todos los commands automáticamente
- `AddValidatorsFromAssembly`: cualquier validator nuevo se registra sin configuración adicional
- Separa validación de lógica de negocio — el handler recibe datos ya validados
- Ejecuta todos los validators, recolecta todos los errores → cliente recibe lista completa en un request

**Anti-injection en strings:**
- Email: formato válido evita injection en queries
- Longitud máxima en todos los campos: previene DoS por strings masivos

**Código a mostrar:**
- `src/ApiSecurity.Application/Common/ValidationBehavior.cs`
  - LINQ: todos los validators, todos los errores, lista completa
- `src/ApiSecurity.Application/Auth/Commands/LoginCommand.cs` + validator
- `src/ApiSecurity.API/Program.cs` — registro: `AddValidatorsFromAssembly` + behavior

### Segmento 3 — Testcontainers + Bug Real (15:00–24:00)

**Decisión central:** Stack real vs mocks para tests de integración.

**Arquitectura del fixture:**
- `WebApplicationFactory<Program>`: app completa en memoria
- `PostgreSqlContainer`: PostgreSQL real en Docker, puerto random
- `IAsyncLifetime`: levanta container en `InitializeAsync`, destruye en `DisposeAsync`
- Override crítico: `ConnectionStrings:Default = _postgres.GetConnectionString()`

**Bug 1: ConnectionStrings sin override (error de setup):**
- Sin override, la app intenta conectar a `localhost:5432`
- Container levantado en puerto random → Connection refused → 500 en todos los endpoints

**Bug 2: IpFilter:Allowlist:0 = "" (el bug famoso):**
- Intención: vaciar el allowlist
- Realidad: `config["IpFilter:Allowlist:0"] = ""` crea `HashSet{""}` — un elemento vacío
- IP del test `127.0.0.1` no matchea `""` → allowlist activo → 403 en todo
- Diagnóstico: 403, no 401 — auth no corría en absoluto
- Fix: no sobreescribir esas keys. El JSON vacío ya produce `HashSet` vacío = allowlist deshabilitado.
- Con mocks: bug nunca aparece. Con stack real: aparece en el primer test.

**Cobertura de tests de integración (13 tests):**
- Happy paths: login, refresh, products con bearer, products con API key
- Security edge cases: password incorrecto → 401, scope incorrecto → 403, rate limit → 429 en request 11
- Combinaciones: bearer Y API key en mismo endpoint
- El test de rate limit hace 11 requests HTTP reales → no mockeable de forma significativa

**Código a mostrar:**
- `tests/ApiSecurity.IntegrationTests/Fixtures/ApiTestFixture.cs`
  - Container setup y teardown
  - Override de ConnectionStrings (correcto) vs bug del IpFilter (incorrecto, mostrar y explicar)
- Ejemplo de test de rate limit — loop de 11 requests
- Ejemplo de test de security headers — headers unitarios vs integración

### Demo (24:00–29:00)

```bash
# Correr todos los tests en vivo
dotnet test --logger "console;verbosity=normal"
# Testcontainers pullea imagen, levanta Postgres, aplica migrations
# 31 tests: 18 unit + 13 integration
# Output con timing — integration tests más lentos por container boot
```

GitHub Actions: mostrar CI yml, badge verde en README.

### Cierre y Recap de Serie (29:00–32:00)

Recap de las 3 capas: identidad, protección runtime, observabilidad.

**Limitaciones documentadas (honestidad):**
- Revocación real de refresh tokens requiere Redis + blacklist distribuida
- Rate limiting no es distribuido — single instance only
- → Proyecto 6 de la serie: IdentityServer para implementación completa

CTA: repo GitHub, ADRs en `docs/adr/`, próximo proyecto.

---

## Requisitos de producción

### Setup técnico para grabación
- Docker Desktop corriendo con `docker compose up` en el repo
- VS Code con repo abierto, terminal integrada visible
- Browser con Scalar UI (`http://localhost:8080/scalar/v1`) y Seq (`http://localhost:5341`)
- Split de pantalla: código izquierda / terminal derecha durante demos

### Archivos que mostrar por episodio

**Ep1:** ADR-001, ADR-003, `ApiKeyHasher.cs`, `ApiKeyAuthenticationHandler.cs`, `ApiKeyScope.cs`, `JwtTokenService.cs`, `AuthenticationExtensions.cs`, `appsettings.json` (sección Jwt), `CorsExtensions.cs`

**Ep2:** ADR-002, `RateLimitingExtensions.cs`, `IpFilterMiddleware.cs`, `SecurityHeadersMiddleware.cs`, `Program.cs` (pipeline completo)

**Ep3:** `AuditLog.cs`, `AuditEventType.cs`, `AuditLogMiddleware.cs`, `ValidationBehavior.cs`, `LoginCommand.cs`, `ApiTestFixture.cs`, test de rate limit, CI yml

### Comandos de demo listos (Ep1)
```bash
# Login
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq

# Bearer
curl http://localhost:8080/products -H "Authorization: Bearer <TOKEN>" | jq

# Crear API key (scope=1 = ReadProducts)
curl -X POST http://localhost:8080/apikeys \
  -H "Authorization: Bearer <TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo Key","scopes":1}' | jq

# Usar API key
curl http://localhost:8080/products -H "X-Api-Key: ask_<key>" | jq

# Audience separation (access token como refresh → 401)
curl -X POST http://localhost:8080/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<ACCESS_TOKEN>"}' | jq
```

### Comandos de demo listos (Ep2)
```bash
# Rate limit trigger
for i in {1..12}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done
```

### Comandos de demo listos (Ep3)
```bash
# Todos los tests
dotnet test --logger "console;verbosity=normal"
```

---

## Métricas de éxito

- Views orgánicas en primeros 30 días (baseline del canal actual)
- Comentarios sobre decisiones específicas de diseño (señal de audiencia técnica senior)
- Visitas al repo en GitHub (link en descripción)
- Inbound desde LinkedIn post de cada episodio
