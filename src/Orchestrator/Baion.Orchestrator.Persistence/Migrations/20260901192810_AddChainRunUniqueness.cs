using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddChainRunUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_script_executions_chain_run_id_script_chain_step_id",
                table: "script_executions",
                columns: new[] { "chain_run_id", "script_chain_step_id" },
                unique: true,
                filter: "[chain_run_id] IS NOT NULL AND [script_chain_step_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_script_executions_chain_run_id_script_chain_step_id",
                table: "script_executions");
        }
    }
}
