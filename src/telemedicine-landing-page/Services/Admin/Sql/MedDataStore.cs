using System.Text.Json;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Kho dữ liệu trong bộ nhớ (singleton, thread-safe) cho miền nghiệp vụ QLCM.
/// Áp dụng tất cả ràng buộc SQL: UQ, FK, CHECK, trigger tương đương.
/// </summary>
public sealed partial class MedDataStore : IMedDataStore
{
    private readonly object _lock = new();
    private long _auditSeq;

    public event Action? StateChanged;

    public MedDataStore()
    {
        MedDataStoreSeed.Apply(this);
    }

    // === Bộ sưu tập dữ liệu ===
    private readonly List<Department> _departments = new();
    private readonly List<DepartmentClosureEdge> _closure = new();
    private readonly List<AppUser> _users = new();
    private readonly List<Role> _roles = new();
    private readonly List<Group> _groups = new();
    private readonly List<UserRole> _userRoles = new();
    private readonly List<UserGroupMember> _userGroupMembers = new();
    private readonly List<ScreenCatalog> _screens = new();
    private readonly List<FeatureCatalog> _features = new();
    private readonly List<MedPermission> _permissions = new();
    private readonly List<RolePermission> _rolePermissions = new();
    private readonly List<GroupPermission> _groupPermissions = new();
    private readonly List<UserPermissionOverride> _userPermissionOverrides = new();
    private readonly List<AuditLog> _auditLogs = new();
    private readonly List<PermissionChangeRequest> _permChangeRequests = new();
    private readonly List<PermissionChangeItem> _permChangeItems = new();
    private readonly List<ProfessionalProcedure> _procedures = new();
    private readonly List<ProcedureVersion> _procedureVersions = new();
    private readonly List<ProcedureStep> _procedureSteps = new();
    private readonly List<ProcedureAttachment> _procedureAttachments = new();
    private readonly List<ProcedureScreenMapping> _procedureScreenMappings = new();
    private readonly List<PatientRef> _patientRefs = new();
    private readonly List<EncounterRef> _encounterRefs = new();
    private readonly List<TechnicalService> _technicalServices = new();
    private readonly List<ResourceCatalogItem> _resourceCatalog = new();
    private readonly List<TechnicalResourceNorm> _technicalResourceNorms = new();
    private readonly List<ProcedureVersionResourceNorm> _procedureVersionResourceNorms = new();
    private readonly List<TechnicalOrder> _technicalOrders = new();
    private readonly List<ResourceAvailabilitySnapshot> _resourceSnapshots = new();
    private readonly List<ActualResourceUsage> _actualResourceUsages = new();
    private readonly List<ClinicalProtocol> _clinicalProtocols = new();
    private readonly List<ClinicalProtocolVersion> _clinicalProtocolVersions = new();
    private readonly List<ClinicalProtocolProcedure> _clinicalProtocolProcedures = new();
    private readonly List<ProtocolApplicabilityRule> _protocolRules = new();
    private readonly List<PatientProtocolApplication> _patientProtocolApps = new();
    private readonly List<SignatureRecord> _signatureRecords = new();
    private readonly List<NotificationPreference> _notificationPrefs = new();
    private readonly List<MedNotification> _notifications = new();
    private readonly List<NotificationDeliveryAttempt> _deliveryAttempts = new();

