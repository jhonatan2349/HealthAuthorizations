namespace HealthAuthorizations.Domain.Catalogos;

/// <summary>Corredor o agente que intermedia la póliza del afiliado.</summary>
public sealed class Intermediario : Catalogo
{
    private Intermediario()
    {
    }

    public Intermediario(string codigo, string nombre)
        : base(codigo, nombre)
    {
    }
}
