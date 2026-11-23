# STATUS — HealthAuthorizations API

Documento vivo para seguir el avance por fases, las buenas prácticas aplicadas y las decisiones técnicas.
Actualizar al cerrar cada fase (y antes de cada merge a `main`).

**Leyenda:** ✅ Hecho · 🚧 En progreso · ⏳ Pendiente · ➖ Descartado (con justificación)

---

## 1. Resumen de fases

| # | Fase | Rama | Estado |
|---|------|------|--------|
| 0 | Fundaciones (solución, capas, Git, Docker SQL, build, pruebas de arquitectura) | `chore/fase-0-fundaciones` | ✅ |
| 1 | Application base (Mapster) | `feature/application-mapster` | ✅ |
| 2 | Dominio + Persistencia (EF Core, migraciones, seed) | `feature/fase-2-dominio-persistencia` | ✅ |
| 3 | Application CQRS (MediatR, FluentValidation, reglas) | `feature/fase-3-cqrs` | ✅ |
| 4 | API REST (endpoints, ProblemDetails, idempotencia, Scalar) | `feature/fase-4-api` | ✅ |
| 5 | Seguridad (JWT, políticas, rate limiting) | `feature/fase-5-seguridad` | ✅ |
| 6 | Observabilidad y resiliencia (logs, OTel, health checks) | `feature/fase-6-observabilidad` | ✅ |
| 7 | Integración (Outbox + RabbitMQ, caché Redis) | `feature/fase-7-integracion` | ✅ |
| 8 | Pruebas y calidad (≥80 % cobertura, Sonar) | `feature/fase-10-cicd` (junto al pipeline) | ✅ |
| 9 | Contenerización (Dockerfile, Compose completo) | `feature/fase-9-contenerizacion` | ✅ |
| 10 | CI/CD Azure DevOps (K8s/GitOps ➖ opcional) | `feature/fase-10-cicd` | ✅ |
| 11 | Documentación final y preparación de la defensa | `feature/fase-11-docs` | ⏳ |

> Las pruebas **no** esperan a la Fase 8: cada fase entrega sus propias pruebas. La Fase 8 cierra brechas de cobertura y añade el análisis estático.

---

## 2. Detalle por fase

### Fase 0 — Fundaciones ✅
- [x] Solución `.slnx` con capas Api / Application / Domain / Infrastructure
- [x] SQL Server en Docker Compose con healthcheck, puerto ligado a `127.0.0.1`
- [x] `.env` ignorado por Git
- [x] `.env.example` con valores de ejemplo
- [x] `Directory.Build.props`: `Nullable`, `ImplicitUsings`, `TreatWarningsAsErrors`, `AnalysisLevel=latest-recommended`, `EnforceCodeStyleInBuild`
- [x] `Directory.Packages.props` (Central Package Management): paquetes .NET alineados a 10.0.15, pinning transitivo
- [x] `.editorconfig` con convenciones de estilo y nomenclatura
- [x] `global.json` fijando el SDK 10 y el runner Microsoft.Testing.Platform
- [x] Eliminar `WeatherForecast` de la plantilla
- [x] `tests/ArchitectureTests` (NetArchTest): 4 reglas de dependencias entre capas
- [x] NuGet Audit + `TreatWarningsAsErrors`: un paquete vulnerable rompe el build
- ➖ `UnitTests` e `IntegrationTests` se crean con su primera prueba (Fases 2 y 4), para no dejar proyectos vacíos

### Fase 1 — Application base ✅
- [x] Mapster registrado con `TypeAdapterConfig.Scan` + `IMapper` (`ServiceMapper`)

### Fase 2 — Dominio + Persistencia ✅
**Dominio (sin dependencias externas)**
- [x] Entidades: `Autorizacion` (aggregate root), `AutorizacionDetalle`, `Afiliado`, `Intermediario`, `Prestador`, `Contratante`, `TipoPrestador`, `TipoServicio`
- [x] Base `Catalogo` (código único + nombre + activo) para no duplicar código en los catálogos
- [x] `Autorizacion` controla sus detalles: colección de solo lectura, backing field `_detalles`
- [x] Invariantes: 1–50 servicios, monto > 0 con máx. 2 decimales, fecha obligatoria, no modificar una autorización inactiva
- [x] Operaciones atómicas: si un servicio es inválido el agregado no cambia (se valida todo antes de mutar)
- [x] `DistribucionMonto` (value object): ARS + afiliado = reclamado al centavo; redondeo half away from zero
- [x] `IPoliticaCobertura` (Strategy) + `PoliticaCoberturaPorcentual` (70 % por defecto, configurable)
- [x] Porcentaje aplicado guardado en cada detalle (el histórico no cambia si cambia la regla)
- [x] Estado enum (`Activa`, `Inactiva`) + fecha y motivo de inactivación (soft delete)
- [x] `Participantes`: contratante e intermediario como snapshot del afiliado al autorizar
- [x] Eventos de dominio: `AutorizacionCreada`, `AutorizacionActualizada`, `AutorizacionEstadoCambiado`
- [x] Auditoría (`CreadoEn/Por`, `ModificadoEn/Por`) con setters privados
- [x] 47 pruebas unitarias de dominio (redondeo, extremos, invariantes, eventos)

