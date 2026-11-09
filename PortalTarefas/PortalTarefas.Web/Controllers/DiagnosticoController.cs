using Microsoft.AspNetCore.Mvc;

namespace PortalTarefas.Web.Controllers;

[ApiController]
[Route("api/diagnostico")]
public class DiagnosticoController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Mensagem = "Endpoint MVC funcionando!",
            Tecnologia = "Controller"
        });
    }
}
