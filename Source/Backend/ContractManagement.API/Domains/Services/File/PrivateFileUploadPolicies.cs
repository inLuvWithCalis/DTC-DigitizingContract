using ContractManagement.Domains.Interfaces.File;

namespace ContractManagement.Domains.Services.File;

public static class PrivateFileUploadPolicies
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] OleCompoundSignature =
        [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public static PrivateFileUploadPolicy SoftwareSupplyTemplateDocument(
        long maximumSizeBytes = 20 * 1024 * 1024)
    {
        return Create(
            [".docx"],
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"],
            maximumSizeBytes,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                [".docx"] = [ZipSignature]
            });
    }

    public static PrivateFileUploadPolicy ContractEvidence(
        long maximumSizeBytes = 20 * 1024 * 1024)
    {
        return Create(
            [".pdf", ".jpg", ".jpeg", ".png"],
            ["application/pdf", "image/jpeg", "image/png"],
            maximumSizeBytes,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = [PdfSignature],
                [".jpg"] = [JpegSignature],
                [".jpeg"] = [JpegSignature],
                [".png"] = [PngSignature]
            });
    }

    public static PrivateFileUploadPolicy ProfileImage(long maximumSizeBytes)
    {
        return Create(
            [".jpg", ".jpeg", ".png"],
            ["image/jpeg", "image/png"],
            maximumSizeBytes,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                [".jpg"] = [JpegSignature],
                [".jpeg"] = [JpegSignature],
                [".png"] = [PngSignature]
            });
    }

    public static PrivateFileUploadPolicy ContractAttachment(
        long maximumSizeBytes = 10 * 1024 * 1024)
    {
        return Create(
            [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg", ".zip"],
            [
                "application/pdf",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "image/png",
                "image/jpeg",
                "application/zip",
                "application/x-zip-compressed"
            ],
            maximumSizeBytes,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = [PdfSignature],
                [".doc"] = [OleCompoundSignature],
                [".docx"] = [ZipSignature],
                [".xls"] = [OleCompoundSignature],
                [".xlsx"] = [ZipSignature],
                [".jpg"] = [JpegSignature],
                [".jpeg"] = [JpegSignature],
                [".png"] = [PngSignature],
                [".zip"] = [ZipSignature]
            });
    }

    public static PrivateFileUploadPolicy SubmittedContractDocx(
        long maximumSizeBytes = 25 * 1024 * 1024) =>
        SoftwareSupplyTemplateDocument(maximumSizeBytes);

    public static PrivateFileUploadPolicy SubmittedContractPdf(
        long maximumSizeBytes = 25 * 1024 * 1024)
    {
        return Create(
            [".pdf"],
            ["application/pdf"],
            maximumSizeBytes,
            new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = [PdfSignature]
            });
    }

    private static PrivateFileUploadPolicy Create(
        IReadOnlyCollection<string> extensions,
        IReadOnlyCollection<string> contentTypes,
        long maximumSizeBytes,
        IReadOnlyDictionary<string, IReadOnlyList<byte[]>> signatures)
    {
        return new PrivateFileUploadPolicy(
            extensions,
            contentTypes,
            maximumSizeBytes,
            signatures);
    }
}
