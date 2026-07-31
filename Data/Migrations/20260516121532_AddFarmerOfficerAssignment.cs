using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmerOfficerAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblOfficerFarmerAssignment");

            migrationBuilder.CreateTable(
                name: "TblFarmerOfficerAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FarmerId = table.Column<int>(type: "int", nullable: false),
                    OfficerId = table.Column<int>(type: "int", nullable: false),
                    District = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Village = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblFarmerOfficerAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblFarmerOfficerAssignment_TblUser_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "TblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblFarmerOfficerAssignment_TblUser_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "TblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$MbFCHmWoFsSWU92FrV.nkuhPdvMqoAM6yTzKLibN93sQSybRjRNrG");

            migrationBuilder.CreateIndex(
                name: "IX_TblFarmerOfficerAssignment_FarmerId_OfficerId",
                table: "TblFarmerOfficerAssignment",
                columns: new[] { "FarmerId", "OfficerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TblFarmerOfficerAssignment_OfficerId",
                table: "TblFarmerOfficerAssignment",
                column: "OfficerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TblFarmerOfficerAssignment");

            migrationBuilder.CreateTable(
                name: "TblOfficerFarmerAssignment",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FarmerId = table.Column<int>(type: "int", nullable: false),
                    OfficerId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    District = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnassignedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Village = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TblOfficerFarmerAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TblOfficerFarmerAssignment_TblUser_FarmerId",
                        column: x => x.FarmerId,
                        principalTable: "TblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TblOfficerFarmerAssignment_TblUser_OfficerId",
                        column: x => x.OfficerId,
                        principalTable: "TblUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$8CskkODF8jbLXUJ2voEqoesa79153BIIP3vnUXPp2bkKi9qUCBVuy");

            migrationBuilder.CreateIndex(
                name: "IX_TblOfficerFarmerAssignment_FarmerId",
                table: "TblOfficerFarmerAssignment",
                column: "FarmerId");

            migrationBuilder.CreateIndex(
                name: "IX_TblOfficerFarmerAssignment_OfficerId",
                table: "TblOfficerFarmerAssignment",
                column: "OfficerId");
        }
    }
}
