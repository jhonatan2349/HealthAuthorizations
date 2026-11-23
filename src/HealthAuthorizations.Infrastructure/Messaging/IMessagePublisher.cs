namespace HealthAuthorizations.Infrastructure.Messaging;

/// <summary>Mensaje listo para publicar, con los metadatos del sobre.</summary>
public sealed record MensajeSaliente(Guid MessageId, string Tipo, string Contenido, DateTimeOffset OcurridoEn);

/// <summary>Publica en el broker. Debe confirmar la entrega (o lanzar) antes de retornar.</summary>
public interface IMessagePublisher
{
    Task PublicarAsync(MensajeSaliente mensaje, CancellationToken cancellationToken);
}
