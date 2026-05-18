using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Giao diện kho dữ liệu trong bộ nhớ (in-memory) cho miền nghiệp vụ QLCM.
/// Mọi phương thức ghi áp dụng ràng buộc tương đương SQL (UQ, FK, CHECK, trigger).
/// </summary>
public interface IMedDataStore
{
    // Sự kiện thay đổi trạng thái
    event Action? StateChanged;

    // === Đọc dữ liệu ===
    IReadOnlyList<Department> Departments { get; }
    IReadOnlyList<DepartmentClosureEdge> DepartmentClosure { get; }
    IReadOnlyList<AppUser> Users { get; }
    IReadOnlyList<Role> Roles { get; }
    IReadOnlyList<Group> Groups { get; }
    IReadOnlyList<UserRole> UserRoles { get; }
    IReadOnlyList<UserGroupMember> UserGroupMembers { get; }
    IReadOnlyList<ScreenCatalog> Screens { get; }
    IReadOnlyList<FeatureCatalog> Features { get; }
    IReadOnlyList<MedPermission> Permissions { get; }
    IReadOnlyList<RolePermission> RolePermissions { get; }
    IReadOnlyList<GroupPermission> GroupPermissions { get; }
    IReadOnlyList<UserPermissionOverride> UserPermissionOverrides { get; }
    IReadOnlyList<AuditLog> AuditLogs { get; }
    IReadOnlyList<PermissionChangeRequest> PermissionChangeRequests { get; }
    IReadOnlyList<PermissionChangeItem> PermissionChangeItems { get; }
    IReadOnlyList<ProfessionalProcedure> Procedures { get; }
    IReadOnlyList<ProcedureVersion> ProcedureVersions { get; }
    IReadOnlyList<ProcedureStep> ProcedureSteps { get; }
    IReadOnlyList<ProcedureAttachment> ProcedureAttachments { get; }
    IReadOnlyList<ProcedureScreenMapping> ProcedureScreenMappings { get; }
    IReadOnlyList<PatientRef> PatientRefs { get; }
    IReadOnlyList<EncounterRef> EncounterRefs { get; }
    IReadOnlyList<TechnicalService> TechnicalServices { get; }
    IReadOnlyList<ResourceCatalogItem> ResourceCatalog { get; }
    IReadOnlyList<TechnicalResourceNorm> TechnicalResourceNorms { get; }
    IReadOnlyList<ProcedureVersionResourceNorm> ProcedureVersionResourceNorms { get; }
    IReadOnlyList<TechnicalOrder> TechnicalOrders { get; }
    IReadOnlyList<ResourceAvailabilitySnapshot> ResourceAvailabilitySnapshots { get; }
    IReadOnlyList<ActualResourceUsage> ActualResourceUsages { get; }
    IReadOnlyList<ClinicalProtocol> ClinicalProtocols { get; }
    IReadOnlyList<ClinicalProtocolVersion> ClinicalProtocolVersions { get; }
    IReadOnlyList<ClinicalProtocolProcedure> ClinicalProtocolProcedures { get; }
    IReadOnlyList<ProtocolApplicabilityRule> ProtocolApplicabilityRules { get; }
    IReadOnlyList<PatientProtocolApplication> PatientProtocolApplications { get; }
    IReadOnlyList<NotificationPreference> NotificationPreferences { get; }
    IReadOnlyList<MedNotification> Notifications { get; }
    IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts { get; }

    // === Ghi dữ liệu ===
    void AddDepartment(Department dept);
    void UpdateDepartmentParent(Guid departmentId, Guid? newParentId);
    void ArchiveDepartment(Guid departmentId);

    void AddUser(AppUser user);
    void UpdateUser(AppUser user);

    void AddRole(Role role);
    void AddGroup(Group group);
    void AddUserRole(UserRole userRole);
    void AddUserGroupMember(UserGroupMember member);

    void AddScreen(ScreenCatalog screen);
    void AddFeature(FeatureCatalog feature);
    void AddPermission(MedPermission permission);
    void AddRolePermission(RolePermission rp);
    void AddGroupPermission(GroupPermission gp);
    void AddUserPermissionOverride(UserPermissionOverride upo);

    void AppendAudit(AuditLog log);

    void AddPermissionChangeRequest(PermissionChangeRequest req);
    void UpdatePermissionChangeRequest(PermissionChangeRequest updated);
    void AddPermissionChangeItem(PermissionChangeItem item);

    void AddProcedure(ProfessionalProcedure proc);
    void AddProcedureVersion(ProcedureVersion ver);
    void UpdateProcedureVersion(ProcedureVersion updated);
    void AddProcedureStep(ProcedureStep step);
    void AddProcedureAttachment(ProcedureAttachment att);
    void AddProcedureScreenMapping(ProcedureScreenMapping mapping);

    void AddPatientRef(PatientRef patient);
    void AddEncounterRef(EncounterRef encounter);

    void AddTechnicalService(TechnicalService svc);
    void AddResourceCatalogItem(ResourceCatalogItem item);
    void AddTechnicalResourceNorm(TechnicalResourceNorm norm);
    void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm);
    void AddTechnicalOrder(TechnicalOrder order);
    void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap);
    void AddActualResourceUsage(ActualResourceUsage usage);

    void AddClinicalProtocol(ClinicalProtocol protocol);
    void AddClinicalProtocolVersion(ClinicalProtocolVersion ver);
    void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp);
    void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule);
    void AddPatientProtocolApplication(PatientProtocolApplication app);

    void AddNotificationPreference(NotificationPreference pref);
    void AddNotification(MedNotification notification);
    void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt);
}
