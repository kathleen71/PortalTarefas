using System.ComponentModel.DataAnnotations;

namespace PortalTarefas.Web.DTOs;

public class AtualizarTarefaRequest
{
    [Required]
    [StringLength(80)]
    public string Titulo { get; set; } = "";

    [StringLength(300)]
    public string Descricao { get; set; } = "";

    [Required]
    public string Situacao { get; set; } = "Pendente";

    [Range(1, int.MaxValue)]
    public int Versao { get; set; }
}
