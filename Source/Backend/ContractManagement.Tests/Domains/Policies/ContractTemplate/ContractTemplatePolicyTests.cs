using ContractManagement.Common.Enums;
using ContractManagement.Domains.Policies.ContractTemplate;

namespace ContractManagement.Tests.Domains.Policies.ContractTemplate;

public class ContractTemplatePolicyTests
{
    private static string ValidSha256Hash => new('a', 64);

    [Fact]
    public void SoftwareSupplyPlaceholderCatalog_V1_HasExpectedShape()
    {
        var catalog = SoftwareSupplyPlaceholderCatalog.All;

        Assert.Equal("V2", SoftwareSupplyPlaceholderCatalog.Version);
        Assert.Equal(37, catalog.Count);
        Assert.Equal(23, catalog.Count(item => item.IsRequired));
        Assert.Equal(14, catalog.Count(item => !item.IsRequired));
        Assert.Equal(
            catalog.Count,
            catalog.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count());
        Assert.All(catalog, item =>
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Key));
            Assert.False(string.IsNullOrWhiteSpace(item.DataSource));
        });
        Assert.Equal(
            "Customer.CustomerRepresentativeName",
            SoftwareSupplyPlaceholderCatalog.Find("CUSTOMER_NAME")?.DataSource);
        Assert.Equal(
            "Customer.CustomerRepresentativeTitle",
            SoftwareSupplyPlaceholderCatalog.Find(
                "CUSTOMER_REPRESENTATIVE_TITLE")?.DataSource);
        Assert.Equal(
            "TenantLegalProfile.LegalEntityName",
            SoftwareSupplyPlaceholderCatalog.Find(
                "PROVIDER_LEGAL_NAME")?.DataSource);
    }

    [Theory]
    [InlineData("CONTRACT_TERMS", TemplatePlaceholderDataKind.DynamicBlock, true, TemplatePlaceholderMultiplicity.ExactlyOne)]
    [InlineData("CONTRACT_ITEM_TABLE", TemplatePlaceholderDataKind.DynamicBlock, true, TemplatePlaceholderMultiplicity.ExactlyOne)]
    [InlineData("SIGNATURE_PROVIDER", TemplatePlaceholderDataKind.DynamicBlock, true, TemplatePlaceholderMultiplicity.ExactlyOne)]
    [InlineData("SIGNATURE_CUSTOMER", TemplatePlaceholderDataKind.DynamicBlock, true, TemplatePlaceholderMultiplicity.ExactlyOne)]
    [InlineData("PAYMENT_SCHEDULE_TABLE", TemplatePlaceholderDataKind.DynamicBlock, false, TemplatePlaceholderMultiplicity.ZeroOrOne)]
    public void SoftwareSupplyPlaceholderCatalog_SpecialPlaceholdersHaveFixedMultiplicity(
        string key,
        TemplatePlaceholderDataKind expectedKind,
        bool expectedRequired,
        TemplatePlaceholderMultiplicity expectedMultiplicity)
    {
        var item = Assert.Single(
            SoftwareSupplyPlaceholderCatalog.All,
            candidate => candidate.Key == key);

        Assert.Equal(expectedKind, item.DataKind);
        Assert.Equal(expectedRequired, item.IsRequired);
        Assert.Equal(expectedMultiplicity, item.Multiplicity);
    }

    // =========================================================
    // OUTPUT KIND
    // =========================================================

    [Theory]
    [InlineData(TemplateDocumentType.SoftwareSupplyContract)]
    [InlineData(TemplateDocumentType.SoftwareMaintenanceContract)]
    [InlineData(TemplateDocumentType.SoftwareUpkeepContract)]
    public void GetOutputKind_ContractDocumentType_ReturnsContract(
        TemplateDocumentType documentType)
    {
        var result = ContractTemplatePolicy.GetOutputKind(documentType);

        Assert.Equal(TemplateOutputKind.Contract, result);
        Assert.True(
            ContractTemplatePolicy.CreatesContract(documentType));
        Assert.False(
            ContractTemplatePolicy.CreatesSupportingDocument(documentType));
    }

    [Theory]
    [InlineData(TemplateDocumentType.Quotation)]
    [InlineData(TemplateDocumentType.PaymentRequest)]
    [InlineData(TemplateDocumentType.HandoverRecord)]
    [InlineData(TemplateDocumentType.AcceptanceRecord)]
    [InlineData(TemplateDocumentType.LiquidationRecord)]
    [InlineData(TemplateDocumentType.Other)]
    public void GetOutputKind_SupportingDocumentType_ReturnsSupportingDocument(
        TemplateDocumentType documentType)
    {
        var result = ContractTemplatePolicy.GetOutputKind(documentType);

        Assert.Equal(TemplateOutputKind.SupportingDocument, result);
        Assert.False(
            ContractTemplatePolicy.CreatesContract(documentType));
        Assert.True(
            ContractTemplatePolicy.CreatesSupportingDocument(documentType));
    }

    [Fact]
    public void GetOutputKind_InvalidDocumentType_Throws()
    {
        var invalidType = (TemplateDocumentType)200;

        Assert.Throws<ArgumentOutOfRangeException>(
            () => ContractTemplatePolicy.GetOutputKind(invalidType));
    }

    // =========================================================
    // STATE TRANSITION
    // =========================================================

    [Theory]
    [InlineData(
        TemplateVersionStatus.Draft,
        TemplateVersionStatus.Published)]
    [InlineData(
        TemplateVersionStatus.Published,
        TemplateVersionStatus.Retired)]
    public void CanTransition_AllowedTransition_ReturnsTrue(
        TemplateVersionStatus currentStatus,
        TemplateVersionStatus targetStatus)
    {
        Assert.True(
            ContractTemplatePolicy.CanTransition(
                currentStatus,
                targetStatus));
    }

    [Theory]
    [InlineData(
        TemplateVersionStatus.Draft,
        TemplateVersionStatus.Retired)]
    [InlineData(
        TemplateVersionStatus.Published,
        TemplateVersionStatus.Draft)]
    [InlineData(
        TemplateVersionStatus.Retired,
        TemplateVersionStatus.Published)]
    [InlineData(
        TemplateVersionStatus.Draft,
        TemplateVersionStatus.Draft)]
    public void CanTransition_ForbiddenTransition_ReturnsFalse(
        TemplateVersionStatus currentStatus,
        TemplateVersionStatus targetStatus)
    {
        Assert.False(
            ContractTemplatePolicy.CanTransition(
                currentStatus,
                targetStatus));
    }

    [Fact]
    public void CanTransition_InvalidCurrentStatus_ReturnsFalse()
    {
        var invalidStatus = (TemplateVersionStatus)200;

        Assert.False(
            ContractTemplatePolicy.CanTransition(
                invalidStatus,
                TemplateVersionStatus.Published));
    }

    [Fact]
    public void EnsureCanTransition_ForbiddenTransition_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => ContractTemplatePolicy.EnsureCanTransition(
                TemplateVersionStatus.Published,
                TemplateVersionStatus.Draft));
    }

    // =========================================================
    // EDIT RULE
    // =========================================================

    [Fact]
    public void CanEdit_Draft_ReturnsTrue()
    {
        Assert.True(
            ContractTemplatePolicy.CanEdit(
                TemplateVersionStatus.Draft));
    }

    [Theory]
    [InlineData(TemplateVersionStatus.Published)]
    [InlineData(TemplateVersionStatus.Retired)]
    public void CanEdit_NonDraft_ReturnsFalse(
        TemplateVersionStatus status)
    {
        Assert.False(
            ContractTemplatePolicy.CanEdit(status));
    }

    [Fact]
    public void EnsureCanEdit_Published_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => ContractTemplatePolicy.EnsureCanEdit(
                TemplateVersionStatus.Published));
    }

    [Fact]
    public void CanCreateDraftFromSource_CurrentPublished_ReturnsTrue()
    {
        Assert.True(ContractTemplatePolicy.CanCreateDraftFromSource(
            TemplateVersionStatus.Published,
            isCurrentPublished: true,
            hasCurrentPublished: true,
            isLatestRetired: false,
            hasExistingDraft: true));
    }

    [Fact]
    public void CanCreateDraftFromSource_LatestRetiredWithoutPublishedOrDraft_ReturnsTrue()
    {
        Assert.True(ContractTemplatePolicy.CanCreateDraftFromSource(
            TemplateVersionStatus.Retired,
            isCurrentPublished: false,
            hasCurrentPublished: false,
            isLatestRetired: true,
            hasExistingDraft: false));
    }

    [Theory]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    public void CanCreateDraftFromSource_IneligibleRetired_ReturnsFalse(
        bool hasCurrentPublished,
        bool isLatestRetired,
        bool hasExistingDraft)
    {
        Assert.False(ContractTemplatePolicy.CanCreateDraftFromSource(
            TemplateVersionStatus.Retired,
            isCurrentPublished: false,
            hasCurrentPublished,
            isLatestRetired,
            hasExistingDraft));
    }

    // =========================================================
    // PUBLISH GATE
    // =========================================================

    [Fact]
    public void CanPublish_AllRequirementsSatisfied_ReturnsTrue()
    {
        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Draft,
            TemplateValidationStatus.Valid,
            documentFileId: 10,
            documentHash: ValidSha256Hash);

        Assert.True(result);
    }

    [Fact]
    public void CanPublish_StatusIsNotDraft_ReturnsFalse()
    {
        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Published,
            TemplateValidationStatus.Valid,
            documentFileId: 10,
            documentHash: ValidSha256Hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData(TemplateValidationStatus.NotValidated)]
    [InlineData(TemplateValidationStatus.Invalid)]
    public void CanPublish_ValidationIsNotValid_ReturnsFalse(
        TemplateValidationStatus validationStatus)
    {
        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Draft,
            validationStatus,
            documentFileId: 10,
            documentHash: ValidSha256Hash);

        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void CanPublish_DocumentFileIdIsInvalid_ReturnsFalse(
        int? documentFileId)
    {
        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Draft,
            TemplateValidationStatus.Valid,
            documentFileId,
            ValidSha256Hash);

        Assert.False(result);
    }

    [Fact]
    public void CanPublish_HashHasWrongLength_ReturnsFalse()
    {
        var invalidHash = new string('a', 63);

        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Draft,
            TemplateValidationStatus.Valid,
            documentFileId: 10,
            documentHash: invalidHash);

        Assert.False(result);
    }

    [Fact]
    public void CanPublish_HashContainsNonHexCharacter_ReturnsFalse()
    {
        var invalidHash = new string('g', 64);

        var result = ContractTemplatePolicy.CanPublish(
            TemplateVersionStatus.Draft,
            TemplateValidationStatus.Valid,
            documentFileId: 10,
            documentHash: invalidHash);

        Assert.False(result);
    }

    [Fact]
    public void EnsureCanPublish_MissingDocument_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => ContractTemplatePolicy.EnsureCanPublish(
                TemplateVersionStatus.Draft,
                TemplateValidationStatus.Valid,
                documentFileId: null,
                documentHash: ValidSha256Hash));
    }

    // =========================================================
    // SELECT FOR NEW DOCUMENT
    // =========================================================

    [Fact]
    public void CanBeSelected_ActiveAndPublished_ReturnsTrue()
    {
        Assert.True(
            ContractTemplatePolicy.CanBeSelectedForNewDocument(
                isTemplateActive: true,
                TemplateVersionStatus.Published));
    }

    [Theory]
    [InlineData(false, TemplateVersionStatus.Published)]
    [InlineData(true, TemplateVersionStatus.Draft)]
    [InlineData(true, TemplateVersionStatus.Retired)]
    [InlineData(false, TemplateVersionStatus.Retired)]
    public void CanBeSelected_NotActiveOrNotPublished_ReturnsFalse(
        bool isTemplateActive,
        TemplateVersionStatus versionStatus)
    {
        Assert.False(
            ContractTemplatePolicy.CanBeSelectedForNewDocument(
                isTemplateActive,
                versionStatus));
    }
}
