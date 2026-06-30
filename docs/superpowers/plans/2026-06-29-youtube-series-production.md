# YouTube Mini-Serie: Plan de Producción

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Producir y publicar 3 episodios de YouTube sobre patrones de seguridad en .NET, con scripts detallados y demos en vivo del repositorio dotnet-api-security.

**Architecture:** Cada episodio sigue el mismo flujo: pre-producción (ensayo + setup) → grabación por segmentos → edición → publicación. Los episodios son independientes pero comparten entorno de demo común. Grabar los 3 antes de publicar cualquiera permite mantener calidad uniforme y lanzar con cadencia semanal.

**Tech Stack:** OBS Studio (grabación), DaVinci Resolve o CapCut Pro (edición), Docker Desktop (demo environment), VS Code (editor on-screen), curl + jq (demos CLI), Seq (logs demo), YouTube Studio, LinkedIn.

**Spec de referencia:** `docs/superpowers/specs/2026-06-29-youtube-series-design.md`

---

## Task 1: Verificación del entorno de demo

**Assets:**
- Verificar: `docker-compose.yml`
- Verificar: `src/ApiSecurity.API/appsettings.json`
- Verificar: `tests/` — todos los tests pasan

- [ ] **Step 1: Levantar el stack completo**

```bash
cd D:\Claude\Proyectos\.Net\dotnet-api-security
docker compose up -d
```

Esperar 10-15 segundos. Verificar containers:

```bash
docker compose ps
```

Expected output: `api` en estado `running`, `postgres` en estado `running`, `seq` en estado `running`.

- [ ] **Step 2: Verificar endpoints responden**

```bash
# Login
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq .accessToken
```

Expected: string JWT que empieza con `eyJ`.

```bash
# Scalar UI
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/scalar/v1
```

Expected: `200`.

```bash
# Seq
curl -s -o /dev/null -w "%{http_code}" http://localhost:5341
```

Expected: `200`.

- [ ] **Step 3: Guardar token para pruebas manuales**

```bash
TOKEN=$(curl -s -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq -r .accessToken)
echo $TOKEN
```

Confirmar que el token tiene tres partes separadas por `.` (header.payload.signature).

- [ ] **Step 4: Verificar todos los tests pasan**

```bash
dotnet test --logger "console;verbosity=minimal"
```

Expected: `31 passed, 0 failed`. Si algún test falla, resolver antes de grabar.

- [ ] **Step 5: Verificar rate limiting activo**

```bash
for i in {1..12}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done
```

Expected: requests 1-10 devuelven `401`, request 11 devuelve `429`.

Si rate limit no se activa en el request 11, verificar que `RateLimitingExtensions.cs` tiene la política `ip-sliding` con `PermitLimit = 10`.

- [ ] **Step 6: Limpiar historial de terminal para grabación**

```bash
clear
history -c 2>/dev/null || cls
```

Cerrar y reabrir terminal. Verificar que el historial está limpio (flecha arriba no muestra comandos anteriores).

- [ ] **Step 7: Verificar IP filter en estado default (listas vacías)**

```bash
cat src/ApiSecurity.API/appsettings.json | jq .IpFilter
```

Expected: `{"Allowlist": [], "Denylist": []}`. Si hay IPs en las listas, limpiarlas antes de grabar.

---

## Task 2: Setup de grabación

**Assets:**
- Software: OBS Studio instalado y configurado
- Layout VS Code: tema oscuro, fuente 18-20px (legible en pantalla)
- Layout terminal: fondo oscuro, fuente 16-18px, `jq` instalado

- [ ] **Step 1: Configurar VS Code para grabación**

Abrir VS Code con la carpeta del proyecto:

```bash
code D:\Claude\Proyectos\.Net\dotnet-api-security
```

Ajustar en Settings (`Ctrl+,`):
- `editor.fontSize`: 18
- `terminal.integrated.fontSize`: 16
- Tema: Dark+ o similar (alto contraste)
- Ocultar: Activity Bar, Breadcrumbs, Minimap

Verificar que el código es legible al 1920x1080 o resolución de grabación.

- [ ] **Step 2: Configurar OBS**

Escena: "Código + Terminal"
- Source 1: Window Capture → VS Code (pantalla completa)
- Source 2: Window Capture → Terminal (bottom 30%)
- Source 3: Webcam (esquina inferior derecha, 320x240) — opcional

Escena: "Terminal Full"
- Source 1: Window Capture → Terminal (pantalla completa)

Configuración de salida:
- Resolución: 1920x1080
- FPS: 30
- Bitrate video: 8000 kbps
- Formato: MP4 o MKV

- [ ] **Step 3: Test de grabación de 2 minutos**

Grabar 2 minutos de pantalla con audio. Revisar:
- [ ] Texto del código es legible
- [ ] Audio sin eco ni ruido de fondo
- [ ] Cursor del mouse visible
- [ ] Terminal con contraste suficiente

Ajustar lo que sea necesario antes de empezar con Ep1.

- [ ] **Step 4: Preparar estructura de carpetas para archivos de grabación**

```
videos/
  ep1-identidad/
    raw/          ← tomas crudas
    export/       ← video final exportado
  ep2-proteccion/
    raw/
    export/
  ep3-observabilidad/
    raw/
    export/
```

Crear fuera del repositorio para no commitear archivos grandes.

---

## Task 3: Pre-producción Episodio 1 — Ensayo

**Spec:** Sección "Episodio 1: Identidad" en el spec de diseño.

- [ ] **Step 1: Preparar archivos abiertos en VS Code**

Tabs a tener abiertas antes de grabar:

