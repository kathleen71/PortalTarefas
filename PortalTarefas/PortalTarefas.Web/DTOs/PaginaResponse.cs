namespace PortalTarefas.Web.DTOs;

public record PaginaResponse<T>(
    IReadOnlyList<T> Itens,
    int Pagina,
    int Tamanho,
    int Total,
    int TotalPaginas
);
