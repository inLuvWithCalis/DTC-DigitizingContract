using ContractManagement.API.Domains.Models.Contract;

namespace ContractManagement.Tests;

internal static class ContractSnapshotTestData
{
    internal static SoftwareSupplyContractSnapshot Create(
        int contractId,
        int versionId,
        int templateVersionId = 1) => new(
        new TenantLegalSnapshot(
            "Tenant Test", "0101", "Hà Nội", "Tenant Rep", "Giám đốc",
            null, null, null, null),
        new CustomerLegalSnapshot(
            1, "Customer Test", "0202", "Đà Nẵng", "Customer Rep",
            "Giám đốc", null, null, null, null),
        new ContractLegalSnapshot(
            contractId, $"HD-{contractId}", "Hợp đồng test", null, 1,
            templateVersionId, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            null, null, null, "VND", 1, 0, 0, 0, 0),
        new ContractVersionLegalSnapshot(
            versionId, 1, null, templateVersionId, "VND", 0, 0, 0, 0),
        [],
        []);
}
