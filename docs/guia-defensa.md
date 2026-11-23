# Guía para explicar el proyecto

Para presentar la solución sin recorrer todo el código: qué decir, qué abrir y cómo responder.
El detalle de cada decisión está en [decisiones-tecnicas.md](decisiones-tecnicas.md) (se citan como ADR-xxx).

---

## 1. La solución en un minuto

> Es una API REST en .NET 10 para gestionar autorizaciones de servicios de salud. Separé el código en cuatro capas (Clean Architecture): el **dominio** tiene las reglas del negocio y no depende de nada; **Application** tiene un caso de uso por operación con MediatR; **Infrastructure** implementa la persistencia con EF Core y SQL Server; y la **API** solo traduce HTTP.
>
> Las reglas del mandato se cumplen en dos lugares: en el dominio y también en la base de datos, con constraints, porque la base es la última línea de defensa. El 70/30 es configurable y cada detalle guarda el porcentaje aplicado, así que cambiar la regla no altera el histórico. Los reintentos no duplican gracias a una clave de idempotencia con índice único, y la concurrencia se controla con `rowversion` y `ETag`.
>
> Cuando cambia una autorización se publica un evento a RabbitMQ con el patrón *Transactional Outbox*, para no perder eventos si el broker cae. Hay 207 pruebas, 91 % de cobertura y 0 % de duplicación en SonarQube, y todo corre con `docker compose up`.

---

## 2. Los 6 archivos que conviene conocer

Con estos se puede explicar el 80 % del proyecto.

| # | Archivo | Qué mostrar |
|---|---|---|
| 1 | [Autorizacion.cs](../src/HealthAuthorizations.Domain/Autorizaciones/Autorizacion.cs) | El agregado: `Crear`, `Actualizar`, `Inactivar`. Setters privados; las reglas no se pueden saltar desde fuera. |
| 2 | [DistribucionMonto.cs](../src/HealthAuthorizations.Domain/Cobertura/DistribucionMonto.cs) | El cálculo 70/30: la ARS se redondea y el afiliado es la **diferencia**, así la suma siempre cuadra al centavo. |
| 3 | [CrearAutorizacionHandler.cs](../src/HealthAuthorizations.Application/Autorizaciones/Crear/CrearAutorizacionHandler.cs) | Un caso de uso completo: idempotencia, validación de entidades, creación y la carrera de dos solicitudes iguales. |
| 4 | [AutorizacionesController.cs](../src/HealthAuthorizations.Api/Controllers/AutorizacionesController.cs) | Controller delgado: `Idempotency-Key`, `ETag`/`If-Match` y `Result` → ProblemDetails. |
| 5 | [AutorizacionConfiguration.cs](../src/HealthAuthorizations.Infrastructure/Persistence/Configurations/AutorizacionConfiguration.cs) | CHECK constraints, índices, `rowversion`, columna calculada `Numero`, HiLo. |
| 6 | [OutboxDispatcher.cs](../src/HealthAuthorizations.Infrastructure/Messaging/OutboxDispatcher.cs) | Publicación de eventos con lease `READPAST`: varias réplicas sin publicar dos veces. |

---

## 3. Recorrido de una solicitud: `POST /api/v1/autorizaciones`

Es la mejor forma de mostrar todas las capas de una vez.

1. **Entrada HTTP**: [CorrelationIdMiddleware](../src/HealthAuthorizations.Api/Middleware/CorrelationIdMiddleware.cs) asigna el id de correlación. Después se validan el JWT y el rate limiting, y la política `Escritura` exige el rol operador.
2. **Controller**: lee `Idempotency-Key`, arma el command y lo envía a MediatR.
3. **Pipeline de MediatR**:
   - `TracingBehavior` abre un span de OpenTelemetry.
   - `LoggingBehavior` registra la duración, sin datos personales.
   - `ValidationBehavior` corre [CrearAutorizacionValidator](../src/HealthAuthorizations.Application/Autorizaciones/Crear/CrearAutorizacionValidator.cs). Si falla, responde **400** con los mensajes en español.
4. **Handler**:
   - **Reintento**: si la clave ya existe, devuelve la autorización ya creada.
   - **Entidades**: si el afiliado, el prestador o un tipo de servicio no existen o están inactivos, responde **422** con un código estable (`Result`, no excepción).
   - **Snapshot**: copia el contratante y el intermediario del afiliado.
5. **Dominio**: `Autorizacion.Crear` valida las invariantes (≥ 1 servicio, máximo 50, montos válidos) y aplica `IPoliticaCobertura` a cada servicio.
6. **Persistencia**: en un solo `SaveChanges`, y por lo tanto una sola transacción:
   - el interceptor de auditoría llena `CreadoEn/CreadoPor`;
   - el interceptor de outbox guarda el evento `autorizacion.creada.v1`.
