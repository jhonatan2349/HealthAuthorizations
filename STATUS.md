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
| 3 | Application CQRS (MediatR, FluentValidation, reglas) | `feature/fase-3-cqrs` | ⏳ |
| 4 | API REST (endpoints, ProblemDetails, idempotencia, Scalar) | `feature/fase-4-api` | ⏳ |
| 5 | Seguridad (JWT, políticas, rate limiting) | `feature/fase-5-seguridad` | ⏳ |
| 6 | Observabilidad y resiliencia (logs, OTel, health checks) | `feature/fase-6-observabilidad` | ⏳ |
| 7 | Integración (Outbox + RabbitMQ, caché Redis) | `feature/fase-7-integracion` | ⏳ |
| 8 | Pruebas y calidad (≥80 % cobertura, Sonar) | `feature/fase-8-pruebas` | ⏳ |
| 9 | Contenerización (Dockerfile, Compose completo) | `feature/fase-9-docker` | ⏳ |
| 10 | CI/CD Azure DevOps + GitOps (K8s, Kustomize/Argo CD) | `feature/fase-10-cicd` | ⏳ |
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

### Fase 3 — Application CQRS ⏳
- [ ] MediatR: un handler por caso de uso (Commands: Crear, Actualizar, Inactivar · Queries: Listar, ObtenerDetalle)
- [ ] `ValidationBehavior` (FluentValidation en el pipeline)
- [ ] `LoggingBehavior` / `PerformanceBehavior`
- [ ] Result pattern para errores de negocio esperados (evitar excepciones como control de flujo)
- [ ] Validación de existencia de entidades relacionadas (afiliado, prestador, etc.)
- [ ] Queries con `AsNoTracking` + proyección directa a DTO (`ProjectToType`)
- [ ] Paginación: keyset (cursor) para el listado masivo; filtros por afiliado, prestador, contratante, estado, rango de fechas
- [ ] DTOs de entrada/salida separados de las entidades

### Fase 4 — API REST ⏳
- [ ] Recursos: `POST/GET /api/v1/autorizaciones`, `GET/PUT /api/v1/autorizaciones/{id}`, `PATCH /api/v1/autorizaciones/{id}/inactivar` (o `DELETE` lógico — decidir y documentar)
- [ ] Endpoints de catálogos de solo lectura
- [ ] Códigos HTTP correctos: 201 + `Location`, 200, 204, 400, 401, 403, 404, 409, 412, 422
- [ ] `ProblemDetails` (RFC 9457) vía `IExceptionHandler` global
- [ ] Header `Idempotency-Key` en `POST` (evita duplicados por reintentos)
- [ ] `ETag` / `If-Match` ligado al `rowversion` en `PUT`
- [ ] Middleware de Correlation-Id
- [ ] Versionado de API
- [ ] OpenAPI + Scalar con ejemplos y esquema de seguridad JWT
- [ ] Compresión de respuestas, `CancellationToken` en todos los endpoints

### Fase 5 — Seguridad ⏳
- [ ] Autenticación JWT Bearer (validación de issuer, audience, lifetime, firma)
- [ ] Endpoint de emisión de tokens solo para demo (documentado como tal)
- [ ] Autorización por políticas/roles (lectura vs escritura)
- [ ] Rate limiting (`AddRateLimiter`)
- [ ] Secretos fuera del código: variables de entorno / User Secrets / K8s Secrets
- [ ] HTTPS, HSTS, headers de seguridad, CORS restrictivo
- [ ] No exponer stack traces ni datos sensibles en respuestas/logs (datos de salud = PII)

### Fase 6 — Observabilidad y resiliencia ⏳
- [ ] Logging estructurado (Serilog o `ILogger` + OTel) con Correlation-Id
- [ ] OpenTelemetry: trazas (ASP.NET Core, EF Core, HTTP), métricas
- [ ] Health checks: `/health/live` y `/health/ready` (SQL, RabbitMQ, Redis)
- [ ] Graceful shutdown

### Fase 7 — Integración ⏳
- [ ] Transactional Outbox: los eventos se guardan en la misma transacción que la autorización
- [ ] Publicador en segundo plano (`BackgroundService`) → RabbitMQ, con reintentos
- [ ] Contratos de eventos versionados; nuevos consumidores sin tocar el core
- [ ] Redis como caché distribuida para catálogos (`HybridCache`)

### Fase 8 — Pruebas y calidad ⏳
- [ ] Unitarias: dominio (invariantes, cálculo 70/30, redondeo), validadores, handlers
- [ ] Integración: `WebApplicationFactory` + Testcontainers (SQL Server real)
- [ ] Escenarios críticos: duplicados por idempotencia, concurrencia (409/412), entidades inexistentes, autorización inactiva
- [ ] Pruebas de arquitectura (NetArchTest): Domain no depende de nada; Application no depende de Infrastructure/Api
- [ ] Cobertura con Coverlet + ReportGenerator (objetivo > 80 %)
- [ ] SonarQube: duplicación < 3 %, sin bugs bloqueantes
- [ ] `dotnet list package --vulnerable` sin críticas

