using TelemedicineLandingPage.Data;
using TelemedicineLandingPage.Models.Admin.Sql;

namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Dịch vụ nhật ký kiểm toán bất biến (append-only).
/// Không cung cấp phương thức Update hoặc Delete.
/// Xác thực action_code trước khi ghi.
/// </summary>
public sealed class AuditTrailService
{
    private readonly MedDbContext _db;

    /// <summary>Danh sách mã hành động hợp lệ.</summary>
    private static readonly HashSet<string> ValidActionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "create", "update", "delete", "archive", "restore",
        "approve", "reject", "submit", "publish", "sign", "revoke", "rollback",
        "assign_role", "remove_role", "assign_permission", "remove_permission",
        "login", "logout", "switch_user",
        "create_order", "complete_order", "cancel_order",
        "apply_protocol", "skip_protocol",
        "return_to_draft", "revoke_signoff"
    };

    public AuditTrailService(MedDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Ghi một bản ghi kiểm toán. Chỉ cho phép append, không có update/delete.
    /// </summary>
    public void Append(AuditLog log)
    {
        if (string.IsNullOrWhiteSpace(log.ActionCode))
            throw new ArgumentException("Mã hành động (action_code) không được để trống.", nameof(log));

        if (!ValidActionCodes.Contains(log.ActionCode))
            throw new ArgumentException(
                $"Mã hành động '{log.ActionCode}' không hợp lệ. Các mã cho phép: {string.Join(", ", ValidActionCodes.Order())}.",
                nameof(log));

        _db.ChangeTracker.Clear();
        _db.AuditLogs.Add(log);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    /// <summary>Lấy toàn bộ nhật ký kiểm toán (chỉ đọc).</summary>
    public IReadOnlyList<AuditLog> GetAll() => _db.AuditLogs.OrderByDescending(a => a.OccurredAt).ToList();

    /// <summary>Lấy nhật ký theo mã hành động.</summary>
    public IReadOnlyList<AuditLog> GetByAction(string actionCode)
        => _db.AuditLogs.Where(a => a.ActionCode == actionCode).ToList();

    /// <summary>Lấy nhật ký theo người thực hiện.</summary>
    public IReadOnlyList<AuditLog> GetByActor(Guid actorUserId)
        => _db.AuditLogs.Where(a => a.ActorUserId == actorUserId).ToList();

    /// <summary>Lấy nhật ký theo đối tượng bị tác động.</summary>
    public IReadOnlyList<AuditLog> GetByTarget(string targetType, string? targetId = null)
    {
        var query = _db.AuditLogs.Where(a => a.TargetType == targetType);
        if (targetId is not null)
            query = query.Where(a => a.TargetId == targetId);
        return query.ToList();
    }

    /// <summary>Kiểm tra mã hành động có hợp lệ hay không.</summary>
    public static bool IsValidActionCode(string actionCode)
        => ValidActionCodes.Contains(actionCode);
}
