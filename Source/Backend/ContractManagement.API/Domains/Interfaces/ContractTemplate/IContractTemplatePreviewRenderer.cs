using ContractManagement.API.Common.Enums;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractTemplatePreviewRenderer
{
    byte[] Render(byte[] sourceDocumentBytes, ContractLanguageMode languageMode);

    byte[] RenderSample(byte[] sourceDocumentBytes, ContractLanguageMode languageMode,
        IReadOnlyList<SoftwareSupplyPlaceholderDefinition> definitions,
        IReadOnlyDictionary<string, string> customSamples,
        ContractTemplateAuthoringPreviewData? authoringData = null) =>
        customSamples.Count == 0 ? Render(sourceDocumentBytes, languageMode)
            : throw new NotSupportedException("Renderer chưa hỗ trợ placeholder tùy chỉnh.");

    byte[] Render(
        byte[] sourceDocumentBytes,
        ContractLanguageMode languageMode,
        ContractTemplateRenderData renderData) =>
        throw new NotSupportedException(
            "Renderer này chưa hỗ trợ dữ liệu hợp đồng động.");
}

/// <summary>
/// Lỗi nghiệp vụ khi một DOCX hợp lệ về catalog không có bố cục an toàn để preview.
/// FailureCode đồng thời là giá trị safelist dùng cho audit.
/// </summary>
public sealed class ContractTemplatePreviewException : InvalidOperationException
{
    public ContractTemplatePreviewException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
