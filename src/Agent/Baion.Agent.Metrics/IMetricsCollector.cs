using System.Threading;
using System.Threading.Tasks;
using Baion.Contracts.Messages;

namespace Baion.Agent.Metrics;

/// <summary>Toma una muestra de CPU, RAM y disco de la máquina donde corre el agente.</summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Captura el estado actual. El uso de CPU se calcula contra la muestra anterior, así que el
    /// recolector es con estado y debe ser el mismo a lo largo de la vida del agente.
    /// </summary>
    Task<MetricsReportMessage> CollectAsync(CancellationToken cancellationToken);
}
