using ContractManagement.API.Common.Enums;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractTemplatePreviewRenderer
{
    byte[] Render(byte[] sourceDocumentBytes, ContractLanguageMode languageMode);

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
