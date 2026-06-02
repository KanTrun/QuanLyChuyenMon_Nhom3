using System.Text.RegularExpressions;

namespace TelemedicineLandingPage.Services.Chatbot;

public interface IChatbotPrivacyGuard
{
    bool CanSend(string input, out string? localReply);
}

public sealed partial class ChatbotPrivacyGuard : IChatbotPrivacyGuard
{
    public const string BlockedUserMarker = "[Nội dung nhạy cảm đã được chặn cục bộ]";

    private const string Refusal =
        "Tôi không thể gửi nội dung có thể chứa dữ liệu bệnh nhân hoặc yêu cầu tư vấn y khoa tới API bên ngoài. " +
        "Vui lòng chỉ hỏi cách thao tác phần mềm bằng dữ liệu giả lập, không định danh.";

    private static readonly string[] MedicalAdviceKeywords =
    [
        "chan doan", "ke don", "lieu dung", "tu van dieu tri", "nen dung thuoc",
        "thuoc nao", "danh gia ca benh", "chi dinh dieu tri"
    ];

    private static readonly string[] PatientContextKeywords =
    [
        "benh nhan", "ho so benh an", "ma benh nhan", "ma luot kham"
    ];

    private static readonly string[] SensitiveDetailKeywords =
    [
        "cccd", "can cuoc", "so dien thoai", "ngay sinh", "dia chi", "email",
        "ho ten", "ten benh nhan", "ma benh nhan", "ma luot kham", "icd"
    ];

    public bool CanSend(string input, out string? localReply)
    {
        var normalized = QlcmChatbotKnowledgeCatalog.Normalize(input);
        var hasMedicalAdvice = MedicalAdviceKeywords.Any(normalized.Contains);
        var hasPatientDetails = PatientContextKeywords.Any(normalized.Contains) &&
            SensitiveDetailKeywords.Any(normalized.Contains);
        var hasDirectIdentifier = PatientCodeRegex().IsMatch(input) ||
            EmailRegex().IsMatch(input) ||
            PhoneRegex().IsMatch(input);

        if (hasMedicalAdvice || hasPatientDetails || hasDirectIdentifier)
        {
            localReply = Refusal;
            return false;
        }

        localReply = null;
        return true;
    }

    [GeneratedRegex(@"\b(?:BN|HSBA|BA|ENC|LK)[-_]?\d{3,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex PatientCodeRegex();

    [GeneratedRegex(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"(?<!\d)(?:\+?84|0)\d{9,10}(?!\d)")]
    private static partial Regex PhoneRegex();
}
