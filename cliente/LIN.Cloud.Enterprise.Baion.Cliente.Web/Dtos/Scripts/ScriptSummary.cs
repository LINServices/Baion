using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

public record ScriptSummary(Guid Id, string Name, string Runtime, int Version, string Checksum, int DefaultTimeoutSeconds);
