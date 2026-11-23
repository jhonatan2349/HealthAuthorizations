# Decisiones técnicas

Registro resumido de las decisiones de arquitectura (ADR). Cada una indica la alternativa considerada y por qué se eligió la opción actual.
Los números son estables: se citan desde el código, el [STATUS](../STATUS.md) y la [guía de la defensa](guia-defensa.md).

## Arquitectura y dominio

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 001 | Clean Architecture en 4 proyectos | Vertical slices, monolito por capas | Lo exige el mandato; separa el dominio de la infraestructura y facilita probarlo. |
| 005 | `IApplicationDbContext` expuesto a Application (con `DbSet<T>`) | Repositorios por agregado | Evita una abstracción sobre otra (EF ya es Unit of Work + Repository). Trade-off: Application depende del paquete `Microsoft.EntityFrameworkCore`. |
| 007 | Distribución de montos con Strategy (`IPoliticaCobertura`) | 70/30 fijo en el código | Cambiar la regla = otra implementación o configuración, sin tocar el agregado ni los casos de uso. El porcentaje se guarda por detalle para que el histórico no cambie. |
| 013 | Id `long` con HiLo para `Autorizacion` | Identity / GUID v7 | El Id se conoce antes del INSERT (los eventos del Outbox lo necesitan) sin un viaje a la BD por fila. Los GUID v7 se fragmentan en SQL Server, que ordena `uniqueidentifier` por los últimos bytes. |
| 014 | `Numero` como columna calculada persistida | Generarlo en la aplicación | Siempre consistente con el Id, único e indexable, sin condiciones de carrera. |
| 015 | Contratante e intermediario guardados en la autorización (snapshot) | Derivarlos del afiliado con un JOIN | Preserva el histórico si el afiliado cambia de empresa y permite filtrar por contratante con índice propio. |
| 016 | Integridad en dominio **y** en BD (CHECK/FK/UNIQUE) | Solo en el dominio | La BD es la última línea de defensa ante scripts, otros sistemas o errores futuros. |
| 017 | Excepciones de dominio para invariantes | Result en el dominio | Una invariante rota es un error del llamador; los handlers validan antes (FluentValidation) y usan Result para errores esperados (Fase 3). |
| 019 | Result pattern para errores esperados; excepciones para lo excepcional | Excepciones para todo | No encontrada, inactiva o referencia inexistente son flujos normales: Result los hace explícitos y baratos. Validación de entrada y concurrencia usan excepciones porque cortan el pipeline y las maneja un handler global. |
| 020 | Lado de lectura con JOINs y proyección directa | Navegaciones en el agregado + Include + Mapster `ProjectToType` | El agregado referencia a otros por Id (DDD). La proyección trae solo las columnas necesarias en una consulta. Mapster se usa donde aporta: mapear contratos de entrada al dominio. |

## Datos y rendimiento

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 003 | SQL Server 2022 en Docker | PostgreSQL | Ecosistema .NET empresarial; `rowversion`, índices filtrados y columnas calculadas nativos. |
| 004 | EF Core para escritura y lectura | Dapper para lecturas | Un solo stack; con `AsNoTracking` + proyecciones el rendimiento es suficiente. Se introduce Dapper solo si una consulta medida lo justifica. |
| 009 | Paginación keyset con cursor opaco, sin total | Offset (`Skip/Take`) + `COUNT(*)` | OFFSET lee y descarta todas las filas previas y el COUNT recorre la tabla: ambos se degradan con el volumen. Keyset cuesta lo mismo en cualquier página; el cursor opaco permite cambiar el orden sin romper el contrato. |

## Casos de uso (CQRS) y validación

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 006 | MediatR 15 + behaviors (logging → validación) | Handlers propios / Wolverine | Lo exige el mandato; los behaviors resuelven aspectos transversales en un solo lugar. Desde v13 requiere license key (gratuita en Community) → `MediatR:LicenseKey` por configuración, nunca en el repo. |
| 002 | Mapster | AutoMapper | Licencia MIT (AutoMapper pasó a licencia comercial), buen rendimiento y `ProjectToType` para proyectar en SQL. |
| 030 | Validación únicamente con FluentValidation | DataAnnotations + FluentValidation | Una sola fuente de reglas, mensajes consistentes en español y probados unitariamente. |
| 008 | Idempotencia: `ClaveIdempotencia` en la autorización con índice único filtrado | Tabla de idempotencia aparte / solo validar duplicados | La clave se guarda en la misma transacción que la autorización: dos solicitudes simultáneas iguales no pueden crear dos registros. El header `Idempotency-Key` se agrega en la Fase 4. |

