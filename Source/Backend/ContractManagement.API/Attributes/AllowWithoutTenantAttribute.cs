namespace ContractManagement.Attributes;

/// <summary>
/// Controller hoặc Action có attribute này
/// không bắt buộc phải resolve tenant.
///
/// Ví dụ:
/// - Tạo tenant
/// - Health check
/// </summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false)]
public sealed class AllowWithoutTenantAttribute : Attribute
{
}