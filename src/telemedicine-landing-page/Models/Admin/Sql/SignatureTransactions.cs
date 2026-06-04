using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TelemedicineLandingPage.Models.Admin.Sql;

[Table("signature_transactions", Schema = "med")]
public sealed record SignatureTransactionRecord
{
    [Key]
    [Column("signature_transaction_id")]
    public Guid SignatureTransactionId { get; init; } = Guid.NewGuid();

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

    [Column("environment_code")]
    [MaxLength(32)]
    public string EnvironmentCode { get; init; } = "sandbox";

    [Column("external_transaction_id")]
    [MaxLength(128)]
    public string? ExternalTransactionId { get; init; }

    [Column("external_transaction_code")]
    [MaxLength(128)]
    public string? ExternalTransactionCode { get; init; }

    [Column("document_id")]
    [MaxLength(128)]
    public required string DocumentId { get; init; }

    [Column("document_hash")]
    [MaxLength(128)]
    public required string DocumentHash { get; init; }

    [Column("ca_subscriber_id")]
    [MaxLength(128)]
    public string? CaSubscriberId { get; init; }

    [Column("requested_certificate_serial")]
    [MaxLength(256)]
    public string? RequestedCertificateSerial { get; init; }

    [Column("transaction_status")]
    [MaxLength(64)]
    public string TransactionStatus { get; init; } = "created";

    [Column("status_message")]
    [MaxLength(512)]
    public string? StatusMessage { get; init; }

    [Column("requested_at")]
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; init; }

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
