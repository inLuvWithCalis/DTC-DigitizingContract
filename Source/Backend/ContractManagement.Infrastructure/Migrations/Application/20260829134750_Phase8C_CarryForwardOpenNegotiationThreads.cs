using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase8C_CarryForwardOpenNegotiationThreads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EventType",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.AddColumn<int>(
                name: "CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EventType",
                table: "tbl_ContractNegotiationCommentEvent",
                sql: "[EventType] IN (1, 2, 3, 4)");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationComment_CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment",
                column: "CarriedForwardFromCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationComment_CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment",
                column: "CarriedForwardFromVersionId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment",
                sql: "[CarriedForwardFromCommentId] IS NULL OR [CarriedForwardFromCommentId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment",
                sql: "[CarriedForwardFromVersionId] IS NULL OR [CarriedForwardFromVersionId] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarryForwardPair",
                table: "tbl_ContractNegotiationComment",
                sql: "([CarriedForwardFromCommentId] IS NULL AND [CarriedForwardFromVersionId] IS NULL) OR ([CarriedForwardFromCommentId] > 0 AND [CarriedForwardFromVersionId] > 0)");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationComment_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationComment]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentId] = d.[CommentId]
                        WHERE i.[CommentId] IS NULL
                           OR i.[ContractId] <> d.[ContractId]
                           OR i.[VersionId] <> d.[VersionId]
                           OR ISNULL(i.[TermId], 0) <> ISNULL(d.[TermId], 0)
                           OR ISNULL(i.[ParentCommentId], 0) <> ISNULL(d.[ParentCommentId], 0)
                           OR ISNULL(i.[CarriedForwardFromCommentId], 0)
                              <> ISNULL(d.[CarriedForwardFromCommentId], 0)
                           OR ISNULL(i.[CarriedForwardFromVersionId], 0)
                              <> ISNULL(d.[CarriedForwardFromVersionId], 0)
                           OR i.[Content] <> d.[Content]
                           OR i.[Source] <> d.[Source]
                           OR ISNULL(i.[RecordedByEmployeeId], 0) <> ISNULL(d.[RecordedByEmployeeId], 0)
                           OR ISNULL(i.[CustomerAccessSessionId], 0) <> ISNULL(d.[CustomerAccessSessionId], 0)
                           OR i.[CreatedDate] <> d.[CreatedDate]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51005, N'Negotiation comments are append-only.', 1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EventType",
                table: "tbl_ContractNegotiationCommentEvent");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractNegotiationComment_CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropIndex(
                name: "IX_tbl_ContractNegotiationComment_CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropCheckConstraint(
                name: "CK_tbl_ContractNegotiationComment_CarryForwardPair",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER TRIGGER [TR_tbl_ContractNegotiationComment_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationComment]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i ON i.[CommentId] = d.[CommentId]
                        WHERE i.[CommentId] IS NULL
                           OR i.[ContractId] <> d.[ContractId]
                           OR i.[VersionId] <> d.[VersionId]
                           OR ISNULL(i.[TermId], 0) <> ISNULL(d.[TermId], 0)
                           OR ISNULL(i.[ParentCommentId], 0) <> ISNULL(d.[ParentCommentId], 0)
                           OR i.[Content] <> d.[Content]
                           OR i.[Source] <> d.[Source]
                           OR ISNULL(i.[RecordedByEmployeeId], 0) <> ISNULL(d.[RecordedByEmployeeId], 0)
                           OR ISNULL(i.[CustomerAccessSessionId], 0) <> ISNULL(d.[CustomerAccessSessionId], 0)
                           OR i.[CreatedDate] <> d.[CreatedDate]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51005, N'Negotiation comments are append-only.', 1;
                    END;
                END;
                """);

            migrationBuilder.DropColumn(
                name: "CarriedForwardFromCommentId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.DropColumn(
                name: "CarriedForwardFromVersionId",
                table: "tbl_ContractNegotiationComment");

            migrationBuilder.AddCheckConstraint(
                name: "CK_tbl_ContractNegotiationCommentEvent_EventType",
                table: "tbl_ContractNegotiationCommentEvent",
                sql: "[EventType] IN (1, 2, 3)");
        }
    }
}
