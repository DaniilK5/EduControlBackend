using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleGradesImagesAndAbsence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StudentGroups",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "Absences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    StudentGroupId = table.Column<int>(type: "integer", nullable: false),
                    InstructorId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Hours = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    IsExcused = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Absences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Absences_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Absences_Users_InstructorId",
                        column: x => x.InstructorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Absences_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GradeImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "text", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    SubjectId = table.Column<int>(type: "integer", nullable: true),
                    StudentGroupId = table.Column<int>(type: "integer", nullable: true),
                    UploaderId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeImages_StudentGroups_StudentGroupId",
                        column: x => x.StudentGroupId,
                        principalTable: "StudentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeImages_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeImages_Users_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParentStudentRelations",
                columns: table => new
                {
                    ChildrenId = table.Column<int>(type: "integer", nullable: false),
                    ParentsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParentStudentRelations", x => new { x.ChildrenId, x.ParentsId });
                    table.ForeignKey(
                        name: "FK_ParentStudentRelations_Users_ChildrenId",
                        column: x => x.ChildrenId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ParentStudentRelations_Users_ParentsId",
                        column: x => x.ParentsId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Absences_InstructorId",
                table: "Absences",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Absences_StudentGroupId",
                table: "Absences",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Absences_StudentId",
                table: "Absences",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeImages_StudentGroupId",
                table: "GradeImages",
                column: "StudentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeImages_SubjectId",
                table: "GradeImages",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeImages_UploaderId",
                table: "GradeImages",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_ParentStudentRelations_ParentsId",
                table: "ParentStudentRelations",
                column: "ParentsId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Absences");

            migrationBuilder.DropTable(
                name: "GradeImages");

            migrationBuilder.DropTable(
                name: "ParentStudentRelations");

            migrationBuilder.DropIndex(
                name: "IX_StudentGroups_Name",
                table: "StudentGroups");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "StudentGroups",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);
        }
    }
}
