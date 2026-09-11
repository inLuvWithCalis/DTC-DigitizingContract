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
    ManualDate = 5
}

public enum ContractPaymentMilestoneStatus : byte
{
    Unpaid = 0,
    Paid = 1
}

public enum PaymentDayCountMode : byte
{
    CalendarDays = 1,
    BusinessDays = 2
}
