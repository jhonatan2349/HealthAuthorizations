using HealthAuthorizations.Application.Autorizaciones.Common;

namespace HealthAuthorizations.Application.Catalogos;

public sealed record CatalogoDto(int Id, string Codigo, string Nombre, bool Activo);

public sealed record PrestadorDto(int Id, string Codigo, string Nombre, string Documento, ReferenciaDto TipoPrestador, bool Activo);

public sealed record AfiliadoDto(
    int Id,
    string NumeroAfiliado,
    string Documento,
    string Nombres,
    string Apellidos,
    DateOnly FechaNacimiento,
    ReferenciaDto Contratante,
    ReferenciaDto? Intermediario,
    bool Activo);
