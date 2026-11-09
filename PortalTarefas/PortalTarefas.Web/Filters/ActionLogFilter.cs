using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PortalTarefas.Web.Filters;

public class ActionLogFilter : IAsyncActionFilter
{
    private readonly ILogger<ActionLogFilter> _logger;

    public ActionLogFilter(ILogger<ActionLogFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var cronometro = Stopwatch.StartNew();
        var nomeAcao = context.ActionDescriptor.DisplayName;

        var resultado = await next();

        cronometro.Stop();

        var tipoResultado = resultado.Result?.GetType().Name
            ?? "Sem resultado";

        _logger.LogInformation(
            "Ação: {Acao} | Resultado: {Resultado} | Duração: {Duracao} ms",
            nomeAcao,
            tipoResultado,
            cronometro.ElapsedMilliseconds);
    }
}
