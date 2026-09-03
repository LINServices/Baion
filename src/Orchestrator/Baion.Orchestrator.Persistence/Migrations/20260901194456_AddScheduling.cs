using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "dispatch_deadline",
                table: "script_executions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scheduled_task_id",
                table: "script_executions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "offline_grace_seconds",
                table: "scheduled_tasks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_scheduled_task_id",
                table: "script_executions",
                column: "scheduled_task_id");

            migrationBuilder.CreateIndex(
                name: "ix_script_executions_status_dispatch_deadline",
                table: "script_executions",
                columns: new[] { "status", "dispatch_deadline" },
                filter: "[dispatch_deadline] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions",
                column: "scheduled_task_id",
                principalTable: "scheduled_tasks",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions");

            migrationBuilder.DropIndex(
                name: "ix_script_executions_scheduled_task_id",
                table: "script_executions");

            migrationBuilder.DropIndex(
                name: "ix_script_executions_status_dispatch_deadline",
                table: "script_executions");

            migrationBuilder.DropColumn(
                name: "dispatch_deadline",
                table: "script_executions");

            migrationBuilder.DropColumn(
                name: "scheduled_task_id",
                table: "script_executions");

            migrationBuilder.DropColumn(
                name: "offline_grace_seconds",
                table: "scheduled_tasks");
        }
    }
}
