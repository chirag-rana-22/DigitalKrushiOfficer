using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConversationPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePathsJson",
                table: "TblQuery",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_TblQuery_FarmerId_ParentQueryId_CreatedDate",
                table: "TblQuery",
                columns: new[] { "FarmerId", "ParentQueryId", "CreatedDate" });
            migrationBuilder.DropIndex(
                name: "IX_TblQuery_FarmerId",
                table: "TblQuery");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_TblQuery_FarmerId",
                table: "TblQuery",
                column: "FarmerId");

            migrationBuilder.DropIndex(
                name: "IX_TblQuery_FarmerId_ParentQueryId_CreatedDate",
                table: "TblQuery");

            migrationBuilder.DropColumn(
                name: "ImagePathsJson",
                table: "TblQuery");


        }
    }
}
