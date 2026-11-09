using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PortalTarefas.Web.Models;

namespace PortalTarefas.Web.Configurations;

public class TarefaConfiguration : IEntityTypeConfiguration<Tarefa>
{
    public void Configure(EntityTypeBuilder<Tarefa> builder)
    {
        builder.ToTable("Tarefas");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Titulo)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(t => t.Descricao)
            .HasMaxLength(300);

        builder.Property(t => t.Situacao)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(t => t.Versao)
            .IsConcurrencyToken();

        builder.HasIndex(t => t.Situacao);
    }
}
