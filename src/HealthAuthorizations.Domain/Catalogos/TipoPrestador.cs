namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Clasificación del prestador: médico, farmacia, clínica, laboratorio.</summary>
public sealed class TipoPrestador : Catalogo
{
    private TipoPrestador()
    {
    }

    public TipoPrestador(string codigo, string nombre)
        : base(codigo, nombre)
    {
    }
}
