/*
    Read-only Phase 0 inventory for persisted JSON and large text columns.

    Run with a login that can read metadata in the current tenant database.
    The script does not update schema or data.
*/
SET NOCOUNT ON;

SELECT
    DB_NAME() AS DatabaseName,
    schemaInfo.name AS SchemaName,
    tableInfo.name AS TableName,
    columnInfo.name AS ColumnName,
    typeInfo.name AS SqlType,
    columnInfo.max_length AS MaxLength
FROM sys.columns AS columnInfo
INNER JOIN sys.tables AS tableInfo
    ON tableInfo.object_id = columnInfo.object_id
INNER JOIN sys.schemas AS schemaInfo
    ON schemaInfo.schema_id = tableInfo.schema_id
INNER JOIN sys.types AS typeInfo
    ON typeInfo.user_type_id = columnInfo.user_type_id
WHERE columnInfo.name LIKE '%Json%'
    OR columnInfo.name IN
    (
        'EncryptedPayload',
        'TermContent',
        'TermContentEn'
    )
ORDER BY schemaInfo.name, tableInfo.name, columnInfo.column_id;

SELECT
    DB_NAME() AS DatabaseName,
    schemaInfo.name AS SchemaName,
    tableInfo.name AS TableName,
    constraintInfo.name AS ConstraintName,
    constraintInfo.definition AS Definition
FROM sys.check_constraints AS constraintInfo
INNER JOIN sys.tables AS tableInfo
    ON tableInfo.object_id = constraintInfo.parent_object_id
INNER JOIN sys.schemas AS schemaInfo
    ON schemaInfo.schema_id = tableInfo.schema_id
WHERE constraintInfo.definition LIKE '%ISJSON%'
ORDER BY schemaInfo.name, tableInfo.name, constraintInfo.name;

SELECT
    DB_NAME() AS DatabaseName,
    schemaInfo.name AS SchemaName,
    tableInfo.name AS TableName,
    columnInfo.name AS ColumnName,
    typeInfo.name AS SqlType
FROM sys.columns AS columnInfo
INNER JOIN sys.tables AS tableInfo
    ON tableInfo.object_id = columnInfo.object_id
INNER JOIN sys.schemas AS schemaInfo
    ON schemaInfo.schema_id = tableInfo.schema_id
INNER JOIN sys.types AS typeInfo
    ON typeInfo.user_type_id = columnInfo.user_type_id
WHERE columnInfo.max_length = -1
    AND typeInfo.name IN ('varchar', 'nvarchar', 'varbinary')
ORDER BY schemaInfo.name, tableInfo.name, columnInfo.column_id;
