namespace PortalTarefas.Web.Models;

public class Tarefa
{
    public int Id { get; set; }

    public string Titulo { get; set; } = "";

    public string Descricao { get; set; } = "";

    public string Situacao { get; set; } = "Pendente";

    public int Versao { get; set; } = 1;
}