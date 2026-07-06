USE MedicalProcedureManagement;
GO

IF OBJECT_ID(N'tempdb..#department_refs') IS NOT NULL
    DROP TABLE #department_refs;

CREATE TABLE #department_refs
(
    department_id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    reference_count INT NOT NULL DEFAULT 0
);

INSERT INTO #department_refs (department_id)
SELECT department_id
FROM med.departments;

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = STRING_AGG(
    CAST(N'
UPDATE refs
SET reference_count = reference_count + counts.ref_count
FROM #department_refs refs
JOIN (
    SELECT source.' + QUOTENAME(parentColumn.name) + N' AS department_id,
           COUNT_BIG(*) AS ref_count
    FROM ' + QUOTENAME(SCHEMA_NAME(parentTable.schema_id)) + N'.' + QUOTENAME(parentTable.name) + N' source
    WHERE source.' + QUOTENAME(parentColumn.name) + N' IS NOT NULL
    GROUP BY source.' + QUOTENAME(parentColumn.name) + N'
) counts
    ON counts.department_id = refs.department_id;' AS NVARCHAR(MAX)),
    NCHAR(10))
FROM sys.foreign_key_columns fkc
JOIN sys.tables parentTable
    ON parentTable.object_id = fkc.parent_object_id
JOIN sys.columns parentColumn
    ON parentColumn.object_id = fkc.parent_object_id
   AND parentColumn.column_id = fkc.parent_column_id
JOIN sys.tables referencedTable
    ON referencedTable.object_id = fkc.referenced_object_id
JOIN sys.schemas referencedSchema
    ON referencedSchema.schema_id = referencedTable.schema_id
WHERE referencedSchema.name = N'med'
  AND referencedTable.name = N'departments'
  AND NOT (
      SCHEMA_NAME(parentTable.schema_id) = N'med'
      AND parentTable.name IN (N'departments', N'department_closure')
  );

IF @sql IS NOT NULL AND LEN(@sql) > 0
BEGIN
    EXEC sp_executesql @sql;
END;

;WITH duplicate_departments AS (
    SELECT d.department_id,
           d.name,
           d.code,
           d.created_at,
           refs.reference_count,
           ROW_NUMBER() OVER (
               PARTITION BY d.name
               ORDER BY
                   CASE WHEN refs.reference_count > 0 THEN 0 ELSE 1 END,
                   refs.reference_count DESC,
                   LEN(d.code) DESC,
                   d.created_at DESC,
                   d.department_id DESC
           ) AS duplicate_rank,
           COUNT(*) OVER (PARTITION BY d.name) AS duplicate_count
    FROM med.departments d
    JOIN #department_refs refs
        ON refs.department_id = d.department_id
    WHERE d.status = N'active'
)
UPDATE d
SET status = N'inactive',
    updated_at = SYSUTCDATETIME()
FROM med.departments d
JOIN duplicate_departments duplicates
    ON duplicates.department_id = d.department_id
WHERE duplicates.duplicate_count > 1
  AND duplicates.duplicate_rank > 1
  AND duplicates.reference_count = 0;

DROP TABLE #department_refs;
GO
