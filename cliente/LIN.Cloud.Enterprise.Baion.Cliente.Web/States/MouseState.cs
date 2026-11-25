namespace LIN.Cloud.Enterprise.Baion.Cliente.Web.States;

public class MouseState
{
    private bool _hovered;
    private bool _pressed;

    public string CssClass => _pressed ? "is-pressed" : _hovered ? "is-hover" : string.Empty;

    public void Enter()
    {
        _hovered = true;
    }

    public void Leave()
    {
        _hovered = false;
        _pressed = false;
    }

    public void Press()
    {
        _pressed = true;
    }

    public void Release()
    {
        _pressed = false;
    }
}
