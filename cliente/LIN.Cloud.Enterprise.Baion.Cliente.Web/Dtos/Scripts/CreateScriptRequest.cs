namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

public record CreateScriptRequest(string Name, string? Description, string Content, string Runtime, int DefaultTimeoutSeconds);
