namespace ContractManagement.Common.Enums;

/// <summary>
/// Kết quả kiểm tra DOCX, placeholder và field mapping.
/// </summary>
public enum TemplateValidationStatus : byte
{
    NotValidated = 0,

    Valid = 1,

    Invalid = 2
}