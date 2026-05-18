namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Dịch vụ kỹ thuật.</summary>
public sealed record TechnicalService
{
    public Guid TechnicalServiceId { get; init; } = Guid.NewGuid();
    public required string ServiceCode { get; init; }
    public required string Name { get; init; }
    public required string ServiceType { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? LinkedProcedureId { get; init; }
    public string? Description { get; init; }
    public string Status { get; init; } = "active";
    public Guid? CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Mục trong danh mục nguồn lực.</summary>
public sealed record ResourceCatalogItem
{
    public Guid ResourceId { get; init; } = Guid.NewGuid();
    public required string ResourceType { get; init; }
    public required string ResourceCode { get; init; }
    public required string Name { get; init; }
    public string? DefaultUnitCode { get; init; }
    public string? ExternalSystemCode { get; init; }
    public string? ExternalResourceId { get; init; }
    public string Status { get; init; } = "active";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Định mức nguồn lực cho dịch vụ kỹ thuật.</summary>
public sealed record TechnicalResourceNorm
{
    public Guid TechnicalResourceNormId { get; init; } = Guid.NewGuid();
    public required Guid TechnicalServiceId { get; init; }
    public required Guid ResourceId { get; init; }
    public required decimal StandardQuantity { get; init; }
    public required string UnitCode { get; init; }
    public bool IsRequired { get; init; } = true;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Định mức nguồn lực theo phiên bản quy trình.</summary>
public sealed record ProcedureVersionResourceNorm
{
    public Guid ProcedureVersionResourceNormId { get; init; } = Guid.NewGuid();
    public required Guid ProcedureVersionId { get; init; }
    public required Guid ResourceId { get; init; }
    public required decimal StandardQuantity { get; init; }
    public required string UnitCode { get; init; }
    public bool IsRequired { get; init; } = true;
    public string? Note { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Phiếu chỉ định kỹ thuật.</summary>
public sealed record TechnicalOrder
{
    public Guid TechnicalOrderId { get; init; } = Guid.NewGuid();
    public required Guid TechnicalServiceId { get; init; }
    public Guid? ProcedureVersionId { get; init; }
    public Guid? PatientRefId { get; init; }
    public Guid? EncounterRefId { get; init; }
    public Guid? OrderingDepartmentId { get; init; }
    public Guid? OrderedBy { get; init; }
    public string OrderStatus { get; init; } = "ordered";
    public DateTime OrderedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; init; }
}

/// <summary>Ảnh chụp tình trạng sẵn có nguồn lực tại thời điểm kiểm tra.</summary>
public sealed record ResourceAvailabilitySnapshot
{
    public Guid ResourceAvailabilitySnapshotId { get; init; } = Guid.NewGuid();
    public required Guid TechnicalOrderId { get; init; }
    public required Guid ResourceId { get; init; }
    public required decimal RequiredQuantity { get; init; }
    public decimal? AvailableQuantity { get; init; }
    public required string UnitCode { get; init; }
    public required string AvailabilityStatus { get; init; }
    public string? ExternalPayloadJson { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Sử dụng nguồn lực thực tế.</summary>
public sealed record ActualResourceUsage
{
    public Guid ActualResourceUsageId { get; init; } = Guid.NewGuid();
    public required Guid TechnicalOrderId { get; init; }
    public required Guid ResourceId { get; init; }
    public required decimal ActualQuantity { get; init; }
    public required string UnitCode { get; init; }
    public string? VarianceReason { get; init; }
    public int RevisionNo { get; init; } = 1;
    public bool IsFinal { get; init; }
    public Guid? CapturedBy { get; init; }
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}
