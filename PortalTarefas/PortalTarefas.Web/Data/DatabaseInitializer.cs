using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Models;

namespace PortalTarefas.Web.Data;

public static class DatabaseInitializer
{
    public static async Task InicializarAsync(
        PortalTarefasDbContext context)
    {
        if (await context.Tarefas.AnyAsync())
            return;

        var tarefas = new List<Tarefa>
        {
            new()
            {
                Titulo = "Organizar documentos",
                Descricao = "Separar e organizar os documentos pendentes.",
                Situacao = "Pendente"
            },
            new()
            {
                Titulo = "Revisar cadastro",
                Descricao = "Conferir os dados cadastrados no sistema.",
                Situacao = "Em andamento"
            },
            new()
            {
                Titulo = "Finalizar relatório",
                Descricao = "Concluir o relatório de atividades.",
                Situacao = "Concluída"
            }
        };

        await context.Tarefas.AddRangeAsync(tarefas);
        await context.SaveChangesAsync();
    }
}