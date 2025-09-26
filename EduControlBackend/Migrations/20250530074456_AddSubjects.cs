using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddSubjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Создаем таблицу предметов
            migrationBuilder.CreateTable(
                name: "Subjects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            // 2. Создаем уникальный индекс для кода предмета
            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                table: "Subjects",
                column: "Code",
                unique: true);

            // 3. Добавляем дефолтный предмет для существующих курсов
            migrationBuilder.Sql(@"
                INSERT INTO ""Subjects"" (""Name"", ""Code"", ""Description"")
                VALUES ('Общий курс', 'GENERAL', 'Автоматически созданный предмет для существующих курсов');
            ");

            // 4. Добавляем столбец SubjectId в таблицу Courses, который может быть NULL
            migrationBuilder.AddColumn<int>(
                name: "SubjectId",
                table: "Courses",
                type: "integer",
                nullable: true);

            // 5. Связываем существующие курсы с дефолтным предметом
            migrationBuilder.Sql(@"
                UPDATE ""Courses""
                SET ""SubjectId"" = (SELECT ""Id"" FROM ""Subjects"" WHERE ""Code"" = 'GENERAL');
            ");

            // 6. Создаем индекс для SubjectId
            migrationBuilder.CreateIndex(
                name: "IX_Courses_SubjectId",
                table: "Courses",
                column: "SubjectId");

            // 7. Добавляем внешний ключ
            migrationBuilder.AddForeignKey(
                name: "FK_Courses_Subjects_SubjectId",
                table: "Courses",
                column: "SubjectId",
                principalTable: "Subjects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // 8. Делаем SubjectId NOT NULL после связывания всех курсов
            migrationBuilder.AlterColumn<int>(
                name: "SubjectId",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Удаляем внешний ключ
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_Subjects_SubjectId",
                table: "Courses");

            // 2. Удаляем индекс
            migrationBuilder.DropIndex(
                name: "IX_Courses_SubjectId",
                table: "Courses");

            // 3. Удаляем таблицу предметов
            migrationBuilder.DropTable(
                name: "Subjects");

            // 4. Удаляем столбец SubjectId
            migrationBuilder.DropColumn(
                name: "SubjectId",
                table: "Courses");
        }
    }
}