**Persistencia**
- [x] `IApplicationDbContext` + `ICurrentUser` (Application) ← `ApplicationDbContext` (Infrastructure)
- [x] Fluent API por entidad (`IEntityTypeConfiguration<T>`), sin data annotations en el dominio
- [x] `decimal(18,2)` por convención, porcentaje `decimal(5,2)`, longitudes desde constantes del dominio
- [x] FKs con `Restrict`; cascada solo dentro del agregado (Autorización → Detalle)
- [x] `CHECK`: montos no negativos, `MontoArs + MontoAfiliado = MontoReclamado`, estado válido, inactivación coherente
- [x] `rowversion` para concurrencia optimista
- [x] Índices únicos: códigos, documentos, `Numero`, `ClaveIdempotencia` (filtrado `IS NOT NULL`)
- [x] Índices por afiliado, prestador, contratante, intermediario (filtrado) y fecha de servicio
- ➖ Índice por `Estado`: baja selectividad (2 valores); se reevalúa con el plan de ejecución en la Fase 3
- [x] HiLo para el Id de autorización + `Numero` (`AUT-0000000001`) como columna calculada persistida
- [x] Interceptor de auditoría; versiona el agregado aunque solo cambien sus detalles
- [x] `EnableRetryOnFailure`
- [x] Migración `InicialEsquemaYCatalogos` + `database/migrations.sql` idempotente
- [x] Seed (`HasData`): 4 tipos de prestador, 5 tipos de servicio, 3 intermediarios, 4 contratantes, 8 prestadores, 10 afiliados
- [x] `AddInfrastructure()`; cadena de conexión vía user-secrets / variable de entorno
- [x] `dotnet-ef` como herramienta local (`dotnet tool restore`)
- [x] Verificado contra SQL Server real: constraints rechazan datos inválidos; round-trip EF (HiLo, auditoría, concurrencia, idempotencia)
- [ ] Pasar la verificación de round-trip a pruebas de integración automatizadas (Testcontainers, Fase 4/8)

### Fase 3 — Application CQRS ✅
- [x] MediatR con un handler por caso de uso, organizados por slice (`Autorizaciones/Crear|Actualizar|Inactivar|Obtener|Listar`)
- [x] `ValidationBehavior` (FluentValidation en el pipeline → `ValidationException` → 400 en la Fase 4)
- [x] `LoggingBehavior` con `LoggerMessage`: duración, código de error y alerta > 500 ms, sin registrar PII
- [x] Result pattern (`Result`/`Result<T>` + `Error` tipado) para errores esperados; códigos estables (`Afiliado.NoExiste`, …)
- [x] Validaciones de forma compartidas entre crear y actualizar (`IDatosAutorizacion`), sin duplicación
- [x] Existencia y estado de afiliado, contratante, prestador y tipos de servicio (errores claros; las FKs siguen como garantía final)
- [x] Idempotencia: reintento con la misma clave devuelve la original; la carrera la resuelve el índice único; otra clave/afiliado → 409
- [x] Concurrencia optimista: versión (If-Match) como valor original del `rowversion`
- [x] Infraestructura traduce `DbUpdateConcurrencyException` y violaciones de índice único a excepciones de Application
- [x] Queries con `AsNoTracking` y proyección directa a DTO con JOINs (una consulta, sin navegaciones entre agregados)
- [x] Listado con paginación keyset (cursor opaco base64url), sin `COUNT(*)`; límite 1–100
- [x] Filtros: número, afiliado, documento del afiliado, prestador, contratante, intermediario, tipo de servicio, estado, rango de fechas
- [x] Política de cobertura desde configuración (`Cobertura:PorcentajeArs`), validada al arrancar
- [x] Mapster para mapear contratos de entrada a objetos de valor del dominio
- [x] Pruebas: 55 unitarias de Application + 23 de integración con Testcontainers (SQL Server real) → 129 en total
- [ ] Prueba de carga del listado con volumen alto (script de 1M de filas + plan de ejecución) → Fase 8
- [x] Consultas de catálogos (prestadores, afiliados, tipos) → hechas en la Fase 4

