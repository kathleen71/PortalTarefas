using System.ComponentModel.DataAnnotations;

namespace PortalTarefas.Web.DTOs;

public class CriarTarefaRequest
{
    [Required]
    [StringLength(80)]
    public string Titulo { get; set; } = "";

    [StringLength(300)]
    public string Descricao { get; set; } = "";

    [Required]
    public string Situacao { get; set; } = "Pendente";
}
