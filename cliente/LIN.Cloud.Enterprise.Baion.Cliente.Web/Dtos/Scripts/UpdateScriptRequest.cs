namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

// La versión y el checksum solo cambian si cambia el contenido.
public record UpdateScriptRequest(string Name, string? Description, string Content, string Runtime, int DefaultTimeoutSeconds);
