using Xunit;

/// <summary>
/// Marca una prueba que necesita un RabbitMQ de verdad. Si no hay broker a mano se omite en lugar de
/// fallar: el resto de la suite tiene que poder correr en una máquina sin infraestructura.
/// </summary>
public sealed class RequiresRabbitMqFactAttribute : FactAttribute
{
    public RequiresRabbitMqFactAttribute()
    {
        if (!RabbitMqProbe.IsReachable())
        {
            Skip = $"No hay RabbitMQ escuchando en {RabbitMqProbe.Description}";
        }
    }
}