### Fase 4 — API REST ✅
- [x] Controllers con `[ApiController]`, rutas versionadas por prefijo `api/v1`
- [x] `POST /api/v1/autorizaciones` → 201 + `Location` + `ETag`
- [x] `GET /api/v1/autorizaciones` con 9 filtros y paginación por cursor (`nextCursor`, `hasMore`)
- [x] `GET /api/v1/autorizaciones/{id}` con `ETag`; `If-None-Match` → 304
- [x] `PUT /api/v1/autorizaciones/{id}` con `If-Match` → 412 si la versión no coincide
- [x] `POST /api/v1/autorizaciones/{id}/inactivacion` (motivo obligatorio) → 204; repetir → 409
- [x] Catálogos de solo lectura: tipos de servicio y prestador, contratantes, intermediarios, prestadores y afiliados (búsqueda + cursor)
- [x] Header `Idempotency-Key`; reintentos responden lo mismo con `Idempotent-Replayed: true`
- [x] Contratos HTTP separados de los comandos (idempotencia y versión viajan en headers)
- [x] `ProblemDetails` (RFC 9457) vía `IExceptionHandler`: 400, 404, 409, 412, 422 y 500 sin detalles internos
- [x] Todo error lleva `code` estable, `traceId` e `instance`, también los del framework (JSON malformado, 404 de ruta)
- [x] Mensajes de validación en español
- [x] Middleware de Correlation-Id: header `X-Correlation-Id`, scope de logging y `traceId` de los errores, con sanitización
- [x] OpenAPI (`/openapi/v1.json`) con comentarios XML + Scalar (`/scalar`), en todos los ambientes salvo producción
- [x] Enums como texto en JSON y en query string
- [x] `CancellationToken` en todos los endpoints; cancelaciones del cliente no se registran como error
- [x] Colección `.http` con el flujo de demostración
- [x] 28 pruebas HTTP con `WebApplicationFactory` sobre SQL Server real → 157 pruebas en total
- [x] Bug encontrado al probar manualmente (validadores parciales en DI) → corregido y el fixture valida el grafo (`ValidateOnBuild`)
- ➖ Compresión de respuestas: se delega al ingress (evita BREACH sobre HTTPS con datos sensibles)
- ➖ Librería de versionado (Asp.Versioning): con una sola versión basta el prefijo de ruta (YAGNI)

### Fase 5 — Seguridad ✅
- [x] JWT Bearer: firma, emisor, audiencia y vigencia obligatoria; solo HS256; tolerancia de reloj de 30 s
- [x] Dos modos sin cambiar código: `Jwt:Authority` (OIDC: Entra ID, Keycloak) o clave HMAC de 32+ bytes
- [x] Claims sin mapeo heredado (`sub`, `name`, `role`); la auditoría registra el `sub` del token
- [x] Emisor de tokens de demo (`POST /api/v1/auth/token`): solo por configuración, prohibido en producción (falla al arrancar), 404 si está deshabilitado, clave comparada en tiempo constante
- [x] Usuarios de demo `lector` y `operador` (solo en `appsettings.Development.json`)
- [x] Fallback policy: todo exige autenticación salvo documentación y emisor (`[AllowAnonymous]`)
- [x] Políticas: lectura (`autorizaciones.lector` u `operador`), escritura (`autorizaciones.operador`)
- [x] 401 / 403 / 429 como ProblemDetails con `code`; 401 con `WWW-Authenticate: Bearer`
- [x] Rate limiting: global por usuario (o IP si es anónimo) y estricto en el emisor de tokens; `Retry-After`
- [x] Headers: nosniff, `X-Frame-Options: DENY`, Referrer-Policy, CORP, CSP restrictiva (salvo `/scalar`)
- [x] `Cache-Control: private, no-cache` (datos de salud; compatible con ETag)
- [x] Sin header `Server`; cuerpo máximo de 1 MB (Kestrel + filtro MVC)
- [x] HSTS fuera de Development; CORS cerrado por defecto y configurable por orígenes
- [x] Secretos fuera del repo: clave de firma en user-secrets (dev) / variables de entorno / K8s Secret
- [x] Configuración insegura impide el arranque (`ValidateOnStart`)
- [x] Validación solo en FluentValidation (se desactiva la implícita de MVC: duplicaba reglas y respondía en inglés)
- [x] Esquema Bearer en OpenAPI/Scalar
- [x] 20 pruebas de seguridad (incluye 413 con Kestrel real) → 177 pruebas en total; sin paquetes vulnerables
- [x] `ForwardedHeaders` para IP real y HTTPS detrás del ingress → hecho en la Fase 9 (solo redes declaradas)
- [x] Revisión de PII en logs con el logging estructurado → hecha en la Fase 6

