namespace ContractManagement.API.Common.Enums;

public enum ContractTermKind : byte
{
    General = 0,
    Payment = 1
}

public enum PaymentDueAnchor : byte
{
    ContractSigned = 1,
    ContractEffectiveDate = 2,
    AcceptanceCompleted = 3,
    PreviousMilestonePaid = 4,
    Manual = 5
}

public enum PaymentDayCountMode : byte
{
    CalendarDays = 1,
    BusinessDays = 2
}
