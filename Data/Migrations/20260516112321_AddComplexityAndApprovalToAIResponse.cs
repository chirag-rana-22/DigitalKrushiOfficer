using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddComplexityAndApprovalToAIResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "TblAIResponse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByOfficerId",
                table: "TblAIResponse",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedDate",
                table: "TblAIResponse",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ComplexityLevel",
                table: "TblAIResponse",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OfficerNotes",
                table: "TblAIResponse",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$C7qCwaVizx0tXGa2512T.eF8hXjo2jQTbguQRpew5RkVWVX/WlsUq");

            migrationBuilder.CreateIndex(
                name: "IX_TblAIResponse_ApprovedByOfficerId",
                table: "TblAIResponse",
                column: "ApprovedByOfficerId");

            migrationBuilder.AddForeignKey(
                name: "FK_TblAIResponse_TblUser_ApprovedByOfficerId",
                table: "TblAIResponse",
                column: "ApprovedByOfficerId",
                principalTable: "TblUser",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TblAIResponse_TblUser_ApprovedByOfficerId",
                table: "TblAIResponse");

            migrationBuilder.DropIndex(
                name: "IX_TblAIResponse_ApprovedByOfficerId",
                table: "TblAIResponse");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "TblAIResponse");

            migrationBuilder.DropColumn(
                name: "ApprovedByOfficerId",
                table: "TblAIResponse");

            migrationBuilder.DropColumn(
                name: "ApprovedDate",
                table: "TblAIResponse");

            migrationBuilder.DropColumn(
                name: "ComplexityLevel",
                table: "TblAIResponse");

            migrationBuilder.DropColumn(
                name: "OfficerNotes",
                table: "TblAIResponse");

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Sd.G5NehodrH8DLf.JbGn.ryMfm37wjPhaZBIXqdjf0XYkk18Amk6");
        }
    }
}
