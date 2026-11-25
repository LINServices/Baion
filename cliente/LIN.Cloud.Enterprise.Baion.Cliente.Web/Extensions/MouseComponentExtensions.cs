using System.Collections.Generic;
using LIN.Cloud.Enterprise.Baion.Cliente.Web.States;
using Microsoft.AspNetCore.Components;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.Extensions;

public static class MouseComponentExtensions
{
    public static Dictionary<string, object> MouseEvents(this ComponentBase receiver, MouseState state)
    {
        return new()
        {
            ["onmouseenter"] = EventCallback.Factory.Create(receiver, state.Enter),
            ["onmouseleave"] = EventCallback.Factory.Create(receiver, state.Leave),
            ["onmousedown"] = EventCallback.Factory.Create(receiver, state.Press),
            ["onmouseup"] = EventCallback.Factory.Create(receiver, state.Release)
        };
    }
}
