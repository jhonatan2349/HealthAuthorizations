using HealthAuthorizations.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HealthAuthorizations.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Tipo).HasMaxLength(OutboxMessage.MaxLongitudTipo).IsUnicode(false).IsRequired();
        builder.Property(m => m.Contenido).IsRequired();
        builder.Property(m => m.TraceParent).HasMaxLength(OutboxMessage.MaxLongitudTraceParent).IsUnicode(false);
        builder.Property(m => m.UltimoError).HasMaxLength(OutboxMessage.MaxLongitudError);

        // El publicador solo lee pendientes en orden de ocurrencia: índice filtrado, pequeño aunque la tabla crezca.
        builder.HasIndex(m => m.OcurridoEn)
            .HasDatabaseName("IX_OutboxMessages_Pendientes")
            .HasFilter("[ProcesadoEn] IS NULL");
    }
}