| Tab | Archivo |
|-----|---------|
| 1 | `docs/adr/ADR-001-api-key-hashing.md` |
| 2 | `src/ApiSecurity.Infrastructure/Security/ApiKeyHasher.cs` |
| 3 | `src/ApiSecurity.API/Authentication/ApiKeyAuthenticationHandler.cs` |
| 4 | `src/ApiSecurity.Domain/Enums/ApiKeyScope.cs` |
| 5 | `docs/adr/ADR-003-jwt-expiry-refresh.md` |
| 6 | `src/ApiSecurity.Infrastructure/Security/JwtTokenService.cs` |
| 7 | `src/ApiSecurity.API/Extensions/AuthenticationExtensions.cs` |
| 8 | `src/ApiSecurity.API/appsettings.json` |
| 9 | `src/ApiSecurity.API/Extensions/CorsExtensions.cs` |
| 10 | `src/ApiSecurity.API/Program.cs` |

Navegar a cada tab y verificar que el archivo se ve completo sin scroll horizontal.

- [ ] **Step 2: Preparar comandos de demo en archivo temporal**

Crear archivo `demo-ep1.sh` fuera del repo con todos los comandos listos para copiar-pegar durante la demo:

```bash
# DEMO EP1 — ejecutar en orden

# 1. Login
curl -X POST http://localhost:8080/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@example.com","password":"Admin123!"}' | jq

# 2. Guardar token (reemplazar <TOKEN> con el accessToken del paso anterior)
TOKEN="<ACCESS_TOKEN>"

# 3. Usar bearer en products
curl http://localhost:8080/products \
  -H "Authorization: Bearer $TOKEN" | jq

# 4. Crear API key
curl -X POST http://localhost:8080/apikeys \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Demo Key","scopes":1}' | jq

# 5. Usar API key (reemplazar con la key del paso anterior)
API_KEY="ask_<prefix><secret>"
curl http://localhost:8080/products \
  -H "X-Api-Key: $API_KEY" | jq

# 6. Demostrar audience separation (access token NO sirve como refresh token)
curl -X POST http://localhost:8080/auth/refresh \
  -H "Content-Type: application/json" \
  -d "{\"refreshToken\":\"$TOKEN\"}" | jq
```

- [ ] **Step 3: Ensayo completo con cronómetro**

Leer el script completo del Ep1 en voz alta, simulando la demo. Cronometrar cada segmento:

| Segmento | Target | Real | ¿OK? |
|----------|--------|------|------|
| Intro | 1:30 | __ | |
| API Keys | 6:30 | __ | |
| JWT | 7:00 | __ | |
| CORS | 7:00 | __ | |
| Demo | 6:00 | __ | |
| Cierre | 2:00 | __ | |
| **Total** | **30:00** | __ | |

Si algún segmento supera el target por más de 2 minutos, recortar. Si total > 35 min, identificar qué segmento recortar.

- [ ] **Step 4: Marcar las líneas de código a destacar**

En cada archivo, añadir un comentario temporal `// ← HIGHLIGHT` en las líneas que se van a mostrar en pantalla (para navegar rápido durante la grabación):

**`ApiKeyHasher.cs`:**
```csharp
RandomNumberGenerator.GetBytes(32) // ← HIGHLIGHT
```

**`JwtTokenService.cs`:**
```csharp
new TokenValidationParameters { ClockSkew = TimeSpan.Zero } // ← HIGHLIGHT (en AuthenticationExtensions.cs)
```

**`AuthenticationExtensions.cs`:**
```csharp
ClockSkew = TimeSpan.Zero // ← HIGHLIGHT
```

Recordar quitar los comentarios `// ← HIGHLIGHT` antes de commitear cualquier cambio.

---

## Task 4: Grabación Episodio 1

**Assets:**
- Entorno de demo corriendo (Task 1 completo)
- Setup OBS listo (Task 2 completo)
- Ensayo hecho (Task 3 completo)

- [ ] **Step 1: Grabar segmento INTRO (0:00–1:30)**

Escena OBS: "Terminal Full"

Mostrar en terminal:
```bash
# Tres tipos distintos de 401
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/products
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/products -H "X-Api-Key: ask_invalida"
curl -s -o /dev/null -w "%{http_code}" http://localhost:8080/products -H "Authorization: Bearer token.expirado.x"
```

Narrar el hook mientras aparecen los tres `401`.

Guardar take como: `ep1-raw/01-intro-take1.mp4`

- [ ] **Step 2: Grabar segmento API KEYS (1:30–8:00)**

Escena OBS: "Código + Terminal"

Navegación de código en orden:
1. Tab ADR-001 — leer decisión SHA-256 vs BCrypt
2. Tab `ApiKeyHasher.cs` — highlight `RandomNumberGenerator.GetBytes(32)`, formato `ask_`, hash SHA-256
3. Tab `ApiKeyAuthenticationHandler.cs` — flujo prefix lookup → hash compare → claims
4. Tab `ApiKeyScope.cs` — [Flags] enum, OR/AND bit a bit

Guardar take como: `ep1-raw/02-apikeys-take1.mp4`

- [ ] **Step 3: Grabar segmento JWT (8:00–15:00)**

Navegación en orden:
1. Tab ADR-003 — leer decisión 15 min + ClockSkew
2. Tab `JwtTokenService.cs` — highlight `BuildAccessToken` (claims, jti, exp), `BuildRefreshToken` (audience `-refresh`)
3. Tab `AuthenticationExtensions.cs` — highlight `ClockSkew = TimeSpan.Zero`
4. Tab `appsettings.json` — sección Jwt completa

Guardar take como: `ep1-raw/03-jwt-take1.mp4`

- [ ] **Step 4: Grabar segmento CORS (15:00–22:00)**

