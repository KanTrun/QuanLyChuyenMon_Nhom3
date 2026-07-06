using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

[Table("signature_records", Schema = "med")]
public sealed record SignatureRecord
{
    [Key]
    [Column("signature_record_id")]
    public Guid SignatureRecordId { get; init; } = Guid.NewGuid();

    [Column("target_type")]
    [MaxLength(64)]
    public required string TargetType { get; init; }

    [Column("target_id")]
    public required Guid TargetId { get; init; }

    [Column("signer_user_id")]
    public required Guid SignerUserId { get; init; }

    [Column("signer_username")]
    [MaxLength(256)]
    public string? SignerUsername { get; init; }

    [Column("provider_code")]
    [MaxLength(32)]
    public required string ProviderCode { get; init; }

    [Column("is_legally_valid")]
    public required bool IsLegallyValid { get; init; }

    [Column("signature_hash")]
    [MaxLength(128)]
    public required string SignatureHash { get; init; }

    [Column("signed_at")]
    public required DateTime SignedAt { get; init; }

    [Column("certificate_subject")]
    [MaxLength(512)]
    public string? CertificateSubject { get; init; }

    [Column("certificate_serial")]
    [MaxLength(256)]
    public string? CertificateSerial { get; init; }

    [Column("certificate_expiry")]
    public DateTime? CertificateExpiry { get; init; }

    [Column("metadata_json")]
    public string? MetadataJson { get; init; }

    [Column("correlation_id")]
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
}
