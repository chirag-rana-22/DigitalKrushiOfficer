using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digital_Krushi_Officer.Data.Migrations
{
    /// <inheritdoc />
    public partial class PreserveAuditColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Older XAMPP imports omit these columns even though migration history includes them.
            // MySQL lacks MariaDB's ADD COLUMN IF NOT EXISTS syntax. Check metadata on both engines.
            foreach (var table in new[] { "TblCrop", "TblDiseaseDataset" })
                migrationBuilder.Sql($"SET @dko_ddl = IF(EXISTS(SELECT 1 FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = '{table}' AND column_name = 'CreatedBy'), 'SELECT 1', 'ALTER TABLE `{table}` ADD COLUMN `CreatedBy` int NULL'); PREPARE dko_stmt FROM @dko_ddl; EXECUTE dko_stmt; DEALLOCATE PREPARE dko_stmt;");
            migrationBuilder.Sql("UPDATE TblQuery q JOIN TblAIResponse a ON a.QueryId = q.Id LEFT JOIN TblEscalation e ON e.QueryId = q.Id SET q.Status = CASE WHEN e.Id IS NULL THEN 'Unassigned' ELSE 'PendingApproval' END WHERE a.ApprovalStatus IN (0,2) AND q.Status = 'Answered';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
