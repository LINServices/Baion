using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddServerGroupBatches : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "batch_id",
                table: "script_executions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "execution_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    server_group_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    server_group_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    script_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    script_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    mode = table.Column<int>(type: "int", nullable: false),
                    requested_by = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: true),
                    total_members = table.Column<int>(type: "int", nullable: false),
                    dispatched_count = table.Column<int>(type: "int", nullable: false),
                    skipped_incompatible_count = table.Column<int>(type: "int", nullable: false),
                    skipped_disabled_count = table.Column<int>(type: "int", nullable: false),
                    failed_count = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_execution_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_execution_batches_scripts_script_id",
                        column: x => x.script_id,
                        principalTable: "scripts",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_execution_batches_server_groups_server_group_id",
                        column: x => x.server_group_id,
                        principalTable: "server_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_execution_batches_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_batch_id",
                table: "script_executions",
                column: "batch_id",
                filter: "[batch_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_execution_batches_script_id",
                table: "execution_batches",
                column: "script_id");

            migrationBuilder.CreateIndex(
                name: "ix_execution_batches_server_group_id",
                table: "execution_batches",
                column: "server_group_id");

            migrationBuilder.CreateIndex(
                name: "ix_execution_batches_tenant_id_created_at",
                table: "execution_batches",
                columns: new[] { "tenant_id", "created_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_script_executions_execution_batches_batch_id",
                table: "script_executions",
                column: "batch_id",
                principalTable: "execution_batches",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_script_executions_execution_batches_batch_id",
                table: "script_executions");

            migrationBuilder.DropTable(
                name: "execution_batches");

            migrationBuilder.DropIndex(
                name: "ix_script_executions_batch_id",
                table: "script_executions");

            migrationBuilder.DropColumn(
                name: "batch_id",
                table: "script_executions");
        }
    }
}
