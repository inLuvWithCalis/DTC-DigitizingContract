namespace ContractManagement.API.Common.Enums;

public enum AcceptanceRecordStatus : byte
{
    Draft = 0,
    Finalized = 1,
    Signed = 2,
    Cancelled = 3
}

public enum AcceptanceKind : byte
{
    Partial = 1,
    Final = 2
}

public enum AcceptanceReferenceType : byte
{
    Contract = 1,
    Appendix = 2
}

public enum AcceptancePartyRole : byte
{
    Customer = 1,
    Tenant = 2
}
