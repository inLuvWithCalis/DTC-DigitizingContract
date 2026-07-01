using ContractManagement.Infrastructure.MultiTenancy.Interfaces;
using ContractManagement.Infrastructure.MultiTenancy.Models;

namespace ContractManagement.Infrastructure.MultiTenancy.Services;

/// <summary>
/// Giữ thông tin tenant của request hiện tại.
///
/// Service này bắt buộc phải đăng ký Scoped.
/// </summary>
public sealed class CurrentTenant : ICurrentTenant
{
    private ResolvedTenant? _tenant;

    public bool IsResolved => _tenant is not null;

    public ResolvedTenant? Value => _tenant;

    public void Set(ResolvedTenant tenant)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        /*
         * Không cho phép request đổi tenant giữa chừng.
         *
         * Ví dụ request đã là tenant DTC,
         * không được đổi thành tenant ABC.
         */
        if (_tenant is not null)
        {
            throw new InvalidOperationException(
                "Tenant của request đã được xác định.");
        }

        _tenant = tenant;
    }

    public ResolvedTenant GetRequiredTenant()
    {
        return _tenant
            ?? throw new InvalidOperationException(
                "Request chưa được xác định tenant.");
    }
}