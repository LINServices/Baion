using System;

namespace LIN.Cloud.Enterprise.Baion.Contracts.Messages;

public record ServiceQueryFailedMessage(Guid RequestId, string Code, string Message) : AgentToServerMessage, IAgentQueryReply
{
    public const string TypeDiscriminator = "service-query-failed";

    public const string NotFoundCode = "service.not_found";

    public const string ForbiddenCode = "service.forbidden";

    public const string ToolFailedCode = "service.tool_failed";
}