7. **Carrera**: si dos solicitudes con la misma clave llegan a la vez, el índice único rechaza la segunda. El handler la convierte en "devolver la existente".
8. **Respuesta**: **201** con `Location` y `ETag`. Un reintento responde lo mismo, con `Idempotent-Replayed: true`.
9. **Después**: `OutboxProcessor` publica el evento a RabbitMQ con confirmación del broker.

---

## 4. Reglas del mandato: dónde se cumplen y qué lo prueba

| Regla | Dónde | Prueba que lo demuestra |
|---|---|---|
| ARS 70 % / afiliado 30 % | `DistribucionMonto`, `PoliticaCoberturaPorcentual` | `PorPorcentaje_LaSumaSiempreCuadraAlCentavo` |
| Distribución configurable | `Cobertura:PorcentajeArs` en `appsettings.json` | `ConPorcentajeConfigurado_CambiaLaReglaSinTocarCodigo` |
| Al menos un servicio | Validador + agregado + BD | `Crear_SinServicios_Lanza`, `Post_SinServicios_DevuelveMensajeDeFluentValidationEnEspanol` |
| Sin montos negativos | Validador + `DistribucionMonto` + CHECK | `PorPorcentaje_ConMontoNoPositivo_Lanza` |
| Sin entidades inexistentes | Handler (`Result`) + FKs | `Crear_ConAfiliadoInexistente_DevuelveErrorDeNegocio`, `Crear_ConTiposDeServicioInexistentes_IndicaCuales` |
| Conservar el histórico | Inactivación lógica + porcentaje guardado por detalle | `Inactivar_ConservaElRegistroYBloqueaModificaciones` |
| Consistencia concurrente | `rowversion` + `If-Match` (412) | `Actualizar_ConVersionDesactualizada_LanzaConflictoDeConcurrencia` |
| Sin duplicados por reintentos | `Idempotency-Key` + índice único filtrado | `Crear_SolicitudesSimultaneasConLaMismaClave_CreaUnaSola` |
| Listado rápido | Keyset + índices + proyección (ADR-009, ADR-020) | `Listar_RecorreTodasLasPaginasSinDuplicadosYDelMasRecienteAlMasAntiguo` |

---

## 5. Diez decisiones para defender (una frase cada una)

1. **Reglas en el dominio y en la BD** (ADR-016): la base protege contra scripts y otros sistemas que no pasan por la API.
2. **El afiliado paga la diferencia** (ADR-007): redondear las dos partes por separado puede descuadrar un centavo.
3. **Se guarda el porcentaje en cada detalle**: cambiar 70/30 a 80/20 no reescribe autorizaciones viejas.
4. **Idempotencia en la misma transacción** (ADR-008): una tabla aparte o un "buscar antes de insertar" dejaría pasar dos solicitudes simultáneas.
5. **Paginación keyset, sin total** (ADR-009): `OFFSET` y `COUNT(*)` se degradan con millones de filas; keyset cuesta lo mismo en la página 1 y en la 10 000.
6. **Result para lo esperado, excepciones para lo excepcional** (ADR-019): "afiliado no existe" es un flujo normal, no un error del programa.
7. **Outbox** (ADR-010): publicar fuera de la transacción pierde eventos o publica cambios que nunca se guardaron.
8. **Eventos sin datos clínicos** (ADR-038): los mensajes se replican fuera de nuestro control; el consumidor consulta la API si necesita más.
9. **Migraciones en un job, no al arrancar** (ADR-042): con varias réplicas, migrar al arrancar compite por el esquema y la API necesitaría permisos DDL.
10. **Pruebas contra SQL Server real** (ADR-021): EF InMemory no aplica constraints, transacciones ni `rowversion`, así que probaría otra base.

---

## 6. Preguntas probables

**¿Por qué MediatR y no llamar al servicio directamente?**
Lo pide el mandato, y además los *behaviors* resuelven validación, logs y trazas en un solo lugar para todos los casos de uso. Cada handler queda enfocado en una operación.

**¿Por qué no usaste repositorios?**
EF Core ya es *Unit of Work* y repositorio; otra capa encima solo reenvía llamadas. El costo es que Application conoce `DbSet`, y lo acepté a conciencia (ADR-005).

**¿Qué pasa si dos usuarios editan la misma autorización?**
Cada uno trae el `ETag` que leyó. El primero guarda; el segundo recibe **412** porque el `rowversion` cambió, y debe releer. No hay bloqueos en la base.

**¿Qué pasa si se cae RabbitMQ?**
La API sigue funcionando: los eventos quedan en `OutboxMessages` y se publican al volver, con reintentos y espera creciente. `/health/ready` reporta *Degraded*, no *Unhealthy*, para no sacar pods del balanceo por una dependencia secundaria (ADR-039).

