using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.CustomerAccess;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Requests.Public;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Services.Contract;
using ContractManagement.Infrastructure.MultiTenancy.Enums;
using ContractManagement.Infrastructure.MultiTenancy.Models;
using ContractManagement.Infrastructure.MultiTenancy.Services;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ContractManagement.Tests.Domains.Services.Contract;

public sealed class ContractServiceSlice06Tests
{
    private const int TenantId = 906;
    private const int ContractId = 600;
    private const int VersionId = 601;
    private const int EmployeeId = 602;
    private const int CustomerId = 603;
    private const int TermId = 604;

    [Theory]
    [InlineData("Fake")]
    [InlineData("Smtp")]
    public async Task DraftToNegotiating_OtpCustomerCommentAndNewRound_ShouldRevokeOldAccess(string provider)
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var (contractService, customerAccess, cryptography) = CreateServices(context, provider);
        var rowVersion = Convert.ToBase64String(InitialRowVersion());

        var phone = await contractService.UpdateCustomerVerificationPhoneAsync(
            ContractId,
            new UpdateContractCustomerVerificationPhoneRequest
            {
                PhoneSource = "CustomerMobile",
                Reason = "Customer selected mobile",
                RowVersion = rowVersion
            },
            EmployeeId);
        Assert.Equal("********5678", phone.MaskedPhoneNumber);

        var link = await contractService.CreateCustomerAccessLinkAsync(
            ContractId,
            new CreateContractCustomerAccessLinkRequest { RowVersion = rowVersion },
            EmployeeId,
            "https://public.example.test");
        Assert.Equal("PendingActivation", link.State);
        var currentLink = await contractService.GetCurrentCustomerAccessLinkAsync(
            ContractId,
            EmployeeId);
        Assert.NotNull(currentLink);
        Assert.Equal(link.LinkId, currentLink.LinkId);
        Assert.Equal(VersionId, currentLink.VersionId);
        Assert.Equal("PendingActivation", currentLink.State);
        Assert.DoesNotContain(
            currentLink.GetType().GetProperties(),
            property => property.Name.Contains("Url", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));

        var pendingToken = new Uri(link.PublicUrl).Segments.Last().Trim('/');
        var pendingAvailability = await customerAccess.GetLinkAvailabilityAsync(pendingToken);
        Assert.False(pendingAvailability.IsAvailable);
        Assert.Equal("PendingActivation", pendingAvailability.State);

