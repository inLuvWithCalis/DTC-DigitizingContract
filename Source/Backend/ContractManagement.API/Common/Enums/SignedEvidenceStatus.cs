namespace ContractManagement.API.Common.Enums;

/// <summary>
/// Trạng thái của bản scan hợp đồng đã ký. Nội dung evidence không bị ghi đè;
/// bản cũ chỉ được đánh dấu Superseded khi có bản thay thế.
/// </summary>
public enum SignedEvidenceStatus : byte
{
    Active = 1,
    Superseded = 2
}
