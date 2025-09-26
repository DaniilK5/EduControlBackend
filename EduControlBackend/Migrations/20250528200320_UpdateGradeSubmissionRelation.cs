using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGradeSubmissionRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала удалим все существующие оценки, так как могут быть дубликаты
            migrationBuilder.Sql("DELETE FROM \"Grades\";");

            // Удаляем старую связь
            migrationBuilder.DropColumn(
                name: "GradeId",
                table: "AssignmentSubmissions");

            // Добавляем новую связь
            migrationBuilder.AddColumn<int>(
                name: "SubmissionId",
                table: "Grades",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Добавляем внешний ключ и уникальный индекс
            migrationBuilder.CreateIndex(
                name: "IX_Grades_SubmissionId",
                table: "Grades",
                column: "SubmissionId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_AssignmentSubmissions_SubmissionId",
                table: "Grades",
                column: "SubmissionId",
                principalTable: "AssignmentSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Сначала удаляем внешний ключ
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_AssignmentSubmissions_SubmissionId",
                table: "Grades");

            // Удаляем уникальный индекс
            migrationBuilder.DropIndex(
                name: "IX_Grades_SubmissionId",
                table: "Grades");

            // Удаляем столбец SubmissionId
            migrationBuilder.DropColumn(
                name: "SubmissionId",
                table: "Grades");

            // Восстанавливаем старую структуру
            migrationBuilder.AddColumn<int>(
                name: "GradeId",
                table: "AssignmentSubmissions",
                type: "integer",
                nullable: true);
        }
    }
}
