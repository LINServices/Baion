using LIN.Cloud.Enterprise.Baion.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Presentacion.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Indica que la instancia del orquestador está viva y con qué versión de protocolo opera.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { Estado = "Ok", ProtocoloVersion = BaionProtocol.Version });
    }
}
