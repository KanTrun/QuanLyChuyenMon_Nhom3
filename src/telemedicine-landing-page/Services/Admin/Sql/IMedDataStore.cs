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
    void Refresh(bool publish = false);

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
    IReadOnlyList<SignatureRecord> SignatureRecords { get; }
    IReadOnlyList<SignatureTransactionRecord> SignatureTransactions { get; }
    IReadOnlyList<NotificationPreference> NotificationPreferences { get; }
    IReadOnlyList<MedNotification> Notifications { get; }
    IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts { get; }

    // === Ghi dữ liệu ===
    void AddDepartment(Department dept);
    void UpdateDepartment(Department dept);
    void UpdateDepartmentParent(Guid departmentId, Guid? newParentId);
    void ArchiveDepartment(Guid departmentId);

    void AddUser(AppUser user);
    void UpdateUser(AppUser user);

    void AddRole(Role role);
    void UpdateRole(Role role);
    void ArchiveRole(Guid roleId);
    void AddGroup(Group group);
    void ArchiveGroup(Guid groupId);
    void AddUserRole(UserRole userRole);
    void RemoveUserRole(Guid userRoleId);
    void AddUserGroupMember(UserGroupMember member);
    void RemoveUserGroupMember(Guid membershipId);

    void AddScreen(ScreenCatalog screen);
    void AddFeature(FeatureCatalog feature);
    void AddPermission(MedPermission permission);
    void AddRolePermission(RolePermission rp);
    void RemoveRolePermission(Guid rolePermissionId);
    void AddGroupPermission(GroupPermission gp);
    void RemoveGroupPermission(Guid groupPermissionId);
    void AddUserPermissionOverride(UserPermissionOverride upo);
    void UpdateUserPermissionOverride(UserPermissionOverride upo);
    void RemoveUserPermissionOverride(Guid userPermissionOverrideId);

    void AppendAudit(AuditLog log);

    void AddPermissionChangeRequest(PermissionChangeRequest req);
    void UpdatePermissionChangeRequest(PermissionChangeRequest updated);
    void AddPermissionChangeItem(PermissionChangeItem item);

    void AddProcedure(ProfessionalProcedure proc);
    void UpdateProcedure(ProfessionalProcedure proc);
    void AddProcedureVersion(ProcedureVersion ver);
    void UpdateProcedureVersion(ProcedureVersion updated);
    void AddProcedureStep(ProcedureStep step);
    void AddProcedureAttachment(ProcedureAttachment att);
    void RemoveProcedureAttachment(Guid attachmentId);
    void AddProcedureScreenMapping(ProcedureScreenMapping mapping);
    void RemoveProcedureScreenMapping(Guid mappingId);

    void AddPatientRef(PatientRef patient);
    void UpdatePatientRef(PatientRef patient);
    void AddEncounterRef(EncounterRef encounter);
    void UpdateEncounterRef(EncounterRef encounter);

    void AddTechnicalService(TechnicalService svc);
    void UpdateTechnicalService(TechnicalService svc);
    void RemoveTechnicalService(Guid technicalServiceId);
    void AddResourceCatalogItem(ResourceCatalogItem item);
    void UpdateResourceCatalogItem(ResourceCatalogItem item);
    void RemoveResourceCatalogItem(Guid resourceId);
    void AddTechnicalResourceNorm(TechnicalResourceNorm norm);
    void RemoveTechnicalResourceNorm(Guid normId);
    void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm);
    void RemoveProcedureVersionResourceNorm(Guid normId);
    void AddTechnicalOrder(TechnicalOrder order);
    void UpdateTechnicalOrder(TechnicalOrder order);
    void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap);
    void AddActualResourceUsage(ActualResourceUsage usage);
    void RemoveActualResourceUsage(Guid usageId);

    void AddClinicalProtocol(ClinicalProtocol protocol);
    void UpdateClinicalProtocol(ClinicalProtocol protocol);
    void AddClinicalProtocolVersion(ClinicalProtocolVersion ver);
    void UpdateClinicalProtocolVersion(ClinicalProtocolVersion ver);
    void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp);
    void RemoveClinicalProtocolProcedure(Guid clinicalProtocolProcedureId);
    void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule);
    void RemoveProtocolApplicabilityRule(Guid ruleId);
    void AddPatientProtocolApplication(PatientProtocolApplication app);
    void UpdatePatientProtocolApplication(PatientProtocolApplication app);
    void AddSignatureRecord(SignatureRecord signature);
    void AddSignatureTransaction(SignatureTransactionRecord transaction);
    void UpdateSignatureTransaction(SignatureTransactionRecord transaction);

    void AddNotificationPreference(NotificationPreference pref);
    void UpdateNotificationPreference(NotificationPreference pref);
    void RemoveNotificationPreference(Guid prefId);
    void AddNotification(MedNotification notification);
    void UpdateNotificationReadAt(Guid notificationId, DateTime readAt);
    void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt);
}
