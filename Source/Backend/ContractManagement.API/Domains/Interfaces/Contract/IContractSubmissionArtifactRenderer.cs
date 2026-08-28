namespace ContractManagement.Domains.Interfaces.Contract;

/// <summary>
/// Tạo đúng một bộ artifact pháp lý từ cùng snapshot của ContractVersion.
/// Kết quả chưa được persist; transaction submit chịu trách nhiệm lưu private,
/// bind metadata và chỉ sau đó mới khóa version.
/// </summary>
public interface IContractSubmissionArtifactRenderer
{
    Task<ContractSubmissionArtifactRenderResult> RenderAsync(
        int contractId,
        int employeeId,
        CancellationToken cancellationToken = default);
}

public sealed record ContractSubmissionArtifactRenderResult(
    string SnapshotJson,
    int SnapshotSchemaVersion,
    int TemplateVersionId,
    byte[] DocxContent,
    string DocxFileName,
    byte[] PdfContent,
    string PdfFileName);
