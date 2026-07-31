using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class CleanNotificationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailVerificationToken",
                table: "TblUser");

            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "TblNotificationLog");

            migrationBuilder.DropColumn(
                name: "ExpiryDate",
                table: "TblNotificationLog");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TblNotificationLog");

            migrationBuilder.DropColumn(
                name: "OTPCode",
                table: "TblNotificationLog");

            migrationBuilder.DropColumn(
                name: "RetentionDays",
                table: "TblNotificationLog");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TblNotificationLog",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TblNotificationLog",
                keyColumn: "NotificationType",
                keyValue: null,
                column: "NotificationType",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "NotificationType",
                table: "TblNotificationLog",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$Sd.G5NehodrH8DLf.JbGn.ryMfm37wjPhaZBIXqdjf0XYkk18Amk6");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailVerificationToken",
                table: "TblUser",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "TblNotificationLog",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "NotificationType",
                table: "TblNotificationLog",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "TblNotificationLog",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDate",
                table: "TblNotificationLog",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TblNotificationLog",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OTPCode",
                table: "TblNotificationLog",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "RetentionDays",
                table: "TblNotificationLog",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "TblUser",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EmailVerificationToken", "PasswordHash" },
                values: new object[] { null, "$2a$11$q/59FyauHa31FV58fCOzleAc88DiMx.JrcdMLfCnN9BI1k6aPt5sa" });
        }
    }
}
