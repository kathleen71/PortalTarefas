namespace PortalTarefas.Web.DTOs;

public record TarefaDetalheResponse(
    int Id,
    string Titulo,
    string Descricao,
    string Situacao,
    int Versao
);