        var pendingError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            customerAccess.RequestOtpAsync(pendingToken, "+84912345678"));
        Assert.Equal(
            "Chỉ có thể xem hợp đồng khi hợp đồng đang ở trạng thái đàm phán.",
            pendingError.Message);
        Assert.Empty(context.TblContractCustomerOtpChallenges);

        await contractService.StartNegotiationAsync(
            ContractId,
            new StartContractNegotiationRequest { RowVersion = rowVersion },
            EmployeeId);

        var activeAvailability = await customerAccess.GetLinkAvailabilityAsync(pendingToken);
        Assert.True(activeAvailability.IsAvailable);
        Assert.Equal("Available", activeAvailability.State);

        var challengeResponse = await customerAccess.RequestOtpAsync(
            pendingToken,
            "+84912345678");
        var outbox = await context.TblContractCustomerOtpDeliveryOutbox.SingleAsync();
        var message = cryptography.DecryptDeliveryPayload(outbox.EncryptedPayload);
        Assert.Equal(provider == "Smtp" ? "Email" : "Sms", challengeResponse.DeliveryChannel);
        Assert.Equal(provider == "Smtp" ? "customer@example.test" : null, message.EmailAddress);
        Assert.Equal((await context.TblContractCustomerOtpChallenges.SingleAsync()).ExpiresAt, message.ExpiresAt);
        var issue = await customerAccess.VerifyOtpAsync(
            pendingToken,
            challengeResponse.PublicChallengeId,
            message.Otp);

        var shared = await customerAccess.GetSharedAsync(issue.SessionSecret);
        Assert.Single(shared.Terms);
        Assert.Empty(shared.Comments);

        var comment = await customerAccess.CreateCommentAsync(
            issue.SessionSecret,
            new CreateCustomerNegotiationCommentRequest
            {
                TermId = TermId,
                Content = "Please clarify the term."
            });
        Assert.Equal("Customer", comment.Source);
        Assert.Equal("Open", comment.LifecycleState);

        var persisted = await context.TblContractNegotiationComments.SingleAsync();
        Assert.Null(persisted.RecordedByEmployeeId);
        Assert.NotNull(persisted.CustomerAccessSessionId);
        Assert.Contains(context.TblContractAudits, x =>
            x.ActionType == ContractAuditActionTypes.CustomerCommentCreated
            && x.ActorType == ContractAuditActorTypes.Customer);

        var round = await contractService.CreateNegotiationRoundAsync(
            ContractId,
            new CreateContractNegotiationRoundRequest
            {
                CurrentVersionId = VersionId,
                RowVersion = rowVersion,
                CurrentVersionRowVersion = rowVersion,
                ChangeNote = "Customer negotiation round two"
            },
            EmployeeId);

        Assert.NotEqual(VersionId, round.CurrentVersion.VersionId);
        Assert.Null((await context.TblContracts.SingleAsync()).CurrentCustomerAccessLinkId);
        Assert.Null(await contractService.GetCurrentCustomerAccessLinkAsync(
            ContractId,
            EmployeeId));
        Assert.NotNull((await context.TblContractCustomerAccessLinks.SingleAsync()).RevokedAt);
        Assert.NotNull((await context.TblContractCustomerAccessSessions.SingleAsync()).RevokedAt);
        var revokedAvailability = await customerAccess.GetLinkAvailabilityAsync(pendingToken);
        Assert.False(revokedAvailability.IsAvailable);
        Assert.Equal("Unavailable", revokedAvailability.State);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => customerAccess.GetSharedAsync(issue.SessionSecret));
    }

    [Fact]
    public async Task VerificationPhoneChange_ShouldAuditBeforeAfterAndRevokedLink()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var (contractService, _, _) = CreateServices(context);
        var rowVersion = Convert.ToBase64String(InitialRowVersion());

        await contractService.UpdateCustomerVerificationPhoneAsync(
            ContractId,
            new UpdateContractCustomerVerificationPhoneRequest
            {
                PhoneSource = "CustomerMobile",
                Reason = "Chọn số khách hàng",
                RowVersion = rowVersion
            },
            EmployeeId);
        var link = await contractService.CreateCustomerAccessLinkAsync(
            ContractId,
            new CreateContractCustomerAccessLinkRequest
            {
                RowVersion = rowVersion
            },
            EmployeeId,
            "https://public.example.test");

        await contractService.UpdateCustomerVerificationPhoneAsync(
            ContractId,
            new UpdateContractCustomerVerificationPhoneRequest
            {
                PhoneSource = "Manual",
                ManualPhoneNumber = "+84987654321",
                Reason = "Khách hàng đổi số",
                RowVersion = rowVersion
            },
            EmployeeId);

        var phoneAudit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType == ContractAuditActionTypes.VerificationPhoneChanged);
        using var previousDocument = JsonDocument.Parse(
            phoneAudit.PreviousValuesJson!);
        using var newDocument = JsonDocument.Parse(
            phoneAudit.NewValuesJson!);
        Assert.Equal(
            "********5678",
            previousDocument.RootElement
                .GetProperty("VerificationPhoneMasked").GetString());
        Assert.Equal(
            "********4321",
            newDocument.RootElement
                .GetProperty("VerificationPhoneMasked").GetString());
        Assert.Equal(
            link.LinkId,
            previousDocument.RootElement.GetProperty("LinkId").GetInt32());

        var revokedAudit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType == ContractAuditActionTypes.CustomerAccessLinkRevoked
            && x.SubjectId == link.LinkId);
        using var revokedDocument = JsonDocument.Parse(
            revokedAudit.NewValuesJson!);
        Assert.Equal(
            "Revoked",
            revokedDocument.RootElement.GetProperty("LinkState").GetString());
        Assert.Null((await context.TblContracts.SingleAsync())
            .CurrentCustomerAccessLinkId);
    }

    [Fact]
    public async Task ReplaceLink_ShouldAuditPreviousAndNewLinkIds()
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        var (contractService, _, _) = CreateServices(context);
        var rowVersion = Convert.ToBase64String(InitialRowVersion());

        await contractService.UpdateCustomerVerificationPhoneAsync(
            ContractId,
            new UpdateContractCustomerVerificationPhoneRequest
            {
                PhoneSource = "CustomerMobile",
                Reason = "Chọn số khách hàng",
                RowVersion = rowVersion
            },
            EmployeeId);
        var previous = await contractService.CreateCustomerAccessLinkAsync(
            ContractId,
            new CreateContractCustomerAccessLinkRequest
            {
                RowVersion = rowVersion
            },
            EmployeeId,
            "https://public.example.test");
        var replacement = await contractService.ReplaceCustomerAccessLinkAsync(
            ContractId,
            previous.LinkId,
            new ReplaceContractCustomerAccessLinkRequest
            {
                RowVersion = rowVersion,
                Reason = "Cấp lại link"
            },
            EmployeeId,
            "https://public.example.test");

        var audit = await context.TblContractAudits.SingleAsync(x =>
            x.ActionType == ContractAuditActionTypes.CustomerAccessLinkReplaced);
        using var previousDocument = JsonDocument.Parse(
            audit.PreviousValuesJson!);
        using var newDocument = JsonDocument.Parse(audit.NewValuesJson!);
        Assert.Equal(
            previous.LinkId,
            previousDocument.RootElement
                .GetProperty("PreviousLinkId").GetInt32());
        Assert.Equal(
            replacement.LinkId,
            newDocument.RootElement.GetProperty("NewLinkId").GetInt32());
    }

    private static DbDtctechContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<DbDtctechContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(
                InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new DbDtctechContext(options);
    }

    private static (ContractService ContractService,
        CustomerContractAccessService CustomerAccess,
        CustomerAccessCryptography Cryptography) CreateServices(DbDtctechContext context, string provider = "Fake")
    {
        var tenant = new CurrentTenant();
        tenant.Set(new ResolvedTenant(
            TenantId,
            "tenant-906",
            "Tenant 906",
            TenantDatabaseMode.Dedicated,
            "InMemory"));
        var audit = new ContractAuditWriter(
            context,
            tenant,
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "slice-06" }
            });
        var options = Options.Create(new CustomerOtpOptions
        {
            Provider = provider,
            HashKey = Convert.ToBase64String(Enumerable.Repeat((byte)1, 32).ToArray()),
            EncryptionKey = Convert.ToBase64String(Enumerable.Repeat((byte)2, 32).ToArray())
        });
        var cryptography = new CustomerAccessCryptography(options);
        var contractService = new ContractService(context, audit, tenant, cryptography);
        return (
            contractService,
            new CustomerContractAccessService(
                context,
                tenant,
                cryptography,
                contractService,
                audit,
                options),
            cryptography);
    }

    [Theory]
    [InlineData(null, "+84912345678")]
    [InlineData("invalid-email", "+84912345678")]
    [InlineData("customer@example.test", "+84999999999")]
    public async Task Smtp_InvalidRecipientOrWrongPhone_DoesNotEnqueueOrDiscloseEmail(
        string? email, string suppliedPhone)
    {
        await using var context = CreateContext();
        await SeedAsync(context);
        (await context.TblCustomers.SingleAsync()).CustomerEmail = email;
        await context.SaveChangesAsync();
        var (contracts, access, _) = CreateServices(context, "Smtp");
        var rowVersion = Convert.ToBase64String(InitialRowVersion());
        await contracts.UpdateCustomerVerificationPhoneAsync(ContractId,
            new UpdateContractCustomerVerificationPhoneRequest
            {
                PhoneSource = "CustomerMobile", Reason = "Select contact", RowVersion = rowVersion
            }, EmployeeId);
        var link = await contracts.CreateCustomerAccessLinkAsync(ContractId,
            new CreateContractCustomerAccessLinkRequest { RowVersion = rowVersion },
            EmployeeId, "https://public.example.test");
        await contracts.StartNegotiationAsync(ContractId,
            new StartContractNegotiationRequest { RowVersion = rowVersion }, EmployeeId);
        var token = new Uri(link.PublicUrl).Segments.Last().Trim('/');
        var response = await access.RequestOtpAsync(token, suppliedPhone);
        Assert.Equal("Email", response.DeliveryChannel);
        Assert.False(string.IsNullOrWhiteSpace(response.PublicChallengeId));
        Assert.Empty(context.TblContractCustomerOtpDeliveryOutbox);
        Assert.Empty(context.TblContractCustomerOtpChallenges);
        Assert.DoesNotContain("@", JsonSerializer.Serialize(response));
    }

    private static async Task SeedAsync(DbDtctechContext context)
    {
        context.TblEmployees.Add(new TblEmployee
        {
            EmployeeId = EmployeeId,
            EmployeeAccount = "slice06-owner",
            EmployeeFullName = "Slice 06 owner",
            EmployeeType = (byte)EmployeeType.Sale,
            Status = 1
        });
        context.TblCustomers.Add(new TblCustomer
        {
            CustomerId = CustomerId,
            CustomerFullName = "Slice 06 customer",
            CustomerCompany = "Slice 06 Customer Company",
            CustomerAddress = "Ha Noi",
            CustomerRepresentativeName = "Slice 06 Representative",
            CustomerRepresentativeTitle = "Director",
            CustomerMobile = "+84912345678",
            CustomerEmail = "customer@example.test",
            Status = 1
        });
        context.TblTenantLegalProfiles.Add(new TblTenantLegalProfile
        {
            TenantLegalProfileId = 1,
            LegalEntityName = "DTC Company",
            TaxCode = "0100000006",
            Address = "Ho Chi Minh City",
            RepresentativeName = "Provider Representative",
            RepresentativeTitle = "General Director",
            CreatedByEmployeeId = EmployeeId,
            UpdatedByEmployeeId = EmployeeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContracts.Add(new TblContract
        {
            ContractId = ContractId,
            CustomerId = CustomerId,
            EmployeeId = EmployeeId,
            ContractType = 1,
            CurrentVersionId = VersionId,
            ContractCode = "HD-S06",
            ContractName = "Slice 06",
            Status = (byte)ContractStatus.Draft,
            CurrencyCode = "VND",
            LanguageMode = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractVersions.Add(new TblContractVersion
        {
            VersionId = VersionId,
            ContractId = ContractId,
            VersionNo = 1,
            CurrencyCode = "VND",
            IsLocked = false,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractItems.Add(new TblContractItem
        {
            ContractItemId = 605,
            ContractId = ContractId,
            VersionId = VersionId,
            ItemType = 1,
            ItemName = "Slice 06 item",
            Quantity = 1,
            UnitPrice = 100,
            LineSubtotal = 100,
            DiscountMode = 0,
            DiscountPercent = 0,
            FixedDiscountAmount = 0,
            DiscountAmount = 0,
            IsTaxable = false,
            VatPercent = 0,
            VatAmount = 0,
            LineTotal = 100,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        context.TblContractTerms.Add(new TblContractTerm
        {
            TermId = TermId,
            ContractId = ContractId,
            VersionId = VersionId,
            TermCode = "NEGOTIABLE",
            TermTitle = "Negotiable term",
            IsNegotiable = true,
            DisplayOrder = 1,
            CreatedEmployeeId = EmployeeId,
            CreatedDate = DateTime.UtcNow,
            RowVersion = InitialRowVersion()
        });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    private static byte[] InitialRowVersion() => [1, 2, 3, 4, 5, 6, 7, 8];
}
