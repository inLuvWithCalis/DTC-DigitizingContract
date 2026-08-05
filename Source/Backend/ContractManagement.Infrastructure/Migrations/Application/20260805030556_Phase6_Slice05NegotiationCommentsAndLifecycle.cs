using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractManagement.Infrastructure.Migrations.Application
{
    /// <inheritdoc />
    public partial class Phase6_Slice05NegotiationCommentsAndLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tbl_ContractNegotiationComment",
                columns: table => new
                {
                    CommentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractId = table.Column<int>(type: "int", nullable: false),
                    VersionId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: true),
                    ParentCommentId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", nullable: false),
                    Source = table.Column<string>(type: "varchar(32)", unicode: false, maxLength: 32, nullable: false, defaultValue: "ExternalFeedback"),
                    RecordedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractNegotiationComment_CreatedDate"),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractNegotiationComment", x => x.CommentId);
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_Content", "LEN(LTRIM(RTRIM([Content]))) BETWEEN 1 AND 4000");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_ContractId", "[ContractId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_ParentCommentId", "[ParentCommentId] IS NULL OR [ParentCommentId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_RecordedByEmployeeId", "[RecordedByEmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_Source", "[Source] = 'ExternalFeedback'");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_State", "[State] IN (0, 1)");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_TermId", "[TermId] IS NULL OR [TermId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationComment_VersionId", "[VersionId] > 0");
                });

            migrationBuilder.CreateTable(
                name: "tbl_ContractNegotiationCommentEvent",
                columns: table => new
                {
                    CommentEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommentId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<byte>(type: "tinyint", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysutcdatetime())")
                        .Annotation("Relational:DefaultConstraintName", "DF_tbl_ContractNegotiationCommentEvent_OccurredAt")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tbl_ContractNegotiationCommentEvent", x => x.CommentEventId);
                    table.CheckConstraint("CK_tbl_ContractNegotiationCommentEvent_CommentId", "[CommentId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationCommentEvent_EmployeeId", "[EmployeeId] > 0");
                    table.CheckConstraint("CK_tbl_ContractNegotiationCommentEvent_EventType", "[EventType] IN (1, 2, 3)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationComment_ParentCommentId",
                table: "tbl_ContractNegotiationComment",
                column: "ParentCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationComment_TermId",
                table: "tbl_ContractNegotiationComment",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationComment_Version_Chronological",
                table: "tbl_ContractNegotiationComment",
                columns: new[] { "ContractId", "VersionId", "CreatedDate", "CommentId" });

            migrationBuilder.CreateIndex(
                name: "IX_tbl_ContractNegotiationCommentEvent_Chronological",
                table: "tbl_ContractNegotiationCommentEvent",
                columns: new[] { "CommentId", "OccurredAt", "CommentEventId" });

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_tbl_ContractNegotiationComment_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationComment]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i
                            ON i.[CommentId] = d.[CommentId]
                        WHERE i.[CommentId] IS NULL
                           OR i.[ContractId] <> d.[ContractId]
                           OR i.[VersionId] <> d.[VersionId]
                           OR ISNULL(i.[TermId], 0) <> ISNULL(d.[TermId], 0)
                           OR ISNULL(i.[ParentCommentId], 0)
                              <> ISNULL(d.[ParentCommentId], 0)
                           OR i.[Content] <> d.[Content]
                           OR i.[Source] <> d.[Source]
                           OR i.[RecordedByEmployeeId]
                              <> d.[RecordedByEmployeeId]
                           OR i.[CreatedDate] <> d.[CreatedDate]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51005,
                            N'Negotiation comments are append-only.',
                            1;
                    END;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE TRIGGER [TR_tbl_ContractNegotiationCommentEvent_AppendOnly]
                ON [dbo].[tbl_ContractNegotiationCommentEvent]
                AFTER UPDATE, DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF EXISTS
                    (
                        SELECT 1
                        FROM [deleted] d
                        LEFT JOIN [inserted] i
                            ON i.[CommentEventId] = d.[CommentEventId]
                        WHERE i.[CommentEventId] IS NULL
                           OR i.[CommentId] <> d.[CommentId]
                           OR i.[EventType] <> d.[EventType]
                           OR i.[EmployeeId] <> d.[EmployeeId]
                           OR i.[OccurredAt] <> d.[OccurredAt]
                    )
                    BEGIN
                        ROLLBACK TRANSACTION;
                        THROW 51006,
                            N'Negotiation comment events are append-only.',
                            1;
                    END;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [dbo].[TR_tbl_ContractNegotiationComment_AppendOnly];");

            migrationBuilder.Sql(
                "DROP TRIGGER IF EXISTS [dbo].[TR_tbl_ContractNegotiationCommentEvent_AppendOnly];");

            migrationBuilder.DropTable(
                name: "tbl_ContractNegotiationComment");

            migrationBuilder.DropTable(
                name: "tbl_ContractNegotiationCommentEvent");
        }
    }
}
