using ContractManagement.API.Domains.DTOs.Requests.Customer;
using ContractManagement.API.Domains.Services.Customer;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Services.Customer;

public sealed class CustomerLookupServiceTests
{
    [Fact]
    public async Task Lookup_ReturnsOnlyMinimalCustomerDataAndMatchesKeyword()
    {
        await using var context = CreateContext();
        context.TblCustomers.AddRange(
            new TblCustomer
            {
                CustomerId = 1,
                CustomerCode = "ACME-01",
                CustomerFullName = "Alice",
                CustomerCompany = "Acme Corp",
                CustomerEmail = "private@example.test",
                CustomerMobile = "0900000000",
                Status = 1
            },
            new TblCustomer
            {
                CustomerId = 2,
                CustomerCode = "OTHER-01",
                CustomerFullName = "Bob",
                CustomerCompany = "Other Corp",
                Status = 0
            });
        await context.SaveChangesAsync();

        var result = await new CustomerService(context).GetLookupAsync(" Acme ");

        var customer = Assert.Single(result);
        Assert.Equal(1, customer.CustomerId);
        Assert.Equal("ACME-01", customer.CustomerCode);
        Assert.Equal("Alice", customer.CustomerFullName);
        Assert.Equal("Acme Corp", customer.CustomerCompany);
        Assert.Equal((byte)1, customer.Status);
    }

    [Fact]
    public async Task CreateAndUpdate_PersistDirectContactSeparatelyFromLegalRepresentative()
    {
        await using var context = CreateContext();
        var service = new CustomerService(context);

        var created = await service.CreateAsync(
            new CreateCustomerRequest
            {
                CustomerFullName = "Công ty khách hàng",
                CustomerRepresentativeName = "Nguyễn Văn Đại Diện",
                CustomerContactPersonName = "  Trần Thị Liên Hệ  ",
                CustomerContactPersonPhone = " 0901234567 ",
                CustomerContactPersonTitle = " Chuyên viên mua hàng "
            },
            createdBy: 11);

        Assert.Equal("Nguyễn Văn Đại Diện", created.CustomerRepresentativeName);
        Assert.Equal("Trần Thị Liên Hệ", created.CustomerContactPersonName);
        Assert.Equal("0901234567", created.CustomerContactPersonPhone);
        Assert.Equal("Chuyên viên mua hàng", created.CustomerContactPersonTitle);

        await service.UpdateAsync(
            created.CustomerId,
            new UpdateCustomerRequest
            {
                CustomerFullName = "Công ty khách hàng",
                CustomerRepresentativeName = "Nguyễn Văn Đại Diện",
                CustomerContactPersonName = "Lê Văn Đầu Mối",
                CustomerContactPersonPhone = "02812345678",
                CustomerContactPersonTitle = "Trưởng phòng mua hàng"
            },
            updatedBy: 12);

        var updated = await service.GetByIdAsync(created.CustomerId);

        Assert.Equal("Nguyễn Văn Đại Diện", updated.CustomerRepresentativeName);
        Assert.Equal("Lê Văn Đầu Mối", updated.CustomerContactPersonName);
        Assert.Equal("02812345678", updated.CustomerContactPersonPhone);
        Assert.Equal("Trưởng phòng mua hàng", updated.CustomerContactPersonTitle);
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }
}
