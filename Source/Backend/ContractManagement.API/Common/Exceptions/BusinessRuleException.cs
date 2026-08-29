namespace ContractManagement.API.Common.Exceptions;

/// <summary>
/// Lỗi nghiệp vụ có mã ổn định để frontend xử lý theo ngữ cảnh.
/// </summary>
public sealed class BusinessRuleException : Exception
{
    public BusinessRuleException(int statusCode, string code, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }

    public int StatusCode { get; }

    public string Code { get; }
}
