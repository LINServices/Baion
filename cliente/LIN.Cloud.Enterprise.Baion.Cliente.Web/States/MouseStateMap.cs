using System.Collections.Generic;

namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.States;

public class MouseStateMap
{
    private readonly Dictionary<object, MouseState> _states = [];

    public MouseState this[object key] => _states.TryGetValue(key, out var state) ? state : _states[key] = new MouseState();
}
