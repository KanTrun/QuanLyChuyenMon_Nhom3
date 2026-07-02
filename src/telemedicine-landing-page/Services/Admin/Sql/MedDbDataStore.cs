using Microsoft.EntityFrameworkCore;
using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Triển khai IMedDataStore dựa trên MedDbContext (SQL Server thật).
/// Thay thế MedDataStore in-memory — tất cả Razor pages hiện tại tiếp tục hoạt động.
/// </summary>
public sealed class MedDbDataStore : IMedDataStore
{
    private readonly MedDbContext _db;
    private readonly IMedDataChangeBus _changeBus;

    public MedDbDataStore(MedDbContext db, IMedDataChangeBus? changeBus = null)
    {
        _db = db;
        _changeBus = changeBus ?? new MedDataChangeBus();
    }

    // Sự kiện thay đổi trạng thái (giữ tương thích giao diện)
    public event Action? StateChanged;
    private void RaiseStateChanged()
    {
        StateChanged?.Invoke();
        _changeBus.Publish();
    }

    public void Refresh(bool publish = false)
    {
        _db.ChangeTracker.Clear();
        StateChanged?.Invoke();
        if (publish)
        {
            _changeBus.Publish();
        }
    }

    private static List<T> ReadAll<T>(IQueryable<T> query) where T : class => query.AsNoTracking().ToList();

    // === Đọc dữ liệu — truy vấn trực tiếp từ SQL Server (AsNoTracking tránh xung đột ChangeTracker khi ghi) ===
    public IReadOnlyList<Department> Departments => ReadAll(_db.Departments);
    public IReadOnlyList<DepartmentClosureEdge> DepartmentClosure => ReadAll(_db.DepartmentClosure);
    public IReadOnlyList<AppUser> Users => ReadAll(_db.Users);
    public IReadOnlyList<Role> Roles => ReadAll(_db.Roles);
    public IReadOnlyList<Group> Groups => ReadAll(_db.Groups);
    public IReadOnlyList<UserRole> UserRoles => ReadAll(_db.UserRoles);
    public IReadOnlyList<UserGroupMember> UserGroupMembers => ReadAll(_db.UserGroupMembers);
    public IReadOnlyList<ScreenCatalog> Screens => ReadAll(_db.Screens);
    public IReadOnlyList<FeatureCatalog> Features => ReadAll(_db.Features);
    public IReadOnlyList<MedPermission> Permissions => ReadAll(_db.Permissions);
    public IReadOnlyList<RolePermission> RolePermissions => ReadAll(_db.RolePermissions);
    public IReadOnlyList<GroupPermission> GroupPermissions => ReadAll(_db.GroupPermissions);
    public IReadOnlyList<UserPermissionOverride> UserPermissionOverrides => ReadAll(_db.UserPermissionOverrides);
    public IReadOnlyList<AuditLog> AuditLogs => ReadAll(_db.AuditLogs.OrderByDescending(a => a.OccurredAt));
    public IReadOnlyList<PermissionChangeRequest> PermissionChangeRequests => ReadAll(_db.PermissionChangeRequests);
    public IReadOnlyList<PermissionChangeItem> PermissionChangeItems => ReadAll(_db.PermissionChangeItems);
    public IReadOnlyList<ProfessionalProcedure> Procedures => ReadAll(_db.Procedures);
    public IReadOnlyList<ProcedureVersion> ProcedureVersions => ReadAll(_db.ProcedureVersions);
    public IReadOnlyList<ProcedureStep> ProcedureSteps => ReadAll(_db.ProcedureSteps);
    public IReadOnlyList<ProcedureAttachment> ProcedureAttachments => ReadAll(_db.ProcedureAttachments);
    public IReadOnlyList<ProcedureScreenMapping> ProcedureScreenMappings => ReadAll(_db.ProcedureScreenMappings);
    public IReadOnlyList<ProcedureDocumentSection> ProcedureDocumentSections => ReadAll(_db.ProcedureDocumentSections);
    public IReadOnlyList<ProcedureDistributionRecipient> ProcedureDistributionRecipients => ReadAll(_db.ProcedureDistributionRecipients);
    public IReadOnlyList<ProcedureRevisionEntry> ProcedureRevisionEntries => ReadAll(_db.ProcedureRevisionEntries);
    public IReadOnlyList<ProcedureSignoffRecord> ProcedureSignoffRecords => ReadAll(_db.ProcedureSignoffRecords);
    public IReadOnlyList<ProcedureVersionAuthorAssignment> ProcedureVersionAuthorAssignments => ReadAll(_db.ProcedureVersionAuthorAssignments);
    public IReadOnlyList<ProcedureStepRoleAssignment> ProcedureStepRoleAssignments => ReadAll(_db.ProcedureStepRoleAssignments);
    public IReadOnlyList<ProcedureStepLocationAssignment> ProcedureStepLocationAssignments => ReadAll(_db.ProcedureStepLocationAssignments);
    public IReadOnlyList<ProcedureStepAttachmentAssignment> ProcedureStepAttachmentAssignments => ReadAll(_db.ProcedureStepAttachmentAssignments);
    public IReadOnlyList<ProcedureVersionSnapshotRecord> ProcedureVersionSnapshots => ReadAll(_db.ProcedureVersionSnapshots);
    public IReadOnlyList<ProcedureVersionDiffRecord> ProcedureVersionDiffRecords => ReadAll(_db.ProcedureVersionDiffRecords);
    public IReadOnlyList<PatientRef> PatientRefs => ReadAll(_db.PatientRefs);
    public IReadOnlyList<EncounterRef> EncounterRefs => ReadAll(_db.EncounterRefs);
    public IReadOnlyList<TechnicalService> TechnicalServices => ReadAll(_db.TechnicalServices);
    public IReadOnlyList<ResourceCatalogItem> ResourceCatalog => ReadAll(_db.ResourceCatalog);
    public IReadOnlyList<TechnicalResourceNorm> TechnicalResourceNorms => ReadAll(_db.TechnicalResourceNorms);
    public IReadOnlyList<ProcedureVersionResourceNorm> ProcedureVersionResourceNorms => ReadAll(_db.ProcedureVersionResourceNorms);
    public IReadOnlyList<TechnicalOrder> TechnicalOrders => ReadAll(_db.TechnicalOrders);
    public IReadOnlyList<ResourceAvailabilitySnapshot> ResourceAvailabilitySnapshots => ReadAll(_db.ResourceAvailabilitySnapshots);
    public IReadOnlyList<ActualResourceUsage> ActualResourceUsages => ReadAll(_db.ActualResourceUsages);
    public IReadOnlyList<ClinicalProtocol> ClinicalProtocols => ReadAll(_db.ClinicalProtocols);
    public IReadOnlyList<ClinicalProtocolVersion> ClinicalProtocolVersions => ReadAll(_db.ClinicalProtocolVersions);
    public IReadOnlyList<ClinicalProtocolProcedure> ClinicalProtocolProcedures => ReadAll(_db.ClinicalProtocolProcedures);
    public IReadOnlyList<ProtocolApplicabilityRule> ProtocolApplicabilityRules => ReadAll(_db.ProtocolApplicabilityRules);
    public IReadOnlyList<PatientProtocolApplication> PatientProtocolApplications => ReadAll(_db.PatientProtocolApplications);
    public IReadOnlyList<SignatureRecord> SignatureRecords => ReadAll(_db.SignatureRecords);
    public IReadOnlyList<NotificationPreference> NotificationPreferences => ReadAll(_db.NotificationPreferences);
    public IReadOnlyList<MedNotification> Notifications => ReadAll(_db.Notifications);
    public IReadOnlyList<NotificationDeliveryAttempt> NotificationDeliveryAttempts => ReadAll(_db.NotificationDeliveryAttempts);

