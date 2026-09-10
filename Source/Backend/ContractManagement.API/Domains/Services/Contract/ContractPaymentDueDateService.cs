using ContractManagement.API.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;

namespace ContractManagement.Domains.Services.Contract;

public sealed class ContractPaymentDueDateService : IContractPaymentDueDateService
{
    public DateTime Calculate(DateTime anchorDate, int offsetDays,
        PaymentDayCountMode dayCountMode)
    {
        if (offsetDays < 0) throw new ArgumentOutOfRangeException(nameof(offsetDays));
        if (!Enum.IsDefined(dayCountMode))
            throw new ArgumentOutOfRangeException(nameof(dayCountMode));
        var value = anchorDate.Date;
        if (dayCountMode == PaymentDayCountMode.CalendarDays)
            return value.AddDays(offsetDays);
        var remaining = offsetDays;
        while (remaining > 0)
        {
            value = value.AddDays(1);
            if (value.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                remaining--;
        }
        return value;
    }
}
