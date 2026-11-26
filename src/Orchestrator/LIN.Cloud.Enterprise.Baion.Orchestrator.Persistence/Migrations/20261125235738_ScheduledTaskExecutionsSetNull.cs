using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LIN.Cloud.Enterprise.Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ScheduledTaskExecutionsSetNull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions");

            migrationBuilder.AddForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions",
                column: "scheduled_task_id",
                principalTable: "scheduled_tasks",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions");

            migrationBuilder.AddForeignKey(
                name: "fk_script_executions_scheduled_tasks_scheduled_task_id",
                table: "script_executions",
                column: "scheduled_task_id",
                principalTable: "scheduled_tasks",
                principalColumn: "id");
        }
    }
}
