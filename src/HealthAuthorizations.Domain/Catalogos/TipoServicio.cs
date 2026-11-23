namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Servicio autorizable: consulta, emergencia, hospitalización, laboratorio, imágenes.</summary>
public sealed class TipoServicio : Catalogo
{
    private TipoServicio()
    {
    }

    public TipoServicio(string codigo, string nombre)
        : base(codigo, nombre)
    {
    }
}
