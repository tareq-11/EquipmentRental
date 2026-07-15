using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "users",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");
            migrationBuilder.Sql("UPDATE users SET \"SecurityStamp\" = md5(random()::text || clock_timestamp()::text || \"Id\"::text) WHERE \"SecurityStamp\" = '';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "users");
        }
    }
}
