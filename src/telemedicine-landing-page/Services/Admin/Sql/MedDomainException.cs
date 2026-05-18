namespace TelemedicineLandingPage.Services.Admin.Sql;

/// <summary>
/// Ngoại lệ miền nghiệp vụ khi vi phạm ràng buộc SQL (constraint, trigger).
/// </summary>
public sealed class MedDomainException : InvalidOperationException
{
    public string? ConstraintName { get; }
    public int SqlErrorNumber { get; }

    public MedDomainException(string? constraintName, int sqlErrorNumber, string message)
        : base(message)
    {
        ConstraintName = constraintName;
        SqlErrorNumber = sqlErrorNumber;
    }

    /// <summary>Tạo ngoại lệ từ tên ràng buộc và mã lỗi SQL.</summary>
    public static MedDomainException Constraint(string name, int sqlErrorNumber, string message)
        => new(name, sqlErrorNumber, message);
}
