# Eventos de integración

La API informa a otros sistemas cuando se registra, modifica o cambia de estado una autorización.
Para integrarse **no hace falta modificar la API**: basta con crear una cola propia y enlazarla al exchange.

## Cómo suscribirse

| Elemento | Valor |
|----------|-------|
| Broker | RabbitMQ |
| Exchange | `healthauthorizations.eventos` (tipo `topic`, durable) |
| Routing key | el tipo del evento (ver tabla) |

```text
cola "facturacion"   ── binding "autorizacion.creada.v1"  ──┐
cola "notificaciones" ─ binding "autorizacion.#"          ──┼── exchange healthauthorizations.eventos
cola "auditoria"     ── binding "#"                       ──┘
```

## Eventos

| Tipo (routing key) | Cuándo | Campos |
|--------------------|--------|--------|
| `autorizacion.creada.v1` | Se registra una autorización | `autorizacionId`, `numero`, `afiliadoId`, `prestadorId`, `contratanteId`, `intermediarioId`, `fechaServicio`, `montoReclamado`, `montoArs`, `montoAfiliado`, `cantidadServicios` |
| `autorizacion.actualizada.v1` | Se modifican datos o servicios | `autorizacionId`, `numero`, `prestadorId`, `fechaServicio`, `montoReclamado`, `montoArs`, `montoAfiliado`, `cantidadServicios` |
| `autorizacion.estado-cambiado.v1` | Cambia el estado (p. ej. inactivación) | `autorizacionId`, `numero`, `estadoAnterior`, `estadoNuevo` |

Ejemplo (`autorizacion.creada.v1`):

```json
{
  "autorizacionId": 501,
  "numero": "AUT-0000000501",
  "afiliadoId": 5,
  "prestadorId": 3,
  "contratanteId": 2,
  "intermediarioId": null,
  "fechaServicio": "2026-11-23",
  "montoReclamado": 1200.00,
  "montoArs": 840.00,
  "montoAfiliado": 360.00,
  "cantidadServicios": 1
}
```

## Propiedades del mensaje

| Propiedad AMQP | Contenido |
|----------------|-----------|
| `message_id` | Identificador único y estable del evento (GUID v7) |
| `type` | Tipo del evento (igual a la routing key) |
| `content_type` | `application/json` |
| `delivery_mode` | `2` (persistente) |
| `timestamp` | Momento en que ocurrió el hecho (Unix, segundos) |
| header `traceparent` | Contexto W3C para continuar la traza distribuida |

## Garantías y responsabilidades del consumidor

- **Al menos una vez.** Ante un fallo entre publicar y confirmar, el mismo evento puede llegar de nuevo con el
  **mismo `message_id`**: el consumidor debe ser idempotente (guardar los `message_id` procesados).
- **Sin orden garantizado entre autorizaciones.** Para una misma autorización, use `timestamp` para descartar
  eventos más antiguos que el último aplicado.
- **Sin datos personales ni clínicos.** Los eventos llevan identificadores y montos; quien necesite el detalle
  lo consulta en `GET /api/v1/autorizaciones/{id}` con su propio token.
- **Versionado.** Los cambios compatibles (agregar campos) mantienen la versión; los incompatibles crean `v2`,
  que se publica en paralelo mientras los consumidores migran. Ignore los campos desconocidos.

## Cómo funciona del lado de la API (Transactional Outbox)

1. El caso de uso guarda la autorización; en el **mismo** `SaveChanges` se inserta el evento en `OutboxMessages`.
   Si la transacción falla no hay evento; si el broker está caído el evento no se pierde.
2. Un proceso en segundo plano toma lotes con un *lease* (`UPDATE … WITH (READPAST)`): varias réplicas pueden
   trabajar a la vez sin publicar el mismo mensaje.
3. Publica con *publisher confirms* y lo marca como procesado. Si falla, reintenta con backoff exponencial;
   tras `MaxIntentos` queda para revisión manual (`Intentos >= MaxIntentos` en `OutboxMessages`, métrica
   `healthauth.outbox.agotados`).
