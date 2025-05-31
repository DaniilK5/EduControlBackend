using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialStatus",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentGroupId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentId",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StudentGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CuratorId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentGroups_Users_CuratorId",
                        column: x => x.CuratorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_StudentGroupId",
                table: "Users",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_CuratorId",
                table: "StudentGroups",
                column: "CuratorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_StudentGroups_StudentGroupId",
                table: "Users",
                column: "StudentGroupId",
                principalTable: "StudentGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_StudentGroups_StudentGroupId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "StudentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Users_StudentGroupId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentGroupId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Users");
        }
    }
}