    // === Truy cập đọc ===
    public IReadOnlyList<Department> Departments => _departments;
    public IReadOnlyList<DepartmentClosureEdge> DepartmentClosure => _closure;
    public IReadOnlyList<AppUser> Users => _users;
    public IReadOnlyList<Role> Roles => _roles;
    public IReadOnlyList<Group> Groups => _groups;
    public IReadOnlyList<UserRole> UserRoles => _userRoles;
    public IReadOnlyList<UserGroupMember> UserGroupMembers => _userGroupMembers;
    public IReadOnlyList<ScreenCatalog> Screens => _screens;
    public IReadOnlyList<FeatureCatalog> Features => _features;
    public IReadOnlyList<MedPermission> Permissions => _permissions;
    public IReadOnlyList<RolePermission> RolePermissions => _rolePermissions;
    public IReadOnlyList<GroupPermission> GroupPermissions => _groupPermissions;
    public IReadOnlyList<UserPermissionOverride> UserPermissionOverrides => _userPermissionOverrides;
    public IReadOnlyList<AuditLog> AuditLogs => _auditLogs;
    public IReadOnlyList<PermissionChangeRequest> PermissionChangeRequests => _permChangeRequests;
    public IReadOnlyList<PermissionChangeItem> PermissionChangeItems => _permChangeItems;
    public IReadOnlyList<ProfessionalProcedure> Procedures => _procedures;
    public IReadOnlyList<ProcedureVersion> ProcedureVersions => _procedureVersions;
    public IReadOnlyList<ProcedureStep> ProcedureSteps => _procedureSteps;
    public IReadOnlyList<ProcedureAttachment> ProcedureAttachments => _procedureAttachments;
    public IReadOnlyList<ProcedureScreenMapping> ProcedureScreenMappings => _procedureScreenMappings;
    public IReadOnlyList<PatientRef> PatientRefs => _patientRefs;
    public IReadOnlyList<EncounterRef> EncounterRefs => _encounterRefs;
    public IReadOnlyList<TechnicalService> TechnicalServices => _technicalServices;
    public IReadOnlyList<ResourceCatalogItem> ResourceCatalog => _resourceCatalog;
    public IReadOnlyList<TechnicalResourceNorm> TechnicalResourceNorms => _technicalResourceNorms;
    public IReadOnlyList<ProcedureVersionResourceNorm> ProcedureVersionResourceNorms
        => _procedureVersionResourceNorms;
    public IReadOnlyList<TechnicalOrder> TechnicalOrders => _technicalOrders;
    public IReadOnlyList<ResourceAvailabilitySnapshot> ResourceAvailabilitySnapshots => _resourceSnapshots;
    public IReadOnlyList<ActualResourceUsage> ActualResourceUsages => _actualResourceUsages;
    public IReadOnlyList<ClinicalProtocol> ClinicalProtocols => _clinicalProtocols;
    public IReadOnlyList<ClinicalProtocolVersion> ClinicalProtocolVersions => _clinicalProtocolVersions;
    public IReadOnlyList<ClinicalProtocolProcedure> ClinicalProtocolProcedures => _clinicalProtocolProcedures;
    public IReadOnlyList<ProtocolApplicabilityRule> ProtocolApplicabilityRules => _protocolRules;
    public IReadOnlyList<PatientProtocolApplication> PatientProtocolApplications => _patientProtocolApps;
    public IReadOnlyList<SignatureRecord> SignatureRecords => _signatureRecords;
    public IReadOnlyList<NotificationPreference> NotificationPreferences => _notificationPrefs;
    public IReadOnlyList<MedNotification> Notifications => _notifications;
    public IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts => _deliveryAttempts;

    private void RaiseStateChanged() => StateChanged?.Invoke();

    /// <summary>Kiểm tra JSON hợp lệ (tương đương ISJSON = 1).</summary>
    internal static void ValidateJson(string? json, string fieldName)
    {
        if (json is null) return;
        try { JsonDocument.Parse(json).Dispose(); }
        catch (JsonException)
        {
            throw MedDomainException.Constraint(
                $"CK_{fieldName}_json", 50001, $"Giá trị {fieldName} không phải JSON hợp lệ.");
        }
    }

    /// <summary>Kiểm tra ngày kết thúc phải sau ngày bắt đầu.</summary>
    internal static void ValidateDates(DateTime? from, DateTime? to, string constraintName)
    {
        if (to.HasValue && from.HasValue && to.Value <= from.Value)
            throw MedDomainException.Constraint(
                constraintName, 50002, "Ngày kết thúc phải sau ngày bắt đầu.");
    }
}
