using ContractManagement.API.Common.Enums;

namespace ContractManagement.Domains.Policies.Contract;

public static class AcceptanceRecordPolicy
{
    public static bool CanTransition(
        AcceptanceRecordStatus current,
        AcceptanceRecordStatus target) => current switch
    {
        AcceptanceRecordStatus.Draft =>
            target is AcceptanceRecordStatus.Finalized
                or AcceptanceRecordStatus.Cancelled,
        AcceptanceRecordStatus.Finalized =>
            target is AcceptanceRecordStatus.Signed
                or AcceptanceRecordStatus.Cancelled,
        _ => false
    };

    public static void EnsureCanTransition(
        AcceptanceRecordStatus current,
        AcceptanceRecordStatus target)
    {
        if (!CanTransition(current, target))
        {
            throw new InvalidOperationException(
                $"Không thể chuyển biên bản nghiệm thu từ {current} sang {target}.");
        }
    }

    public static void EnsureCanCreate(bool contractIsSigned, DateTime? signDate)
    {
        if (!contractIsSigned || signDate is null)
        {
            throw new InvalidOperationException(
                "Chỉ được tạo biên bản nghiệm thu khi hợp đồng đã ký và có ngày ký nghiệp vụ.");
        }
    }
}
