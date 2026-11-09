using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Antiforgery;
using PortalTarefas.Web.Data;
using PortalTarefas.Web.DTOs;
using PortalTarefas.Web.Models;
using PortalTarefas.Web.Observability;
using System.Diagnostics;

namespace PortalTarefas.Web.Endpoints;

public static class TarefaEndpoints
{
    public static void MapTarefaEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/api/tarefas")
            .WithTags("Tarefas")
            .RequireRateLimiting("tarefas")
            .AddEndpointFilter(async (context, next) =>
            {
                var metodo = context.HttpContext.Request.Method;

                if (metodo is "POST" or "PUT" or "DELETE")
                {
                    var antiforgery = context.HttpContext.RequestServices
                        .GetRequiredService<IAntiforgery>();

                    try
                    {
                        await antiforgery.ValidateRequestAsync(
                            context.HttpContext);
                    }
                    catch (AntiforgeryValidationException)
                    {
                        return Results.Problem(
                            title: "Token de segurança inválido",
                            detail: "Atualize o token CSRF e tente novamente.",
                            statusCode: 400);
                    }
                }

                return await next(context);
            });

        // GET: listar com paginação, filtros e ordenação
        grupo.MapGet("/", async (
            int? pagina,
            int? tamanho,
            string? ordenarPor,
            string? direcao,
            string? situacao,
            string? termo,
            PortalTarefasDbContext db) =>
        {
            var numeroPagina = pagina ?? 1;
            var tamanhoPagina = tamanho ?? 10;
            var campo = (ordenarPor ?? "id").ToLowerInvariant();
            var sentido = (direcao ?? "asc").ToLowerInvariant();

            if (numeroPagina < 1)
                return Results.Problem(
                    title: "Página inválida",
                    detail: "O número da página deve ser maior ou igual a 1.",
                    statusCode: 400);

            if (tamanhoPagina < 1 || tamanhoPagina > 50)
                return Results.Problem(
                    title: "Tamanho inválido",
                    detail: "O tamanho deve estar entre 1 e 50.",
                    statusCode: 400);

            var camposPermitidos = new[] { "id", "titulo", "situacao", "versao" };

            if (!camposPermitidos.Contains(campo))
                return Results.Problem(
                    title: "Ordenação inválida",
                    detail: "Utilize id, titulo, situacao ou versao.",
                    statusCode: 400);

            if (sentido != "asc" && sentido != "desc")
                return Results.Problem(
                    title: "Direção inválida",
                    detail: "Utilize asc ou desc.",
                    statusCode: 400);

            IQueryable<Tarefa> consulta = db.Tarefas.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(situacao))
                consulta = consulta.Where(t => t.Situacao == situacao);

            if (!string.IsNullOrWhiteSpace(termo))
                consulta = consulta.Where(t => t.Titulo.Contains(termo));

            var total = await consulta.CountAsync();

            IOrderedQueryable<Tarefa> ordenada = (campo, sentido) switch
            {
                ("id", "asc") => consulta.OrderBy(t => t.Id),
                ("id", "desc") => consulta.OrderByDescending(t => t.Id),
                ("titulo", "asc") => consulta.OrderBy(t => t.Titulo),
                ("titulo", "desc") => consulta.OrderByDescending(t => t.Titulo),
                ("situacao", "asc") => consulta.OrderBy(t => t.Situacao),
                ("situacao", "desc") => consulta.OrderByDescending(t => t.Situacao),
                ("versao", "asc") => consulta.OrderBy(t => t.Versao),
                _ => consulta.OrderByDescending(t => t.Versao)
            };

            var itens = await ordenada
                .Skip((numeroPagina - 1) * tamanhoPagina)
                .Take(tamanhoPagina)
                .Select(t => new TarefaResumoResponse(
                    t.Id, t.Titulo, t.Situacao, t.Versao))
                .ToListAsync();

            var resultado = new PaginaResponse<TarefaResumoResponse>(
                itens,
                numeroPagina,
                tamanhoPagina,
                total,
                (int)Math.Ceiling(total / (double)tamanhoPagina));

            return Results.Ok(resultado);
        })
        .WithName("ListarTarefas")
        .WithSummary("Lista tarefas com paginação, filtros, ordenação e busca por título")
        .Produces<PaginaResponse<TarefaResumoResponse>>(200)
        .ProducesProblem(400)
        .RequireAuthorization("TarefasPrivadas");

        // GET: consultar detalhes
        grupo.MapGet("/{id:int}", async (
            int id,
            PortalTarefasDbContext db) =>
        {
            var tarefa = await db.Tarefas
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TarefaDetalheResponse(
                    t.Id, t.Titulo, t.Descricao, t.Situacao, t.Versao))
                .FirstOrDefaultAsync();

            return tarefa is null
                ? Results.Problem(
                    title: "Tarefa não encontrada",
                    detail: $"Não existe tarefa com o ID {id}.",
                    statusCode: 404)
                : Results.Ok(tarefa);
        })
        .WithName("BuscarTarefa")
        .WithSummary("Consulta os detalhes de uma tarefa")
        .Produces<TarefaDetalheResponse>(200)
        .ProducesProblem(404)
        .RequireAuthorization("TarefasPrivadas");

        // POST: cadastrar
        grupo.MapPost("/", async (
            CriarTarefaRequest request,
            PortalTarefasDbContext db) =>
        {
            using var atividade = PortalTelemetry.ActivitySource.StartActivity("CriarTarefa");
            var cronometro = Stopwatch.StartNew();

            atividade?.SetTag("tarefa.operacao", "criar");

            try
            {
                var erros = Validar(request);

                if (erros.Count > 0)
                    return Results.ValidationProblem(erros);

                if (!SituacaoValida(request.Situacao))
                    return Results.Problem(
                        title: "Situação inválida",
                        detail: "Utilize Pendente, Em andamento ou Concluída.",
                        statusCode: 400);

                var tarefa = new Tarefa
                {
                    Titulo = request.Titulo,
                    Descricao = request.Descricao,
                    Situacao = request.Situacao,
                    Versao = 1
                };

                db.Tarefas.Add(tarefa);
                await db.SaveChangesAsync();

                PortalTelemetry.TarefasCriadas.Add(1);
                atividade?.SetStatus(ActivityStatusCode.Ok);

                var resposta = new TarefaDetalheResponse(
                    tarefa.Id, tarefa.Titulo, tarefa.Descricao,
                    tarefa.Situacao, tarefa.Versao);

                return Results.Created($"/api/tarefas/{tarefa.Id}", resposta);
            }
            catch (Exception)
            {
                atividade?.SetStatus(ActivityStatusCode.Error);
                throw;
            }
            finally
            {
                cronometro.Stop();
                PortalTelemetry.DuracaoCadastro.Record(cronometro.Elapsed.TotalMilliseconds);
            }
        })
        .WithName("CriarTarefa")
        .WithSummary("Cadastra uma nova tarefa")
        .Produces<TarefaDetalheResponse>(201)
        .ProducesValidationProblem(400)
        .RequireAuthorization("CriarTarefa");

        // PUT: atualizar
        grupo.MapPut("/{id:int}", async (
            int id,
            AtualizarTarefaRequest request,
            PortalTarefasDbContext db) =>
        {
            var erros = Validar(request);

            if (erros.Count > 0)
                return Results.ValidationProblem(erros);

            if (!SituacaoValida(request.Situacao))
                return Results.Problem(
                    title: "Situação inválida",
                    detail: "Utilize Pendente, Em andamento ou Concluída.",
                    statusCode: 400);

            var tarefa = await db.Tarefas.FindAsync(id);

            if (tarefa is null)
                return Results.Problem(
                    title: "Tarefa não encontrada",
                    detail: $"Não existe tarefa com o ID {id}.",
                    statusCode: 404);

            if (tarefa.Versao != request.Versao)
                return Results.Problem(
                    title: "Conflito de versão",
                    detail: "A tarefa foi alterada. Consulte os dados atualizados antes de tentar novamente.",
                    statusCode: 409);

            tarefa.Titulo = request.Titulo;
            tarefa.Descricao = request.Descricao;
            tarefa.Situacao = request.Situacao;
            tarefa.Versao++;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Problem(
                    title: "Conflito de atualização",
                    detail: "A tarefa foi modificada por outra operação.",
                    statusCode: 409);
            }

            return Results.Ok(new TarefaDetalheResponse(
                tarefa.Id, tarefa.Titulo, tarefa.Descricao,
                tarefa.Situacao, tarefa.Versao));
        })
        .WithName("AtualizarTarefa")
        .WithSummary("Atualiza uma tarefa usando controle de versão")
        .Produces<TarefaDetalheResponse>(200)
        .ProducesProblem(400)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .RequireAuthorization("GerenciarTarefas");

        // DELETE: excluir
        grupo.MapDelete("/{id:int}", async (
            int id,
            PortalTarefasDbContext db) =>
        {
            var tarefa = await db.Tarefas.FindAsync(id);

            if (tarefa is null)
                return Results.Problem(
                    title: "Tarefa não encontrada",
                    detail: $"Não existe tarefa com o ID {id}.",
                    statusCode: 404);

            db.Tarefas.Remove(tarefa);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Results.Problem(
                    title: "Conflito ao excluir",
                    detail: "A tarefa foi modificada por outra operação.",
                    statusCode: 409);
            }

            return Results.NoContent();
        })
        .WithName("ExcluirTarefa")
        .WithSummary("Exclui uma tarefa")
        .Produces(204)
        .ProducesProblem(404)
        .ProducesProblem(409)
        .RequireAuthorization("GerenciarTarefas");
    }

    private static Dictionary<string, string[]> Validar<T>(T objeto)
    {
        var resultados = new List<ValidationResult>();

        Validator.TryValidateObject(
            objeto!,
            new ValidationContext(objeto!),
            resultados,
            validateAllProperties: true);

        return resultados
            .SelectMany(r => r.MemberNames.Select(nome => new
            {
                Nome = nome,
                Erro = r.ErrorMessage ?? "Valor inválido."
            }))
            .GroupBy(r => r.Nome)
            .ToDictionary(
                grupo => grupo.Key,
                grupo => grupo.Select(r => r.Erro).ToArray());
    }

    private static bool SituacaoValida(string situacao)
    {
        return situacao is "Pendente" or "Em andamento" or "Concluída";
    }
}
