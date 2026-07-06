USE MedicalProcedureManagement;
GO

/* Support multiple writers, multiple step roles/locations/attachments, and immutable version snapshots/diffs. */

IF COL_LENGTH('med.procedure_versions', 'required_writer_signatures') IS NULL
BEGIN
    ALTER TABLE med.procedure_versions
        ADD required_writer_signatures INT NOT NULL
            CONSTRAINT DF_procedure_versions_required_writer_signatures DEFAULT (1);
END
GO

IF OBJECT_ID(N'med.procedure_version_author_assignments', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_version_author_assignments
    (
        procedure_version_author_assignment_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_version_author_assignments PRIMARY KEY,
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        signoff_role NVARCHAR(32) NOT NULL
            CONSTRAINT DF_procedure_version_author_assignments_role DEFAULT (N'writer'),
        display_order INT NOT NULL
            CONSTRAINT DF_procedure_version_author_assignments_display_order DEFAULT (1),
        assigned_user_id UNIQUEIDENTIFIER NOT NULL,
        assigned_username NVARCHAR(256) NULL,
        assigned_full_name NVARCHAR(256) NULL,
        created_at DATETIME2(0) NOT NULL
            CONSTRAINT DF_procedure_version_author_assignments_created_at DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_author_assignments_version')
BEGIN
    ALTER TABLE med.procedure_version_author_assignments
        ADD CONSTRAINT FK_procedure_version_author_assignments_version
            FOREIGN KEY (procedure_version_id)
            REFERENCES med.procedure_versions (procedure_version_id)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_author_assignments_user')
BEGIN
    ALTER TABLE med.procedure_version_author_assignments
        ADD CONSTRAINT FK_procedure_version_author_assignments_user
            FOREIGN KEY (assigned_user_id)
            REFERENCES med.users (user_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_procedure_version_author_assignments_role_user'
      AND object_id = OBJECT_ID(N'med.procedure_version_author_assignments')
)
BEGIN
    CREATE UNIQUE INDEX UX_procedure_version_author_assignments_role_user
        ON med.procedure_version_author_assignments (procedure_version_id, signoff_role, assigned_user_id);
END
GO

IF OBJECT_ID(N'med.procedure_step_role_assignments', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_step_role_assignments
    (
        procedure_step_role_assignment_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_step_role_assignments PRIMARY KEY,
        procedure_step_id UNIQUEIDENTIFIER NOT NULL,
        role_id UNIQUEIDENTIFIER NOT NULL,
        display_order INT NOT NULL
            CONSTRAINT DF_procedure_step_role_assignments_display_order DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_role_assignments_step')
BEGIN
    ALTER TABLE med.procedure_step_role_assignments
        ADD CONSTRAINT FK_procedure_step_role_assignments_step
            FOREIGN KEY (procedure_step_id)
            REFERENCES med.procedure_steps (procedure_step_id)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_role_assignments_role')
BEGIN
    ALTER TABLE med.procedure_step_role_assignments
        ADD CONSTRAINT FK_procedure_step_role_assignments_role
            FOREIGN KEY (role_id)
            REFERENCES med.roles (role_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_procedure_step_role_assignments_step_role'
      AND object_id = OBJECT_ID(N'med.procedure_step_role_assignments')
)
BEGIN
    CREATE UNIQUE INDEX UX_procedure_step_role_assignments_step_role
        ON med.procedure_step_role_assignments (procedure_step_id, role_id);
END
GO

IF OBJECT_ID(N'med.procedure_step_location_assignments', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_step_location_assignments
    (
        procedure_step_location_assignment_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_step_location_assignments PRIMARY KEY,
        procedure_step_id UNIQUEIDENTIFIER NOT NULL,
        department_id UNIQUEIDENTIFIER NOT NULL,
        display_order INT NOT NULL
            CONSTRAINT DF_procedure_step_location_assignments_display_order DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_location_assignments_step')
BEGIN
    ALTER TABLE med.procedure_step_location_assignments
        ADD CONSTRAINT FK_procedure_step_location_assignments_step
            FOREIGN KEY (procedure_step_id)
            REFERENCES med.procedure_steps (procedure_step_id)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_location_assignments_department')
BEGIN
    ALTER TABLE med.procedure_step_location_assignments
        ADD CONSTRAINT FK_procedure_step_location_assignments_department
            FOREIGN KEY (department_id)
            REFERENCES med.departments (department_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_procedure_step_location_assignments_step_department'
      AND object_id = OBJECT_ID(N'med.procedure_step_location_assignments')
)
BEGIN
    CREATE UNIQUE INDEX UX_procedure_step_location_assignments_step_department
        ON med.procedure_step_location_assignments (procedure_step_id, department_id);
END
GO

IF OBJECT_ID(N'med.procedure_step_attachment_assignments', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_step_attachment_assignments
    (
        procedure_step_attachment_assignment_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_step_attachment_assignments PRIMARY KEY,
        procedure_step_id UNIQUEIDENTIFIER NOT NULL,
        procedure_attachment_id UNIQUEIDENTIFIER NOT NULL,
        display_order INT NOT NULL
            CONSTRAINT DF_procedure_step_attachment_assignments_display_order DEFAULT (1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_attachment_assignments_step')
BEGIN
    ALTER TABLE med.procedure_step_attachment_assignments
        ADD CONSTRAINT FK_procedure_step_attachment_assignments_step
            FOREIGN KEY (procedure_step_id)
            REFERENCES med.procedure_steps (procedure_step_id)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_step_attachment_assignments_attachment')
BEGIN
    ALTER TABLE med.procedure_step_attachment_assignments
        ADD CONSTRAINT FK_procedure_step_attachment_assignments_attachment
            FOREIGN KEY (procedure_attachment_id)
            REFERENCES med.procedure_attachments (procedure_attachment_id)
            ON DELETE CASCADE;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_procedure_step_attachment_assignments_step_attachment'
      AND object_id = OBJECT_ID(N'med.procedure_step_attachment_assignments')
)
BEGIN
    CREATE UNIQUE INDEX UX_procedure_step_attachment_assignments_step_attachment
        ON med.procedure_step_attachment_assignments (procedure_step_id, procedure_attachment_id);
END
GO

IF OBJECT_ID(N'med.procedure_version_snapshots', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_version_snapshots
    (
        procedure_version_snapshot_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_version_snapshots PRIMARY KEY,
        procedure_version_id UNIQUEIDENTIFIER NOT NULL,
        snapshot_kind NVARCHAR(50) NOT NULL,
        content_hash_sha256 NVARCHAR(128) NOT NULL,
        snapshot_json NVARCHAR(MAX) NOT NULL,
        created_by UNIQUEIDENTIFIER NULL,
        created_at DATETIME2(0) NOT NULL
            CONSTRAINT DF_procedure_version_snapshots_created_at DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_snapshots_version')
BEGIN
    ALTER TABLE med.procedure_version_snapshots
        ADD CONSTRAINT FK_procedure_version_snapshots_version
            FOREIGN KEY (procedure_version_id)
            REFERENCES med.procedure_versions (procedure_version_id)
            ON DELETE CASCADE;
END
GO

IF OBJECT_ID(N'med.procedure_version_diff_records', N'U') IS NULL
BEGIN
    CREATE TABLE med.procedure_version_diff_records
    (
        procedure_version_diff_record_id UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_procedure_version_diff_records PRIMARY KEY,
        procedure_id UNIQUEIDENTIFIER NOT NULL,
        from_version_id UNIQUEIDENTIFIER NOT NULL,
        to_version_id UNIQUEIDENTIFIER NOT NULL,
        diff_json NVARCHAR(MAX) NOT NULL,
        created_by UNIQUEIDENTIFIER NULL,
        created_at DATETIME2(0) NOT NULL
            CONSTRAINT DF_procedure_version_diff_records_created_at DEFAULT (SYSUTCDATETIME())
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_diff_records_procedure')
BEGIN
    ALTER TABLE med.procedure_version_diff_records
        ADD CONSTRAINT FK_procedure_version_diff_records_procedure
            FOREIGN KEY (procedure_id)
            REFERENCES med.professional_procedures (procedure_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_diff_records_from_version')
BEGIN
    ALTER TABLE med.procedure_version_diff_records
        ADD CONSTRAINT FK_procedure_version_diff_records_from_version
            FOREIGN KEY (from_version_id)
            REFERENCES med.procedure_versions (procedure_version_id);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_procedure_version_diff_records_to_version')
BEGIN
    ALTER TABLE med.procedure_version_diff_records
        ADD CONSTRAINT FK_procedure_version_diff_records_to_version
            FOREIGN KEY (to_version_id)
            REFERENCES med.procedure_versions (procedure_version_id);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'UX_procedure_version_diff_records_from_to'
      AND object_id = OBJECT_ID(N'med.procedure_version_diff_records')
)
BEGIN
    CREATE UNIQUE INDEX UX_procedure_version_diff_records_from_to
        ON med.procedure_version_diff_records (from_version_id, to_version_id);
END
GO

/* Backfill role-based step assignments from legacy single-role column. */
INSERT INTO med.procedure_step_role_assignments
(
    procedure_step_role_assignment_id,
    procedure_step_id,
    role_id,
    display_order
)
SELECT NEWID(), s.procedure_step_id, s.actor_role_id, 1
FROM med.procedure_steps AS s
WHERE s.actor_role_id IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM med.procedure_step_role_assignments AS x
      WHERE x.procedure_step_id = s.procedure_step_id
        AND x.role_id = s.actor_role_id
  );
GO

/* Backfill step attachment links from legacy form_attachment_id. */
INSERT INTO med.procedure_step_attachment_assignments
(
    procedure_step_attachment_assignment_id,
    procedure_step_id,
    procedure_attachment_id,
    display_order
)
SELECT NEWID(), s.procedure_step_id, s.form_attachment_id, 1
FROM med.procedure_steps AS s
WHERE s.form_attachment_id IS NOT NULL
  AND NOT EXISTS
  (
      SELECT 1
      FROM med.procedure_step_attachment_assignments AS x
      WHERE x.procedure_step_id = s.procedure_step_id
        AND x.procedure_attachment_id = s.form_attachment_id
  );
GO