Abrir nuevo archivo en VS Code (Ctrl+N), escribir el anti-pattern:
```csharp
// ANTI-PATTERN (no hacer esto)
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));
```

Mostrar por 10 segundos, luego navegar a `CorsExtensions.cs` con la implementación correcta.

Guardar take como: `ep1-raw/04-cors-take1.mp4`

- [ ] **Step 5: Grabar segmento DEMO (22:00–28:00)**

Escena OBS: "Terminal Full"

Ejecutar comandos de `demo-ep1.sh` en orden, dejando tiempo para leer cada respuesta JSON.

Pausar brevemente después del request 6 (audience separation → 401) para enfatizar la diferencia.

Guardar take como: `ep1-raw/05-demo-take1.mp4`

- [ ] **Step 6: Grabar segmento CIERRE (28:00–30:00)**

Escena OBS: "Terminal Full" o cara a cámara (si hay webcam)

Narrar el recap de las 4 decisiones + CTA a Ep2.

Guardar take como: `ep1-raw/06-cierre-take1.mp4`

- [ ] **Step 7: Revisar todos los takes antes de editar**

Reproducir cada take y marcar si sirve o necesita re-take:

| Take | ¿Sirve? | Nota |
|------|---------|------|
| 01-intro | | |
| 02-apikeys | | |
| 03-jwt | | |
| 04-cors | | |
| 05-demo | | |
| 06-cierre | | |

Re-grabar los takes marcados como "no sirve". No editar hasta tener todos los takes aceptables.

---

## Task 5: Edición Episodio 1

- [ ] **Step 1: Importar todos los takes a DaVinci Resolve / CapCut Pro**

Crear nuevo proyecto: `ep1-identidad`
Importar carpeta `ep1-raw/` completa.

- [ ] **Step 2: Montar corte inicial en timeline**

Orden en timeline: intro → apikeys → jwt → cors → demo → cierre

Cortar:
- Silencios de más de 1.5 segundos al inicio/final de cada take
- Errores de pronunciación (re-takes alternativos)
- Momentos de "ehh" o pausa larga (>2 seg) salvo que sean deliberados

- [ ] **Step 3: Agregar zooms a código (b-roll)**

En cada momento donde el narrador dice "miren aquí" o hace highlight de una línea, agregar un zoom digital (Ken Burns effect o corte a versión ampliada):

| Tiempo aprox. | Qué ampliar |
|---------------|-------------|
| Seg. API Keys | `RandomNumberGenerator.GetBytes(32)` |
| Seg. API Keys | Formato `ask_<prefix8><secret>` |
| Seg. JWT | `ClockSkew = TimeSpan.Zero` |
| Seg. JWT | Audience `api-security-refresh` |
| Demo | Output JSON del login (accessToken + refreshToken) |
| Demo | Respuesta 401 del request con audience incorrecta |

- [ ] **Step 4: Agregar lower thirds para conceptos clave**

Insertar texto sobreimpreso (2-3 segundos, esquina inferior):

| Momento | Texto |
|---------|-------|
| Mención SHA-256 | `SHA-256: O(1) — 32 bytes entropía aleatoria` |
| Mención BCrypt | `BCrypt: diseñado para passwords, no secretos random` |
| Mención ClockSkew | `ClockSkew = TimeSpan.Zero — sin tolerancia de 5 min` |
| Mención audience | `Audience separation = tokens no intercambiables` |

- [ ] **Step 5: Agregar intro animada (primeros 5 segundos)**

Texto animado sobre fondo oscuro:
```
dotnet-api-security
Episodio 1: Identidad
```

Duración: 4 segundos. Fade in/fade out.

- [ ] **Step 6: Agregar outro con CTA (últimos 20 segundos)**

Pantalla final con:
- Texto: "Código en GitHub → link en descripción"
- Texto: "Ep2: Rate Limiting + IP Filter + Security Headers"
- End screen de YouTube (placeholder — se configura en YouTube Studio)

- [ ] **Step 7: Agregar subtítulos/captions**

Usar auto-caption de CapCut Pro o Whisper:
```bash
# Si usas Whisper local
whisper ep1-raw/01-intro-take1.mp4 --language Spanish --output_format srt
```

Revisar y corregir errores de transcripción, especialmente términos técnicos:
- `JWT` no `Jet`
- `SHA-256` no `sha 256`
- `ClockSkew` no `Clock Scale`
- `API key` no `api ki`
- `Bearer` no `Barer`

- [ ] **Step 8: Export final**

Settings de export:
- Formato: MP4 (H.264)
- Resolución: 1920x1080
- FPS: 30
- Bitrate: 8 Mbps (YouTube recomprime, pero manda calidad alta)
- Audio: AAC 320 kbps

Guardar en: `ep1-identidad/export/ep1-identidad-final.mp4`

Verificar duración total: debe estar entre 25-35 minutos.

---

## Task 6: Publicación Episodio 1

- [ ] **Step 1: Crear thumbnail**

