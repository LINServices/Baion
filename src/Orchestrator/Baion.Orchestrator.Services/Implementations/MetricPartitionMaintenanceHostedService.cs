using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Baion.Orchestrator.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baion.Orchestrator.Services.Implementations;

/// <summary>
/// Mantiene los límites mensuales de la partición de <c>metrics</c>. Sin esto la tabla sigue funcionando,
/// pero todo lo nuevo cae en la última partición y se pierde la eliminación de particiones al consultar.
/// Es idempotente y tolera que varias instancias lo ejecuten a la vez.
/// </summary>
internal class MetricPartitionMaintenanceHostedService(IServiceScopeFactory scopeFactory, IOptions<MetricPartitionOptions> options, TimeProvider timeProvider, ILogger<MetricPartitionMaintenanceHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(settings.CheckIntervalHours, 1)), timeProvider);

        do
        {
            try
            {
                await EnsureBoundariesAsync(settings, stoppingToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "No se pudieron mantener las particiones de métricas");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task EnsureBoundariesAsync(MetricPartitionOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<BaionDbContext>();

        var existentes = await ReadBoundariesAsync(context, cancellationToken);

        if (existentes is null)
        {
            logger.LogWarning("No existe la función de partición {Funcion}; se omite el mantenimiento", PartitionFunction);
            return;
        }

        var creados = 0;

        foreach (var limite in EnumerateBoundaries(settings, timeProvider.GetUtcNow()))
        {
            if (existentes.Contains(limite))
            {
                continue;
            }

            if (await TrySplitAsync(context, limite, cancellationToken))
            {
                creados++;
            }
        }

        if (creados > 0)
        {
            logger.LogInformation("Creadas {Particiones} particiones mensuales de métricas", creados);
        }
    }

    private async Task<HashSet<DateTimeOffset>?> ReadBoundariesAsync(BaionDbContext context, CancellationToken cancellationToken)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State is not System.Data.ConnectionState.Open)
        {
            await context.Database.OpenConnectionAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT CAST(prv.value AS datetimeoffset(7))
            FROM sys.partition_range_values prv
            JOIN sys.partition_functions pf ON prv.function_id = pf.function_id
            WHERE pf.name = '{PartitionFunction}'
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var boundaries = new HashSet<DateTimeOffset>();

        while (await reader.ReadAsync(cancellationToken))
        {
            boundaries.Add(reader.GetFieldValue<DateTimeOffset>(0));
        }

        return boundaries.Count == 0 ? null : boundaries;
    }

    private async Task<bool> TrySplitAsync(BaionDbContext context, DateTimeOffset boundary, CancellationToken cancellationToken)
    {
        try
        {
            // Las dos sentencias van juntas: NEXT USED solo vale para el SPLIT inmediatamente posterior.
            var sql = $"ALTER PARTITION SCHEME {PartitionScheme} NEXT USED [PRIMARY]; ALTER PARTITION FUNCTION {PartitionFunction}() SPLIT RANGE (@p0);";
            await context.Database.ExecuteSqlRawAsync(sql, [boundary], cancellationToken);

            return true;
        }
        catch (DbException exception)
        {
            // Otra instancia se adelantó a crear el mismo límite; no es un fallo.
            logger.LogDebug("No se creó la partición para {Limite}: {Motivo}", boundary, exception.Message);
            return false;
        }
    }

    /// <summary>Límites mensuales en UTC, desde los meses hacia atrás configurados hasta los de adelanto.</summary>
    private static IEnumerable<DateTimeOffset> EnumerateBoundaries(MetricPartitionOptions settings, DateTimeOffset now)
    {
        var inicio = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero).AddMonths(-Math.Max(settings.PastMonths, 0));
        var meses = Math.Max(settings.PastMonths, 0) + Math.Max(settings.FutureMonths, 1) + 1;

        for (var indice = 0; indice < meses; indice++)
        {
            yield return inicio.AddMonths(indice);
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private const string PartitionFunction = "pf_metrics_monthly";

    private const string PartitionScheme = "ps_metrics_monthly";
}