## API REST

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 022 | Controllers con `[ApiController]` | Minimal APIs | El mandato pide ASP.NET Core Web API; los controllers agrupan endpoints, filtros y metadatos OpenAPI de forma conocida por cualquier equipo. Los handlers ya son delgados, así que el costo es mínimo. |
| 023 | Inactivación como `POST /{id}/inactivacion` con motivo | `DELETE` lógico / `PATCH` de estado | `DELETE` sugiere borrar y no admite cuerpo de forma estándar; el motivo es obligatorio. El sub-recurso expresa la intención y permite agregar otros cambios de estado sin romper el contrato. |
| 024 | Concurrencia con `ETag` / `If-Match` (412) y `If-None-Match` (304) | Versión en el cuerpo | Es el mecanismo estándar de HTTP: clientes, proxies y herramientas lo entienden. El 304 ahorra transferencia en consultas repetidas. |
| 025 | Reintento idempotente responde 201 + `Idempotent-Replayed: true` | 200 / 409 | El cliente recibe la misma respuesta que en el primer intento (semántica del draft IETF Idempotency-Key) y puede distinguir el reintento si le interesa. |
| 026 | Versionado por prefijo de ruta `api/v1` | Asp.Versioning (header / query) | Hay una sola versión: el prefijo es visible, cacheable y no agrega dependencias. Se adopta la librería si aparece una v2 que conviva con la v1. |

