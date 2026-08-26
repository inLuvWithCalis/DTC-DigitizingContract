using ContractManagement.API.Domains.DTOs.Requests.LegalProfiles;
using ContractManagement.API.Domains.Services.LegalProfiles;
using ContractManagement.Infrastructure.Persistence.Application;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Tests.Domains.Services.Tenant;

public sealed class TenantLegalProfileServiceTests
{
    [Fact]
    public async Task Upsert_CreatesThenUpdatesSingletonProfile()
    {
        await using var context = CreateContext();
        var service = new TenantLegalProfileService(context);

        var created = await service.UpsertAsync(Request(), employeeId: 11);
        var updatedRequest = Request();
        updatedRequest.LegalEntityName = "Công ty TNHH DTC Mới";
        updatedRequest.RowVersion = created.RowVersion;

        var updated = await service.UpsertAsync(updatedRequest, employeeId: 12);

        Assert.Equal(1, await context.TblTenantLegalProfiles.CountAsync());
        Assert.Equal("Công ty TNHH DTC Mới", updated.LegalEntityName);
        Assert.Equal(11, updated.CreatedByEmployeeId);
        Assert.Equal(12, updated.UpdatedByEmployeeId);
    }

    [Fact]
    public async Task Upsert_RejectsStaleRowVersion()
    {
        await using var context = CreateContext();
        var service = new TenantLegalProfileService(context);
        await service.UpsertAsync(Request(), employeeId: 11);

        var stale = Request();
        stale.RowVersion = Convert.ToBase64String(new byte[8]);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            service.UpsertAsync(stale, employeeId: 12));
    }

    [Fact]
    public async Task SeparateTenantDatabases_DoNotShareLegalProfile()
    {
        await using var tenantA = CreateContext("tenant-a");
        await using var tenantB = CreateContext("tenant-b");
        await new TenantLegalProfileService(tenantA)
            .UpsertAsync(Request(), employeeId: 11);

        var otherTenantProfile = await new TenantLegalProfileService(tenantB)
            .GetAsync();

        Assert.Null(otherTenantProfile);
    }

    private static UpsertTenantLegalProfileRequest Request() => new()
    {
        LegalEntityName = "Công ty TNHH DTC",
        TaxCode = "0101234567",
        Address = "Hà Nội",
        RepresentativeName = "Nguyễn Văn A",
        RepresentativeTitle = "Giám đốc"
    };

    private static DbDtctechContext CreateContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
        return new DbDtctechContext(options);
    }
}
