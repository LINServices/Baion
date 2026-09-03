using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Baion.Orchestrator.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "agent_token_hash",
                table: "servers",
                type: "char(64)",
                unicode: false,
                fixedLength: true,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "machine_id",
                table: "servers",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "enrollment_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    token_hash = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    default_server_kind = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    max_uses = table.Column<int>(type: "int", nullable: true),
                    use_count = table.Column<int>(type: "int", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    tenant_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_enrollment_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_enrollment_tokens_tenants_tenant_id",
                        column: x => x.tenant_id,
                        principalTable: "tenants",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_servers_agent_token_hash",
                table: "servers",
                column: "agent_token_hash",
                unique: true,
                filter: "[agent_token_hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_servers_tenant_id_machine_id",
                table: "servers",
                columns: new[] { "tenant_id", "machine_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_tokens_tenant_id",
                table: "enrollment_tokens",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_enrollment_tokens_token_hash",
                table: "enrollment_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "enrollment_tokens");

            migrationBuilder.DropIndex(
                name: "ix_servers_agent_token_hash",
                table: "servers");

            migrationBuilder.DropIndex(
                name: "ix_servers_tenant_id_machine_id",
                table: "servers");

            migrationBuilder.DropColumn(
                name: "agent_token_hash",
                table: "servers");

            migrationBuilder.DropColumn(
                name: "machine_id",
                table: "servers");
        }
    }
}
