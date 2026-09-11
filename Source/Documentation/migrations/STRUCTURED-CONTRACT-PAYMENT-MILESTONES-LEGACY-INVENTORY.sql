/*
Run this read-only query in every tenant database before enabling structured
payment milestones. It deliberately does not infer a VersionId or TermId for
legacy rows because tbl_PaymentSchedule only stores ContractId.
*/
SET NOCOUNT ON;

SELECT
    DB_NAME() AS TenantDatabase,
    COUNT_BIG(*) AS LegacyScheduleRows,
    COUNT_BIG(DISTINCT Schedule.ContractId) AS ContractsWithLegacySchedule,
    SUM(CASE WHEN Contract.ContractId IS NULL THEN 1 ELSE 0 END) AS OrphanRows,
    SUM(CASE WHEN VersionCounts.VersionCount > 1 THEN 1 ELSE 0 END) AS RowsOnMultiVersionContracts
FROM dbo.tbl_PaymentSchedule AS Schedule
LEFT JOIN dbo.tbl_Contract AS Contract
    ON Contract.ContractId = Schedule.ContractId
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS VersionCount
    FROM dbo.tbl_ContractVersion AS VersionRow
    WHERE VersionRow.ContractId = Schedule.ContractId
) AS VersionCounts;

SELECT
    Schedule.ScheduleId,
    Schedule.ContractId,
    Contract.ContractCode,
    Contract.CurrentVersionId,
    VersionCounts.VersionCount,
    Schedule.DueDate,
    Schedule.Amount,
    Schedule.PaidAmount,
    Schedule.PaymentStatus,
    Schedule.Note,
    CASE
        WHEN Contract.ContractId IS NULL THEN N'Orphan: contract không tồn tại'
        WHEN VersionCounts.VersionCount = 0 THEN N'Không có contract version'
        WHEN VersionCounts.VersionCount > 1 THEN N'Mơ hồ: contract có nhiều version'
        ELSE N'Có thể review thủ công, không tự động migrate'
    END AS MigrationAssessment
FROM dbo.tbl_PaymentSchedule AS Schedule
LEFT JOIN dbo.tbl_Contract AS Contract
    ON Contract.ContractId = Schedule.ContractId
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS VersionCount
    FROM dbo.tbl_ContractVersion AS VersionRow
    WHERE VersionRow.ContractId = Schedule.ContractId
) AS VersionCounts
ORDER BY Schedule.ContractId, Schedule.DueDate, Schedule.ScheduleId;
