using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

public record ScriptRuntimeOption(string Value, string Label, IReadOnlyList<string> Platforms);