    // === Ghi dữ liệu — ghi trực tiếp vào SQL Server ===
    public void AddDepartment(Department dept)
    {
        var normalized = NormalizeDepartment(dept);
        ValidateDepartmentUniqueCode(normalized.DepartmentId, normalized.Code);
        ValidateDepartmentParent(normalized.DepartmentId, normalized.ParentDepartmentId);
        _db.Departments.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateDepartment(Department dept)
    {
        var existing = _db.Departments.FirstOrDefault(d => d.DepartmentId == dept.DepartmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var normalized = NormalizeDepartment(dept);
        ValidateDepartmentUniqueCode(normalized.DepartmentId, normalized.Code);
        ValidateDepartmentParent(normalized.DepartmentId, normalized.ParentDepartmentId);
        _db.Departments.Entry(existing).CurrentValues.SetValues(normalized with { UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateDepartmentParent(Guid departmentId, Guid? newParentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        ValidateDepartmentParent(departmentId, newParentId);
        var updated = dept with { ParentDepartmentId = newParentId, UpdatedAt = DateTime.UtcNow };
        _db.Departments.Entry(dept).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveDepartment(Guid departmentId)
    {
        var dept = _db.Departments.FirstOrDefault(d => d.DepartmentId == departmentId)
            ?? throw new InvalidOperationException("Khoa/phòng không tồn tại.");
        var hasActiveChildren = _db.Departments.Any(d => d.ParentDepartmentId == departmentId && d.Status == "active");
        if (hasActiveChildren)
        {
            throw MedDomainException.Constraint("CK_departments_archive_children", 50021,
                "Không thể lưu trữ khoa/phòng còn đơn vị con đang hoạt động.");
        }
        var updated = dept with { Status = "archived", UpdatedAt = DateTime.UtcNow };
        _db.Departments.Entry(dept).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddUser(AppUser user) { _db.Users.Add(user); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateUser(AppUser user)
    {
        var existing = _db.Users.FirstOrDefault(u => u.UserId == user.UserId);
        if (existing is not null) _db.Users.Entry(existing).CurrentValues.SetValues(user);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddRole(Role role)
    {
        var normalized = NormalizeRole(role);
        ValidateRoleUniqueCode(normalized.RoleId, normalized.Code);
        _db.Roles.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateRole(Role role)
    {
        var existing = _db.Roles.FirstOrDefault(r => r.RoleId == role.RoleId)
            ?? throw new InvalidOperationException("Vai trò không tồn tại.");
        var normalized = NormalizeRole(role);
        ValidateRoleUniqueCode(normalized.RoleId, normalized.Code);
        _db.Roles.Entry(existing).CurrentValues.SetValues(normalized with
        {
            IsSystem = existing.IsSystem,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveRole(Guid roleId)
    {
        var existing = _db.Roles.FirstOrDefault(r => r.RoleId == roleId)
            ?? throw new InvalidOperationException("Vai trò không tồn tại.");
        if (existing.IsSystem)
            throw new InvalidOperationException("Không thể lưu trữ vai trò hệ thống.");

        _db.Roles.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        var activeAssignments = _db.UserRoles.Where(ur => ur.RoleId == roleId && ur.EffectiveTo == null).ToList();
        foreach (var assignment in activeAssignments)
        {
            _db.UserRoles.Entry(assignment).CurrentValues.SetValues(assignment with { EffectiveTo = DateTime.UtcNow });
        }
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddGroup(Group group)
    {
        var normalized = NormalizeGroup(group);
        ValidateGroupUniqueCode(normalized.GroupId, normalized.Code);
        ValidateGroupDepartment(normalized.DepartmentId);
        _db.Groups.Add(normalized);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void ArchiveGroup(Guid groupId)
    {
        var existing = _db.Groups.FirstOrDefault(g => g.GroupId == groupId)
            ?? throw new InvalidOperationException("Nhom khong ton tai.");
        var now = DateTime.UtcNow;
        _db.Groups.Entry(existing).CurrentValues.SetValues(existing with
        {
            Status = "archived",
            UpdatedAt = now
        });
        var activeMemberships = _db.UserGroupMembers.Where(m => m.GroupId == groupId && m.EffectiveTo == null).ToList();
        foreach (var membership in activeMemberships)
        {
            _db.UserGroupMembers.Entry(membership).CurrentValues.SetValues(membership with { EffectiveTo = now });
        }
        var activePermissions = _db.GroupPermissions.Where(p => p.GroupId == groupId && p.EffectiveTo == null).ToList();
        foreach (var permission in activePermissions)
        {
            _db.GroupPermissions.Entry(permission).CurrentValues.SetValues(permission with { EffectiveTo = now });
        }
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddUserRole(UserRole userRole) { _db.UserRoles.Add(userRole); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddUserGroupMember(UserGroupMember member)
    {
        EnsureActiveGroup(member.GroupId);
        _db.UserGroupMembers.Add(member);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveUserRole(Guid userRoleId)
    {
        var existing = _db.UserRoles.FirstOrDefault(r => r.UserRoleId == userRoleId)
            ?? throw new InvalidOperationException("Gán vai trò không tồn tại.");
        _db.UserRoles.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveUserGroupMember(Guid membershipId)
    {
        var existing = _db.UserGroupMembers.FirstOrDefault(m => m.UserGroupMemberId == membershipId)
            ?? throw new InvalidOperationException("Thành viên nhóm không tồn tại.");
        EnsureActiveGroup(existing.GroupId);
        _db.UserGroupMembers.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddScreen(ScreenCatalog screen) { _db.Screens.Add(screen); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddFeature(FeatureCatalog feature) { _db.Features.Add(feature); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPermission(MedPermission permission) { _db.Permissions.Add(permission); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddRolePermission(RolePermission rp) { _db.RolePermissions.Add(rp); _db.SaveChanges(); RaiseStateChanged(); }
    public void RemoveRolePermission(Guid rolePermissionId)
    {
        var existing = _db.RolePermissions.FirstOrDefault(p => p.RolePermissionId == rolePermissionId)
            ?? throw new InvalidOperationException("Quyền vai trò không tồn tại.");
        _db.RolePermissions.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddGroupPermission(GroupPermission gp)
    {
        EnsureActiveGroup(gp.GroupId);
        _db.GroupPermissions.Add(gp);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddUserPermissionOverride(UserPermissionOverride upo) { _db.UserPermissionOverrides.Add(upo); _db.SaveChanges(); RaiseStateChanged(); }
    public void RemoveGroupPermission(Guid groupPermissionId)
    {
        var existing = _db.GroupPermissions.FirstOrDefault(p => p.GroupPermissionId == groupPermissionId)
            ?? throw new InvalidOperationException("Quyền nhóm không tồn tại.");
        EnsureActiveGroup(existing.GroupId);
        _db.GroupPermissions.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateUserPermissionOverride(UserPermissionOverride upo)
    {
        var existing = _db.UserPermissionOverrides.FirstOrDefault(p => p.UserPermissionOverrideId == upo.UserPermissionOverrideId)
            ?? throw new InvalidOperationException("Ghi đè quyền không tồn tại.");
        _db.UserPermissionOverrides.Entry(existing).CurrentValues.SetValues(upo);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveUserPermissionOverride(Guid userPermissionOverrideId)
    {
        var existing = _db.UserPermissionOverrides.FirstOrDefault(p => p.UserPermissionOverrideId == userPermissionOverrideId)
            ?? throw new InvalidOperationException("Ghi đè quyền không tồn tại.");
        _db.UserPermissionOverrides.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AppendAudit(AuditLog log) { _db.AuditLogs.Add(log); _db.SaveChanges(); }

    private Group EnsureActiveGroup(Guid groupId)
    {
        var group = _db.Groups.FirstOrDefault(g => g.GroupId == groupId)
            ?? throw MedDomainException.Constraint("PK_groups", 547, "Nhom khong ton tai.");
        if (group.Status != "active")
        {
            throw MedDomainException.Constraint(
                "CK_groups_active_mutation",
                50022,
                "Nhom da luu tru khong cho phep thay doi thanh vien/quyen.");
        }

        return group;
    }

    public void AddPermissionChangeRequest(PermissionChangeRequest req) { _db.PermissionChangeRequests.Add(req); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdatePermissionChangeRequest(PermissionChangeRequest updated)
    {
        var existing = _db.PermissionChangeRequests.FirstOrDefault(r => r.PermissionChangeRequestId == updated.PermissionChangeRequestId);
        if (existing is not null) _db.PermissionChangeRequests.Entry(existing).CurrentValues.SetValues(updated);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddPermissionChangeItem(PermissionChangeItem item) { _db.PermissionChangeItems.Add(item); _db.SaveChanges(); RaiseStateChanged(); }

    public void AddProcedure(ProfessionalProcedure proc) { _db.Procedures.Add(proc); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateProcedure(ProfessionalProcedure proc)
    {
        _db.ChangeTracker.Clear();
        var affected = _db.Procedures
            .Where(p => p.ProcedureId == proc.ProcedureId)
            .ExecuteUpdate(setters => setters
                .SetProperty(p => p.Name, proc.Name)
                .SetProperty(p => p.ProcedureType, proc.ProcedureType)
                .SetProperty(p => p.OwnerDepartmentId, proc.OwnerDepartmentId)
                .SetProperty(p => p.Description, proc.Description)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
        if (affected == 0)
            throw new InvalidOperationException("Quy trình không tồn tại.");
        RaiseStateChanged();
    }

    public void AddProcedureVersion(ProcedureVersion ver) { _db.ChangeTracker.Clear(); _db.ProcedureVersions.Add(ver); _db.SaveChanges(); _db.ChangeTracker.Clear(); RaiseStateChanged(); }
    public void UpdateProcedureVersion(ProcedureVersion updated)
    {
        _db.ChangeTracker.Clear();
        var affected = _db.ProcedureVersions
            .Where(v => v.ProcedureVersionId == updated.ProcedureVersionId)
            .ExecuteUpdate(setters => setters
                .SetProperty(v => v.DepartmentId, updated.DepartmentId)
                .SetProperty(v => v.Title, updated.Title)
                .SetProperty(v => v.Summary, updated.Summary)
                .SetProperty(v => v.ChangeReason, updated.ChangeReason)
                .SetProperty(v => v.EffectiveFrom, updated.EffectiveFrom)
                .SetProperty(v => v.EffectiveTo, updated.EffectiveTo)
                .SetProperty(v => v.IssueDate, updated.IssueDate)
                .SetProperty(v => v.IssueNumber, updated.IssueNumber)
                .SetProperty(v => v.SourcePdfFileName, updated.SourcePdfFileName)
                .SetProperty(v => v.SourcePdfChecksumSha256, updated.SourcePdfChecksumSha256)
                .SetProperty(v => v.StatusCode, updated.StatusCode)
                .SetProperty(v => v.SubmittedBy, updated.SubmittedBy)
                .SetProperty(v => v.SubmittedAt, updated.SubmittedAt)
                .SetProperty(v => v.ApprovedBy, updated.ApprovedBy)
                .SetProperty(v => v.ApprovedAt, updated.ApprovedAt)
                .SetProperty(v => v.PublishedBy, updated.PublishedBy)
                .SetProperty(v => v.PublishedAt, updated.PublishedAt)
                .SetProperty(v => v.RequiredWriterSignatures, updated.RequiredWriterSignatures));
        if (affected == 0)
            throw new InvalidOperationException("Phiên bản quy trình không tồn tại.");
        RaiseStateChanged();
    }
    public void AddProcedureStep(ProcedureStep step) { _db.ProcedureSteps.Add(step); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureAttachment(ProcedureAttachment att) { _db.ProcedureAttachments.Add(att); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureScreenMapping(ProcedureScreenMapping mapping) { _db.ProcedureScreenMappings.Add(mapping); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureDocumentSection(ProcedureDocumentSection section) { _db.ProcedureDocumentSections.Add(section); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateProcedureDocumentSection(ProcedureDocumentSection section)
    {
        var existing = _db.ProcedureDocumentSections.FirstOrDefault(s => s.ProcedureDocumentSectionId == section.ProcedureDocumentSectionId)
            ?? throw new InvalidOperationException("Muc tai lieu quy trinh khong ton tai.");
        _db.ProcedureDocumentSections.Entry(existing).CurrentValues.SetValues(section);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void AddProcedureDistributionRecipient(ProcedureDistributionRecipient recipient) { _db.ProcedureDistributionRecipients.Add(recipient); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureRevisionEntry(ProcedureRevisionEntry revision) { _db.ProcedureRevisionEntries.Add(revision); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureSignoffRecord(ProcedureSignoffRecord signoff)
    {
        _db.ChangeTracker.Clear();
        _db.ProcedureSignoffRecords.Add(signoff);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        RaiseStateChanged();
    }
    public void AddProcedureVersionAuthorAssignment(ProcedureVersionAuthorAssignment assignment) { _db.ProcedureVersionAuthorAssignments.Add(assignment); _db.SaveChanges(); RaiseStateChanged(); }
    public void ClearProcedureVersionDocument(Guid versionId)
    {
        _db.ChangeTracker.Clear();
        var stepIds = _db.ProcedureSteps
            .Where(item => item.ProcedureVersionId == versionId)
            .Select(item => item.ProcedureStepId)
            .ToList();
        if (stepIds.Count > 0)
        {
            _db.ProcedureStepAttachmentAssignments.RemoveRange(
                _db.ProcedureStepAttachmentAssignments.Where(item => stepIds.Contains(item.ProcedureStepId)));
            _db.ProcedureStepRoleAssignments.RemoveRange(
                _db.ProcedureStepRoleAssignments.Where(item => stepIds.Contains(item.ProcedureStepId)));
            _db.ProcedureStepLocationAssignments.RemoveRange(
                _db.ProcedureStepLocationAssignments.Where(item => stepIds.Contains(item.ProcedureStepId)));
            _db.ProcedureSteps.RemoveRange(_db.ProcedureSteps.Where(item => item.ProcedureVersionId == versionId));
        }

        _db.ProcedureDocumentSections.RemoveRange(_db.ProcedureDocumentSections.Where(item => item.ProcedureVersionId == versionId));
        _db.ProcedureDistributionRecipients.RemoveRange(_db.ProcedureDistributionRecipients.Where(item => item.ProcedureVersionId == versionId));
        _db.ProcedureRevisionEntries.RemoveRange(_db.ProcedureRevisionEntries.Where(item => item.ProcedureVersionId == versionId));
        _db.ProcedureVersionAuthorAssignments.RemoveRange(_db.ProcedureVersionAuthorAssignments.Where(item => item.ProcedureVersionId == versionId));
        _db.ProcedureAttachments.RemoveRange(_db.ProcedureAttachments.Where(item => item.ProcedureVersionId == versionId));
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        RaiseStateChanged();
    }
    public void AddProcedureStepRoleAssignment(ProcedureStepRoleAssignment assignment) { _db.ProcedureStepRoleAssignments.Add(assignment); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureStepLocationAssignment(ProcedureStepLocationAssignment assignment) { _db.ProcedureStepLocationAssignments.Add(assignment); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureStepAttachmentAssignment(ProcedureStepAttachmentAssignment assignment) { _db.ProcedureStepAttachmentAssignments.Add(assignment); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureVersionSnapshot(ProcedureVersionSnapshotRecord snapshot)
    {
        _db.ChangeTracker.Clear();
        _db.ProcedureVersionSnapshots.Add(snapshot);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        RaiseStateChanged();
    }
    public void AddOrUpdateProcedureVersionDiff(ProcedureVersionDiffRecord diff)
    {
        _db.ChangeTracker.Clear();
        var existing = _db.ProcedureVersionDiffRecords.FirstOrDefault(item => item.FromVersionId == diff.FromVersionId && item.ToVersionId == diff.ToVersionId);
        if (existing is null)
        {
            _db.ProcedureVersionDiffRecords.Add(diff);
        }
        else
        {
            _db.ProcedureVersionDiffRecords.Entry(existing).CurrentValues.SetValues(diff);
        }
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        RaiseStateChanged();
    }
    public void RemoveProcedureAttachment(Guid attachmentId)
    {
        var existing = _db.ProcedureAttachments.FirstOrDefault(a => a.ProcedureAttachmentId == attachmentId)
            ?? throw new InvalidOperationException("Tệp đính kèm không tồn tại.");
        _db.ProcedureAttachments.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProcedureScreenMapping(Guid mappingId)
    {
        var existing = _db.ProcedureScreenMappings.FirstOrDefault(m => m.ProcedureScreenMappingId == mappingId)
            ?? throw new InvalidOperationException("Ánh xạ màn hình không tồn tại.");
        _db.ProcedureScreenMappings.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddPatientRef(PatientRef patient) { _db.PatientRefs.Add(patient); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddEncounterRef(EncounterRef encounter) { _db.EncounterRefs.Add(encounter); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdatePatientRef(PatientRef patient)
    {
        var existing = _db.PatientRefs.FirstOrDefault(p => p.PatientRefId == patient.PatientRefId)
            ?? throw new InvalidOperationException("Bệnh nhân không tồn tại.");
        _db.PatientRefs.Entry(existing).CurrentValues.SetValues(patient);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateEncounterRef(EncounterRef encounter)
    {
        var existing = _db.EncounterRefs.FirstOrDefault(e => e.EncounterRefId == encounter.EncounterRefId)
            ?? throw new InvalidOperationException("Lượt khám không tồn tại.");
        _db.EncounterRefs.Entry(existing).CurrentValues.SetValues(encounter);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddTechnicalService(TechnicalService svc) { _db.TechnicalServices.Add(svc); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceCatalogItem(ResourceCatalogItem item) { _db.ResourceCatalog.Add(item); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalResourceNorm(TechnicalResourceNorm norm) { _db.TechnicalResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProcedureVersionResourceNorm(ProcedureVersionResourceNorm norm) { _db.ProcedureVersionResourceNorms.Add(norm); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddTechnicalOrder(TechnicalOrder order) { _db.TechnicalOrders.Add(order); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddResourceAvailabilitySnapshot(ResourceAvailabilitySnapshot snap) { _db.ResourceAvailabilitySnapshots.Add(snap); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddActualResourceUsage(ActualResourceUsage usage) { _db.ActualResourceUsages.Add(usage); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateTechnicalService(TechnicalService svc)
    {
        var existing = _db.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == svc.TechnicalServiceId)
            ?? throw new InvalidOperationException("Dịch vụ kỹ thuật không tồn tại.");
        _db.TechnicalServices.Entry(existing).CurrentValues.SetValues(svc);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveTechnicalService(Guid technicalServiceId)
    {
        var existing = _db.TechnicalServices.FirstOrDefault(s => s.TechnicalServiceId == technicalServiceId)
            ?? throw new InvalidOperationException("Dịch vụ kỹ thuật không tồn tại.");
        _db.TechnicalServices.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateResourceCatalogItem(ResourceCatalogItem item)
    {
        var existing = _db.ResourceCatalog.FirstOrDefault(r => r.ResourceId == item.ResourceId)
            ?? throw new InvalidOperationException("Tài nguyên không tồn tại.");
        _db.ResourceCatalog.Entry(existing).CurrentValues.SetValues(item);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveResourceCatalogItem(Guid resourceId)
    {
        var existing = _db.ResourceCatalog.FirstOrDefault(r => r.ResourceId == resourceId)
            ?? throw new InvalidOperationException("Tài nguyên không tồn tại.");
        _db.ResourceCatalog.Entry(existing).CurrentValues.SetValues(existing with { Status = "archived", UpdatedAt = DateTime.UtcNow });
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveTechnicalResourceNorm(Guid normId)
    {
        var existing = _db.TechnicalResourceNorms.FirstOrDefault(n => n.TechnicalResourceNormId == normId)
            ?? throw new InvalidOperationException("Định mức dịch vụ không tồn tại.");
        _db.TechnicalResourceNorms.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProcedureVersionResourceNorm(Guid normId)
    {
        var existing = _db.ProcedureVersionResourceNorms.FirstOrDefault(n => n.ProcedureVersionResourceNormId == normId)
            ?? throw new InvalidOperationException("Định mức phiên bản quy trình không tồn tại.");
        _db.ProcedureVersionResourceNorms.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateTechnicalOrder(TechnicalOrder order)
    {
        var existing = _db.TechnicalOrders.FirstOrDefault(o => o.TechnicalOrderId == order.TechnicalOrderId)
            ?? throw new InvalidOperationException("Chỉ định kỹ thuật không tồn tại.");
        _db.TechnicalOrders.Entry(existing).CurrentValues.SetValues(order);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveActualResourceUsage(Guid usageId)
    {
        var existing = _db.ActualResourceUsages.FirstOrDefault(u => u.ActualResourceUsageId == usageId)
            ?? throw new InvalidOperationException("Ghi nhận sử dụng thực tế không tồn tại.");
        _db.ActualResourceUsages.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddClinicalProtocol(ClinicalProtocol protocol) { _db.ClinicalProtocols.Add(protocol); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateClinicalProtocol(ClinicalProtocol protocol)
    {
        var existing = _db.ClinicalProtocols.FirstOrDefault(p => p.ClinicalProtocolId == protocol.ClinicalProtocolId)
            ?? throw new InvalidOperationException("Phác đồ không tồn tại.");
        _db.ClinicalProtocols.Entry(existing).CurrentValues.SetValues(protocol with
        {
            CreatedAt = existing.CreatedAt,
            CreatedBy = existing.CreatedBy,
            UpdatedAt = DateTime.UtcNow
        });
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddClinicalProtocolVersion(ClinicalProtocolVersion ver) { _db.ClinicalProtocolVersions.Add(ver); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddClinicalProtocolProcedure(ClinicalProtocolProcedure cpp) { _db.ClinicalProtocolProcedures.Add(cpp); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddProtocolApplicabilityRule(ProtocolApplicabilityRule rule) { _db.ProtocolApplicabilityRules.Add(rule); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddPatientProtocolApplication(PatientProtocolApplication app)
    {
        ValidateManualProtocolApplicationStatus(app.ApplicationStatus);
        _db.PatientProtocolApplications.Add(app);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdatePatientProtocolApplication(PatientProtocolApplication app)
    {
        ValidateManualProtocolApplicationStatus(app.ApplicationStatus);
        var existing = _db.PatientProtocolApplications.FirstOrDefault(a => a.PatientProtocolApplicationId == app.PatientProtocolApplicationId)
            ?? throw new InvalidOperationException("Áp dụng phác đồ không tồn tại.");
        _db.PatientProtocolApplications.Entry(existing).CurrentValues.SetValues(app);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateClinicalProtocolVersion(ClinicalProtocolVersion ver)
    {
        var existing = _db.ClinicalProtocolVersions.FirstOrDefault(v => v.ClinicalProtocolVersionId == ver.ClinicalProtocolVersionId)
            ?? throw new InvalidOperationException("Phiên bản phác đồ không tồn tại.");
        _db.ClinicalProtocolVersions.Entry(existing).CurrentValues.SetValues(ver);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveClinicalProtocolProcedure(Guid clinicalProtocolProcedureId)
    {
        var existing = _db.ClinicalProtocolProcedures.FirstOrDefault(p => p.ClinicalProtocolProcedureId == clinicalProtocolProcedureId)
            ?? throw new InvalidOperationException("Liên kết phác đồ - quy trình không tồn tại.");
        _db.ClinicalProtocolProcedures.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveProtocolApplicabilityRule(Guid ruleId)
    {
        var existing = _db.ProtocolApplicabilityRules.FirstOrDefault(r => r.ProtocolApplicabilityRuleId == ruleId)
            ?? throw new InvalidOperationException("Quy tắc áp dụng không tồn tại.");
        _db.ProtocolApplicabilityRules.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }

    public void AddSignatureRecord(SignatureRecord signature) { _db.SignatureRecords.Add(signature); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotificationPreference(NotificationPreference pref) { _db.NotificationPreferences.Add(pref); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotification(MedNotification notification) { _db.Notifications.Add(notification); _db.SaveChanges(); RaiseStateChanged(); }
    public void AddNotificationDeliveryAttempt(NotificationDeliveryAttempt attempt) { _db.NotificationDeliveryAttempts.Add(attempt); _db.SaveChanges(); RaiseStateChanged(); }
    public void UpdateNotificationPreference(NotificationPreference pref)
    {
        var existing = _db.NotificationPreferences.FirstOrDefault(p => p.NotificationPreferenceId == pref.NotificationPreferenceId)
            ?? throw new InvalidOperationException("Cài đặt thông báo không tồn tại.");
        _db.NotificationPreferences.Entry(existing).CurrentValues.SetValues(pref);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void RemoveNotificationPreference(Guid prefId)
    {
        var existing = _db.NotificationPreferences.FirstOrDefault(p => p.NotificationPreferenceId == prefId)
            ?? throw new InvalidOperationException("Cài đặt thông báo không tồn tại.");
        _db.NotificationPreferences.Remove(existing);
        _db.SaveChanges();
        RaiseStateChanged();
    }
    public void UpdateNotificationReadAt(Guid notificationId, DateTime readAt)
    {
        var existing = _db.Notifications.FirstOrDefault(n => n.NotificationId == notificationId)
            ?? throw new InvalidOperationException("Thông báo không tồn tại.");
        _db.Notifications.Entry(existing).CurrentValues.SetValues(existing with { ReadAt = readAt });
        _db.SaveChanges();
        RaiseStateChanged();
    }

    private Department NormalizeDepartment(Department dept)
        => dept with
        {
            Code = dept.Code.Trim().ToUpperInvariant(),
            Name = dept.Name.Trim(),
            ParentDepartmentId = dept.ParentDepartmentId == Guid.Empty ? null : dept.ParentDepartmentId
        };

    private void ValidateDepartmentUniqueCode(Guid departmentId, string code)
    {
        if (_db.Departments.Any(d => d.DepartmentId != departmentId && d.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_departments_code", 2627,
                "Mã khoa/phòng đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private void ValidateDepartmentParent(Guid departmentId, Guid? parentDepartmentId)
    {
        if (parentDepartmentId is null)
        {
            return;
        }

        if (parentDepartmentId == departmentId)
        {
            throw MedDomainException.Constraint("CK_departments_parent_not_self", 50020,
                "Khoa/phòng không thể là đơn vị cha của chính nó.");
        }

        var parentExists = _db.Departments.Any(d => d.DepartmentId == parentDepartmentId && d.Status == "active");
        if (!parentExists)
        {
            throw MedDomainException.Constraint("FK_departments_parent", 547,
                "Đơn vị cha không tồn tại hoặc đã lưu trữ.");
        }

        var parentIsDescendant = _db.DepartmentClosure.Any(e =>
            e.AncestorDepartmentId == departmentId &&
            e.DescendantDepartmentId == parentDepartmentId);
        if (parentIsDescendant)
        {
            throw MedDomainException.Constraint("TR_department_closure_cycle", 50020,
                "Không thể chuyển vào đơn vị con vì sẽ tạo vòng lặp tổ chức.");
        }
    }

    private static Role NormalizeRole(Role role)
        => role with
        {
            Code = role.Code.Trim().ToUpperInvariant(),
            Name = role.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(role.Description) ? null : role.Description.Trim()
        };

    private void ValidateRoleUniqueCode(Guid roleId, string code)
    {
        if (_db.Roles.Any(r => r.RoleId != roleId && r.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_roles_code", 2627,
                "Mã vai trò đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private static Group NormalizeGroup(Group group)
        => group with
        {
            Code = group.Code.Trim().ToUpperInvariant(),
            Name = group.Name.Trim(),
            DepartmentId = group.DepartmentId == Guid.Empty ? null : group.DepartmentId,
            Description = string.IsNullOrWhiteSpace(group.Description) ? null : group.Description.Trim()
        };

    private void ValidateGroupUniqueCode(Guid groupId, string code)
    {
        if (_db.Groups.Any(g => g.GroupId != groupId && g.Code.ToUpper() == code.ToUpper()))
        {
            throw MedDomainException.Constraint("UQ_groups_code", 2627,
                "Mã nhóm đã tồn tại. Vui lòng dùng mã khác.");
        }
    }

    private void ValidateGroupDepartment(Guid? departmentId)
    {
        if (departmentId is null)
        {
            return;
        }

        var departmentExists = _db.Departments.Any(d => d.DepartmentId == departmentId && d.Status == "active");
        if (!departmentExists)
        {
            throw MedDomainException.Constraint("FK_groups_department", 547,
                "Khoa/phòng của nhóm không tồn tại hoặc đã lưu trữ.");
        }
    }

    private static void ValidateManualProtocolApplicationStatus(string status)
    {
        if (string.Equals(status, "signed", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "revoked", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Trang thai ky va thu hoi chi duoc cap nhat qua quy trinh chu ky.");
        }
    }
}