**¿Un consumidor puede recibir un evento dos veces?**
Sí, la garantía es "al menos una vez", que es el costo honesto del outbox. Cada mensaje trae un `message_id` estable para descartar duplicados. Está documentado en [eventos-integracion.md](eventos-integracion.md).

**¿Cómo escala el listado con un millón de registros?**
Con paginación keyset sobre índices: cada página es un *seek* al índice, no un recorrido. La consulta proyecta solo las columnas del resumen con JOINs y sin cargar entidades.

**¿Dónde están los secretos?**
Nunca en el repositorio: user-secrets en desarrollo y variables de entorno en Docker y Azure DevOps. Si falta una clave, o es insegura, la API no arranca (`ValidateOnStart`).

**¿Por qué no hiciste Kubernetes?**
Era opcional. Prioricé calidad verificable: pipeline, Sonar y Trivy. La solución ya cumple lo que necesita un clúster (ADR-051):
- imagen no root y de solo lectura;
- sondas `live` y `ready`;
- apagado ordenado;
- migrador listo para correr como Job;
- `ForwardedHeaders`.

**¿Por qué Mapster y Shouldly, y no AutoMapper y FluentAssertions?**
Ambas alternativas pasaron a licencia comercial. Mapster y Shouldly son MIT y cubren lo necesario (ADR-002, ADR-011).

---

## 7. Cambios que podrían pedir en vivo

| Pedido | Cómo se hace | Esfuerzo |
|---|---|---|
| Cambiar 70/30 a 80/20 | `"Cobertura": { "PorcentajeArs": 80 }` en `appsettings.json` o con la variable `Cobertura__PorcentajeArs=80`. Sin tocar código. Las autorizaciones existentes conservan su 70 %. | 1 min |
| Porcentaje distinto por tipo de servicio | `IPoliticaCobertura.Distribuir` ya recibe `tipoServicioId`: nueva implementación (p. ej. diccionario por tipo) y registrarla en `Application/DependencyInjection.cs`. El agregado no cambia. | 15 min |
| Nuevo filtro en el listado | Parámetro en `ListarAutorizacionesQuery` + `Where` en `ListarAutorizacionesHandler` + regla en su validador. Índice si el volumen lo pide. | 10 min |
| Nuevo campo en la autorización | Propiedad en el agregado → configuración EF → `dotnet ef migrations add` → DTO/request → validador. El pipeline avisa si falta la migración. | 20 min |
| Nueva regla de negocio (p. ej. monto máximo) | Regla en el validador (mensaje claro) **y** guarda en el agregado (invariante). | 10 min |

Para la demo del 80/20 en Docker: `Cobertura__PorcentajeArs: "80"` en el servicio `api` de `docker-compose.yml` y `docker compose up -d api`.

---

## 8. Guion de demostración (≈ 10 min)

1. **Levantar todo**: `docker compose up -d --build`, mostrar `docker compose ps` (el `migrator` terminó con código 0).
2. **Scalar** (http://localhost:8080/scalar):
   - Obtener un token con `operador`.
   - Crear una autorización con 2 servicios y mostrar los montos (ARS + afiliado = total).
   - Repetir la misma solicitud con el mismo `Idempotency-Key`: misma autorización, `Idempotent-Replayed: true`.
   - Pedir el detalle (`ETag`), hacer el PUT con `If-Match` y repetirlo: **412**.
   - Inactivar con motivo e intentar actualizar: error de negocio. El registro sigue consultable.
   - Crear con un afiliado inexistente: **422** con ProblemDetails, `code` y `traceId`.
3. **RabbitMQ** (http://localhost:15672): el exchange `healthauthorizations.eventos` recibió los mensajes.
4. **Aspire** (http://localhost:18888): buscar el `traceId` del error y mostrar la traza completa, del HTTP al SQL.
5. **Calidad**: `azure-pipelines.yml` (etapas) y el STATUS con cobertura 91 %, Sonar OK y Trivy limpio.
6. **Cambio en vivo**: 80/20 por configuración y crear otra autorización.

---

## 9. Números para recordar

| | |
|---|---|
| Pruebas | 207 (unitarias, arquitectura, integración con SQL Server y RabbitMQ reales) |
| Cobertura de líneas | 91 % |
| Duplicación | 0,0 % (meta < 3 %) |
| SonarQube | Quality gate OK, 0 bugs, 0 vulnerabilidades, calificación A |
| Vulnerabilidades | 0 en paquetes NuGet, 0 HIGH/CRITICAL en imágenes |
| Decisiones documentadas | 51 ADR |
