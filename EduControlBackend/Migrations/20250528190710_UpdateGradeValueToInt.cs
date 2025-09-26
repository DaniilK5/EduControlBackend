using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EduControlBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGradeValueToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Сначала создаем временный столбец
            migrationBuilder.AddColumn<int>(
                name: "ValueTemp",
                table: "Grades",
                type: "integer",
                nullable: true);

            // Конвертируем данные с проверкой на корректность
            migrationBuilder.Sql(@"
                UPDATE ""Grades"" 
                SET ""ValueTemp"" = 
                    CASE 
                        WHEN ""Value"" ~ '^[0-9]+$' THEN
                            LEAST(GREATEST(CAST(""Value"" AS integer), 0), 100)
                        ELSE 0
                    END;");

            // Удаляем старый столбец
            migrationBuilder.DropColumn(
                name: "Value",
                table: "Grades");

            // Добавляем новый столбец
            migrationBuilder.AddColumn<int>(
                name: "Value",
                table: "Grades",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Копируем данные
            migrationBuilder.Sql(@"
                UPDATE ""Grades"" 
                SET ""Value"" = ""ValueTemp"";");

            // Удаляем временный столбец
            migrationBuilder.DropColumn(
                name: "ValueTemp",
                table: "Grades");

            // Добавляем ограничение на диапазон значений
            migrationBuilder.Sql(@"
                ALTER TABLE ""Grades"" 
                ADD CONSTRAINT ""CK_Grades_Value_Range"" 
                CHECK (""Value"" >= 0 AND ""Value"" <= 100);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Удаляем ограничение на диапазон
            migrationBuilder.Sql(@"
                ALTER TABLE ""Grades"" 
                DROP CONSTRAINT IF EXISTS ""CK_Grades_Value_Range"";");

            // Создаем временный столбец
            migrationBuilder.AddColumn<string>(
                name: "ValueTemp",
                table: "Grades",
                type: "text",
                nullable: true);

            // Конвертируем данные обратно в строки
            migrationBuilder.Sql(@"
                UPDATE ""Grades"" 
                SET ""ValueTemp"" = ""Value""::text;");

            // Удаляем старый столбец
            migrationBuilder.DropColumn(
                name: "Value",
                table: "Grades");

            // Добавляем новый текстовый столбец
            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "Grades",
                type: "text",
                nullable: false,
                defaultValue: "0");

            // Копируем данные
            migrationBuilder.Sql(@"
                UPDATE ""Grades"" 
                SET ""Value"" = ""ValueTemp"";");

            // Удаляем временный столбец
            migrationBuilder.DropColumn(
                name: "ValueTemp",
                table: "Grades");
        }
    }
}
