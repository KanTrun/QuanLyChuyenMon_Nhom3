namespace TelemedicineLandingPage.Models.Admin;

/// <summary>Hospital department / specialty enumeration shared across modules.</summary>
public enum Department
{
    TimMach,
    NoiTiet,
    NhiKhoa,
    NgoaiTongQuat,
    SanPhuKhoa,
    ChanDoanHinhAnh,
    XetNghiem,
    DuocLamSang,
    KhoVatTu,
    HanhChinh,
}

/// <summary>Type of resource consumed by a technical service (drug / supply / device / chemical).</summary>
public enum ResourceType
{
    Thuoc,
    VatTu,
    ThietBi,
    HoaChat,
}

/// <summary>Service classification used to group the technical service catalog.</summary>
public enum ServiceType
{
    KyThuat,
    XetNghiem,
    ChanDoanHinhAnh,
    PhauThuat,
    ThuThuat,
}

/// <summary>Lifecycle status applied to catalog items (services, protocols, etc.).</summary>
public enum CatalogStatus
{
    HoatDong,
    TamNgung,
    NgungSuDung,
}

/// <summary>One resource norm (định mức) attached to a technical service.</summary>
public sealed record ResourceNorm(
    ResourceType ResourceType,
    string ResourceCode,
    string ResourceName,
    string Unit,
    decimal StandardQuantity,
    string Note);

/// <summary>A technical service (kỹ thuật chuyên môn) with its associated resource norms.</summary>
public sealed record TechnicalServiceRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Code { get; init; }
    public required string Name { get; init; }
    public required ServiceType ServiceType { get; init; }
    public required Department Department { get; init; }
    public required CatalogStatus Status { get; init; }
    public IReadOnlyList<ResourceNorm> ResourceNorms { get; init; } = Array.Empty<ResourceNorm>();
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Filter applied to the technical service list page.</summary>
public sealed record CatalogFilter(
    string? Search = null,
    ServiceType? ServiceType = null,
    Department? Department = null,
    CatalogStatus? Status = null);
