using ContractManagement.Common.Enums;

namespace ContractManagement.Domains.Policies.ContractTemplate;

/// <summary>
/// Các quy tắc nghiệp vụ thuần của template.
///
/// Policy không truy cập database, HTTP, session hoặc file system.
/// Service sẽ lấy dữ liệu thực tế rồi truyền vào policy để kiểm tra.
/// </summary>
public static class ContractTemplatePolicy
{
    /// <summary>
    /// Xác định template tạo hợp đồng mới hay chỉ tạo chứng từ hỗ trợ.
    /// </summary>
    public static TemplateOutputKind GetOutputKind(
        TemplateDocumentType documentType)
    {
        return documentType switch
        {
            TemplateDocumentType.SoftwareSupplyContract
                => TemplateOutputKind.Contract,

            TemplateDocumentType.SoftwareMaintenanceContract
                => TemplateOutputKind.Contract,

            TemplateDocumentType.SoftwareUpkeepContract
                => TemplateOutputKind.Contract,

            TemplateDocumentType.Quotation
                => TemplateOutputKind.SupportingDocument,

            TemplateDocumentType.PaymentRequest
                => TemplateOutputKind.SupportingDocument,

            TemplateDocumentType.HandoverRecord
                => TemplateOutputKind.SupportingDocument,

            TemplateDocumentType.AcceptanceRecord
                => TemplateOutputKind.SupportingDocument,

            TemplateDocumentType.LiquidationRecord
                => TemplateOutputKind.SupportingDocument,

            /*
             * Other mặc định chỉ là chứng từ hỗ trợ.
             * Muốn một loại mới tạo được hợp đồng pháp lý,
             * developer phải khai báo rõ trong policy này.
             */
            TemplateDocumentType.Other
                => TemplateOutputKind.SupportingDocument,

            _ => throw new ArgumentOutOfRangeException(
                nameof(documentType),
                documentType,
                "TemplateDocumentType không hợp lệ.")
        };
    }

    public static bool CreatesContract(
        TemplateDocumentType documentType)
    {
        return GetOutputKind(documentType)
            == TemplateOutputKind.Contract;
    }

    public static bool CreatesSupportingDocument(
        TemplateDocumentType documentType)
    {
        return GetOutputKind(documentType)
            == TemplateOutputKind.SupportingDocument;
    }

    /// <summary>
    /// Kiểm tra state transition của template version.
    ///
    /// Draft -> Published
    /// Published -> Retired
    /// Retired là terminal.
    /// </summary>
    public static bool CanTransition(
        TemplateVersionStatus currentStatus,
        TemplateVersionStatus targetStatus)
    {
        return currentStatus switch
        {
            TemplateVersionStatus.Draft
                => targetStatus == TemplateVersionStatus.Published,

            TemplateVersionStatus.Published
                => targetStatus == TemplateVersionStatus.Retired,

            TemplateVersionStatus.Retired
                => false,

            _ => false
        };
    }

    public static void EnsureCanTransition(
        TemplateVersionStatus currentStatus,
        TemplateVersionStatus targetStatus)
    {
        if (!CanTransition(currentStatus, targetStatus))
        {
            throw new InvalidOperationException(
                $"Không thể chuyển template version " +
                $"từ {currentStatus} sang {targetStatus}.");
        }
    }

    /// <summary>
    /// Chỉ Draft version được chỉnh sửa.
    /// Published/Retired đều bất biến.
    /// </summary>
    public static bool CanEdit(
        TemplateVersionStatus status)
    {
        return status == TemplateVersionStatus.Draft;
    }

    public static void EnsureCanEdit(
        TemplateVersionStatus status)
    {
        if (!CanEdit(status))
        {
            throw new InvalidOperationException(
                $"Template version ở trạng thái {status} " +
                "không được phép chỉnh sửa.");
        }
    }

    /// <summary>
    /// Published hiện hành có thể làm nguồn cho Draft kế tiếp.
    /// Khi template không còn Published hiện hành, chỉ Retired mới nhất được
    /// dùng làm nguồn và chỉ khi chưa có Draft đang làm việc.
    /// Retired nguồn vẫn bất biến, không phải transition ngược về Draft.
    /// </summary>
    public static bool CanCreateDraftFromSource(
        TemplateVersionStatus sourceStatus,
        bool isCurrentPublished,
        bool hasCurrentPublished,
        bool isLatestRetired,
        bool hasExistingDraft)
    {
        if (sourceStatus == TemplateVersionStatus.Published)
        {
            return isCurrentPublished;
        }

        return sourceStatus == TemplateVersionStatus.Retired
            && !hasCurrentPublished
            && isLatestRetired
            && !hasExistingDraft;
    }

    /// <summary>
    /// Kiểm tra toàn bộ điều kiện dữ liệu để publish.
    ///
    /// CanTransition chỉ kiểm tra đường đi giữa hai trạng thái.
    /// CanPublish kiểm tra version đã có đủ dữ liệu thực tế hay chưa.
    /// </summary>
    public static bool CanPublish(
        TemplateVersionStatus currentStatus,
        TemplateValidationStatus validationStatus,
        int? documentFileId,
        string? documentHash)
    {
        return currentStatus == TemplateVersionStatus.Draft
            && validationStatus == TemplateValidationStatus.Valid
            && documentFileId is > 0
            && IsValidSha256Hash(documentHash);
    }

    public static void EnsureCanPublish(
        TemplateVersionStatus currentStatus,
        TemplateValidationStatus validationStatus,
        int? documentFileId,
        string? documentHash)
    {
        if (!CanPublish(
                currentStatus,
                validationStatus,
                documentFileId,
                documentHash))
        {
            throw new InvalidOperationException(
                "Template version chưa đủ điều kiện publish. " +
                "Version phải ở Draft, validation phải Valid, " +
                "đã có DOCX và SHA-256 hợp lệ.");
        }
    }

    /// <summary>
    /// Chỉ template còn active và version Published
    /// mới được chọn để tạo văn bản mới.
    ///
    /// Retired version vẫn được giữ để tra cứu lịch sử,
    /// nhưng không được chọn cho giao dịch mới.
    /// </summary>
    public static bool CanBeSelectedForNewDocument(
        bool isTemplateActive,
        TemplateVersionStatus versionStatus)
    {
        return isTemplateActive
            && versionStatus == TemplateVersionStatus.Published;
    }

    private static bool IsValidSha256Hash(string? documentHash)
    {
        if (string.IsNullOrWhiteSpace(documentHash)
            || documentHash.Length != 64)
        {
            return false;
        }

        return documentHash.All(Uri.IsHexDigit);
    }
}
