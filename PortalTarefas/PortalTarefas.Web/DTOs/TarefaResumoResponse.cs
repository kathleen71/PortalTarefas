namespace PortalTarefas.Web.DTOs;

public record TarefaResumoResponse(
    int Id,
    string Titulo,
    string Situacao,
    int Versao
);
