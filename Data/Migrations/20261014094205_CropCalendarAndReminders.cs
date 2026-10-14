using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class CropCalendarAndReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CropContext",
                table: "TblQuery",
                type: "varchar(3000)",
                maxLength: 3000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FarmerCropId",
                table: "TblQuery",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailRemindersEnabled",
                table: "TblFarmerCrop",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FieldName",
                table: "TblFarmerCrop",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "HarvestedDate",
                table: "TblFarmerCrop",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PlanVersion",
                table: "TblFarmerCrop",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Variety",
                table: "TblFarmerCrop",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CropDigests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FarmerId = table.Column<int>(type: "int", nullable: false),
                    LocalDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Attempts = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropDigests", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CropTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FarmerCropId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Kind = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CompletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Version = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CropTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CropTasks_TblFarmerCrop_FarmerCropId",
                        column: x => x.FarmerCropId,
                        principalTable: "TblFarmerCrop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_CropDigests_FarmerId_LocalDate",
                table: "CropDigests",
                columns: new[] { "FarmerId", "LocalDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CropTasks_FarmerCropId",
                table: "CropTasks",
                column: "FarmerCropId");

            migrationBuilder.CreateIndex(
                name: "IX_CropTasks_Status_DueDate",
                table: "CropTasks",
                columns: new[] { "Status", "DueDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropDigests");

            migrationBuilder.DropTable(
                name: "CropTasks");

            migrationBuilder.DropColumn(
                name: "CropContext",
                table: "TblQuery");

            migrationBuilder.DropColumn(
                name: "FarmerCropId",
                table: "TblQuery");

            migrationBuilder.DropColumn(
                name: "EmailRemindersEnabled",
                table: "TblFarmerCrop");

            migrationBuilder.DropColumn(
                name: "FieldName",
                table: "TblFarmerCrop");

            migrationBuilder.DropColumn(
                name: "HarvestedDate",
                table: "TblFarmerCrop");

            migrationBuilder.DropColumn(
                name: "PlanVersion",
                table: "TblFarmerCrop");

            migrationBuilder.DropColumn(
                name: "Variety",
                table: "TblFarmerCrop");
        }
    }
}
