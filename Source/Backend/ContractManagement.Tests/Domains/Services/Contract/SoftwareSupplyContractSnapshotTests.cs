using ContractManagement.API.Domains.Models.Contract;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using static ContractManagement.Tests.ContractRichTextTestData;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class SoftwareSupplyContractSnapshotTests
{
    [Fact]
    public void Create_CapturesTenantCustomerTermsItemsAndFinance()
    {
        var tenant = new TblTenantLegalProfile
        {
            LegalEntityName = "DTC",
            TaxCode = "0101",
            Address = "Hà Nội",
            RepresentativeName = "Nguyễn A",
            RepresentativeTitle = "Giám đốc",
            PhoneNumber = "02473000001",
            FaxNumber = "02473000002",
            BankAccountNumber = "098765432109",
            BankName = "Ngân hàng DTC"
        };
        var customer = new TblCustomer
        {
            CustomerId = 2,
            CustomerCompany = "Khách hàng ABC",
            CustomerTaxCode = "0202",
            CustomerAddress = "Đà Nẵng",
            CustomerRepresentativeName = "Trần B",
            CustomerRepresentativeTitle = "Tổng giám đốc",
            CustomerPhone = "02367300001",
            CustomerFaxNumber = "02367300002",
            CustomerBankAccountNumber = "012345678901",
            CustomerBankName = "Ngân hàng ABC"
        };
        var contract = new TblContract
        {
            ContractId = 3,
            ContractCode = "HD-001",
            ContractName = "Cung cấp phần mềm",
            ContractType = 1,
            TemplateVersionId = 7,
            CurrencyCode = "VND",
            TotalAmount = 1_100_000,
            Subtotal = 1_000_000,
            TotalVat = 100_000
        };
        var version = new TblContractVersion
        {
            VersionId = 4,
            VersionNo = 1,
            TemplateVersionId = 7,
            CurrencyCode = "VND",
            TotalAmount = 1_100_000,
            Subtotal = 1_000_000,
            TotalVat = 100_000
        };
        var items = new[]
        {
            new TblContractItem
            {
                ContractItemId = 5,
                ItemName = "Phần mềm DTC",
                Quantity = 1,
                UnitPrice = 1_000_000,
                LineSubtotal = 1_000_000,
                VatAmount = 100_000,
                LineTotal = 1_100_000
            }
        };
        var terms = new[]
        {
            new TblContractTerm
            {
                TermId = 6,
                TermCode = "PAYMENT",
                TermTitle = "Thanh toán",
                TermContent = RichText("Thanh toán một lần")
            }
        };

        var snapshot = SoftwareSupplyContractSnapshotFactory.Create(
            tenant,
            customer,
            contract,
            version,
            items,
            terms);
        var hash = SoftwareSupplyContractSnapshotFactory.CalculateHash(snapshot);
        var persisted = SoftwareSupplyContractSnapshotFactory.CreatePersistenceGraph(
            snapshot, 10, DateTime.UtcNow);

        tenant.LegalEntityName = "Tenant master đã đổi";
        customer.CustomerRepresentativeName = "Customer master đã đổi";
        contract.ContractName = "Contract master đã đổi";

        Assert.Equal(contract.CreatedDate, snapshot.Contract.CreatedDate);
        Assert.Equal("DTC", snapshot.Tenant.LegalEntityName);
        Assert.Equal("Trần B", snapshot.Customer.RepresentativeName);
        Assert.Equal("Cung cấp phần mềm", snapshot.Contract.ContractName);
        Assert.Equal("02367300001", snapshot.Customer.PhoneNumber);
        Assert.Equal("02367300002", snapshot.Customer.FaxNumber);
        Assert.Equal("012345678901", snapshot.Customer.BankAccountNumber);
        Assert.Equal("Ngân hàng ABC", snapshot.Customer.BankName);
        Assert.Equal("02473000001", snapshot.Tenant.PhoneNumber);
        Assert.Equal("02473000002", snapshot.Tenant.FaxNumber);
        Assert.Equal("098765432109", snapshot.Tenant.BankAccountNumber);
        Assert.Equal("Ngân hàng DTC", snapshot.Tenant.BankName);
        Assert.Single(snapshot.Items);
        Assert.Single(snapshot.Terms);
        Assert.Equal(64, hash.Length);
        Assert.Equal(hash, SoftwareSupplyContractSnapshotFactory.CalculateHash(snapshot));
        Assert.Equal(2, persisted.Parties.Count);
        Assert.Equal(1_100_000, persisted.TotalAmount);
    }

    [Fact]
    public void CalculateHash_ChangesWhenOneScalarChanges()
    {
        var snapshot = ContractSnapshotTestData.Create(1, 2);
        var changed = snapshot with
        {
            Contract = snapshot.Contract with { ContractName = "Tên đã đổi" }
        };

        Assert.NotEqual(
            SoftwareSupplyContractSnapshotFactory.CalculateHash(snapshot),
            SoftwareSupplyContractSnapshotFactory.CalculateHash(changed));
    }

    [Fact]
    public void CalculateHash_ChangesWhenAppendixTermChanges()
    {
        var source = ContractSnapshotTestData.Create(1, 2);
        var term = new ContractAppendixTermLegalSnapshot(
            11, 21, "PL-TERM", "Phạm vi", null,
            RichText("Nội dung ban đầu"), null, 1);
        var snapshot = source with
        {
            Appendices =
            [
                new ContractAppendixLegalSnapshot(
                    10, 20, "PL-01", "Phụ lục", null, null,
                    true, 1, [term])
            ]
        };
        var changed = snapshot with
        {
            Appendices =
            [
                snapshot.Appendices![0] with
                {
                    Terms = [term with { TermContent = RichText("Nội dung đã đổi") }]
                }
            ]
        };

        Assert.NotEqual(
            SoftwareSupplyContractSnapshotFactory.CalculateHash(snapshot),
            SoftwareSupplyContractSnapshotFactory.CalculateHash(changed));
    }

}
