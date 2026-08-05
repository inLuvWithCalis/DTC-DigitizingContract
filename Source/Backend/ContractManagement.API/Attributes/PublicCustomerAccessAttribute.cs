namespace ContractManagement.Attributes;

/// <summary>
/// A public customer endpoint that still needs tenant resolution from tenantCode.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class PublicCustomerAccessAttribute : Attribute
{
}
