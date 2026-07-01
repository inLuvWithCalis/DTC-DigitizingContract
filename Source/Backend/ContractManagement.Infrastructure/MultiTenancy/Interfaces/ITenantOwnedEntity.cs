namespace ContractManagement.Infrastructure.MultiTenancy.Interfaces;

/// <summary>
/// Entity implement interface này là dữ liệu thuộc về một tenant.
/// 
/// Khi dùng Shared Database, các bảng này bắt buộc phải có TenantId
/// để phân biệt dữ liệu của từng tenant.
/// </summary>
public interface ITenantOwnedEntity
{
    int TenantId { get; set; }
}