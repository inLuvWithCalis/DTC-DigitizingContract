using ContractManagement.API.Common.Enums;

namespace ContractManagement.Domains.Interfaces.ContractTemplate;

public interface IContractTemplatePreviewRenderer
{
    byte[] Render(byte[] sourceDocumentBytes, ContractLanguageMode languageMode);
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
