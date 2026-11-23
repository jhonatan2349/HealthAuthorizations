using HealthAuthorizations.Domain.Catalogos;
using Microsoft.EntityFrameworkCore;

namespace HealthAuthorizations.Infrastructure.Persistence.Seed;

/// <summary>
/// Datos iniciales de catálogos (ficticios) para demostrar la funcionalidad.
/// Se versionan con las migraciones mediante HasData, por lo que forman parte del script SQL.
/// Se usan objetos anónimos porque las entidades protegen sus setters.
/// </summary>
internal static class CatalogosSeed
{
    private const string Usuario = "seed";
    private static readonly DateTimeOffset Fecha = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void Aplicar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TipoPrestador>().HasData(
            Catalogo(1, "MED", "Médico"),
            Catalogo(2, "FAR", "Farmacia"),
            Catalogo(3, "CLI", "Clínica"),
            Catalogo(4, "LAB", "Laboratorio"));

        modelBuilder.Entity<TipoServicio>().HasData(
            Catalogo(1, "CON", "Consulta"),
            Catalogo(2, "EME", "Emergencia"),
            Catalogo(3, "HOS", "Hospitalización"),
            Catalogo(4, "LAB", "Laboratorio"),
            Catalogo(5, "IMG", "Imágenes"));

        modelBuilder.Entity<Intermediario>().HasData(
            Catalogo(1, "INT-001", "Corredora de Seguros del Caribe"),
            Catalogo(2, "INT-002", "Asesores de Salud Quisqueya"),
            Catalogo(3, "INT-003", "Agentes Unidos de Seguros"));

        modelBuilder.Entity<Contratante>().HasData(
            ConDocumento(1, "CTR-001", "Constructora Los Andes SRL", "130000001"),
            ConDocumento(2, "CTR-002", "Banco Comercial del Este SA", "130000002"),
            ConDocumento(3, "CTR-003", "Distribuidora Alimentos del Norte SRL", "130000003"),
            ConDocumento(4, "CTR-004", "Plan Individual", "00100000099"));

        modelBuilder.Entity<Prestador>().HasData(
            Prestador(1, "PRE-001", "Dr. Carlos Martínez - Medicina Interna", "00100000101", tipo: 1),
            Prestador(2, "PRE-002", "Dra. Laura Gómez - Pediatría", "00100000102", tipo: 1),
            Prestador(3, "PRE-003", "Farmacia Salud Total", "130000101", tipo: 2),
            Prestador(4, "PRE-004", "Farmacia del Pueblo", "130000102", tipo: 2),
            Prestador(5, "PRE-005", "Clínica Central", "130000103", tipo: 3),
            Prestador(6, "PRE-006", "Centro Médico del Valle", "130000104", tipo: 3),
            Prestador(7, "PRE-007", "Laboratorio Clínico Referencia", "130000105", tipo: 4),
            Prestador(8, "PRE-008", "Laboratorio e Imágenes Diagnósticas", "130000106", tipo: 4));

        modelBuilder.Entity<Afiliado>().HasData(
            Afiliado(1, "María", "Rodríguez Peña", new DateOnly(1985, 3, 14), contratante: 1, intermediario: 1),
            Afiliado(2, "José", "Fernández Díaz", new DateOnly(1979, 7, 2), contratante: 1, intermediario: 1),
            Afiliado(3, "Ana", "Santos Reyes", new DateOnly(1992, 11, 23), contratante: 2, intermediario: 2),
            Afiliado(4, "Luis", "Castillo Mejía", new DateOnly(1968, 1, 30), contratante: 2, intermediario: 2),
            Afiliado(5, "Carmen", "Jiménez Torres", new DateOnly(2001, 5, 9), contratante: 2, intermediario: null),
            Afiliado(6, "Pedro", "Guzmán Núñez", new DateOnly(1975, 9, 17), contratante: 3, intermediario: 3),
            Afiliado(7, "Rosa", "Vargas Cruz", new DateOnly(1988, 12, 5), contratante: 3, intermediario: 3),
            Afiliado(8, "Miguel", "Herrera Polanco", new DateOnly(1995, 4, 21), contratante: 3, intermediario: null),
            Afiliado(9, "Elena", "Batista Familia", new DateOnly(1960, 8, 11), contratante: 4, intermediario: 1),
            Afiliado(10, "Ramón", "De la Cruz Ortiz", new DateOnly(2010, 2, 28), contratante: 4, intermediario: null));
    }

    private static object Catalogo(int id, string codigo, string nombre) =>
        new { Id = id, Codigo = codigo, Nombre = nombre, Activo = true, CreadoEn = Fecha, CreadoPor = Usuario };

    private static object ConDocumento(int id, string codigo, string nombre, string documento) =>
        new { Id = id, Codigo = codigo, Nombre = nombre, Documento = documento, Activo = true, CreadoEn = Fecha, CreadoPor = Usuario };

    private static object Prestador(int id, string codigo, string nombre, string documento, int tipo) =>
        new
        {
            Id = id,
            Codigo = codigo,
            Nombre = nombre,
            Documento = documento,
            TipoPrestadorId = tipo,
            Activo = true,
            CreadoEn = Fecha,
            CreadoPor = Usuario,
        };

    private static object Afiliado(int id, string nombres, string apellidos, DateOnly nacimiento, int contratante, int? intermediario) =>
        new
        {
            Id = id,
            NumeroAfiliado = $"AF-{id:D6}",
            Documento = $"402{id:D8}",
            Nombres = nombres,
            Apellidos = apellidos,
            FechaNacimiento = nacimiento,
            ContratanteId = contratante,
            IntermediarioId = intermediario,
            Activo = true,
            CreadoEn = Fecha,
            CreadoPor = Usuario,
        };
}
