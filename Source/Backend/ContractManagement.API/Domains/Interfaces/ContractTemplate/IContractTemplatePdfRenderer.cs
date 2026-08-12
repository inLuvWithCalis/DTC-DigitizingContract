namespace ContractManagement.Domains.Interfaces.ContractTemplate;

/// <summary>
/// Converts an already-rendered, fixed-dataset DOCX preview to its immutable
/// PDF counterpart. It never receives a real Contract document.
/// </summary>
public interface IContractTemplatePdfRenderer
{
    Task<byte[]> ConvertPreviewToPdfAsync(
        byte[] previewDocx,
        CancellationToken cancellationToken = default);
}

public sealed class ContractTemplatePdfRenderingException : Exception
{
    public ContractTemplatePdfRenderingException(string failureCode, string message)
        : base(message)
    {
        FailureCode = failureCode;
    }

    public string FailureCode { get; }
}
