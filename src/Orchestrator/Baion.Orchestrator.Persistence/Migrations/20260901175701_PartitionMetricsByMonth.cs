using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baion.Orchestrator.Persistence.Migrations
{
    /// <summary>
    /// Convierte <c>metrics</c> en una tabla particionada por mes sobre <c>captured_at</c>.
    ///
    /// El índice agrupado pasa a vivir en el esquema de partición y lidera por <c>server_id</c>, de modo
    /// que una consulta "este servidor en este rango" elimina particiones por fecha y luego busca por
    /// servidor dentro de la que queda. La clave primaria se deja no agrupada sobre <c>id</c> y fuera del
    /// esquema: no está alineada, lo que descarta SWITCH, pero conserva un modelo de una sola columna en
    /// EF y deja disponible <c>TRUNCATE TABLE ... WITH (PARTITIONS ...)</c> para la retención.
    ///
    /// La función arranca con un único límite fijo en 2020-01-01 para que la migración sea determinista;
    /// los límites mensuales siguientes los crea MetricPartitionMaintenanceHostedService al arrancar.
    /// </summary>
    public partial class PartitionMetricsByMonth : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'pf_metrics_monthly')
    CREATE PARTITION FUNCTION pf_metrics_monthly (datetimeoffset(7)) AS RANGE RIGHT FOR VALUES ('2020-01-01T00:00:00+00:00');
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'ps_metrics_monthly')
    CREATE PARTITION SCHEME ps_metrics_monthly AS PARTITION pf_metrics_monthly ALL TO ([PRIMARY]);
");

            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_metrics_tenant_id_captured_at ON metrics;
DROP INDEX IF EXISTS ix_metrics_server_id_captured_at ON metrics;
");

            // Único para evitar el uniquifier: id ya es identidad, así que la terna nunca se repite.
            migrationBuilder.Sql(@"
CREATE UNIQUE CLUSTERED INDEX ix_metrics_server_id_captured_at
    ON metrics (server_id, captured_at, id)
    ON ps_metrics_monthly (captured_at);
");

            migrationBuilder.Sql(@"
CREATE NONCLUSTERED INDEX ix_metrics_tenant_id_captured_at
    ON metrics (tenant_id, captured_at)
    ON ps_metrics_monthly (captured_at);
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX IF EXISTS ix_metrics_tenant_id_captured_at ON metrics;
DROP INDEX IF EXISTS ix_metrics_server_id_captured_at ON metrics;
");

            migrationBuilder.Sql(@"
CREATE CLUSTERED INDEX ix_metrics_server_id_captured_at ON metrics (server_id, captured_at);
CREATE NONCLUSTERED INDEX ix_metrics_tenant_id_captured_at ON metrics (tenant_id, captured_at);
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.partition_schemes WHERE name = 'ps_metrics_monthly')
    DROP PARTITION SCHEME ps_metrics_monthly;
IF EXISTS (SELECT 1 FROM sys.partition_functions WHERE name = 'pf_metrics_monthly')
    DROP PARTITION FUNCTION pf_metrics_monthly;
");
        }
    }
}
