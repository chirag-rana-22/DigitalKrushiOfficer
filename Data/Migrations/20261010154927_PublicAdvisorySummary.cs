using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class PublicAdvisorySummary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicSummary",
                table: "TblAIResponse",
                type: "varchar(3000)",
                maxLength: 3000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicSummary",
                table: "TblAIResponse");
        }
    }
}
