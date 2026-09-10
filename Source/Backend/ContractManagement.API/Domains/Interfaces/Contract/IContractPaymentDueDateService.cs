using ContractManagement.API.Common.Enums;

namespace ContractManagement.Domains.Interfaces.Contract;

public interface IContractPaymentDueDateService
{
    DateTime Calculate(DateTime anchorDate, int offsetDays,
        PaymentDayCountMode dayCountMode);
}
