using System.ComponentModel.DataAnnotations;

namespace PortalTarefas.Web.ViewModels;

[ValidarPrazoPrioridade]
public class TarefaInputModel
{
    [Required(ErrorMessage = "Informe o título da tarefa.")]
    [StringLength(80, MinimumLength = 3,
        ErrorMessage = "O título deve ter entre 3 e 80 caracteres.")]
    public string Titulo { get; set; } = "";

    [StringLength(300,
        ErrorMessage = "A descrição deve ter no máximo 300 caracteres.")]
    public string Descricao { get; set; } = "";

    [Range(1, 3, ErrorMessage = "Selecione uma prioridade válida.")]
    public int Prioridade { get; set; } = 2;

    [Required(ErrorMessage = "Informe o prazo da tarefa.")]
    [DataType(DataType.Date)]
    public DateTime? Prazo { get; set; }

    public string Situacao { get; set; } = "Pendente";
}

public class ValidarPrazoPrioridadeAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value, ValidationContext validationContext)
    {
        if (value is not TarefaInputModel model || model.Prazo is null)
            return ValidationResult.Success;

        if (model.Prazo.Value.Date < DateTime.Today)
            return new ValidationResult(
                "O prazo não pode ser anterior à data de hoje.");

        if (model.Prioridade == 3 &&
            model.Prazo.Value.Date == DateTime.Today)
            return new ValidationResult(
                "Tarefas de alta prioridade precisam de prazo a partir de amanhã.");

        return ValidationResult.Success;
    }
}