## Seguridad

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 027 | JWT validado por la API; emisión delegada a un IdP (OIDC) | Usuarios y contraseñas en la API | La API no debe gestionar credenciales. Con `Jwt:Authority` se integra con Entra ID o Keycloak sin cambiar código; el emisor de demo existe solo para poder evaluar la solución sin infraestructura extra y no puede activarse en producción. |
| 028 | Autorización por roles en políticas con nombre | `[Authorize(Roles=...)]` disperso | Las políticas centralizan la regla: cambiar quién puede escribir es un cambio en un solo lugar. El fallback obliga a que un endpoint nuevo nazca protegido. |
| 029 | Rate limiting en la API (ventana fija por usuario/IP) | Solo en el gateway / ingress | Defensa en profundidad: la API se protege aunque el ingress cambie. En el clúster el límite efectivo por réplica se complementa con el del ingress. |
| 018 | Cadena de conexión en user-secrets / variables de entorno | `appsettings.json` | Ningún secreto en el repositorio. |
| 045 | `ForwardedHeaders` solo desde redes declaradas | `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (confía en todo) | Confiar en cualquier `X-Forwarded-For` permite falsear la IP y evadir el rate limiting por IP. Probado con Kestrel real. |

## Observabilidad y resiliencia

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 031 | OpenTelemetry con exportación OTLP | SDK de un proveedor (App Insights, Datadog) | Estándar abierto: cambiar de backend es configuración. En local se ve con Aspire Dashboard; en Azure, vía collector o Azure Monitor. |
| 032 | Logging nativo (`ILogger` + consola JSON + OTel) | Serilog | Cubre lo necesario (estructurado, scopes, JSON, exportación OTel) sin otra dependencia ni configuración paralela. |
| 033 | Métricas y trazas de negocio con APIs del BCL en Application | OpenTelemetry en Application | `ActivitySource` y `Meter` son .NET estándar; Application queda libre de frameworks y la regla de arquitectura se mantiene. |
| 034 | Sonda de SQL propia, sin reintentos | `AddDbContextCheck` (usa la estrategia de reintentos de EF) | Una sonda debe responder en segundos; con reintentos de EF podría tardar más que el timeout del probe y dar falsos negativos. |
| 035 | Liveness sin dependencias | Liveness que consulta la BD | Si la BD cae, reiniciar pods no la arregla y agrava el problema; readiness saca el pod del balanceo mientras tanto. |

## Integración y caché

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 010 | Transactional Outbox + RabbitMQ (al menos una vez) | Publicar directo al broker tras guardar | Publicar fuera de la transacción pierde eventos (cae el broker) o crea fantasmas (falla la BD). El outbox lo resuelve con la transacción que ya existe; el costo es entrega al menos una vez, que los consumidores manejan con `MessageId`. |
| 036 | RabbitMQ.Client directo | MassTransit / Wolverine | MassTransit v9 pasó a licencia comercial; el caso (publicar desde un outbox) cabe en ~150 líneas explicables y probadas. Si aparecen sagas o muchos consumidores, se reevalúa. |
| 037 | Lease con `UPDATE … READPAST` y backoff en el mismo campo | Transacción larga con bloqueo / elección de líder | No mantiene transacciones abiertas mientras publica y escala horizontalmente sin coordinación extra. |
| 038 | Eventos sin PII, consumidores consultan la API | Evento con todos los datos (event-carried state) | Los mensajes se replican y retienen en sistemas fuera de nuestro control; los datos de salud no deben viajar ahí. |
| 039 | Broker como dependencia degradada, no de readiness | Readiness falla si cae RabbitMQ | El outbox desacopla la disponibilidad: sacar pods por el broker convertiría una caída parcial en total. |
| 040 | HybridCache (L1 + Redis L2) solo para catálogos | Redis para todo / sin caché | Los catálogos son pequeños, muy leídos y casi estáticos. HybridCache evita la estampida y sigue funcionando sin Redis. Las autorizaciones cambian y requieren consistencia: no se cachean. |
| 041 | Sin MongoDB ni SignalR | Usarlos por estar en la lista | El mandato pide seleccionar con criterio: no hay datos documentales ni clientes web en tiempo real. |

## Calidad y pruebas

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 011 | xUnit v3 + Microsoft.Testing.Platform + Shouldly | VSTest + FluentAssertions | MTP es el runner nativo de .NET 10. FluentAssertions v8 pasó a licencia comercial; Shouldly es MIT. Cobertura con `Microsoft.Testing.Extensions.CodeCoverage` (Cobertura → SonarQube). |
| 012 | Warnings como errores + analizadores | Solo convenciones | La calidad se verifica en cada build, no solo en revisión; apoya la meta de “0 errores bloqueantes” de Sonar. |
| 021 | Pruebas de integración con Testcontainers (SQL Server real) | EF InMemory / SQLite | InMemory no aplica constraints, transacciones ni rowversion, y SQLite no soporta HiLo ni la columna calculada: probarían otra base. El contenedor se comparte por ensamblado y se aísla por datos. |
| 048 | Cobertura con ReportGenerator → formato genérico de SonarQube | Coverlet + OpenCover | El runner MTP ya trae cobertura de Microsoft; ReportGenerator la consolida (3 proyectos) para Azure DevOps y para Sonar, y aplica el mínimo de 80 % en el pipeline. |
| 049 | El pipeline verifica que las migraciones estén al día | Confiar en la revisión | `has-pending-model-changes` y el diff de `migrations.sql` evitan desplegar código cuyo modelo no coincide con la BD. En este proyecto detectó un cambio que habría borrado columnas de auditoría. |

## Contenedores y entrega

| # | Decisión | Alternativas | Justificación |
|---|----------|--------------|---------------|
| 042 | Migraciones en un job aparte (bundle de EF Core) | `Database.Migrate()` al arrancar la API | Con varias réplicas, migrar al arrancar compite por el esquema y obliga a que la API tenga permisos DDL. El job corre una vez antes del despliegue (servicio de compose / Job o hook PreSync en Kubernetes) y la API puede usar un usuario sin DDL. |
| 043 | Imágenes *chiseled* (Ubuntu sin shell) | `aspnet` estándar / Alpine | Menor superficie de ataque y menos CVE que escanear; no root por defecto. Variante `extra` por ICU (culturas) y tzdata. Trade-off: sin shell para depurar → se usa `kubectl debug` o un contenedor efímero. |
| 044 | Un Dockerfile con targets `api` y `migrator` | Dos Dockerfiles | Ambas imágenes salen del mismo build: imposible desplegar una API con un esquema de otra versión del código. |
| 046 | Sin `HEALTHCHECK` en la imagen | `curl` en la imagen | La imagen no tiene shell ni curl, a propósito. La salud la verifica el orquestador por HTTP (`/health/live`, `/health/ready`), que es lo que usará Kubernetes. |
| 047 | Entorno `Demo` para el compose | `Development` / `Production` en compose | `Development` no viaja en la imagen y `Production` prohíbe el emisor de tokens y oculta Scalar. `Demo` deja evaluar la solución con la misma imagen que iría a producción, cambiando solo configuración. |
| 050 | Trivy en el pipeline antes del push | Escaneo solo en el registro | Una imagen vulnerable no llega al registro; `--ignore-unfixed` evita bloquear por CVE sin parche disponible. |
| 051 | Sin Kubernetes/GitOps | Manifiestos + Argo CD | Opcionales en el mandato; se priorizó la calidad verificable. El diseño ya cumple lo que pide un clúster (ver Fase 10). |
