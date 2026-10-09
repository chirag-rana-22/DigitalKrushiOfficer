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
            migrationBuilder.Sql("ALTER TABLE TblCrop ADD COLUMN IF NOT EXISTS CreatedBy int NULL;");
            migrationBuilder.Sql("ALTER TABLE TblDiseaseDataset ADD COLUMN IF NOT EXISTS CreatedBy int NULL;");
            migrationBuilder.Sql("UPDATE TblQuery q JOIN TblAIResponse a ON a.QueryId = q.Id LEFT JOIN TblEscalation e ON e.QueryId = q.Id SET q.Status = CASE WHEN e.Id IS NULL THEN 'Unassigned' ELSE 'PendingApproval' END WHERE a.ApprovalStatus IN (0,2) AND q.Status = 'Answered';");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