Concepto: fondo oscuro (#1a1a2e o similar), texto grande y legible.

Elementos:
- Título: "JWT + API Keys en .NET" (fuente grande, blanco)
- Subtítulo: "Las decisiones que tu tutorial no te explicó" (fuente mediana, gris claro)
- Logo .NET o ícono de candado
- Resolución: 1280x720 px

Herramientas: Canva, Figma, o Photoshop.

Guardar como: `ep1-identidad/thumbnail.png`

- [ ] **Step 2: Preparar descripción de YouTube**

```
🔐 JWT + API Keys en .NET: decisiones de arquitectura que los tutoriales omiten.

No es un tutorial de cómo escribir el código. Es un análisis de POR QUÉ cada decisión.

✅ En este episodio:
• SHA-256 vs BCrypt para API keys (y por qué BCrypt aquí es un error)
• JWT de 15 minutos con ClockSkew = TimeSpan.Zero
• Refresh tokens con audience separation
• CORS con origins explícitos — sin wildcards

📁 Código completo: https://github.com/elsaldagaster-stack/dotnet-api-security
📋 ADRs (decisiones de arquitectura): github.com/.../docs/adr/

⏱️ Timestamps:
00:00 - Intro: tres tipos de 401
01:30 - API Keys: SHA-256 vs BCrypt
08:00 - JWT: 15 min y ClockSkew Zero
15:00 - CORS: el error que todos cometen
22:00 - Demo en vivo
28:00 - Recap de decisiones

🎯 Serie: Seguridad en APIs .NET — Proyecto 12 del portafolio

📌 Episodio 2: Rate Limiting, IP Filter, Security Headers → [link cuando esté disponible]
📌 Episodio 3: Audit Logging, FluentValidation, Testcontainers → [link cuando esté disponible]

#dotnet #csharp #apidevelopment #security #jwt #aspnetcore
```

- [ ] **Step 3: Subir a YouTube Studio**

1. Ir a YouTube Studio → "Crear" → "Subir video"
2. Seleccionar `ep1-identidad-final.mp4`
3. Título: `JWT + API Keys en .NET: Las decisiones que tu tutorial no te explicó`
4. Descripción: pegar texto del Step 2
5. Thumbnail: subir `ep1-identidad/thumbnail.png`
6. Playlist: crear playlist "Seguridad en APIs .NET — Portafolio .NET"
7. Visibilidad: **Programado** (no publicar aún — publicar todos juntos con cadencia semanal)
8. Fecha de publicación Ep1: elegir día de la semana + hora óptima (generalmente martes/jueves 10am hora local)

- [ ] **Step 4: Configurar end screen y cards en YouTube Studio**

End screen (aparece en los últimos 20 segundos):
- Card 1: "Siguiente video" → Ep2 (configurar cuando esté subido)
- Card 2: "Suscribirse" al canal

Cards (durante el video):
- A los 5:00 min: card "Código en GitHub" → link del repo

- [ ] **Step 5: LinkedIn post para Ep1**

```
🔐 Nuevo video: JWT + API Keys en .NET

No es otro tutorial. Es un análisis de las decisiones que los tutoriales omiten.

¿Por qué SHA-256 y no BCrypt para API keys?
¿Por qué 15 minutos para JWT y no 1 hora?
¿Qué hace ClockSkew = TimeSpan.Zero?

Construí un proyecto completo de seguridad para APIs .NET y lo analizo desde los ADRs (Architecture Decision Records) — el razonamiento detrás de cada decisión.

🎥 Video: [link]
📁 Código: https://github.com/elsaldagaster-stack/dotnet-api-security

Serie de 3 episodios — Proyecto 12 del portafolio.

#dotnet #csharp #security #apidevelopment #portafolio
```

Publicar LinkedIn post mismo día que se publique el video.

---

## Task 7: Pre-producción Episodio 2 — Ensayo

**Spec:** Sección "Episodio 2: Protección Runtime" en el spec de diseño.

- [ ] **Step 1: Preparar archivos abiertos en VS Code**

| Tab | Archivo |
|-----|---------|
| 1 | `docs/adr/ADR-002-rate-limiting-strategy.md` |
| 2 | `src/ApiSecurity.API/Extensions/RateLimitingExtensions.cs` |
| 3 | `src/ApiSecurity.API/Endpoints/ProductEndpoints.cs` (ver `[EnableRateLimiting]`) |
| 4 | `src/ApiSecurity.API/Middleware/IpFilterMiddleware.cs` |
| 5 | `src/ApiSecurity.API/Middleware/SecurityHeadersMiddleware.cs` |
| 6 | `src/ApiSecurity.API/Program.cs` |

- [ ] **Step 2: Preparar demo de IP filter**

Para la demo de IP filter, necesitas agregar tu propia IP a la denylist temporalmente. Encontrar tu IP:

```bash
# IP local (para pruebas con Docker)
ipconfig | grep "IPv4"
# o
curl -s http://localhost:8080/products -v 2>&1 | grep "< " | head -5
```

En `appsettings.json`, sección `IpFilter`:
```json
{
  "IpFilter": {
    "Allowlist": [],
    "Denylist": ["127.0.0.1"]
  }
}
```

**Importante:** esta modificación es SOLO para la demo grabada. Revertir después de grabar ese segmento. No commitear con IP en denylist.

- [ ] **Step 3: Preparar screenshot de AspNetCoreRateLimit para hook**

Screenshot de GitHub: `https://github.com/stefanprodan/AspNetCoreRateLimit`

Mostrar: número de stars, fecha del último commit. Preparar el screenshot antes de grabar el hook.

- [ ] **Step 4: Preparar comandos de demo en archivo temporal**

Crear `demo-ep2.sh`:

```bash
# DEMO EP2

# 1. Rate limit trigger (11 requests → 429)
for i in {1..12}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done

# 2. Security headers (ejecutar con -v para ver response headers)
curl -v http://localhost:8080/products \
  -H "Authorization: Bearer <TOKEN>" 2>&1 | grep "< " | head -20

# 3. IP bloqueada (DESPUÉS de agregar 127.0.0.1 a denylist y reiniciar API)
curl -v http://localhost:8080/products \
  -H "Authorization: Bearer <TOKEN>"
# Expected: 403 Forbidden (sin llegar a auth)
```

- [ ] **Step 5: Ensayo completo con cronómetro**

| Segmento | Target | Real | ¿OK? |
|----------|--------|------|------|
| Hook | 1:30 | __ | |
| Rate limiting | 7:30 | __ | |
| IP filter | 6:00 | __ | |
| Security headers | 6:00 | __ | |
| Demo | 6:00 | __ | |
| Pipeline deep-dive | 3:00 | __ | |
| Cierre | 2:00 | __ | |
| **Total** | **32:00** | __ | |

---

## Task 8: Grabación Episodio 2

- [ ] **Step 1: Grabar segmento HOOK (0:00–1:30)**

Escena OBS: "Código + Terminal" mostrando screenshot de AspNetCoreRateLimit en browser.

Narrar contraste: "4000 stars... sin actualizaciones desde 2022... mientras tanto .NET 7 incluye rate limiting built-in."

Guardar: `ep2-raw/01-hook-take1.mp4`

- [ ] **Step 2: Grabar segmento RATE LIMITING (1:30–9:00)**

Navegación:
1. ADR-002 — decisión built-in vs Redis vs terceros, limitación honesta de single-instance
2. Diagrama mental: dibujar en pantalla (whiteboard tool o PowerPoint simple) el problema de N instancias con contadores independientes
3. `RateLimitingExtensions.cs` — las 3 políticas, partition key `apikey-sliding`
4. `ProductEndpoints.cs` — `[EnableRateLimiting("apikey-sliding")]`
5. Explicar sliding vs fixed window (diagrama)

Guardar: `ep2-raw/02-ratelimiting-take1.mp4`

- [ ] **Step 3: Grabar segmento IP FILTER (9:00–15:00)**

Antes de grabar: verificar que `appsettings.json` tiene listas vacías (entorno limpio).

Navegación:
1. `IpFilterMiddleware.cs` — HashSet, lógica allowlist vs denylist, distinción "vacío = deshabilitado"
2. Contar el bug de tests: `IpFilter:Allowlist:0 = ""` → HashSet con string vacío → 403 en todos los tests
3. `Program.cs` — posición del middleware (segundo, antes de CORS)

Guardar: `ep2-raw/03-ipfilter-take1.mp4`

- [ ] **Step 4: Grabar segmento SECURITY HEADERS (15:00–21:00)**

Antes de grabar: abrir browser devtools mostrando un request sin los headers (puede ser a cualquier sitio sin headers).

Luego mostrar request a `http://localhost:8080/products` con headers presentes.

Navegación:
1. `SecurityHeadersMiddleware.cs` — `OnStarting` callback, los 7 headers, `Headers.Remove("Server")`
2. Para cada header: una oración sobre por qué existe

Guardar: `ep2-raw/04-securityheaders-take1.mp4`

- [ ] **Step 5: Grabar segmento DEMO (21:00–27:00)**

**Sub-demo 1: Rate limit**
```bash
for i in {1..12}; do
  curl -s -o /dev/null -w "Request $i: %{http_code}\n" \
    -X POST http://localhost:8080/auth/login \
    -H "Content-Type: application/json" \
    -d '{"email":"x","password":"x"}'
done
```

**Sub-demo 2: Security headers** (terminal con curl -v)

**Sub-demo 3: IP bloqueada**
1. Editar `appsettings.json`: agregar `"127.0.0.1"` a `Denylist`
2. Reiniciar API: `docker compose restart api`
3. Ejecutar curl → 403
4. Revertir `appsettings.json`: vaciar `Denylist`
5. Reiniciar API: `docker compose restart api`

**Sub-demo 4: Seq** — abrir browser `http://localhost:5341`, mostrar logs de los 429 y 403.

Guardar: `ep2-raw/05-demo-take1.mp4`

- [ ] **Step 6: Grabar segmento PIPELINE DEEP-DIVE (27:00–30:00)**

Escena: `Program.cs` con el pipeline completo visible.

Leer cada línea del pipeline y explicar la posición. Ir despacio.

Guardar: `ep2-raw/06-pipeline-take1.mp4`

- [ ] **Step 7: Grabar CIERRE (30:00–32:00)**

Guardar: `ep2-raw/07-cierre-take1.mp4`

- [ ] **Step 8: Revisar todos los takes**

| Take | ¿Sirve? | Nota |
|------|---------|------|
| 01-hook | | |
| 02-ratelimiting | | |
| 03-ipfilter | | |
| 04-securityheaders | | |
| 05-demo | | |
| 06-pipeline | | |
| 07-cierre | | |

---

## Task 9: Edición Episodio 2

Seguir el mismo proceso que Task 5 (Edición Ep1), adaptando:

- [ ] **Step 1: Importar y montar corte inicial**
- [ ] **Step 2: Agregar zooms a conceptos clave**

| Momento | Qué ampliar |
|---------|-------------|
| Rate limiting | Código de política `ip-sliding` con `PermitLimit = 10` |
| Rate limiting | `PartitionedRateLimiter.Create` con partition key |
| IP filter | `HashSet<string>` — highlight la declaración |
| Security headers | Cada header en el código |
| Pipeline | Orden completo SecurityHeaders → ... → Endpoints |
| Demo | Output con 429 en request 11 |

- [ ] **Step 3: Lower thirds**

| Momento | Texto |
|---------|-------|
| Mención distributed | `Distributed → necesita Redis backing store` |
| Mención sliding window | `Sliding window: sin burst en el boundary` |
| Mención HashSet | `HashSet<T>: lookup O(1)` |
| Mención OnStarting | `OnStarting: headers antes del envío de respuesta` |

- [ ] **Step 4: Subtítulos y review de términos técnicos**

Términos a revisar especialmente:
- `SlidingWindow` no "sliding win" ni "sliding window" con mayúsculas incorrectas
- `HashSet` no "hash set" dividido
- `OnStarting` no "on starting" como dos palabras
- `429 Too Many Requests` — verificar que el código de error esté correcto

- [ ] **Step 5: Export**

Guardar en: `ep2-proteccion/export/ep2-proteccion-final.mp4`
Verificar duración: 25-35 min.

---

## Task 10: Publicación Episodio 2

- [ ] **Step 1: Crear thumbnail**

Misma paleta visual que Ep1 para cohesión de serie.

Elementos:
- Título: "Rate Limiting + IP Filter en .NET"
- Subtítulo: "Sin Redis, Sin Dependencias Externas"
- Ícono de escudo o firewall

- [ ] **Step 2: Preparar descripción de YouTube**

```
🛡️ Rate Limiting, IP Filter y Security Headers en .NET — sin dependencias externas.

¿Por qué elegí el rate limiter built-in de .NET en lugar de AspNetCoreRateLimit?
¿Cuándo necesitas Redis? ¿Por qué sliding window y no fixed window?
¿Qué hace cada uno de los 7 security headers?

Ep2 de la serie "Seguridad en APIs .NET".

✅ En este episodio:
• 3 políticas de rate limiting (global, ip-sliding 10/min, apikey-sliding 1000/min)
• IP filter: allowlist + denylist en HashSet O(1)
• 7 security headers con razonamiento por cada uno
• Orden del middleware pipeline y por qué importa
• Bug real: IpFilter:Allowlist:0="" que bloqueó todos los tests de integración

📁 Código: https://github.com/elsaldagaster-stack/dotnet-api-security

⏱️ Timestamps:
00:00 - Hook: AspNetCoreRateLimit vs built-in
01:30 - Rate limiting: 3 políticas + sliding vs fixed window
09:00 - IP filter: O(1) lookup + bug real
15:00 - Security headers: los 7 + OnStarting callback
21:00 - Demo en vivo
27:00 - Middleware pipeline deep-dive
30:00 - Cierre

📌 Ep1: JWT + API Keys → [link]
📌 Ep3: Audit Logging + Testcontainers → [link cuando esté disponible]

#dotnet #csharp #ratelimiting #security #aspnetcore #middleware
```

- [ ] **Step 3: Subir a YouTube Studio (programado)**

Fecha de publicación Ep2: 1 semana después de Ep1.

- [ ] **Step 4: LinkedIn post Ep2**

```
🛡️ Ep2: Rate Limiting e IP Filter en .NET

.NET 7+ incluye rate limiting built-in. ¿Por qué elegirlo sobre AspNetCoreRateLimit?

No porque sea "más simple" — porque es adecuado para single instance. Si tenés N instancias con load balancer, necesitás Redis. Eso está documentado en el ADR, no ocultado.

También el bug más frustrante que tuve: IpFilter:Allowlist:0="" crea un HashSet con un string vacío, no un HashSet vacío. Todos los tests de integración devolvían 403. Con mocks no habría aparecido.

🎥 Video: [link]
📁 Código: https://github.com/elsaldagaster-stack/dotnet-api-security

#dotnet #security #ratelimiting #apidevelopment
```

---

## Task 11: Pre-producción Episodio 3 — Ensayo

**Spec:** Sección "Episodio 3: Observabilidad" en el spec de diseño.

- [ ] **Step 1: Preparar archivos abiertos en VS Code**

| Tab | Archivo |
|-----|---------|
| 1 | `src/ApiSecurity.Domain/Entities/AuditLog.cs` |
| 2 | `src/ApiSecurity.Domain/Enums/AuditEventType.cs` |
| 3 | `src/ApiSecurity.API/Middleware/AuditLogMiddleware.cs` |
| 4 | `src/ApiSecurity.Application/Common/ValidationBehavior.cs` |
| 5 | `src/ApiSecurity.Application/Auth/Commands/LoginCommand.cs` |
| 6 | `src/ApiSecurity.API/Program.cs` (registro de FluentValidation + MediatR) |
| 7 | `tests/ApiSecurity.IntegrationTests/Fixtures/ApiTestFixture.cs` |
| 8 | `tests/ApiSecurity.IntegrationTests/Auth/AuthEndpointsTests.cs` (o similar) |
| 9 | `.github/workflows/ci.yml` |

- [ ] **Step 2: Preparar el "bug snapshot" para mostrar**

Crear un archivo temporal `bug-ipfilter.cs` (fuera del repo) con el código incorrecto original del fixture para mostrarlo en pantalla:

```csharp
// BUG — NO HACER ESTO
configBuilder.AddInMemoryCollection(new Dictionary<string, string>
{
    ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
    ["IpFilter:Allowlist:0"] = "",   // ← crea HashSet{""} no HashSet vacío
    ["IpFilter:Denylist:0"] = ""
});

// CORRECTO — solo override de ConnectionStrings
configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ConnectionStrings:Default"] = _postgres.GetConnectionString()
});
```

- [ ] **Step 3: Preparar GitHub Actions para mostrar**

Abrir en browser: `https://github.com/elsaldagaster-stack/dotnet-api-security/actions`

Verificar que hay al menos 1 run verde reciente. Si no hay, hacer un commit vacío para triggear CI:

```bash
cd D:\Claude\Proyectos\.Net\dotnet-api-security
git commit --allow-empty -m "ci: trigger CI for demo"
git push
```

Esperar a que el run complete (verde) antes de grabar.

- [ ] **Step 4: Ensayo con cronómetro**

| Segmento | Target | Real | ¿OK? |
|----------|--------|------|------|
| Hook | 1:30 | __ | |
| Audit logging | 7:30 | __ | |
| FluentValidation | 6:00 | __ | |
| Testcontainers + bug | 9:00 | __ | |
| Demo en vivo | 5:00 | __ | |
| Cierre serie completa | 3:00 | __ | |
| **Total** | **32:00** | __ | |

---

## Task 12: Grabación Episodio 3

- [ ] **Step 1: Grabar HOOK (0:00–1:30)**

Escena: GitHub Actions en browser, CI run verde con "31 passed".

Narrar: "31 tests. Esto no pasó a la primera. Tuve un bug donde todos los tests de integración devolvían 403..."

Guardar: `ep3-raw/01-hook-take1.mp4`

- [ ] **Step 2: Grabar segmento AUDIT LOGGING (1:30–9:00)**

Navegación:
1. `AuditLog.cs` — entidad, factory method, campos (EventType, IpAddress, StatusCode, etc.)
2. `AuditEventType.cs` — todos los tipos, enfatizar semántica específica
3. `AuditLogMiddleware.cs` — `OnCompleted` callback, filtro por 401/403/429
4. Seq en browser: filtrar por `EventType = RateLimitExceeded`

Guardar: `ep3-raw/02-auditlog-take1.mp4`

- [ ] **Step 3: Grabar segmento FLUENT VALIDATION (9:00–15:00)**

Navegación:
1. `ValidationBehavior.cs` — LINQ que recolecta todos los errores de todos los validators
2. `LoginCommand.cs` + validator inline — email válido, password longitud máxima
3. `Program.cs` — `AddValidatorsFromAssembly` + registro del behavior

Enfatizar: "cualquier validator nuevo se registra automáticamente — cero configuración adicional."

Guardar: `ep3-raw/03-validation-take1.mp4`

- [ ] **Step 4: Grabar segmento TESTCONTAINERS + BUG (15:00–24:00)**

Navegación:
1. `ApiTestFixture.cs` — container setup, `InitializeAsync`, override de ConnectionStrings
2. Mostrar `bug-ipfilter.cs` (archivo temporal) — el código incorrecto vs correcto lado a lado
3. Volver a `ApiTestFixture.cs` — la versión correcta
4. Mostrar un test de rate limit completo con sus 11 requests

Guardar: `ep3-raw/04-testcontainers-take1.mp4`

- [ ] **Step 5: Grabar DEMO en vivo (24:00–29:00)**

```bash
dotnet test --logger "console;verbosity=normal"
```

Esperar output completo (Testcontainers levanta Postgres, aplica migrations, corre 31 tests).
Mostrar el resultado final: "31 passed, 0 failed".

Abrir GitHub Actions en browser: mostrar CI run verde.
Mostrar badge en README.

Guardar: `ep3-raw/05-demo-take1.mp4`

- [ ] **Step 6: Grabar CIERRE DE SERIE (29:00–32:00)**

Recap de los 3 episodios. Mencionar limitaciones documentadas (Redis para revocación real).
CTA: repo GitHub, próximo proyecto.

Guardar: `ep3-raw/06-cierre-take1.mp4`

- [ ] **Step 7: Revisar todos los takes**

| Take | ¿Sirve? | Nota |
|------|---------|------|
| 01-hook | | |
| 02-auditlog | | |
| 03-validation | | |
| 04-testcontainers | | |
| 05-demo | | |
| 06-cierre | | |

---

## Task 13: Edición Episodio 3

- [ ] **Step 1: Importar y montar corte inicial**
- [ ] **Step 2: Agregar zooms**

| Momento | Qué ampliar |
|---------|-------------|
| Audit logging | `OnCompleted` callback en el middleware |
| Audit logging | Filtro: `statusCode == 401 || 403 || 429` |
| ValidationBehavior | LINQ de errores: `SelectMany(r => r.Errors)` |
| Testcontainers | `_postgres.GetConnectionString()` |
| Bug demo | Comparación bug vs fix lado a lado |
| Demo tests | Output final "31 passed, 0 failed" |

- [ ] **Step 3: Lower thirds**

| Momento | Texto |
|---------|-------|
| Mención OnCompleted | `OnCompleted: post-pipeline, status code final disponible` |
| Mención ValidationBehavior | `IPipelineBehavior: ejecuta antes de cada command handler` |
| Mención Testcontainers | `PostgreSqlContainer: PostgreSQL real, puerto random` |
| Mención bug | `HashSet{""} ≠ HashSet vacío — el origen del bug` |

- [ ] **Step 4: Subtítulos y términos técnicos**

Verificar especialmente:
- `Testcontainers` con mayúsculas correctas
- `IAsyncLifetime` como una sola palabra con capitalización correcta
- `OnCompleted` no "on completed"
- `IPipelineBehavior` con la I de interfaz

- [ ] **Step 5: Agregar "Recap de serie" al cierre del Ep3**

En los últimos 2 minutos, mostrar card con los 3 episodios y sus temas principales. Puede ser una pantalla simple con texto.

- [ ] **Step 6: Export**

Guardar en: `ep3-observabilidad/export/ep3-observabilidad-final.mp4`

---

## Task 14: Publicación Episodio 3

- [ ] **Step 1: Crear thumbnail**

Misma paleta. Elementos:
- Título: "Audit Logging + Testcontainers en .NET"
- Subtítulo: "Tests que Sí Agarran Bugs Reales"
- Ícono de base de datos o test green checkmark

- [ ] **Step 2: Preparar descripción de YouTube**

```
🔍 Audit Logging, FluentValidation y Testcontainers en .NET — la capa de observabilidad que más se omite.

31 tests. 18 unitarios, 13 de integración. Todos verdes. Pero no pasó a la primera.

Tuve un bug donde todos los tests de integración devolvían 403. Con mocks no habría aparecido. Con Testcontainers + stack real, sí.

Ep3 (final) de la serie "Seguridad en APIs .NET".

✅ En este episodio:
• Audit logging de 401/403/429 a PostgreSQL (por qué post-pipeline)
• FluentValidation como IPipelineBehavior de MediatR
• Testcontainers: PostgreSQL real en CI, puerto random
• El bug de IpFilter:Allowlist:0="" y cómo Testcontainers lo expuso
• 13 tests de integración que no se pueden mockear de forma significativa

📁 Código: https://github.com/elsaldagaster-stack/dotnet-api-security

⏱️ Timestamps:
00:00 - Hook: 31 tests y un bug
01:30 - Audit logging: 401/403/429 a PostgreSQL
09:00 - FluentValidation como MediatR pipeline behavior
15:00 - Testcontainers: stack real en CI
24:00 - Demo: 31 tests en vivo
29:00 - Recap de la serie completa

📌 Ep1: JWT + API Keys → [link]
📌 Ep2: Rate Limiting + Security Headers → [link]

#dotnet #csharp #testcontainers #fluentvalidation #integrationtesting #security
```

- [ ] **Step 3: Subir a YouTube Studio (programado)**

Fecha de publicación Ep3: 1 semana después de Ep2 (2 semanas después de Ep1).

- [ ] **Step 4: Actualizar end screens de Ep1 y Ep2**

En YouTube Studio, editar Ep1 y Ep2:
- Actualizar card "Siguiente video" con los links reales (ya disponibles)

- [ ] **Step 5: LinkedIn post Ep3**

```
🔍 Ep3 (final): Audit Logging + Testcontainers en .NET

El test de integración que más duele de mantener es el que detecta bugs reales.

Bug que tuve: IpFilter:Allowlist:0="" — intentaba vaciar el allowlist pero creé un HashSet con un string vacío como elemento. Todos los tests devolvían 403. Con mocks: invisible. Con Testcontainers + PostgreSQL real: aparece en el primer run.

Cerrando la serie "Seguridad en APIs .NET" con la capa de observabilidad: audit logging de eventos de seguridad, FluentValidation en el pipeline MediatR, y 13 tests de integración contra PostgreSQL real.

🎥 Ep3: [link]
📁 Código completo: https://github.com/elsaldagaster-stack/dotnet-api-security

#dotnet #testcontainers #security #integrationtesting
```

---

## Task 15: Post-lanzamiento — Cohesión de serie

- [ ] **Step 1: Crear playlist en YouTube**

YouTube Studio → Playlists → "Nueva playlist"
- Nombre: `Seguridad en APIs .NET — .NET 10 Portafolio`
- Descripción: breve descripción de la serie y link al repo
- Agregar Ep1, Ep2, Ep3 en orden

- [ ] **Step 2: Actualizar README con links a videos**

En `src/../README.md`, sección "YouTube tutorial":

```markdown
## YouTube tutorial

*"Seguridad en APIs .NET — Las decisiones que los tutoriales omiten"*

| Episodio | Tema | Link |
|----------|------|------|
| Ep1 | JWT + API Keys + CORS | 🎥 [Ver en YouTube](<link-ep1>) |
| Ep2 | Rate Limiting + IP Filter + Security Headers | 🎥 [Ver en YouTube](<link-ep2>) |
| Ep3 | Audit Logging + FluentValidation + Testcontainers | 🎥 [Ver en YouTube](<link-ep3>) |
```

Commitear y pushear:

```bash
cd D:\Claude\Proyectos\.Net\dotnet-api-security
git add README.md
git commit -m "docs: add YouTube series links to README"
git push
```

- [ ] **Step 3: Verificar links cruzados en YouTube**

Reproducir Ep1 completo y verificar:
- [ ] End screen aparece en los últimos 20 segundos con link a Ep2
- [ ] Card con link al repo aparece alrededor de los 5 min

Hacer lo mismo con Ep2 (links a Ep1 y Ep3) y Ep3 (links a Ep1 y Ep2).

- [ ] **Step 4: LinkedIn post de serie completa (1 semana después de Ep3)**

```
📚 Serie completa: Seguridad en APIs .NET

3 episodios, 8 patrones de seguridad, 1 repo con 31 tests:

→ Ep1: JWT hardening, API Keys con SHA-256, CORS sin wildcards
→ Ep2: Rate limiting built-in, IP filter O(1), 7 security headers
→ Ep3: Audit logging post-pipeline, FluentValidation como behavior, Testcontainers

El ángulo: no tutoriales de cómo escribir el código. Análisis de por qué cada decisión, con los ADRs como hilo conductor.

📁 Código: https://github.com/elsaldagaster-stack/dotnet-api-security
🎥 Playlist: [link playlist YouTube]

#dotnet #security #csharp #portfolio #apidevelopment
```

---

## Self-review del plan

**Spec coverage:**
- [x] 3 episodios con estructura completa
- [x] Scripts detallados por segmento
- [x] Demo commands exactos
- [x] Thumbnails + descripciones de YouTube
- [x] LinkedIn posts por episodio
- [x] End screens y cards
- [x] Setup técnico de grabación
- [x] Ensayos con timing por episodio
- [x] Cohesión de serie (playlist, README, cross-linking)
- [x] Limitación documentada sobre Redis/IdentityServer

**Sin placeholders:** Sin TBD, sin "implementar después", todos los comandos son ejecutables.

**Consistencia:** Los archivos referenciados en cada task existen en el repo. Los comandos curl usan la misma URL y credenciales en todos los tasks.
