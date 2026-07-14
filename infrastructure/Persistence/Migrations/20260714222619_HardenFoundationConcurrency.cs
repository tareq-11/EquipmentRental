using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenFoundationConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_idempotent_requests_Key_RequestName",
                table: "idempotent_requests");

            migrationBuilder.AddColumn<string>(
                name: "ActorScope",
                table: "idempotent_requests",
                type: "character varying(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestFingerprint",
                table: "idempotent_requests",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActorType",
                table: "audit_logs",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_idempotent_requests_Key_RequestName_ActorScope",
                table: "idempotent_requests",
                columns: new[] { "Key", "RequestName", "ActorScope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_idempotent_requests_Key_RequestName_ActorScope",
                table: "idempotent_requests");

            migrationBuilder.DropColumn(
                name: "ActorScope",
                table: "idempotent_requests");

            migrationBuilder.DropColumn(
                name: "RequestFingerprint",
                table: "idempotent_requests");

            migrationBuilder.DropColumn(
                name: "ActorType",
                table: "audit_logs");

            migrationBuilder.CreateIndex(
                name: "IX_idempotent_requests_Key_RequestName",
                table: "idempotent_requests",
                columns: new[] { "Key", "RequestName" },
                unique: true);
        }
    }
}
