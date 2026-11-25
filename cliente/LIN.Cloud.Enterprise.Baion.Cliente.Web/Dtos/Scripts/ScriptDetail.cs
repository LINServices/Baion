using System;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Dtos.Scripts;

public record ScriptDetail(Guid Id, string Name, string? Description, string Runtime, int Version, string Checksum, int DefaultTimeoutSeconds, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, string Content);