### Fase 9 — Contenerización ⏳
- [ ] Dockerfile multi-stage, imagen `aspnet` mínima, usuario no root
- [ ] `.dockerignore`
- [ ] Compose: api + sqlserver + rabbitmq + redis (+ collector OTel/Jaeger opcional), `depends_on` con `condition: service_healthy`
- [ ] Aplicación automática de migraciones al arrancar (solo en Development) o job de migración

### Fase 10 — CI/CD + GitOps ⏳
- [ ] `azure-pipelines.yml`: restore → build → test + cobertura → Sonar → build/push imagen → actualizar tag en repo GitOps
- [ ] Repositorio GitOps separado: Kustomize (`base/` + `overlays/dev|prod`), Deployment, Service, ConfigMap, Secret, probes, resources, HPA
- [ ] Aplicación de Argo CD apuntando al repo GitOps

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
| 006 | _MediatR — pendiente_ | Handlers propios | Lo exige el mandato. Nota: desde v13 MediatR requiere license key (Community gratuita); documentarlo. |
| 007 | Distribución de montos con Strategy (`IPoliticaCobertura`) | 70/30 fijo en el código | Cambiar la regla = otra implementación o configuración, sin tocar el agregado ni los casos de uso. El porcentaje se guarda por detalle para que el histórico no cambie. |
| 008 | Idempotencia: `ClaveIdempotencia` en la autorización con índice único filtrado | Tabla de idempotencia aparte / solo validar duplicados | La clave se guarda en la misma transacción que la autorización: dos solicitudes simultáneas iguales no pueden crear dos registros. El header `Idempotency-Key` se agrega en la Fase 4. |
| 009 | _Paginación — pendiente_ | Offset (`Skip/Take`) | Keyset: costo constante en páginas profundas con volúmenes altos. Los índices por FK ya incluyen el Id (clave agrupada). |
| 010 | _Mensajería — pendiente_ | Publicar directo al broker | Outbox: sin eventos perdidos ni fantasmas si falla la BD o el broker. |
| 011 | xUnit v3 + Microsoft.Testing.Platform + Shouldly | VSTest + FluentAssertions | MTP es el runner nativo de .NET 10. FluentAssertions v8 pasó a licencia comercial; Shouldly es MIT. Cobertura con `Microsoft.Testing.Extensions.CodeCoverage` (Cobertura → SonarQube). |
| 012 | Warnings como errores + analizadores | Solo convenciones | La calidad se verifica en cada build, no solo en revisión; apoya la meta de “0 errores bloqueantes” de Sonar. |
| 013 | Id `long` con HiLo para `Autorizacion` | Identity / GUID v7 | El Id se conoce antes del INSERT (los eventos del Outbox lo necesitan) sin un viaje a la BD por fila. Los GUID v7 se fragmentan en SQL Server, que ordena `uniqueidentifier` por los últimos bytes. |
| 014 | `Numero` como columna calculada persistida | Generarlo en la aplicación | Siempre consistente con el Id, único e indexable, sin condiciones de carrera. |
| 015 | Contratante e intermediario guardados en la autorización (snapshot) | Derivarlos del afiliado con un JOIN | Preserva el histórico si el afiliado cambia de empresa y permite filtrar por contratante con índice propio. |
| 016 | Integridad en dominio **y** en BD (CHECK/FK/UNIQUE) | Solo en el dominio | La BD es la última línea de defensa ante scripts, otros sistemas o errores futuros. |
| 017 | Excepciones de dominio para invariantes | Result en el dominio | Una invariante rota es un error del llamador; los handlers validan antes (FluentValidation) y usan Result para errores esperados (Fase 3). |
| 018 | Cadena de conexión en user-secrets / variables de entorno | `appsettings.json` | Ningún secreto en el repositorio. |
---

## 5. Bitácora

| Fecha | Avance |
|-------|--------|
| 2026-11-23 | Estructura inicial, Mapster (PR #1), SQL Server en Compose. Inicio de Fase 2: paquetes EF Core y `IApplicationDbContext` vacío. Se crea este `STATUS.md`. |
| 2026-11-23 | Fase 0 cerrada: build común, CPM, `.editorconfig`, `.env.example`, limpieza de plantilla, pruebas de arquitectura. |
| 2026-11-23 | Fase 2 cerrada: dominio rico, política de cobertura, persistencia EF Core, migración inicial, seed y script SQL. 51 pruebas en verde. |
