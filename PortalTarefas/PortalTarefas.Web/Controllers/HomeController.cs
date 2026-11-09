using Microsoft.AspNetCore.Mvc;
using PortalTarefas.Web.Filters;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Services;
using PortalTarefas.Web.ViewModels;

namespace PortalTarefas.Web.Controllers;

[ServiceFilter(typeof(ActionLogFilter))]
public class HomeController : Controller
{
    private readonly TarefaService _service;

    public HomeController(TarefaService service)
    {
        _service = service;
    }

    public IActionResult Index()
    {
        return View(_service.Listar());
    }

    public IActionResult Details(int id)
    {
        var tarefa = _service.Buscar(id);

        if (tarefa is null)
            return NotFound();

        return View(tarefa);
    }

    public IActionResult Create()
    {
        return View(new TarefaInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(TarefaInputModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tarefa = new Tarefa
        {
            Titulo = model.Titulo,
            Descricao = model.Descricao,
            Situacao = model.Situacao
        };

        _service.Adicionar(tarefa);

        TempData["Sucesso"] = "Tarefa cadastrada com sucesso!";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var tarefa = _service.Buscar(id);

        if (tarefa is null)
            return NotFound();

        var model = new TarefaInputModel
        {
            Titulo = tarefa.Titulo,
            Descricao = tarefa.Descricao,
            Situacao = tarefa.Situacao
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, TarefaInputModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var tarefa = new Tarefa
        {
            Id = id,
            Titulo = model.Titulo,
            Descricao = model.Descricao,
            Situacao = model.Situacao
        };

        if (!_service.Atualizar(tarefa))
            return NotFound();

        TempData["Sucesso"] = "Tarefa atualizada com sucesso!";
        return RedirectToAction(nameof(Index));
    }
}