### Fase 6 — Observabilidad y resiliencia ✅
- [x] Logs estructurados con el logging nativo: JSON en UTC (producción) y texto de una línea (Development)
- [x] Scope `CorrelationId` en cada log (legible en texto, propiedad estructurada en JSON/OTel)
- [x] Sin PII en logs: solo nombre del caso de uso, duración y código de error; EF Core en Warning fuera de dev (verificado)
- [x] OpenTelemetry: trazas (ASP.NET Core, HttpClient, SqlClient sin texto SQL) + span por caso de uso (`TracingBehavior`)
- [x] Métricas: runtime, ASP.NET Core, HttpClient y de negocio (creadas, actualizadas, inactivadas, reintentos idempotentes, monto reclamado)
- [x] Application solo usa `ActivitySource`/`IMeterFactory` del BCL: no depende de OpenTelemetry
- [x] Exportación OTLP solo si existe `OTEL_EXPORTER_OTLP_ENDPOINT` (independiente del proveedor)
- [x] Aspire Dashboard en docker-compose como visor local (http://localhost:18888)
- [x] `X-Correlation-Id` = TraceId del span: un incidente reportado se ubica en trazas y logs (probado)
- [x] `/health/live` (proceso) y `/health/ready` (SQL Server con `SELECT 1`, sin reintentos, 5 s), anónimas, sin rate limiting, sin detalles
- [x] Timeout por solicitud (30 s → 504 ProblemDetails) que cancela hasta SQL Server; timeout de comandos SQL
- [x] Reintentos de EF ante fallos transitorios (Fase 2) + apagado ordenado (`ShutdownTimeout`, verificado con SIGTERM)
- [x] 10 pruebas nuevas (span, métricas, sondas, 503 sin SQL, correlación) → 187 en total
- [x] Defecto encontrado en prueba manual: scope del CorrelationId ilegible en consola → corregido
- [ ] `preStop` + `terminationGracePeriodSeconds` y probes en los manifiestos → Fase 10
- [ ] Panel/alertas (p. ej. Grafana) sobre las métricas → fuera de alcance; se documenta qué alertar

### Fase 7 — Integración ✅
- [x] Transactional Outbox: `OutboxInterceptor` guarda los eventos en el mismo `SaveChanges` que el cambio (probado: sin evento si la operación falla)
- [x] Eventos de integración versionados (`autorizacion.creada.v1`, `.actualizada.v1`, `.estado-cambiado.v1`), separados de los eventos de dominio
- [x] Sin PII ni datos clínicos en los eventos (sin nombres, diagnóstico ni motivo)
- [x] Número del evento calculado con la misma regla que la columna (`Autorizacion.FormatearNumero`, probado contra la BD)
- [x] `OutboxProcessor` (BackgroundService) + `OutboxDispatcher` con lease `READPAST`: varias réplicas sin duplicar (probado con 3 en paralelo)
- [x] RabbitMQ: exchange topic durable, mensajes persistentes, publisher confirms, `MessageId` estable, propagación de `traceparent`
- [x] Reintentos con backoff exponencial (2 s → 5 min) y límite para revisión manual; métricas publicados/fallidos/agotados
- [x] Broker caído: la API sigue operando, `/health/ready` informa Degraded (200) y el outbox acumula
- [x] Mensajería deshabilitable por configuración sin perder eventos
- [x] HybridCache para catálogos simples: L1 memoria (2 min) + Redis L2 compartido (10 min) si se configura; protección contra estampida
- [x] Afiliados fuera de caché (PII); validación de referencias al crear siempre contra la BD
- [x] RabbitMQ y Redis en docker-compose; credenciales en `.env` / user-secrets
- [x] Contrato para consumidores en [docs/eventos-integracion.md](docs/eventos-integracion.md)
- [x] 9 pruebas de integración nuevas (incluye RabbitMQ real con Testcontainers) → 203 en total, 3 corridas seguidas en verde
- [x] Prueba inestable de la Fase 5 (413 con Kestrel bajo carga) detectada y corregida con `Expect: 100-continue`
- ➖ MongoDB / SignalR: sin un caso de uso que lo justifique (los consumidores en tiempo real pueden suscribirse a RabbitMQ)
- [ ] Consumidor de ejemplo (worker) → opcional, si queda tiempo para la demo

### Fase 8 — Pruebas y calidad ✅
Las pruebas se escribieron en cada fase; aquí se consolidan y se miden (SonarQube 25.10 local, mismo análisis que el pipeline).
- [x] Unitarias: dominio (invariantes, 70/30, redondeo), validadores, handlers, mapeo de eventos
- [x] Integración: `WebApplicationFactory` + Testcontainers (SQL Server y RabbitMQ reales)
- [x] Escenarios críticos: idempotencia, concurrencia (409/412), entidades inexistentes, autorización inactiva, outbox multi-réplica
- [x] Arquitectura (NetArchTest): reglas de dependencia entre capas
- [x] **207 pruebas** en verde
- [x] Cobertura consolidada con `Microsoft.Testing.Extensions.CodeCoverage` + ReportGenerator: **91 % de líneas** (Release, sin migraciones), 73 % de ramas
- [x] SonarQube: quality gate **OK**, duplicación **0,0 %**, 0 bugs, 0 vulnerabilidades, 0 hotspots, calificación A en confiabilidad, seguridad y mantenibilidad
- [x] Corregidos: variables sin comillas en el Dockerfile (S6570) y constructor de `Program` (S1118)
- [x] Falsos positivos aceptados (marcar como *Accepted* en el servidor): S1144 ×8 (setters privados que usa EF Core; quitarlos borraría columnas, comprobado con `has-pending-model-changes`) y S6964 ×5 (campos omitidos llegan como 0 y FluentValidation los rechaza)
- [x] `dotnet list package --vulnerable --include-transitive`: sin vulnerabilidades; imágenes con Trivy: 0 HIGH/CRITICAL
- [ ] Prueba de carga del listado con ~1 M de filas → ➖ fuera de alcance; el diseño (keyset + índices) se explica en ADR-009

### Fase 9 — Contenerización ✅
- [x] `Dockerfile` multi-stage con dos imágenes finales: `api` y `migrator` (mismo build, mismas versiones)
- [x] Imágenes *chiseled* `noble-chiseled-extra`: sin shell ni gestor de paquetes, usuario no root (UID 1654), con ICU/tzdata
- [x] Compilación multi-arquitectura (`$BUILDPLATFORM` → `$TARGETARCH`): amd64 y arm64 sin emulación en el build
- [x] Capa de `restore` separada (solo se invalida si cambia un `.csproj`/props) + caché de NuGet con BuildKit
- [x] `.dockerignore`: sin `.env`, secretos, `bin/obj`, pruebas ni `.git` en el contexto
- [x] `appsettings.Development.json` (usuarios demo) excluido de la publicación
- [x] Migraciones como **job** (`migrator`, bundle de EF Core autocontenido), no al arrancar la API; idempotente (probado 2 veces sobre la misma BD)
- [x] Compose completo: `sqlserver` → `migrator` (`service_completed_successfully`) → `api` (+ rabbitmq, redis, aspire con `service_healthy`)
- [x] Endurecimiento en compose: `read_only`, `tmpfs /tmp`, `cap_drop: ALL`, `no-new-privileges`
- [x] `stop_grace_period` 30 s > `ShutdownTimeout` 25 s; SIGTERM verificado (sale con código 0 en < 1 s sin tráfico)
- [x] `ForwardedHeaders` configurable (`ProxyInverso`): solo redes CIDR declaradas, `ValidateOnStart` si se habilita sin redes
- [x] Redirección HTTPS configurable: la imagen la desactiva (TLS termina en el ingress)
- [x] Sin warnings al arrancar: Data Protection no se usa (API sin estado) → su categoría de log en `Error`
- [x] 4 pruebas nuevas (proxy confiable / no confiable con Kestrel real, configuración insegura) → 207 en total
- [x] Prueba de humo en contenedor: token, creación, reintento idempotente, listado, outbox publicado, `/health/ready`
- [ ] Escaneo de vulnerabilidades de la imagen (Trivy) y firma → Fase 10 (pipeline)
- [ ] Usuario de BD con mínimo privilegio para la API (hoy `sa` en local) → Fase 10 (Secret por ambiente)

### Fase 10 — CI/CD Azure DevOps ✅
- [x] `azure-pipelines.yml` con dos stages: **CI** (PR y main) e **Imágenes**
- [x] CI: restore con caché → build (warnings = errores) → paquetes vulnerables → modelo de EF sin cambios pendientes y `migrations.sql` al día → pruebas con Docker (Testcontainers) → TRX + cobertura publicadas → mínimo 80 % → SonarQube con `qualitygate.wait`
- [x] Imágenes: build de `api` y `migrator` etiquetadas `<buildId>-<commit>` → Trivy (falla con HIGH/CRITICAL corregibles) → push al registro solo desde `main`
- [x] Artefacto `migraciones-sql` para el DBA
- [x] Cada paso de script se ejecutó en local antes del commit (la verificación de migraciones detectó un cambio que habría borrado columnas)
- [x] SonarQube opcional por parámetro (`ejecutarSonar`) y service connections parametrizadas
- [ ] Kubernetes, GitOps (Kustomize/Argo CD) → ➖ opcionales en el mandato; la solución queda lista para ello: imagen no root y de solo lectura, sondas `/health/live` y `/health/ready`, apagado ordenado, migrador como Job, `ForwardedHeaders`
- [ ] Usuario de BD con mínimo privilegio para la API → se configura por ambiente (hoy `sa` en local)

### Fase 11 — Documentación ⏳
- [ ] README: requisitos, cómo ejecutar (Docker y local), cómo obtener token, ejemplos
- [ ] `docs/decisiones-tecnicas.md` (a partir de la sección 4 de este documento)
- [ ] Diagrama de arquitectura y modelo ER
- [ ] Colección `.http` con los flujos de la demo
- [ ] Guion de la defensa + cambios que podrían pedir en vivo (p. ej. cambiar 70/30 → 80/20)

---

## 3. Buenas prácticas (checklist permanente)

**Arquitectura**
- Regla de dependencias: `Api → Application → Domain`; `Infrastructure → Application`. El dominio no conoce EF, HTTP ni MediatR.
- El dominio protege sus invariantes (constructores/métodos de fábrica, setters privados). Nada de modelos anémicos para `Autorizacion`.
- Un caso de uso = un handler. Commands cambian estado; Queries no.
- YAGNI: no agregar patrones (repositorio genérico, Unit of Work sobre EF, microservicios) sin un problema que resolver.

**Datos y consistencia**
- Integridad en dos capas: dominio + constraints en BD (la BD es la última línea de defensa).
- Dinero siempre `decimal`; redondeo explícito; la parte del afiliado = reclamado − ARS (la suma siempre cuadra).
- Guardar el porcentaje aplicado en cada detalle: el histórico no cambia si cambia la regla.
- Concurrencia optimista con `rowversion`; idempotencia con índice único.
- Soft delete; los inactivos se conservan.

**Rendimiento**
- `AsNoTracking` + proyecciones en lecturas; evitar N+1 y `Include` innecesarios.
- Paginación obligatoria; nunca devolver la tabla completa.
- Índices alineados con los filtros reales; revisar el plan de ejecución.
- `async` de punta a punta con `CancellationToken`.

**Seguridad**
- Ningún secreto en el repositorio. `.env` ignorado; `.env.example` documentado.
- Todo endpoint protegido salvo health checks y docs.
- Validar toda entrada (FluentValidation) y responder errores con ProblemDetails sin filtrar detalles internos.

**Calidad y Git**
- Conventional Commits (`feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`).
- Una rama por fase, PR a `main` con descripción; commits pequeños y con sentido (el historial se evalúa).
- Warnings como errores; analizadores activos.
- Cada PR incluye pruebas de lo que agrega.

---

## 4. Registro de decisiones técnicas (ADR resumido)

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 001 | Clean Architecture en 4 proyectos | Vertical slices, monolito por capas | Lo exige el mandato; separa el dominio de la infraestructura y facilita probarlo. |
| 002 | Mapster | AutoMapper | Licencia MIT (AutoMapper pasó a licencia comercial), buen rendimiento y `ProjectToType` para proyectar en SQL. |
| 003 | SQL Server 2022 en Docker | PostgreSQL | Ecosistema .NET empresarial; `rowversion`, índices filtrados y columnas calculadas nativos. |
| 004 | EF Core para escritura y lectura | Dapper para lecturas | Un solo stack; con `AsNoTracking` + proyecciones el rendimiento es suficiente. Se introduce Dapper solo si una consulta medida lo justifica. |
| 005 | `IApplicationDbContext` expuesto a Application (con `DbSet<T>`) | Repositorios por agregado | Evita una abstracción sobre otra (EF ya es Unit of Work + Repository). Trade-off: Application depende del paquete `Microsoft.EntityFrameworkCore`. |
| 006 | MediatR 15 + behaviors (logging → validación) | Handlers propios / Wolverine | Lo exige el mandato; los behaviors resuelven aspectos transversales en un solo lugar. Desde v13 requiere license key (gratuita en Community) → `MediatR:LicenseKey` por configuración, nunca en el repo. |
| 007 | Distribución de montos con Strategy (`IPoliticaCobertura`) | 70/30 fijo en el código | Cambiar la regla = otra implementación o configuración, sin tocar el agregado ni los casos de uso. El porcentaje se guarda por detalle para que el histórico no cambie. |
| 008 | Idempotencia: `ClaveIdempotencia` en la autorización con índice único filtrado | Tabla de idempotencia aparte / solo validar duplicados | La clave se guarda en la misma transacción que la autorización: dos solicitudes simultáneas iguales no pueden crear dos registros. El header `Idempotency-Key` se agrega en la Fase 4. |
| 009 | Paginación keyset con cursor opaco, sin total | Offset (`Skip/Take`) + `COUNT(*)` | OFFSET lee y descarta todas las filas previas y el COUNT recorre la tabla: ambos se degradan con el volumen. Keyset cuesta lo mismo en cualquier página; el cursor opaco permite cambiar el orden sin romper el contrato. |
| 010 | Transactional Outbox + RabbitMQ (al menos una vez) | Publicar directo al broker tras guardar | Publicar fuera de la transacción pierde eventos (cae el broker) o crea fantasmas (falla la BD). El outbox lo resuelve con la transacción que ya existe; el costo es entrega al menos una vez, que los consumidores manejan con `MessageId`. |
| 011 | xUnit v3 + Microsoft.Testing.Platform + Shouldly | VSTest + FluentAssertions | MTP es el runner nativo de .NET 10. FluentAssertions v8 pasó a licencia comercial; Shouldly es MIT. Cobertura con `Microsoft.Testing.Extensions.CodeCoverage` (Cobertura → SonarQube). |
| 012 | Warnings como errores + analizadores | Solo convenciones | La calidad se verifica en cada build, no solo en revisión; apoya la meta de “0 errores bloqueantes” de Sonar. |
| 013 | Id `long` con HiLo para `Autorizacion` | Identity / GUID v7 | El Id se conoce antes del INSERT (los eventos del Outbox lo necesitan) sin un viaje a la BD por fila. Los GUID v7 se fragmentan en SQL Server, que ordena `uniqueidentifier` por los últimos bytes. |
| 014 | `Numero` como columna calculada persistida | Generarlo en la aplicación | Siempre consistente con el Id, único e indexable, sin condiciones de carrera. |
| 015 | Contratante e intermediario guardados en la autorización (snapshot) | Derivarlos del afiliado con un JOIN | Preserva el histórico si el afiliado cambia de empresa y permite filtrar por contratante con índice propio. |
| 016 | Integridad en dominio **y** en BD (CHECK/FK/UNIQUE) | Solo en el dominio | La BD es la última línea de defensa ante scripts, otros sistemas o errores futuros. |
| 017 | Excepciones de dominio para invariantes | Result en el dominio | Una invariante rota es un error del llamador; los handlers validan antes (FluentValidation) y usan Result para errores esperados (Fase 3). |
| 019 | Result pattern para errores esperados; excepciones para lo excepcional | Excepciones para todo | No encontrada, inactiva o referencia inexistente son flujos normales: Result los hace explícitos y baratos. Validación de entrada y concurrencia usan excepciones porque cortan el pipeline y las maneja un handler global. |
| 020 | Lado de lectura con JOINs y proyección directa | Navegaciones en el agregado + Include + Mapster `ProjectToType` | El agregado referencia a otros por Id (DDD). La proyección trae solo las columnas necesarias en una consulta. Mapster se usa donde aporta: mapear contratos de entrada al dominio. |
| 021 | Pruebas de integración con Testcontainers (SQL Server real) | EF InMemory / SQLite | InMemory no aplica constraints, transacciones ni rowversion, y SQLite no soporta HiLo ni la columna calculada: probarían otra base. El contenedor se comparte por ensamblado y se aísla por datos. |
| 022 | Controllers con `[ApiController]` | Minimal APIs | El mandato pide ASP.NET Core Web API; los controllers agrupan endpoints, filtros y metadatos OpenAPI de forma conocida por cualquier equipo. Los handlers ya son delgados, así que el costo es mínimo. |
| 023 | Inactivación como `POST /{id}/inactivacion` con motivo | `DELETE` lógico / `PATCH` de estado | `DELETE` sugiere borrar y no admite cuerpo de forma estándar; el motivo es obligatorio. El sub-recurso expresa la intención y permite agregar otros cambios de estado sin romper el contrato. |
| 024 | Concurrencia con `ETag` / `If-Match` (412) y `If-None-Match` (304) | Versión en el cuerpo | Es el mecanismo estándar de HTTP: clientes, proxies y herramientas lo entienden. El 304 ahorra transferencia en consultas repetidas. |
| 025 | Reintento idempotente responde 201 + `Idempotent-Replayed: true` | 200 / 409 | El cliente recibe la misma respuesta que en el primer intento (semántica del draft IETF Idempotency-Key) y puede distinguir el reintento si le interesa. |
| 026 | Versionado por prefijo de ruta `api/v1` | Asp.Versioning (header / query) | Hay una sola versión: el prefijo es visible, cacheable y no agrega dependencias. Se adopta la librería si aparece una v2 que conviva con la v1. |
| 027 | JWT validado por la API; emisión delegada a un IdP (OIDC) | Usuarios y contraseñas en la API | La API no debe gestionar credenciales. Con `Jwt:Authority` se integra con Entra ID o Keycloak sin cambiar código; el emisor de demo existe solo para poder evaluar la solución sin infraestructura extra y no puede activarse en producción. |
| 028 | Autorización por roles en políticas con nombre | `[Authorize(Roles=...)]` disperso | Las políticas centralizan la regla: cambiar quién puede escribir es un cambio en un solo lugar. El fallback obliga a que un endpoint nuevo nazca protegido. |
| 029 | Rate limiting en la API (ventana fija por usuario/IP) | Solo en el gateway / ingress | Defensa en profundidad: la API se protege aunque el ingress cambie. En el clúster el límite efectivo por réplica se complementa con el del ingress. |
| 030 | Validación únicamente con FluentValidation | DataAnnotations + FluentValidation | Una sola fuente de reglas, mensajes consistentes en español y probados unitariamente. |
| 031 | OpenTelemetry con exportación OTLP | SDK de un proveedor (App Insights, Datadog) | Estándar abierto: cambiar de backend es configuración. En local se ve con Aspire Dashboard; en Azure, vía collector o Azure Monitor. |
| 032 | Logging nativo (`ILogger` + consola JSON + OTel) | Serilog | Cubre lo necesario (estructurado, scopes, JSON, exportación OTel) sin otra dependencia ni configuración paralela. |
| 033 | Métricas y trazas de negocio con APIs del BCL en Application | OpenTelemetry en Application | `ActivitySource` y `Meter` son .NET estándar; Application queda libre de frameworks y la regla de arquitectura se mantiene. |
| 034 | Sonda de SQL propia, sin reintentos | `AddDbContextCheck` (usa la estrategia de reintentos de EF) | Una sonda debe responder en segundos; con reintentos de EF podría tardar más que el timeout del probe y dar falsos negativos. |
| 035 | Liveness sin dependencias | Liveness que consulta la BD | Si la BD cae, reiniciar pods no la arregla y agrava el problema; readiness saca el pod del balanceo mientras tanto. |
| 036 | RabbitMQ.Client directo | MassTransit / Wolverine | MassTransit v9 pasó a licencia comercial; el caso (publicar desde un outbox) cabe en ~150 líneas explicables y probadas. Si aparecen sagas o muchos consumidores, se reevalúa. |
| 037 | Lease con `UPDATE … READPAST` y backoff en el mismo campo | Transacción larga con bloqueo / elección de líder | No mantiene transacciones abiertas mientras publica y escala horizontalmente sin coordinación extra. |
| 038 | Eventos sin PII, consumidores consultan la API | Evento con todos los datos (event-carried state) | Los mensajes se replican y retienen en sistemas fuera de nuestro control; los datos de salud no deben viajar ahí. |
| 039 | Broker como dependencia degradada, no de readiness | Readiness falla si cae RabbitMQ | El outbox desacopla la disponibilidad: sacar pods por el broker convertiría una caída parcial en total. |
| 040 | HybridCache (L1 + Redis L2) solo para catálogos | Redis para todo / sin caché | Los catálogos son pequeños, muy leídos y casi estáticos. HybridCache evita la estampida y sigue funcionando sin Redis. Las autorizaciones cambian y requieren consistencia: no se cachean. |
| 041 | Sin MongoDB ni SignalR | Usarlos por estar en la lista | El mandato pide seleccionar con criterio: no hay datos documentales ni clientes web en tiempo real. |
| 042 | Migraciones en un job aparte (bundle de EF Core) | `Database.Migrate()` al arrancar la API | Con varias réplicas, migrar al arrancar compite por el esquema y obliga a que la API tenga permisos DDL. El job corre una vez antes del despliegue (servicio de compose / Job o hook PreSync en Kubernetes) y la API puede usar un usuario sin DDL. |
| 043 | Imágenes *chiseled* (Ubuntu sin shell) | `aspnet` estándar / Alpine | Menor superficie de ataque y menos CVE que escanear; no root por defecto. Variante `extra` por ICU (culturas) y tzdata. Trade-off: sin shell para depurar → se usa `kubectl debug` o un contenedor efímero. |
| 044 | Un Dockerfile con targets `api` y `migrator` | Dos Dockerfiles | Ambas imágenes salen del mismo build: imposible desplegar una API con un esquema de otra versión del código. |
| 045 | `ForwardedHeaders` solo desde redes declaradas | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (confía en todo) | Confiar en cualquier `X-Forwarded-For` permite falsear la IP y evadir el rate limiting por IP. Probado con Kestrel real. |
| 046 | Sin `HEALTHCHECK` en la imagen | `curl` en la imagen | La imagen no tiene shell ni curl, a propósito. La salud la verifica el orquestador por HTTP (`/health/live`, `/health/ready`), que es lo que usará Kubernetes. |
| 047 | Entorno `Demo` para el compose | `Development` / `Production` en compose | `Development` no viaja en la imagen y `Production` prohíbe el emisor de tokens y oculta Scalar. `Demo` deja evaluar la solución con la misma imagen que iría a producción, cambiando solo configuración. |
| 048 | Cobertura con ReportGenerator → formato genérico de SonarQube | Coverlet + OpenCover | El runner MTP ya trae cobertura de Microsoft; ReportGenerator la consolida (3 proyectos) para Azure DevOps y para Sonar, y aplica el mínimo de 80 % en el pipeline. |
| 049 | El pipeline verifica que las migraciones estén al día | Confiar en la revisión | `has-pending-model-changes` y el diff de `migrations.sql` evitan desplegar código cuyo modelo no coincide con la BD. En este proyecto detectó un cambio que habría borrado columnas de auditoría. |
| 050 | Trivy en el pipeline antes del push | Escaneo solo en el registro | Una imagen vulnerable no llega al registro; `--ignore-unfixed` evita bloquear por CVE sin parche disponible. |
| 051 | Sin Kubernetes/GitOps | Manifiestos + Argo CD | Opcionales en el mandato; se priorizó la calidad verificable. El diseño ya cumple lo que pide un clúster (ver Fase 10). |
| 018 | Cadena de conexión en user-secrets / variables de entorno | `appsettings.json` | Ningún secreto en el repositorio. |
---

## 5. Bitácora

| Fecha | Avance |
|-------|--------|
| 2026-11-23 | Estructura inicial, Mapster (PR #1), SQL Server en Compose. Inicio de Fase 2: paquetes EF Core y `IApplicationDbContext` vacío. Se crea este `STATUS.md`. |
| 2026-11-23 | Fase 0 cerrada: build común, CPM, `.editorconfig`, `.env.example`, limpieza de plantilla, pruebas de arquitectura. |
| 2026-11-23 | Fase 2 cerrada: dominio rico, política de cobertura, persistencia EF Core, migración inicial, seed y script SQL. 51 pruebas en verde. |
| 2026-11-23 | Fase 3 cerrada: CQRS con MediatR, validación en pipeline, Result pattern, idempotencia, concurrencia, listado keyset. 129 pruebas en verde (incluye integración con SQL Server real). |
| 2026-11-23 | Fase 4 cerrada: API REST completa con ProblemDetails, idempotencia, ETag/If-Match, Correlation-Id, catálogos y Scalar. 157 pruebas en verde (28 HTTP). |
| 2026-11-23 | Fase 5 cerrada: JWT con políticas por rol, emisor de demo controlado, rate limiting, headers de seguridad, límites y CORS. 177 pruebas en verde. |
| 2026-11-23 | Fase 6 cerrada: logs JSON con CorrelationId, OpenTelemetry (trazas, métricas de negocio, logs), Aspire Dashboard, sondas live/ready, timeouts y apagado ordenado. 187 pruebas en verde. |
| 2026-11-23 | Fase 7 cerrada: Transactional Outbox con RabbitMQ (lease multi-réplica, confirms, backoff), eventos versionados sin PII, HybridCache + Redis, docs para consumidores. 203 pruebas en verde. |
| 2026-11-23 | Fase 9 cerrada (antes que la 8): Dockerfile multi-stage con imágenes chiseled de API y migrador, compose completo endurecido, ForwardedHeaders, migraciones como job. 207 pruebas en verde. |
| 2026-11-23 | Fases 8 y 10 cerradas: pipeline de Azure DevOps (CI, calidad, Trivy, push), cobertura 91 %, SonarQube quality gate OK con 0,0 % de duplicación. Kubernetes/GitOps descartados por ser opcionales. 207 pruebas en verde. |
