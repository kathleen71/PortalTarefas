using Microsoft.EntityFrameworkCore;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.Models;

namespace PortalTarefas.Web.Services;

public class TarefaService
{
    private readonly PortalTarefasDbContext _context;

    public TarefaService(PortalTarefasDbContext context)
    {
        _context = context;
    }

    public List<Tarefa> Listar()
    {
        return _context.Tarefas
            .AsNoTracking()
            .ToList();
    }

    public Tarefa? Buscar(int id)
    {
        return _context.Tarefas.Find(id);
    }

    public void Adicionar(Tarefa tarefa)
    {
        _context.Tarefas.Add(tarefa);
        _context.SaveChanges();
    }

    public bool Atualizar(Tarefa tarefa)
    {
        var existente = Buscar(tarefa.Id);

        if (existente is null)
            return false;

        existente.Titulo = tarefa.Titulo;
        existente.Descricao = tarefa.Descricao;
        existente.Situacao = tarefa.Situacao;
        existente.Versao++;

        _context.SaveChanges();

        return true;
    }
}
