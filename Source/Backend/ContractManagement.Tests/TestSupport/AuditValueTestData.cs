using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application.Models;

namespace ContractManagement.Tests;

internal static class AuditValueTestData
{
    public static TblContractAuditValue ContractValue(
        TblContractAudit audit,
        AuditValueSide side,
        ContractAuditFieldCode fieldCode)
    {
        return Assert.Single(audit.Values, value =>
            value.ValueSide == side && value.FieldCode == (byte)fieldCode);
    }

    public static TblContractTemplateAuditValue TemplateValue(
        TblContractTemplateAudit audit,
        AuditValueSide side,
        ContractTemplateAuditFieldCode fieldCode)
    {
        return Assert.Single(audit.Values, value =>
            value.ValueSide == side && value.FieldCode == (short)fieldCode);
    }
}
