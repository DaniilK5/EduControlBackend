using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class addAdminSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SiteName = table.Column<string>(type: "text", nullable: false),
                    DefaultTimeZone = table.Column<string>(type: "text", nullable: false),
                    MaxFileSize = table.Column<int>(type: "integer", nullable: false),
                    AllowedFileTypes = table.Column<string[]>(type: "text[]", nullable: false),
                    MaxUploadFilesPerMessage = table.Column<int>(type: "integer", nullable: false),
                    DefaultPageSize = table.Column<int>(type: "integer", nullable: false),
                    RequireEmailVerification = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordMinLength = table.Column<int>(type: "integer", nullable: false),
                    RequireStrongPassword = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings");
        }
    }
}
