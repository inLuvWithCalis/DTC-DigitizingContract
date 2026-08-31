namespace ContractManagement.API.Common.Enums;

public enum ContractNegotiationCommentState : byte
{
    Open = 0,
    Resolved = 1
}

public enum ContractNegotiationCommentEventType : byte
{
    Created = 1,
    Resolved = 2,
    Reopened = 3,
    CarriedForward = 4
}
