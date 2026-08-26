namespace ContractManagement.Domains.Interfaces.ContractTemplate;

/// <summary>
/// Converts an already-rendered DOCX to PDF. The converter has no database or
/// placeholder knowledge, so template preview and contract preview can share it.
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
