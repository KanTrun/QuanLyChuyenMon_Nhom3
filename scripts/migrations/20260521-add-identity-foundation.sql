USE MedicalProcedureManagement;
GO

IF NOT EXISTS (SELECT 1 FROM sys.schemas WHERE name = N'auth')
    EXEC(N'CREATE SCHEMA auth');
GO

IF OBJECT_ID(N'auth.identity_users', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_users (
        Id UNIQUEIDENTIFIER NOT NULL,
        MedUserId UNIQUEIDENTIFIER NULL,
        FullName NVARCHAR(255) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_identity_users_status DEFAULT N'active',
        UserName NVARCHAR(256) NULL,
        NormalizedUserName NVARCHAR(256) NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL CONSTRAINT DF_identity_users_email_confirmed DEFAULT 0,
        PasswordHash NVARCHAR(MAX) NULL,
        SecurityStamp NVARCHAR(MAX) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        PhoneNumber NVARCHAR(MAX) NULL,
        PhoneNumberConfirmed BIT NOT NULL CONSTRAINT DF_identity_users_phone_confirmed DEFAULT 0,
        TwoFactorEnabled BIT NOT NULL CONSTRAINT DF_identity_users_two_factor DEFAULT 0,
        LockoutEnd DATETIMEOFFSET NULL,
        LockoutEnabled BIT NOT NULL CONSTRAINT DF_identity_users_lockout_enabled DEFAULT 1,
        AccessFailedCount INT NOT NULL CONSTRAINT DF_identity_users_failed_count DEFAULT 0,
        CONSTRAINT PK_identity_users PRIMARY KEY (Id),
        CONSTRAINT FK_identity_users_med_users FOREIGN KEY (MedUserId) REFERENCES med.users(user_id),
        CONSTRAINT FK_identity_users_status FOREIGN KEY (Status) REFERENCES med.lookup_record_status(code)
    );
END;
GO

IF OBJECT_ID(N'auth.identity_roles', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_roles (
        Id UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(256) NULL,
        NormalizedName NVARCHAR(256) NULL,
        ConcurrencyStamp NVARCHAR(MAX) NULL,
        CONSTRAINT PK_identity_roles PRIMARY KEY (Id)
    );
END;
GO

IF OBJECT_ID(N'auth.identity_user_roles', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_user_roles (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_identity_user_roles PRIMARY KEY (UserId, RoleId),
        CONSTRAINT FK_identity_user_roles_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE,
        CONSTRAINT FK_identity_user_roles_roles FOREIGN KEY (RoleId) REFERENCES auth.identity_roles(Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'auth.identity_user_claims', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_user_claims (
        Id INT IDENTITY(1,1) NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_identity_user_claims PRIMARY KEY (Id),
        CONSTRAINT FK_identity_user_claims_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'auth.identity_user_logins', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_user_logins (
        LoginProvider NVARCHAR(128) NOT NULL,
        ProviderKey NVARCHAR(128) NOT NULL,
        ProviderDisplayName NVARCHAR(MAX) NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_identity_user_logins PRIMARY KEY (LoginProvider, ProviderKey),
        CONSTRAINT FK_identity_user_logins_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'auth.identity_role_claims', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_role_claims (
        Id INT IDENTITY(1,1) NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,
        ClaimType NVARCHAR(MAX) NULL,
        ClaimValue NVARCHAR(MAX) NULL,
        CONSTRAINT PK_identity_role_claims PRIMARY KEY (Id),
        CONSTRAINT FK_identity_role_claims_roles FOREIGN KEY (RoleId) REFERENCES auth.identity_roles(Id) ON DELETE CASCADE
    );
END;
GO

IF OBJECT_ID(N'auth.identity_user_tokens', N'U') IS NULL
BEGIN
    CREATE TABLE auth.identity_user_tokens (
        UserId UNIQUEIDENTIFIER NOT NULL,
        LoginProvider NVARCHAR(128) NOT NULL,
        Name NVARCHAR(128) NOT NULL,
        Value NVARCHAR(MAX) NULL,
        CONSTRAINT PK_identity_user_tokens PRIMARY KEY (UserId, LoginProvider, Name),
        CONSTRAINT FK_identity_user_tokens_users FOREIGN KEY (UserId) REFERENCES auth.identity_users(Id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_identity_users_med_user')
    CREATE UNIQUE INDEX UX_identity_users_med_user ON auth.identity_users(MedUserId) WHERE MedUserId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_identity_users_normalized_username')
    CREATE UNIQUE INDEX UX_identity_users_normalized_username ON auth.identity_users(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_identity_users_normalized_email')
    CREATE INDEX IX_identity_users_normalized_email ON auth.identity_users(NormalizedEmail);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_identity_roles_normalized_name')
    CREATE UNIQUE INDEX UX_identity_roles_normalized_name ON auth.identity_roles(NormalizedName) WHERE NormalizedName IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_identity_user_roles_role_id')
    CREATE INDEX IX_identity_user_roles_role_id ON auth.identity_user_roles(RoleId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_identity_user_claims_user_id')
    CREATE INDEX IX_identity_user_claims_user_id ON auth.identity_user_claims(UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_identity_user_logins_user_id')
    CREATE INDEX IX_identity_user_logins_user_id ON auth.identity_user_logins(UserId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_identity_role_claims_role_id')
    CREATE INDEX IX_identity_role_claims_role_id ON auth.identity_role_claims(RoleId);
GO
