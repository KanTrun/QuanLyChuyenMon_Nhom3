using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

/// <summary>Dịch vụ kỹ thuật.</summary>
[Table("technical_services", Schema = "med")]
public sealed record TechnicalService
{
    [Key]
    [Column("technical_service_id")]
    public Guid TechnicalServiceId { get; init; } = Guid.NewGuid();

    [Column("service_code")]
    public required string ServiceCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("service_type")]
    public required string ServiceType { get; init; }

    [Column("department_id")]
    public Guid? DepartmentId { get; init; }

    [Column("linked_procedure_id")]
    public Guid? LinkedProcedureId { get; init; }

    [Column("description")]
    public string? Description { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_by")]
    public Guid? CreatedBy { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Mục trong danh mục nguồn lực.</summary>
[Table("resource_catalog", Schema = "med")]
public sealed record ResourceCatalogItem
{
    [Key]
    [Column("resource_id")]
    public Guid ResourceId { get; init; } = Guid.NewGuid();

    [Column("resource_type")]
    public required string ResourceType { get; init; }

    [Column("resource_code")]
    public required string ResourceCode { get; init; }

    [Column("name")]
    public required string Name { get; init; }

    [Column("default_unit_code")]
    public string? DefaultUnitCode { get; init; }

    [Column("external_system_code")]
    public string? ExternalSystemCode { get; init; }

    [Column("external_resource_id")]
    public string? ExternalResourceId { get; init; }

    [Column("status")]
    public string Status { get; init; } = "active";

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Định mức nguồn lực cho dịch vụ kỹ thuật.</summary>
[Table("technical_resource_norms", Schema = "med")]
public sealed record TechnicalResourceNorm
{
    [Key]
    [Column("technical_resource_norm_id")]
    public Guid TechnicalResourceNormId { get; init; } = Guid.NewGuid();

    [Column("technical_service_id")]
    public required Guid TechnicalServiceId { get; init; }

    [Column("resource_id")]
    public required Guid ResourceId { get; init; }

    [Column("standard_quantity")]
    public required decimal StandardQuantity { get; init; }

    [Column("unit_code")]
    public required string UnitCode { get; init; }

    [Column("is_required")]
    public bool IsRequired { get; init; } = true;

    [Column("note")]
    public string? Note { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Định mức nguồn lực theo phiên bản quy trình.</summary>
[Table("procedure_version_resource_norms", Schema = "med")]
public sealed record ProcedureVersionResourceNorm
{
    [Key]
    [Column("procedure_version_resource_norm_id")]
    public Guid ProcedureVersionResourceNormId { get; init; } = Guid.NewGuid();

    [Column("procedure_version_id")]
    public required Guid ProcedureVersionId { get; init; }

    [Column("resource_id")]
    public required Guid ResourceId { get; init; }

    [Column("standard_quantity")]
    public required decimal StandardQuantity { get; init; }

    [Column("unit_code")]
    public required string UnitCode { get; init; }

    [Column("is_required")]
    public bool IsRequired { get; init; } = true;

    [Column("note")]
    public string? Note { get; init; }

    [Column("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Phiếu chỉ định kỹ thuật.</summary>
[Table("technical_orders", Schema = "med")]
public sealed record TechnicalOrder
{
    [Key]
    [Column("technical_order_id")]
    public Guid TechnicalOrderId { get; init; } = Guid.NewGuid();

    [Column("technical_service_id")]
    public required Guid TechnicalServiceId { get; init; }

    [Column("procedure_version_id")]
    public Guid? ProcedureVersionId { get; init; }

    [Column("patient_ref_id")]
    public Guid? PatientRefId { get; init; }

    [Column("encounter_ref_id")]
    public Guid? EncounterRefId { get; init; }

    [Column("ordering_department_id")]
    public Guid? OrderingDepartmentId { get; init; }

    [Column("ordered_by")]
    public Guid? OrderedBy { get; init; }

    [Column("order_status")]
    public string OrderStatus { get; init; } = "ordered";

    [Column("ordered_at")]
    public DateTime OrderedAt { get; init; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; init; }
}

/// <summary>Ảnh chụp tình trạng sẵn có nguồn lực tại thời điểm kiểm tra.</summary>
[Table("resource_availability_snapshots", Schema = "med")]
public sealed record ResourceAvailabilitySnapshot
{
    [Key]
    [Column("resource_availability_snapshot_id")]
    public Guid ResourceAvailabilitySnapshotId { get; init; } = Guid.NewGuid();

    [Column("technical_order_id")]
    public required Guid TechnicalOrderId { get; init; }

    [Column("resource_id")]
    public required Guid ResourceId { get; init; }

    [Column("required_quantity")]
    public required decimal RequiredQuantity { get; init; }

    [Column("available_quantity")]
    public decimal? AvailableQuantity { get; init; }

    [Column("unit_code")]
    public required string UnitCode { get; init; }

    [Column("availability_status")]
    public required string AvailabilityStatus { get; init; }

    [Column("external_payload_json")]
    public string? ExternalPayloadJson { get; init; }

    [Column("checked_at")]
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
}

/// <summary>Sử dụng nguồn lực thực tế.</summary>
[Table("actual_resource_usages", Schema = "med")]
public sealed record ActualResourceUsage
{
    [Key]
    [Column("actual_resource_usage_id")]
    public Guid ActualResourceUsageId { get; init; } = Guid.NewGuid();

    [Column("technical_order_id")]
    public required Guid TechnicalOrderId { get; init; }

    [Column("resource_id")]
    public required Guid ResourceId { get; init; }

    [Column("actual_quantity")]
    public required decimal ActualQuantity { get; init; }

    [Column("unit_code")]
    public required string UnitCode { get; init; }

    [Column("variance_reason")]
    public string? VarianceReason { get; init; }

    [Column("revision_no")]
    public int RevisionNo { get; init; } = 1;

    [Column("is_final")]
    public bool IsFinal { get; init; }

    [Column("captured_by")]
    public Guid? CapturedBy { get; init; }

    [Column("captured_at")]
    public DateTime CapturedAt { get; init; } = DateTime.UtcNow;
}
