using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tenants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    identity_mode = table.Column<int>(type: "int", nullable: false),
                    external_tenant_id = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "script_chains",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_script_chains", x => x.id);
                    table.ForeignKey(
                        name: "fk_script_chains_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "scripts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    checksum = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "int", nullable: false),
                    runtime = table.Column<int>(type: "int", nullable: false),
                    default_timeout_seconds = table.Column<int>(type: "int", nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scripts", x => x.id);
                    table.ForeignKey(
                        name: "fk_scripts_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "server_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_server_groups", x => x.id);
                    table.ForeignKey(
                        name: "fk_server_groups_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "servers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    hostname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    kind = table.Column<int>(type: "int", nullable: false),
                    platform = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    agent_version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    runtime_identifier = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    orchestrator_instance_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    connected_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    last_seen_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    max_concurrent_executions = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_servers", x => x.id);
                    table.ForeignKey(
                        name: "fk_servers_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "script_chain_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_chain_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    step_order = table.Column<int>(type: "int", nullable: false),
                    failure_policy = table.Column<int>(type: "int", nullable: false),
                    timeout_seconds_override = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_script_chain_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_script_chain_steps_script_chains_script_chain_id",
                        column: x => x.script_chain_id,
                        principalTable: "script_chains",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_script_chain_steps_scripts_script_id",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_script_chain_steps_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    captured_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    cpu_usage_percent = table.Column<double>(type: "float", nullable: false),
                    cpu_core_count = table.Column<int>(type: "int", nullable: false),
                    load_average1m = table.Column<double>(type: "float", nullable: true),
                    memory_total_bytes = table.Column<long>(type: "bigint", nullable: false),
                    memory_available_bytes = table.Column<long>(type: "bigint", nullable: false),
                    disks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metrics", x => x.id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "fk_metrics_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    cron_expression = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    time_zone_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    script_chain_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    server_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    mode = table.Column<int>(type: "int", nullable: false),
                    is_enabled = table.Column<bool>(type: "bit", nullable: false),
                    last_run_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    next_run_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scheduled_tasks", x => x.id);
                    table.CheckConstraint("ck_scheduled_tasks_payload", "(CASE WHEN [script_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [script_chain_id] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.CheckConstraint("ck_scheduled_tasks_target", "(CASE WHEN [server_id] IS NULL THEN 0 ELSE 1 END + CASE WHEN [server_group_id] IS NULL THEN 0 ELSE 1 END) = 1");
                    table.ForeignKey(
                        name: "fk_scheduled_tasks_script_chains_script_chain_id",
                        column: x => x.script_chain_id,
                        principalTable: "script_chains",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_scheduled_tasks_scripts_script_id",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_scheduled_tasks_server_groups_server_group_id",
                        column: x => x.server_group_id,
                        principalTable: "server_groups",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_scheduled_tasks_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_scheduled_tasks_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "server_group_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    server_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_server_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_server_group_members_server_groups_server_group_id",
                        column: x => x.server_group_id,
                        principalTable: "server_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_server_group_members_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_server_group_members_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "script_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    server_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_chain_step_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    chain_run_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    mode = table.Column<int>(type: "int", nullable: false),
                    exit_code = table.Column<int>(type: "int", nullable: true),
                    std_out = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    std_err = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    error_message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    queued_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_script_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_script_executions_script_chain_steps_script_chain_step_id",
                        column: x => x.script_chain_step_id,
                        principalTable: "script_chain_steps",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_script_executions_scripts_script_id",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_script_executions_servers_server_id",
                        column: x => x.server_id,
                        principalTable: "servers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_script_executions_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_metrics_server_id_captured_at",
                table: "metrics",
                columns: new[] { "server_id", "captured_at" })
                .Annotation("SqlServer:Clustered", true);

            migrationBuilder.CreateIndex(
                name: "ix_metrics_tenant_id_captured_at",
                table: "metrics",
                columns: new[] { "tenant_id", "captured_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_is_enabled_next_run_at",
                table: "scheduled_tasks",
                columns: new[] { "is_enabled", "next_run_at" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_script_chain_id",
                table: "scheduled_tasks",
                column: "script_chain_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_script_id",
                table: "scheduled_tasks",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_server_group_id",
                table: "scheduled_tasks",
                column: "server_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_server_id",
                table: "scheduled_tasks",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_tenant_id_name",
                table: "scheduled_tasks",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_script_chain_steps_script_chain_id_step_order",
                table: "script_chain_steps",
                columns: new[] { "script_chain_id", "step_order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_script_chain_steps_script_id",
                table: "script_chain_steps",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_chain_steps_tenant_id",
                table: "script_chain_steps",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_chains_tenant_id_name",
                table: "script_chains",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_chain_run_id",
                table: "script_executions",
                column: "chain_run_id",
                filter: "[chain_run_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_script_chain_step_id",
                table: "script_executions",
                column: "script_chain_step_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_script_id",
                table: "script_executions",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_server_id",
                table: "script_executions",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_tenant_id_server_id_queued_at",
                table: "script_executions",
                columns: new[] { "tenant_id", "server_id", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_tenant_id_status",
                table: "script_executions",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_scripts_tenant_id_name",
                table: "scripts",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_server_group_members_server_group_id_server_id",
                table: "server_group_members",
                columns: new[] { "server_group_id", "server_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_server_group_members_server_id",
                table: "server_group_members",
                column: "server_id");

            migrationBuilder.CreateIndex(
                name: "ix_server_group_members_tenant_id",
                table: "server_group_members",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_server_groups_tenant_id_name",
                table: "server_groups",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servers_orchestrator_instance_id",
                table: "servers",
                column: "orchestrator_instance_id",
                filter: "[orchestrator_instance_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_servers_tenant_id_name",
                table: "servers",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_servers_tenant_id_status",
                table: "servers",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_tenants_external_tenant_id",
                table: "tenants",
                column: "external_tenant_id",
                filter: "[external_tenant_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenants_slug",
                table: "tenants",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "scheduled_tasks");

            migrationBuilder.DropTable(
                name: "script_executions");

            migrationBuilder.DropTable(
                name: "server_group_members");

            migrationBuilder.DropTable(
                name: "script_chain_steps");

            migrationBuilder.DropTable(
                name: "server_groups");

            migrationBuilder.DropTable(
                name: "servers");

            migrationBuilder.DropTable(
                name: "script_chains");

            migrationBuilder.DropTable(
                name: "scripts");

            migrationBuilder.DropTable(
                name: "tenants");
        }
    }
}
