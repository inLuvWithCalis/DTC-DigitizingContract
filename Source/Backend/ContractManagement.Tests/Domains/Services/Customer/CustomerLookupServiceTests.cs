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

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }
}
