using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Exceptions;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.Models.Contract;
using ContractManagement.API.Domains.Policies.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Interfaces.File;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Domains.Services.File;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ContractManagement.API.Domains.CustomerAccess;
using ContractManagement.Infrastructure.MultiTenancy.Interfaces;

namespace ContractManagement.Domains.Services.Contract
{
    /// <summary>
    /// Service xử lý nghiệp vụ chính của hợp đồng.
    /// </summary>
    public partial class ContractService : IContractService
    {
        private const decimal MaxMoney = 9999999999999999.99m;
        private const byte ActiveEmployeeStatus = 1;
        private const int MaxAuditSummaryLength = 500;
        private const string SubmittedContractArtifactObjectType =
            "ContractVersionArtifact";
        private const string SubmittedDocxContentType =
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        private const string SubmittedPdfContentType = "application/pdf";
        private static long _syntheticCommentRowVersionSeed = 1000;

        private readonly DbDtctechContext _dbContext;
        private readonly IContractAuditWriter _contractAuditWriter;
        private readonly ICurrentTenant? _currentTenant;
        private readonly CustomerAccessCryptography? _customerAccessCryptography;
        private readonly IContractSubmissionArtifactRenderer?
            _submissionArtifactRenderer;
        private readonly IPrivateFileStorage? _privateFileStorage;

        public ContractService(
            DbDtctechContext dbContext,
            IContractAuditWriter contractAuditWriter)
        {
            _dbContext = dbContext;
            _contractAuditWriter = contractAuditWriter;
        }

        public ContractService(
            DbDtctechContext dbContext,
            IContractAuditWriter contractAuditWriter,
            ICurrentTenant currentTenant,
            CustomerAccessCryptography customerAccessCryptography)
        {
            _dbContext = dbContext;
            _contractAuditWriter = contractAuditWriter;
            _currentTenant = currentTenant;
            _customerAccessCryptography = customerAccessCryptography;
        }

        public ContractService(
            DbDtctechContext dbContext,
            IContractAuditWriter contractAuditWriter,
            ICurrentTenant currentTenant,
            CustomerAccessCryptography customerAccessCryptography,
            IContractSubmissionArtifactRenderer submissionArtifactRenderer,
            IPrivateFileStorage privateFileStorage)
            : this(
                dbContext,
                contractAuditWriter,
                currentTenant,
                customerAccessCryptography)
        {
            _submissionArtifactRenderer = submissionArtifactRenderer;
            _privateFileStorage = privateFileStorage;
        }

        public ContractService(
            DbDtctechContext dbContext,
            IContractAuditWriter contractAuditWriter,
            ICurrentTenant currentTenant,
            IContractSubmissionArtifactRenderer submissionArtifactRenderer,
            IPrivateFileStorage privateFileStorage)
        {
            _dbContext = dbContext;
            _contractAuditWriter = contractAuditWriter;
            _currentTenant = currentTenant;
            _submissionArtifactRenderer = submissionArtifactRenderer;
            _privateFileStorage = privateFileStorage;
        }

        /// <summary>
        /// Lấy danh sách hợp đồng mà nhân viên đăng nhập đang phụ trách.
        /// API danh sách chỉ trả dữ liệu tóm tắt để Frontend tải nhanh.
        /// </summary>
        public async Task<PagedResult<ContractListItemResponse>> GetListAsync(
            ContractFilterRequest filter,
            int employeeId,
            bool canReadTenant = false)
        {
            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đang đăng nhập.");
            }

            if (filter.Page <= 0)
            {
                filter.Page = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 20;
            }

            // Không cho một request tải quá nhiều bản ghi.
            if (filter.PageSize > 100)
            {
                filter.PageSize = 100;
            }

            var offset = ((long)filter.Page - 1) * filter.PageSize;

            if (offset > int.MaxValue)
            {
                throw new ArgumentException(
                    "Số trang vượt giới hạn cho phép.");
            }

            if (filter.Status.HasValue
                && !Enum.IsDefined(filter.Status.Value))
            {
                throw new ArgumentException(
                    "Trạng thái hợp đồng không hợp lệ.");
            }

            if (filter.ContractType.HasValue
                && !Enum.IsDefined(filter.ContractType.Value))
            {
                throw new ArgumentException(
                    "Loại hợp đồng không hợp lệ.");
            }

            /*
             * Join Customer và Employee vì màn hình danh sách
             * cần tên khách hàng và tên người phụ trách.
             */
            var query =
                from contract in _dbContext.TblContracts.AsNoTracking()

                join customer in _dbContext.TblCustomers.AsNoTracking()
                    on contract.CustomerId equals customer.CustomerId

                join employee in _dbContext.TblEmployees.AsNoTracking()
                    on contract.EmployeeId equals employee.EmployeeId

                where canReadTenant || contract.EmployeeId == employeeId

                select new
                {
                    Contract = contract,
                    Customer = customer,
                    Employee = employee
                };

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.Contract.ContractCode != null
                        && x.Contract.ContractCode.Contains(keyword))
                    || x.Contract.ContractName.Contains(keyword)
                    || (x.Contract.ContractNameEn != null
                        && x.Contract.ContractNameEn.Contains(keyword))
                    || (x.Customer.CustomerCode != null
                        && x.Customer.CustomerCode.Contains(keyword))
                    || (x.Customer.CustomerFullName != null
                        && x.Customer.CustomerFullName.Contains(keyword))
                    || (x.Customer.CustomerCompany != null
                        && x.Customer.CustomerCompany.Contains(keyword)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(x =>
                    x.Contract.Status == (byte)filter.Status.Value);
            }

            if (filter.ContractType.HasValue)
            {
                query = query.Where(x =>
                    x.Contract.ContractType ==
                    (byte)filter.ContractType.Value);
            }

            if (filter.CustomerId.HasValue)
            {
                query = query.Where(x =>
                    x.Contract.CustomerId == filter.CustomerId.Value);
            }

            var totalCount = await query.CountAsync();

            var contracts = await query
                .OrderByDescending(x => x.Contract.CreatedDate)
                .ThenByDescending(x => x.Contract.ContractId)
                .Skip((int)offset)
                .Take(filter.PageSize)
                .Select(x => new ContractListItemResponse
                {
                    ContractId = x.Contract.ContractId,
                    ContractCode = x.Contract.ContractCode,
                    ContractName = x.Contract.ContractName,
                    ContractType =
                        (ContractType)x.Contract.ContractType,
                    Status =
                        (ContractStatus)x.Contract.Status,

                    CustomerId = x.Customer.CustomerId,
                    CustomerCode = x.Customer.CustomerCode,
                    CustomerName = x.Customer.CustomerFullName,
                    CustomerCompany = x.Customer.CustomerCompany,

                    ResponsibleEmployeeId = x.Employee.EmployeeId,
                    ResponsibleEmployeeName =
                        x.Employee.EmployeeFullName,

                    CurrentVersionId = x.Contract.CurrentVersionId,

                    CurrentVersionNo = _dbContext.TblContractVersions
                        .Where(version =>
                            version.VersionId ==
                            x.Contract.CurrentVersionId)
                        .Select(version => (int?)version.VersionNo)
                        .FirstOrDefault(),

                    IsCurrentVersionLocked =
                        _dbContext.TblContractVersions
                            .Where(version =>
                                version.VersionId ==
                                x.Contract.CurrentVersionId)
                            .Select(version => version.IsLocked)
                            .FirstOrDefault(),

                    TotalAmount = x.Contract.TotalAmount,
                    CurrencyCode = x.Contract.CurrencyCode,
                    EffectiveDate = x.Contract.EffectiveDate,
                    ExpireDate = x.Contract.ExpireDate,
                    CreatedDate = x.Contract.CreatedDate,
                    UpdatedDate = x.Contract.UpdateDate
                })
                .ToListAsync();

            return new PagedResult<ContractListItemResponse>
            {
                Items = contracts,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        /// <summary>
        /// Tìm hợp đồng cung cấp phần mềm đã Completed
        /// để làm hợp đồng gốc cho bảo trì hoặc duy trì.
        /// </summary>
        public async Task<PagedResult<EligibleParentContractResponse>>
            GetEligibleParentsAsync(
                EligibleParentContractFilterRequest filter,
                int employeeId,
                bool canReadTenant = false)
        {
            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đang đăng nhập.");
            }

            if (filter.CustomerId <= 0)
            {
                throw new ArgumentException(
                    "CustomerId phải lớn hơn 0.");
            }

            // API này chỉ dùng khi tạo hợp đồng bảo trì hoặc duy trì.
            if (filter.TargetContractType !=
                    ContractType.SoftwareMaintenance
                && filter.TargetContractType !=
                    ContractType.SoftwareUpkeep)
            {
                throw new ArgumentException(
                    "TargetContractType phải là hợp đồng bảo trì hoặc duy trì.");
            }

            if (filter.Page <= 0)
            {
                filter.Page = 1;
            }

            if (filter.PageSize <= 0)
            {
                filter.PageSize = 20;
            }

            if (filter.PageSize > 100)
            {
                filter.PageSize = 100;
            }

            var offset = ((long)filter.Page - 1) * filter.PageSize;

            if (offset > int.MaxValue)
            {
                throw new ArgumentException(
                    "Số trang vượt giới hạn cho phép.");
            }

            /*
             * Chỉ hợp đồng cung cấp phần mềm đã hoàn thành
             * mới được làm hợp đồng nguồn.
             */
            var query = _dbContext.TblContracts
                .AsNoTracking()
                .Where(x =>
                    (canReadTenant || x.EmployeeId == employeeId)
                    && x.CustomerId == filter.CustomerId
                    && x.ContractType ==
                        (byte)ContractType.SoftwareSupply
                    && x.Status ==
                        (byte)ContractStatus.Completed);

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim();

                query = query.Where(x =>
                    (x.ContractCode != null
                        && x.ContractCode.Contains(keyword))
                    || x.ContractName.Contains(keyword)
                    || (x.ContractNameEn != null
                        && x.ContractNameEn.Contains(keyword)));
            }

            var totalCount = await query.CountAsync();

            var contracts = await query
                .OrderByDescending(x => x.CreatedDate)
                .ThenByDescending(x => x.ContractId)
                .Skip((int)offset)
                .Take(filter.PageSize)
                .Select(x => new EligibleParentContractResponse
                {
                    ContractId = x.ContractId,
                    ContractCode = x.ContractCode,
                    ContractName = x.ContractName,
                    ContractType = (ContractType)x.ContractType,
                    Status = (ContractStatus)x.Status,
                    EffectiveDate = x.EffectiveDate,
                    ExpireDate = x.ExpireDate
                })
                .ToListAsync();

            return new PagedResult<EligibleParentContractResponse>
            {
                Items = contracts,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        /// <summary>
        /// Tạo đồng thời:
        /// Contract Draft → Version 1 → Item snapshot → Term snapshot.
        /// </summary>
        public async Task<CreateContractResponse> CreateAsync(
            CreateContractRequest request,
            int createdEmployeeId)
        {
            // Kiểm tra dữ liệu đầu vào không cần truy cập database.
            ValidateRequest(request, createdEmployeeId);

            /*
             * DbContext đang bật EnableRetryOnFailure().
             * Vì vậy toàn bộ transaction phải chạy bên trong execution strategy.
             */
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                /*
                 * Transaction phải được tạo bên trong ExecuteAsync().
                 * Khi SQL Server gặp lỗi tạm thời, EF Core có thể chạy lại
                 * toàn bộ khối thao tác này như một đơn vị hoàn chỉnh.
                 */
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    await ValidateEmployeeAsync(createdEmployeeId);

                    var responsibleEmployeeId =
                        request.ResponsibleEmployeeId
                        ?? createdEmployeeId;

                    await ValidateResponsibleEmployeeAsync(
                        responsibleEmployeeId);

                    var customerAudit =
                        await ValidateCustomerAsync(request.CustomerId);
                    await ValidateTemplateAsync(request);
                    await ValidateParentContractAsync(
                        request,
                        createdEmployeeId);
                    await ValidateCatalogSourcesAsync(request.Items);

                    // Lấy điều khoản từ đúng template version đã chọn.
                    var templateTerms = await _dbContext
                        .TblContractTemplateTerms
                        .AsNoTracking()
                        .Where(x =>
                            x.TemplateVersionId ==
                            request.TemplateVersionId)
                        .OrderBy(x => x.DisplayOrder)
                        .ThenBy(x => x.TemplateTermId)
                        .ToListAsync();

                    if (templateTerms.Count == 0)
                    {
                        throw new InvalidOperationException(
                            "Template version chưa có điều khoản để tạo hợp đồng.");
                    }

                    ValidateCreateTermsAgainstTemplate(
                        request,
                        templateTerms);

                    var now = DateTime.UtcNow;
                    var currencyCode = NormalizeCurrencyCode(
                        request.CurrencyCode);

                    /*
                     * Bước 1: Tạo Contract trước để SQL Server sinh ContractId.
                     * CurrentVersionId tạm thời để null vì Version 1 chưa tồn tại.
                     */
                    var contract = new TblContract
                    {
                        CustomerId = request.CustomerId,

                        EmployeeId = responsibleEmployeeId,
                        CreatedEmployeeId = createdEmployeeId,

                        ContractType = (byte)request.ContractType,
                        TemplateVersionId = request.TemplateVersionId,
                        ParentContractId = request.ParentContractId,
                        CurrentVersionId = null,

                        // Sinh sau khi đã lấy được ContractId.
                        ContractCode = null,

                        ContractName = request.ContractName.Trim(),
                        ContractNameEn =
                            NormalizeOptional(request.ContractNameEn),

                        SignDate = null,
                        EffectiveDate = request.EffectiveDate,
                        ExpireDate = request.ExpireDate,

                        // Hợp đồng mới luôn bắt đầu tại Draft.
                        Status = (byte)ContractStatus.Draft,

                        // Backend tính lại từ ContractItem.
                        TotalAmount = 0m,
                        Subtotal = 0m,
                        TotalDiscount = 0m,
                        TotalVat = 0m,

                        CurrencyCode = currencyCode,

                        LanguageMode = (byte)request.LanguageMode,

                        // API này chỉ tạo hợp đồng mới, không tạo legacy.
                        IsLegacy = false,

                        CreatedDate = now
                    };

                    _dbContext.TblContracts.Add(contract);
                    await _dbContext.SaveChangesAsync();

                    // ContractId đảm bảo duy nhất nên ContractCode cũng duy nhất.
                    contract.ContractCode =
                        BuildContractCode(now, contract.ContractId);

                    /*
                     * Bước 2: Tạo Version 1.
                     * Version Draft chưa khóa nên SnapshotJson/Hash để null.
                     */
                    var contractVersion = new TblContractVersion
                    {
                        ContractId = contract.ContractId,
                        VersionNo = 1,
                        SourceVersionId = null,
                        TemplateVersionId = request.TemplateVersionId,
                        ChangeNote = "Khởi tạo hợp đồng.",
                        CurrencyCode = currencyCode,
                        Subtotal = 0m,
                        TotalDiscount = 0m,
                        TotalVat = 0m,
                        TotalAmount = 0m,

                        SnapshotJson = null,
                        SnapshotHash = null,

                        IsLocked = false,
                        LockedDate = null,
                        LockedByEmployeeId = null,

                        CreatedEmployeeId = createdEmployeeId,
                        CreatedDate = now
                    };

                    _dbContext.TblContractVersions.Add(contractVersion);
                    await _dbContext.SaveChangesAsync();

                    /*
                     * Bước 3: Tạo snapshot Product/Service.
                     * Không đọc lại tên hoặc giá từ catalog về sau.
                     */
                    var contractItems = new List<TblContractItem>();
                    var totals = ContractFinancialTotals.Zero;

                    for (var index = 0;
                         index < request.Items.Count;
                         index++)
                    {
                        var requestItem = request.Items[index];

                        var amounts = CalculateItemAmounts(
                            requestItem,
                            currencyCode);

                        totals = AddToContractTotals(
                            totals,
                            amounts);

                        contractItems.Add(new TblContractItem
                        {
                            ContractId = contract.ContractId,
                            VersionId = contractVersion.VersionId,

                            ItemType = (byte)requestItem.ItemType,
                            SourceProductId = requestItem.SourceProductId,
                            SourceServiceId = requestItem.SourceServiceId,

                            ItemCode =
                                NormalizeOptional(requestItem.ItemCode),

                            ItemName = requestItem.ItemName.Trim(),
                            ItemNameEn =
                                NormalizeOptional(requestItem.ItemNameEn),

                            ItemDescription =
                                NormalizeOptional(
                                    requestItem.ItemDescription),

                            ItemDescriptionEn =
                                NormalizeOptional(
                                    requestItem.ItemDescriptionEn),

                            UnitName =
                                NormalizeOptional(requestItem.UnitName),

                            UnitNameEn =
                                NormalizeOptional(requestItem.UnitNameEn),

                            Quantity = requestItem.Quantity,
                            UnitPrice = requestItem.UnitPrice,

                            LineSubtotal = amounts.LineSubtotal,

                            DiscountMode =
                                (byte)requestItem.DiscountMode,

                            DiscountPercent =
                                requestItem.DiscountPercent,

                            FixedDiscountAmount =
                                requestItem.FixedDiscountAmount,

                            DiscountAmount =
                                amounts.DiscountAmount,

                            IsTaxable = requestItem.IsTaxable,
                            VatPercent = requestItem.VatPercent,
                            VatAmount = amounts.VatAmount,
                            LineTotal = amounts.LineTotal,

                            /*
                             * Nếu frontend truyền 0 thì backend tự xếp
                             * theo thứ tự item trong request.
                             */
                            DisplayOrder =
                                requestItem.DisplayOrder > 0
                                    ? requestItem.DisplayOrder
                                    : index + 1,

                            CreatedEmployeeId = createdEmployeeId,
                            CreatedDate = now
                        });
                    }

                    /*
                     * Bước 4: Snapshot điều khoản từ template.
                     * Template thay đổi sau này cũng không ảnh hưởng hợp đồng.
                     */
                    var contractTerms = request.Terms is null
                        ? templateTerms
                            .Select(templateTerm => new TblContractTerm
                            {
                                ContractId = contract.ContractId,
                                VersionId = contractVersion.VersionId,
                                SourceTemplateTermId = templateTerm.TemplateTermId,
                                TermCode = templateTerm.TermCode,
                                TermTitle = templateTerm.TermTitle,
                                TermTitleEn = templateTerm.TermTitleEn,
                                TermContent = templateTerm.TermContent,
                                TermContentEn = templateTerm.TermContentEn,
                                IsNegotiable = templateTerm.IsNegotiable,
                                DisplayOrder = templateTerm.DisplayOrder,
                                CreatedEmployeeId = createdEmployeeId,
                                CreatedDate = now
                            })
                            .ToList()
                        : request.Terms
                            .Select((requestTerm, index) =>
                                new TblContractTerm
                                {
                                    ContractId = contract.ContractId,
                                    VersionId = contractVersion.VersionId,
                                    SourceTemplateTermId =
                                        requestTerm.SourceTemplateTermId,
                                    TermCode = requestTerm.TermCode
                                        .Trim()
                                        .ToUpperInvariant(),
                                    TermTitle = requestTerm.TermTitle.Trim(),
                                    TermTitleEn = NormalizeOptional(
                                        requestTerm.TermTitleEn),
                                    TermContent = NormalizeOptional(
                                        requestTerm.TermContent),
                                    TermContentEn = NormalizeOptional(
                                        requestTerm.TermContentEn),
                                    IsNegotiable = requestTerm.IsNegotiable,
                                    DisplayOrder = requestTerm.DisplayOrder > 0
                                        ? requestTerm.DisplayOrder
                                        : index + 1,
                                    CreatedEmployeeId = createdEmployeeId,
                                    CreatedDate = now
                                })
                            .ToList();

                    _dbContext.TblContractItems.AddRange(contractItems);
                    _dbContext.TblContractTerms.AddRange(contractTerms);

                    /*
                     * Contract trỏ trực tiếp đến Version 1 vừa tạo.
                     */
                    contract.CurrentVersionId =
                        contractVersion.VersionId;

                    ApplyFinancialTotals(
                        contract,
                        contractVersion,
                        currencyCode,
                        totals);

                    var createdItemsAudit = BuildAuditSummary(
                        contractItems.Select(item => BuildItemAuditEntry(
                            null,
                            item.ItemCode,
                            item.ItemName)).ToList());
                    var createdTermsAudit = BuildAuditSummary(
                        contractTerms.Select(term => BuildTermAuditEntry(
                            null,
                            term.TermCode,
                            term.TermTitle)).ToList());

                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            contractVersion.VersionId,
                            createdEmployeeId,
                            ContractAuditActionTypes.ContractCreated,
                            ContractAuditResults.Succeeded,
                            now,
                            NewContractStatus: contract.Status,
                            NewResponsibleEmployeeId:
                                responsibleEmployeeId,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: contract.ContractId,
                            NewValues: ContractAuditValues.Create(
                                ("Status", contract.Status),
                                ("ResponsibleEmployeeId", responsibleEmployeeId),
                                ("CurrentVersionId", contractVersion.VersionId),
                                ("CustomerId", customerAudit.CustomerId),
                                ("CustomerName", customerAudit.DisplayName),
                                ("ContractType", contract.ContractType),
                                ("LanguageMode", contract.LanguageMode),
                                ("TemplateVersionId", request.TemplateVersionId),
                                ("ParentContractId", contract.ParentContractId),
                                ("ContractName", BuildAuditSafeText(contract.ContractName)),
                                ("ContractNameEn", BuildAuditSafeText(contract.ContractNameEn)),
                                ("EffectiveDate", contract.EffectiveDate),
                                ("ExpireDate", contract.ExpireDate),
                                ("CurrencyCode", contract.CurrencyCode),
                                ("Subtotal", contract.Subtotal),
                                ("TotalDiscount", contract.TotalDiscount),
                                ("TotalVat", contract.TotalVat),
                                ("TotalAmount", contract.TotalAmount),
                                ("ItemCount", contractItems.Count),
                                ("TermCount", contractTerms.Count),
                                ("AddedItems", createdItemsAudit),
                                ("AddedTerms", createdTermsAudit))),

                        new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            contractVersion.VersionId,
                            createdEmployeeId,
                            ContractAuditActionTypes.ResponsibleAssigned,
                            ContractAuditResults.Succeeded,
                            now,
                            NewContractStatus: contract.Status,
                            PreviousResponsibleEmployeeId: null,
                            NewResponsibleEmployeeId:
                                responsibleEmployeeId,
                            Reason: null,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: contract.ContractId,
                            NewValues: ContractAuditValues.Create(
                                ("Status", contract.Status),
                                ("ResponsibleEmployeeId", responsibleEmployeeId),
                                ("CurrentVersionId", contractVersion.VersionId),
                                ("CustomerId", customerAudit.CustomerId),
                                ("CustomerName", customerAudit.DisplayName),
                                ("ContractName", BuildAuditSafeText(contract.ContractName)),
                                ("ContractNameEn", BuildAuditSafeText(contract.ContractNameEn)),
                                ("EffectiveDate", contract.EffectiveDate),
                                ("ExpireDate", contract.ExpireDate),
                                ("CurrencyCode", contract.CurrencyCode),
                                ("Subtotal", contract.Subtotal),
                                ("TotalDiscount", contract.TotalDiscount),
                                ("TotalVat", contract.TotalVat),
                                ("TotalAmount", contract.TotalAmount),
                                ("ItemCount", contractItems.Count),
                                ("TermCount", contractTerms.Count)))
                    ]);

                    await _dbContext.SaveChangesAsync();

                    // Chỉ commit khi toàn bộ Contract, Version, Item và Term thành công.
                    await transaction.CommitAsync();

                    return new CreateContractResponse
                    {
                        ContractId = contract.ContractId,
                        ContractCode = contract.ContractCode!,
                        ContractName = contract.ContractName,
                        Status = ContractStatus.Draft,

                        CurrentVersionId =
                            contractVersion.VersionId,

                        VersionNo = contractVersion.VersionNo,
                        CustomerId = contract.CustomerId,
                        ContractType = request.ContractType,

                        TemplateVersionId =
                            request.TemplateVersionId,

                        TotalAmount = contract.TotalAmount,
                        Subtotal = contract.Subtotal,
                        TotalDiscount = contract.TotalDiscount,
                        TotalVat = contract.TotalVat,
                        TotalPayment = contract.TotalAmount,
                        CurrencyCode = contract.CurrencyCode,
                        LanguageMode = request.LanguageMode,

                        EmployeeId = responsibleEmployeeId,
                        CreatedDate = contract.CreatedDate,

                        ItemCount = contractItems.Count,
                        TermCount = contractTerms.Count,

                        // Trả RowVersion để Frontend có thể cập nhật Draft ngay.
                        RowVersion = EncodeRowVersion(contract.RowVersion),

                        CurrentVersionRowVersion =
                            EncodeRowVersion(contractVersion.RowVersion)
                    };
                }
                catch (Exception exception)
                {
                    /*
                     * Execution strategy có thể chạy lại delegate với cùng
                     * DbContext. Rollback database không tự xóa tracked state,
                     * nên phải dọn toàn bộ attempt thất bại trước khi retry.
                     * Rollback/cleanup không được che mất exception gốc.
                     */
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch
                    {
                        // Giữ exception gốc của operation.
                    }

                    try
                    {
                        _dbContext.ChangeTracker.Clear();
                    }
                    catch
                    {
                        // Giữ exception gốc của operation.
                    }

                    System.Runtime.ExceptionServices.ExceptionDispatchInfo
                        .Capture(exception)
                        .Throw();

                    throw;
                }
            });
        }

        /// <summary>
        /// Chuyển giao người phụ trách hiện tại của Contract.
        /// </summary>
        public async Task<TransferContractResponsibilityResponse>
            TransferResponsibilityAsync(
                int contractId,
                TransferContractResponsibilityRequest request,
                int actorEmployeeId)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (actorEmployeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            if (request.NewResponsibleEmployeeId <= 0)
            {
                throw new ArgumentException(
                    "NewResponsibleEmployeeId phải lớn hơn 0.");
            }

            var reason = request.Reason?.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Lý do chuyển giao không được để trống.");
            }

            if (reason.Length > 1000)
            {
                throw new ArgumentException(
                    "Lý do chuyển giao không được vượt quá 1000 ký tự.");
            }

            var expectedRowVersion = DecodeRowVersion(
                request.RowVersion,
                nameof(request.RowVersion));

            var occurredAt = DateTime.UtcNow;
            var strategy =
                _dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction =
                        await _dbContext.Database
                            .BeginTransactionAsync();

                    try
                    {
                        var actor = await _dbContext.TblEmployees
                            .AsNoTracking()
                            .Where(x =>
                                x.EmployeeId == actorEmployeeId)
                            .Select(x => new
                            {
                                x.EmployeeId,
                                x.EmployeeType
                            })
                            .FirstOrDefaultAsync();

                        if (actor == null)
                        {
                            throw new UnauthorizedAccessException(
                                "Không xác định được nhân viên đăng nhập.");
                        }

                        var contract =
                            await _dbContext.TblContracts
                                .FirstOrDefaultAsync(x =>
                                    x.ContractId == contractId);

                        if (contract == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy hợp đồng.");
                        }

                        var canTransfer =
                            contract.EmployeeId == actorEmployeeId
                            || actor.EmployeeType ==
                                (byte)EmployeeType.Manager
                            || actor.EmployeeType ==
                                (byte)EmployeeType.AdminOfficer;

                        if (!canTransfer)
                        {
                            throw new UnauthorizedAccessException(
                                "Bạn không có quyền chuyển giao " +
                                "người phụ trách hợp đồng.");
                        }

                        /*
                         * Chỉ actor đang có quyền mới được nhận thông tin
                         * concurrency của Contract.
                         */
                        EnsureRowVersionMatches(
                            contract.RowVersion,
                            expectedRowVersion,
                            "Hợp đồng");

                        await ValidateResponsibleEmployeeAsync(
                            request.NewResponsibleEmployeeId);

                        if (contract.EmployeeId ==
                            request.NewResponsibleEmployeeId)
                        {
                            throw new InvalidOperationException(
                                "Nhân viên được chọn đang là " +
                                "người phụ trách hợp đồng.");
                        }

                        var previousResponsibleEmployeeId =
                            contract.EmployeeId;

                        _dbContext.Entry(contract)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedRowVersion;

                        contract.EmployeeId =
                            request.NewResponsibleEmployeeId;
                        contract.UpdatedEmployeeId = actorEmployeeId;
                        contract.UpdateDate = occurredAt;

                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                                contract.ContractId,
                                contract.CurrentVersionId,
                                actorEmployeeId,
                                ContractAuditActionTypes
                                    .ResponsibilityTransferred,
                                ContractAuditResults.Succeeded,
                                occurredAt,
                                PreviousResponsibleEmployeeId:
                                    previousResponsibleEmployeeId,
                                NewResponsibleEmployeeId:
                                    request.NewResponsibleEmployeeId,
                                Reason: reason,
                                SubjectType: ContractAuditSubjectTypes.Contract,
                                SubjectId: contract.ContractId,
                                PreviousValues: ContractAuditValues.Create(
                                    ("Status", contract.Status),
                                    ("ResponsibleEmployeeId", previousResponsibleEmployeeId),
                                    ("CurrentVersionId", contract.CurrentVersionId)),
                                NewValues: ContractAuditValues.Create(
                                    ("Status", contract.Status),
                                    ("ResponsibleEmployeeId", request.NewResponsibleEmployeeId),
                                    ("CurrentVersionId", contract.CurrentVersionId)))
                        ]);

                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return
                            new TransferContractResponsibilityResponse
                            {
                                ContractId = contract.ContractId,
                                PreviousResponsibleEmployeeId =
                                    previousResponsibleEmployeeId,
                                ResponsibleEmployeeId =
                                    contract.EmployeeId,
                                TransferredByEmployeeId =
                                    actorEmployeeId,
                                TransferredAt = occurredAt,
                                RowVersion =
                                    EncodeRowVersion(
                                        contract.RowVersion)
                            };
                    }
                    catch (Exception exception)
                    {
                        try
                        {
                            await transaction.RollbackAsync();
                        }
                        catch
                        {
                            // Giữ exception gốc của operation.
                        }

                        try
                        {
                            _dbContext.ChangeTracker.Clear();
                        }
                        catch
                        {
                            // Giữ exception gốc của operation.
                        }

                        System.Runtime.ExceptionServices
                            .ExceptionDispatchInfo
                            .Capture(exception)
                            .Throw();

                        throw;
                    }
                });
            }
            catch (DbUpdateConcurrencyException exception)
            {
                /*
                 * Success transaction đã rollback và tracker đã sạch.
                 * Failed audit được persist bằng transaction riêng.
                 */
                await PersistResponsibilityTransferConflictAuditAsync(
                    contractId,
                    actorEmployeeId,
                    occurredAt);

                throw new DbUpdateConcurrencyException(
                    "Hợp đồng đã được cập nhật. " +
                    "Vui lòng tải lại dữ liệu trước khi chuyển giao.",
                    exception);
            }
        }

        private async Task
            PersistResponsibilityTransferConflictAuditAsync(
                int contractId,
                int actorEmployeeId,
                DateTime occurredAt)
        {
            var strategy =
                _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _dbContext.Database
                        .BeginTransactionAsync();

                try
                {
                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contractId,
                            VersionId: null,
                            actorEmployeeId,
                            ContractAuditActionTypes
                                .ResponsibilityTransferred,
                            ContractAuditResults
                                .ConcurrencyConflict,
                            occurredAt,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: contractId,
                            FailureCode: ContractAuditFailureCodes.StaleRowVersion)
                    ]);

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception exception)
                {
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch
                    {
                        // Giữ exception của failed-audit operation.
                    }

                    try
                    {
                        _dbContext.ChangeTracker.Clear();
                    }
                    catch
                    {
                        // Giữ exception của failed-audit operation.
                    }

                    System.Runtime.ExceptionServices
                        .ExceptionDispatchInfo
                        .Capture(exception)
                        .Throw();

                    throw;
                }
            });
        }

        private async Task PersistContractConcurrencyAuditAsync(
            int contractId,
            int? versionId,
            int actorEmployeeId,
            string requestedActionType,
            DateTime occurredAt)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database
                    .BeginTransactionAsync();
                try
                {
                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contractId,
                            versionId,
                            actorEmployeeId,
                            requestedActionType,
                            ContractAuditResults.ConcurrencyConflict,
                            occurredAt,
                            SubjectType: versionId.HasValue
                                ? ContractAuditSubjectTypes.ContractVersion
                                : ContractAuditSubjectTypes.Contract,
                            SubjectId: versionId ?? contractId,
                            FailureCode: ContractAuditFailureCodes.StaleRowVersion)
                    ]);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await RollbackAndClearAsync(transaction);
                    throw;
                }
            });
        }


        /// <summary>
        /// Lấy chi tiết hợp đồng tại version hiện hành.
        /// </summary>
        public async Task<ContractDetailResponse> GetDetailAsync(
            int contractId,
            int employeeId,
            bool canReadTenant = false)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đang đăng nhập.");
            }

            /*
             * Lọc EmployeeId ngay trong truy vấn.
             *
             * Như vậy:
             * - Người khác không đọc được hợp đồng.
             * - API không tiết lộ hợp đồng có tồn tại hay không.
             * - Không cần tải hợp đồng ra rồi mới kiểm tra quyền.
             */
            var header = await (
                from contracts in _dbContext.TblContracts.AsNoTracking()

                join customer in _dbContext.TblCustomers.AsNoTracking()
                    on contracts.CustomerId equals customer.CustomerId

                join responsibleEmployee in
                    _dbContext.TblEmployees.AsNoTracking()
                    on contracts.EmployeeId
                    equals responsibleEmployee.EmployeeId

                where contracts.ContractId == contractId
                      && (canReadTenant
                          || contracts.EmployeeId == employeeId)

                select new
                {
                    Contract = contracts,
                    Customer = customer,
                    ResponsibleEmployee = responsibleEmployee
                })
                .FirstOrDefaultAsync();

            /*
             * - Hợp đồng không tồn tại.
             * - Hợp đồng tồn tại nhưng người dùng không có quyền.
             */
            if (header == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng.");
            }

            var contract = header.Contract;

            if (contract.CurrentVersionId is null)
            {
                throw new InvalidOperationException(
                    "Hợp đồng chưa có version hiện hành.");
            }

            /*
             * Không lấy MAX(VersionNo).
             * Luôn đọc đúng version được Contract.CurrentVersionId trỏ tới.
             */
            var version = await _dbContext.TblContractVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.VersionId == contract.CurrentVersionId.Value
                    && x.ContractId == contract.ContractId);

            if (version == null)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy version hiện hành của hợp đồng.");
            }

            /*
             * Items phải đồng thời thuộc đúng Contract và CurrentVersion.
             * Không đọc lại tên hoặc giá từ Product/Service catalog.
             */
            var items = await _dbContext.TblContractItems
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contract.ContractId
                    && x.VersionId == version.VersionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ContractItemId)
                .ToListAsync();

            /*
             * Terms cũng được đọc từ snapshot của hợp đồng,
             * không đọc lại từ ContractTemplateTerm.
             */
            var terms = await _dbContext.TblContractTerms
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contract.ContractId
                    && x.VersionId == version.VersionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.TermId)
                .ToListAsync();

            var comments = await LoadCommentResponsesAsync(
                contract.ContractId,
                version.VersionId);

            var approvalReadiness = await GetApprovalReadinessAsync(
                contract,
                version,
                items,
                terms,
                comments.Count(comment =>
                    comment.State == ContractNegotiationCommentState.Open));

            return new ContractDetailResponse
            {
                ContractId = contract.ContractId,
                ContractCode = contract.ContractCode,
                ContractName = contract.ContractName,
                ContractNameEn = contract.ContractNameEn,

                ContractType = (ContractType)contract.ContractType,
                TemplateVersionId = contract.TemplateVersionId,
                ParentContractId = contract.ParentContractId,
                Status = (ContractStatus)contract.Status,

                SignDate = contract.SignDate,
                EffectiveDate = contract.EffectiveDate,
                ExpireDate = contract.ExpireDate,

                TotalAmount = contract.TotalAmount,
                Subtotal = contract.Subtotal,
                TotalDiscount = contract.TotalDiscount,
                TotalVat = contract.TotalVat,
                TotalPayment = contract.TotalAmount,
                CurrencyCode = contract.CurrencyCode,

                LanguageMode =
                    (ContractLanguageMode)contract.LanguageMode,

                IsLegacy = contract.IsLegacy,
                CreatedEmployeeId = contract.CreatedEmployeeId,
                CreatedDate = contract.CreatedDate,
                UpdatedDate = contract.UpdateDate,

                RowVersion = EncodeRowVersion(contract.RowVersion),

                Customer = new ContractCustomerSummaryResponse
                {
                    CustomerId = header.Customer.CustomerId,
                    CustomerCode = header.Customer.CustomerCode,
                    CustomerFullName = header.Customer.CustomerFullName,
                    CustomerCompany = header.Customer.CustomerCompany,
                    CustomerTaxCode = header.Customer.CustomerTaxCode,
                    CustomerEmail = header.Customer.CustomerEmail,
                    CustomerMobile = header.Customer.CustomerMobile,
                    CustomerAddress = header.Customer.CustomerAddress
                },

                ResponsibleEmployee =
                    new ContractEmployeeSummaryResponse
                    {
                        EmployeeId =
                            header.ResponsibleEmployee.EmployeeId,

                        EmployeeCode =
                            header.ResponsibleEmployee.EmployeeCode,

                        EmployeeFullName =
                            header.ResponsibleEmployee.EmployeeFullName,

                        EmployeeEmail =
                            header.ResponsibleEmployee.EmployeeEmail,

                        EmployeeMobile =
                            header.ResponsibleEmployee.EmployeeMobile
                    },

                CurrentVersion = new ContractVersionDetailResponse
                {
                    VersionId = version.VersionId,
                    VersionNo = version.VersionNo,
                    SourceVersionId = version.SourceVersionId,
                    TemplateVersionId = version.TemplateVersionId,
                    ChangeNote = version.ChangeNote,
                    CurrencyCode = version.CurrencyCode,
                    Subtotal = version.Subtotal,
                    TotalDiscount = version.TotalDiscount,
                    TotalVat = version.TotalVat,
                    TotalPayment = version.TotalAmount,
                    SnapshotHash = version.SnapshotHash,

                    IsLocked = version.IsLocked,
                    LockedDate = version.LockedDate,
                    LockedByEmployeeId = version.LockedByEmployeeId,

                    CreatedEmployeeId = version.CreatedEmployeeId,
                    CreatedDate = version.CreatedDate,
                    RowVersion = EncodeRowVersion(version.RowVersion),

                    Items = items
                        .Select(item => new ContractItemDetailResponse
                        {
                            ContractItemId = item.ContractItemId,

                            ItemType =
                                (ContractItemType)item.ItemType,

                            SourceProductId = item.SourceProductId,
                            SourceServiceId = item.SourceServiceId,
                            ItemCode = item.ItemCode,
                            ItemName = item.ItemName,
                            ItemNameEn = item.ItemNameEn,
                            ItemDescription = item.ItemDescription,

                            ItemDescriptionEn =
                                item.ItemDescriptionEn,

                            UnitName = item.UnitName,
                            UnitNameEn = item.UnitNameEn,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            LineSubtotal = item.LineSubtotal,

                            DiscountMode =
                                (ContractItemDiscountMode)
                                    item.DiscountMode,

                            DiscountPercent =
                                item.DiscountPercent,

                            FixedDiscountAmount =
                                item.FixedDiscountAmount,

                            DiscountAmount = item.DiscountAmount,
                            IsTaxable = item.IsTaxable,
                            VatPercent = item.VatPercent,
                            VatAmount = item.VatAmount,
                            LineTotal = item.LineTotal,
                            DisplayOrder = item.DisplayOrder,
                            RowVersion = EncodeRowVersion(item.RowVersion)
                        })
                        .ToList(),

                    Terms = terms
                        .Select(term => new ContractTermDetailResponse
                        {
                            TermId = term.TermId,

                            SourceTemplateTermId =
                                term.SourceTemplateTermId,

                            TermCode = term.TermCode,
                            TermTitle = term.TermTitle,
                            TermTitleEn = term.TermTitleEn,
                            TermContent = term.TermContent,
                            TermContentEn = term.TermContentEn,
                            IsNegotiable = term.IsNegotiable,
                            DisplayOrder = term.DisplayOrder,
                            RowVersion = EncodeRowVersion(term.RowVersion)
                        })
                        .ToList(),

                    Comments = comments
                },

                ApprovalReadiness = approvalReadiness
            };
        }

        /// <summary>
        /// Cập nhật toàn bộ nội dung của hợp đồng khi còn là Draft.
        /// </summary>
        public async Task<ContractDetailResponse> UpdateDraftAsync(
            int contractId,
            UpdateContractDraftRequest request,
            int employeeId)
        {
            ValidateUpdateRequest(contractId, request, employeeId);

            /*
             * DbContext đang bật EnableRetryOnFailure().
             * Vì vậy transaction phải nằm trong execution strategy.
             */
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                        /*
                         * Lọc EmployeeId ngay từ truy vấn để bảo vệ dữ liệu:
                         * người khác không được cập nhật hợp đồng.
                         */
                        var contract = await _dbContext.TblContracts
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contractId
                                && x.EmployeeId == employeeId);

                        if (contract == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy hợp đồng.");
                        }

                        var currentStatus = (ContractStatus)contract.Status;

                        if (!ContractLifecyclePolicy.CanEditContent(currentStatus))
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng ở trạng thái hiện tại không được sửa nội dung.");
                        }

                        if (contract.IsLegacy)
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng legacy không được chỉnh sửa bằng API này.");
                        }

                        if (!contract.CurrentVersionId.HasValue)
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng chưa có version hiện hành.");
                        }

                        /*
                         * Nếu CurrentVersionId đã thay đổi thì client đang sửa
                         * dựa trên dữ liệu cũ.
                         */
                        if (contract.CurrentVersionId.Value !=
                            request.CurrentVersionId)
                        {
                            throw new DbUpdateConcurrencyException(
                                "Version hiện hành đã thay đổi.");
                        }

                        var version = await _dbContext.TblContractVersions
                            .FirstOrDefaultAsync(x =>
                                x.VersionId == request.CurrentVersionId
                                && x.ContractId == contract.ContractId);

                        if (version == null)
                        {
                            throw new InvalidOperationException(
                                "Không tìm thấy version hiện hành.");
                        }

                        if (version.IsLocked)
                        {
                            throw new InvalidOperationException(
                                "Version đã bị khóa, không thể cập nhật trực tiếp.");
                        }

                        /*
                         * Kiểm tra optimistic concurrency cho Contract.
                         */
                        var expectedContractRowVersion =
                            DecodeRowVersion(
                                request.RowVersion,
                                nameof(request.RowVersion));

                        EnsureRowVersionMatches(
                            contract.RowVersion,
                            expectedContractRowVersion,
                            "Hợp đồng");

                        _dbContext.Entry(contract)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedContractRowVersion;

                        /*
                         * Kiểm tra optimistic concurrency cho CurrentVersion.
                         */
                        var expectedVersionRowVersion =
                            DecodeRowVersion(
                                request.CurrentVersionRowVersion,
                                nameof(request.CurrentVersionRowVersion));

                        EnsureRowVersionMatches(
                            version.RowVersion,
                            expectedVersionRowVersion,
                            "Version hợp đồng");

                        var versionEntry = _dbContext.Entry(version);

                        versionEntry
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedVersionRowVersion;

                        var hasEverBeenShared = await _dbContext
                            .TblContractCustomerAccessLinks
                            .AsNoTracking()
                            .AnyAsync(link =>
                                link.ContractId == contract.ContractId
                                && link.VersionId == version.VersionId
                                && link.ActivatedAt.HasValue);

                        if (hasEverBeenShared)
                        {
                            throw new BusinessRuleException(
                                StatusCodes.Status409Conflict,
                                ContractApprovalReadinessCodes
                                    .CurrentVersionAlreadyShared,
                                "Version hiện tại đã được chia sẻ với khách hàng. Hãy tạo vòng đàm phán mới để chỉnh sửa.");
                        }

                        /*
                         * Đánh dấu version đã tham gia lần cập nhật này.
                         * SQL Server sẽ sinh RowVersion mới cho version.
                         */
                        versionEntry
                            .Property(x => x.ChangeNote)
                            .IsModified = true;

                        var requestedCustomerAudit =
                            await ValidateCustomerAsync(request.CustomerId);

                        var previousCustomerAudit =
                            contract.CustomerId == request.CustomerId
                                ? requestedCustomerAudit
                                : await GetCustomerAuditSnapshotAsync(
                                    contract.CustomerId);

                        await ValidateParentContractForCustomerAsync(
                            contract.ParentContractId,
                            request.CustomerId);

                        ValidateBilingualUpdate(contract, request);

                        await ValidateCatalogSourcesAsync(
                            request.Items
                                .Where(x => !x.ContractItemId.HasValue)
                                .Cast<CreateContractItemRequest>()
                                .ToList());

                        var existingItems = await _dbContext.TblContractItems
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == version.VersionId)
                            .ToListAsync();

                        var existingTerms = await _dbContext.TblContractTerms
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == version.VersionId)
                            .ToListAsync();

                        var previousAuditValues = ContractAuditValues.Create(
                            ("Status", contract.Status),
                            ("ResponsibleEmployeeId", contract.EmployeeId),
                            ("CurrentVersionId", contract.CurrentVersionId),
                            ("CustomerId", previousCustomerAudit.CustomerId),
                            ("CustomerName", previousCustomerAudit.DisplayName),
                            ("ContractName", BuildAuditSafeText(contract.ContractName)),
                            ("ContractNameEn", BuildAuditSafeText(contract.ContractNameEn)),
                            ("EffectiveDate", contract.EffectiveDate),
                            ("ExpireDate", contract.ExpireDate),
                            ("CurrencyCode", contract.CurrencyCode),
                            ("Subtotal", contract.Subtotal),
                            ("TotalDiscount", contract.TotalDiscount),
                            ("TotalVat", contract.TotalVat),
                            ("TotalAmount", contract.TotalAmount),
                            ("ItemCount", existingItems.Count),
                            ("TermCount", existingTerms.Count));

                        var addedItemAudits = new List<string>();
                        var updatedItemAudits = new List<string>();
                        var removedItemAudits = new List<string>();
                        var addedTermAudits = new List<string>();
                        var updatedTermAudits = new List<string>();
                        var removedTermAudits = new List<string>();

                        var existingItemById = existingItems
                            .ToDictionary(x => x.ContractItemId);

                        var existingTermById = existingTerms
                            .ToDictionary(x => x.TermId);

                        var requestedItemIds = request.Items
                            .Where(x => x.ContractItemId.HasValue)
                            .Select(x => x.ContractItemId!.Value)
                            .ToHashSet();

                        var requestedTermIds = request.Terms
                            .Where(x => x.TermId.HasValue)
                            .Select(x => x.TermId!.Value)
                            .ToHashSet();

                        var now = DateTime.UtcNow;
                        var currencyCode = NormalizeCurrencyCode(
                            request.CurrencyCode);
                        var totals = ContractFinancialTotals.Zero;

                        /*
                         * Thêm mới hoặc cập nhật Items.
                         */
                        for (var index = 0;
                             index < request.Items.Count;
                             index++)
                        {
                            var requestItem = request.Items[index];
                            var amounts = CalculateItemAmounts(
                                requestItem,
                                currencyCode);

                            totals = AddToContractTotals(
                                totals,
                                amounts);

                            var displayOrder =
                                requestItem.DisplayOrder > 0
                                    ? requestItem.DisplayOrder
                                    : index + 1;

                            TblContractItem item;

                            if (requestItem.ContractItemId.HasValue)
                            {
                                if (!existingItemById.TryGetValue(
                                        requestItem.ContractItemId.Value,
                                        out item!))
                                {
                                    throw new ArgumentException(
                                        $"Item {requestItem.ContractItemId.Value} " +
                                        "không thuộc version hiện hành.");
                                }

                                var expectedItemRowVersion =
                                    DecodeRowVersion(
                                        requestItem.RowVersion,
                                        $"Items[{index}].RowVersion");

                                EnsureRowVersionMatches(
                                    item.RowVersion,
                                    expectedItemRowVersion,
                                    $"Item {item.ContractItemId}");

                                _dbContext.Entry(item)
                                    .Property(x => x.RowVersion)
                                    .OriginalValue = expectedItemRowVersion;

                                if (item.ItemType != (byte)requestItem.ItemType
                                    || item.SourceProductId != requestItem.SourceProductId
                                    || item.SourceServiceId != requestItem.SourceServiceId)
                                {
                                    throw new InvalidOperationException(
                                        "Không được thay đổi loại hoặc nguồn Catalog của item đã lưu.");
                                }

                                if (item.SourceProductId.HasValue
                                    || item.SourceServiceId.HasValue)
                                {
                                    requestItem.ItemCode = item.ItemCode;
                                }

                                var changedFields = GetChangedItemFields(
                                    item,
                                    requestItem,
                                    displayOrder);

                                if (changedFields.Count > 0)
                                {
                                    updatedItemAudits.Add(
                                        BuildItemAuditEntry(
                                            item.ContractItemId,
                                            requestItem.ItemCode,
                                            requestItem.ItemName,
                                            changedFields));
                                }

                                item.UpdatedEmployeeId = employeeId;
                                item.UpdatedDate = now;
                            }
                            else
                            {
                                item = new TblContractItem
                                {
                                    ContractId = contract.ContractId,
                                    VersionId = version.VersionId,
                                    CreatedEmployeeId = employeeId,
                                    CreatedDate = now
                                };

                                _dbContext.TblContractItems.Add(item);

                                addedItemAudits.Add(
                                    BuildItemAuditEntry(
                                        null,
                                        requestItem.ItemCode,
                                        requestItem.ItemName));
                            }

                            ApplyItemSnapshot(
                                item,
                                requestItem,
                                amounts,
                                displayOrder);
                        }

                        /*
                         * Item cũ không còn trong request sẽ bị xóa.
                         */
                        var removedItems = existingItems
                            .Where(x =>
                                !requestedItemIds.Contains(x.ContractItemId))
                            .ToList();

                        removedItemAudits.AddRange(
                            removedItems.Select(item =>
                                BuildItemAuditEntry(
                                    item.ContractItemId,
                                    item.ItemCode,
                                    item.ItemName)));

                        _dbContext.TblContractItems.RemoveRange(removedItems);

                        /*
                         * Thêm mới hoặc cập nhật Terms.
                         */
                        for (var index = 0;
                             index < request.Terms.Count;
                             index++)
                        {
                            var requestTerm = request.Terms[index];
                            var displayOrder =
                                requestTerm.DisplayOrder > 0
                                    ? requestTerm.DisplayOrder
                                    : index + 1;

                            TblContractTerm term;

                            if (requestTerm.TermId.HasValue)
                            {
                                if (!existingTermById.TryGetValue(
                                        requestTerm.TermId.Value,
                                        out term!))
                                {
                                    throw new ArgumentException(
                                        $"Term {requestTerm.TermId.Value} " +
                                        "không thuộc version hiện hành.");
                                }

                                var expectedTermRowVersion =
                                    DecodeRowVersion(
                                        requestTerm.RowVersion,
                                        $"Terms[{index}].RowVersion");

                                EnsureRowVersionMatches(
                                    term.RowVersion,
                                    expectedTermRowVersion,
                                    $"Term {term.TermId}");

                                _dbContext.Entry(term)
                                    .Property(x => x.RowVersion)
                                    .OriginalValue = expectedTermRowVersion;

                                /*
                                 * TermCode là mã ổn định.
                                 * Không cho đổi mã của term đã tồn tại.
                                 */
                                if (!string.Equals(
                                        term.TermCode,
                                        requestTerm.TermCode.Trim(),
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidOperationException(
                                        $"Không được thay đổi TermCode của term {term.TermId}.");
                                }

                                var changedFields = GetChangedTermFields(
                                    term,
                                    requestTerm,
                                    displayOrder);

                                if (changedFields.Count > 0)
                                {
                                    updatedTermAudits.Add(
                                        BuildTermAuditEntry(
                                            term.TermId,
                                            requestTerm.TermCode,
                                            requestTerm.TermTitle,
                                            changedFields));
                                }

                                term.UpdatedEmployeeId = employeeId;
                                term.UpdatedDate = now;
                            }
                            else
                            {
                                term = new TblContractTerm
                                {
                                    ContractId = contract.ContractId,
                                    VersionId = version.VersionId,
                                    SourceTemplateTermId = null,

                                    TermCode = requestTerm.TermCode
                                        .Trim()
                                        .ToUpperInvariant(),

                                    CreatedEmployeeId = employeeId,
                                    CreatedDate = now
                                };

                                _dbContext.TblContractTerms.Add(term);

                                addedTermAudits.Add(
                                    BuildTermAuditEntry(
                                        null,
                                        requestTerm.TermCode,
                                        requestTerm.TermTitle));
                            }

                            term.TermTitle = requestTerm.TermTitle.Trim();
                            term.TermTitleEn =
                                NormalizeOptional(requestTerm.TermTitleEn);

                            term.TermContent =
                                NormalizeOptional(requestTerm.TermContent);

                            term.TermContentEn =
                                NormalizeOptional(requestTerm.TermContentEn);

                            term.IsNegotiable = requestTerm.IsNegotiable;

                            term.DisplayOrder = displayOrder;
                        }

                        /*
                         * Term cũ không còn trong request sẽ bị xóa.
                         */
                        var removedTerms = existingTerms
                            .Where(x => !requestedTermIds.Contains(x.TermId))
                            .ToList();

                        removedTermAudits.AddRange(
                            removedTerms.Select(term =>
                                BuildTermAuditEntry(
                                    term.TermId,
                                    term.TermCode,
                                    term.TermTitle)));

                        _dbContext.TblContractTerms.RemoveRange(removedTerms);

                        /*
                         * Cập nhật phần header của Contract.
                         *
                         * ContractType, TemplateVersionId, LanguageMode,
                         * Status và ContractCode không được thay đổi tại API này.
                         */
                        contract.CustomerId = request.CustomerId;
                        contract.ContractName = request.ContractName.Trim();

                        contract.ContractNameEn =
                            NormalizeOptional(request.ContractNameEn);

                        contract.EffectiveDate = request.EffectiveDate;
                        contract.ExpireDate = request.ExpireDate;

                        ApplyFinancialTotals(
                            contract,
                            version,
                            currencyCode,
                            totals);
                        contract.UpdatedEmployeeId = employeeId;
                        contract.UpdateDate = now;

                        var previousAuditValuesWithRemovedEntries =
                            previousAuditValues.ToDictionary(
                                entry => entry.Key,
                                entry => entry.Value,
                                StringComparer.Ordinal);

                        previousAuditValuesWithRemovedEntries["RemovedItems"] =
                            BuildAuditSummary(removedItemAudits);
                        previousAuditValuesWithRemovedEntries["RemovedTerms"] =
                            BuildAuditSummary(removedTermAudits);

                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            version.VersionId,
                            employeeId,
                            ContractAuditActionTypes.DraftUpdated,
                            ContractAuditResults.Succeeded,
                            now,
                            PreviousContractStatus: contract.Status,
                            NewContractStatus: contract.Status,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: contract.ContractId,
                            PreviousValues:
                                previousAuditValuesWithRemovedEntries,
                            NewValues: ContractAuditValues.Create(
                                ("Status", contract.Status),
                                ("ResponsibleEmployeeId", contract.EmployeeId),
                                ("CurrentVersionId", contract.CurrentVersionId),
                                ("CustomerId", requestedCustomerAudit.CustomerId),
                                ("CustomerName", requestedCustomerAudit.DisplayName),
                                ("ContractName", BuildAuditSafeText(contract.ContractName)),
                                ("ContractNameEn", BuildAuditSafeText(contract.ContractNameEn)),
                                ("EffectiveDate", contract.EffectiveDate),
                                ("ExpireDate", contract.ExpireDate),
                                ("CurrencyCode", contract.CurrencyCode),
                                ("Subtotal", contract.Subtotal),
                                ("TotalDiscount", contract.TotalDiscount),
                                ("TotalVat", contract.TotalVat),
                                ("TotalAmount", contract.TotalAmount),
                                ("ItemCount", request.Items.Count),
                                ("TermCount", request.Terms.Count),
                                ("AddedItems", BuildAuditSummary(addedItemAudits)),
                                ("UpdatedItems", BuildAuditSummary(updatedItemAudits)),
                                ("AddedTerms", BuildAuditSummary(addedTermAudits)),
                                ("UpdatedTerms", BuildAuditSummary(updatedTermAudits))))
                        ]);

                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();
                    }
                    catch (DbUpdateConcurrencyException exception)
                    {
                        await transaction.RollbackAsync();

                        throw new DbUpdateConcurrencyException(
                            "Hợp đồng đã được người khác cập nhật. " +
                            "Vui lòng tải lại dữ liệu trước khi lưu.",
                            exception);
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _dbContext.ChangeTracker.Clear();
                await PersistContractConcurrencyAuditAsync(
                    contractId,
                    request.CurrentVersionId,
                    employeeId,
                    ContractAuditActionTypes.DraftUpdated,
                    DateTime.UtcNow);
                throw new DbUpdateConcurrencyException(
                    "Hợp đồng đã được người khác cập nhật. " +
                    "Vui lòng tải lại dữ liệu trước khi lưu.",
                    exception);
            }

            /*
             * Đọc lại dữ liệu sau khi transaction hoàn tất để trả:
             * - RowVersion mới
             * - TotalAmount mới
             * - Items và Terms mới nhất
             */
            return await GetDetailAsync(contractId, employeeId);
        }

        /// <summary>
        /// Bắt đầu giai đoạn đàm phán hợp đồng.
        /// Version vẫn chưa khóa và vẫn được chỉnh sửa.
        /// </summary>
        public async Task<ContractDetailResponse> StartNegotiationAsync(
            int contractId,
            StartContractNegotiationRequest request,
            int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            var expectedRowVersion = DecodeRowVersion(
                request.RowVersion,
                nameof(request.RowVersion));

            try
            {
                var contract = await _dbContext.TblContracts
                    .FirstOrDefaultAsync(x =>
                        x.ContractId == contractId
                        && x.EmployeeId == employeeId);

                if (contract == null)
                {
                    throw new KeyNotFoundException(
                        "Không tìm thấy hợp đồng.");
                }

                if (contract.IsLegacy)
                {
                    throw new InvalidOperationException(
                        "Hợp đồng legacy không hỗ trợ quy trình này.");
                }

                var currentStatus =
                    (ContractStatus)contract.Status;

                ContractLifecyclePolicy.EnsureCanTransition(
                    currentStatus,
                    ContractStatus.Negotiating);

                EnsureRowVersionMatches(
                    contract.RowVersion,
                    expectedRowVersion,
                    "Hợp đồng");

                _dbContext.Entry(contract)
                    .Property(x => x.RowVersion)
                    .OriginalValue = expectedRowVersion;

                var now = DateTime.UtcNow;
                var previousStatus = contract.Status;
                contract.Status =
                    (byte)ContractStatus.Negotiating;

                contract.UpdatedEmployeeId = employeeId;
                contract.UpdateDate = now;

                if (contract.CurrentCustomerAccessLinkId.HasValue)
                {
                    var pendingLink = await _dbContext.TblContractCustomerAccessLinks
                        .SingleOrDefaultAsync(x =>
                            x.CustomerAccessLinkId == contract.CurrentCustomerAccessLinkId.Value
                            && x.ContractId == contract.ContractId);
                    if (pendingLink is not null
                        && !pendingLink.RevokedAt.HasValue
                        && pendingLink.ExpiresAt > now
                        && !pendingLink.ActivatedAt.HasValue)
                    {
                        pendingLink.ActivatedAt = now;
                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                                contract.ContractId,
                                pendingLink.VersionId,
                                employeeId,
                                ContractAuditActionTypes.CustomerAccessLinkActivated,
                                ContractAuditResults.Succeeded,
                                now,
                                SubjectType: ContractAuditSubjectTypes.CustomerAccessLink,
                                SubjectId: pendingLink.CustomerAccessLinkId,
                                PreviousValues: ContractAuditValues.Create(
                                    ("VerificationPhoneId", pendingLink.VerificationPhoneId),
                                    ("LinkId", pendingLink.CustomerAccessLinkId),
                                    ("CurrentVersionId", pendingLink.VersionId),
                                    ("ExpiresAt", pendingLink.ExpiresAt),
                                    ("LinkState", "Pending")),
                                NewValues: ContractAuditValues.Create(
                                    ("VerificationPhoneId", pendingLink.VerificationPhoneId),
                                    ("LinkId", pendingLink.CustomerAccessLinkId),
                                    ("CurrentVersionId", pendingLink.VersionId),
                                    ("ExpiresAt", pendingLink.ExpiresAt),
                                    ("LinkState", "Active")))
                        ]);
                    }
                }

                _contractAuditWriter.StageEmployeeAudits(
                [
                    new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        contract.CurrentVersionId,
                        employeeId,
                        ContractAuditActionTypes.NegotiationStarted,
                        ContractAuditResults.Succeeded,
                        now,
                        PreviousContractStatus: previousStatus,
                        NewContractStatus: contract.Status,
                        SubjectType: ContractAuditSubjectTypes.Contract,
                        SubjectId: contract.ContractId,
                        PreviousValues: ContractAuditValues.Create(
                            ("Status", previousStatus),
                            ("ResponsibleEmployeeId", contract.EmployeeId),
                            ("CurrentVersionId", contract.CurrentVersionId)),
                        NewValues: ContractAuditValues.Create(
                            ("Status", contract.Status),
                            ("ResponsibleEmployeeId", contract.EmployeeId),
                            ("CurrentVersionId", contract.CurrentVersionId)))
                ]);

                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _dbContext.ChangeTracker.Clear();
                await PersistContractConcurrencyAuditAsync(
                    contractId,
                    versionId: null,
                    employeeId,
                    ContractAuditActionTypes.NegotiationStarted,
                    DateTime.UtcNow);
                throw new DbUpdateConcurrencyException(
                    "Hợp đồng đã được cập nhật. " +
                    "Vui lòng tải lại dữ liệu.",
                    exception);
            }

            return await GetDetailAsync(contractId, employeeId);
        }

        public async Task<CreateContractNegotiationRoundResponse>
            CreateNegotiationRoundAsync(
                int contractId,
                CreateContractNegotiationRoundRequest request,
                int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            ArgumentNullException.ThrowIfNull(request);

            var changeNote = request.ChangeNote?.Trim();

            if (string.IsNullOrWhiteSpace(changeNote)
                || changeNote.Length > 2000)
            {
                throw new ArgumentException(
                    "ChangeNote bắt buộc và không vượt quá 2000 ký tự.");
            }

            var expectedContractRowVersion = DecodeRowVersion(
                request.RowVersion,
                nameof(request.RowVersion));

            var expectedVersionRowVersion = DecodeRowVersion(
                request.CurrentVersionRowVersion,
                nameof(request.CurrentVersionRowVersion));

            var strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                        var contract = await _dbContext.TblContracts
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contractId
                                && x.EmployeeId == employeeId);

                        if (contract == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy hợp đồng.");
                        }

                        if (contract.IsLegacy)
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng legacy không hỗ trợ tạo vòng đàm phán.");
                        }

                        if ((ContractStatus)contract.Status !=
                            ContractStatus.Negotiating)
                        {
                            throw new InvalidOperationException(
                                "Chỉ Contract đang Negotiating mới được tạo vòng đàm phán mới.");
                        }

                        if (!contract.CurrentVersionId.HasValue
                            || contract.CurrentVersionId.Value !=
                            request.CurrentVersionId)
                        {
                            throw new DbUpdateConcurrencyException(
                                "Version hiện hành đã thay đổi.");
                        }

                        EnsureRowVersionMatches(
                            contract.RowVersion,
                            expectedContractRowVersion,
                            "Hợp đồng");

                        _dbContext.Entry(contract)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedContractRowVersion;

                        var sourceVersion = await _dbContext
                            .TblContractVersions
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == request.CurrentVersionId);

                        if (sourceVersion == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy version hiện hành.");
                        }

                        if (sourceVersion.IsLocked)
                        {
                            throw new InvalidOperationException(
                                "Version nguồn đã bị khóa.");
                        }

                        EnsureRowVersionMatches(
                            sourceVersion.RowVersion,
                            expectedVersionRowVersion,
                            "Version hợp đồng");

                        _dbContext.Entry(sourceVersion)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedVersionRowVersion;

                        var customer = await _dbContext.TblCustomers
                            .AsNoTracking()
                            .FirstOrDefaultAsync(x =>
                                x.CustomerId == contract.CustomerId);

                        if (customer == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy khách hàng của hợp đồng.");
                        }

                        var tenantLegalProfile = await _dbContext
                            .TblTenantLegalProfiles
                            .AsNoTracking()
                            .SingleOrDefaultAsync()
                            ?? throw new InvalidOperationException(
                                "Hồ sơ pháp lý doanh nghiệp chưa được cấu hình.");

                        var sourceItems = await _dbContext.TblContractItems
                            .AsNoTracking()
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == sourceVersion.VersionId)
                            .OrderBy(x => x.DisplayOrder)
                            .ThenBy(x => x.ContractItemId)
                            .ToListAsync();

                        var sourceTerms = await _dbContext.TblContractTerms
                            .AsNoTracking()
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == sourceVersion.VersionId)
                            .OrderBy(x => x.DisplayOrder)
                            .ThenBy(x => x.TermId)
                            .ToListAsync();

                        if (sourceItems.Count == 0 || sourceTerms.Count == 0)
                        {
                            throw new InvalidOperationException(
                                "Version nguồn phải có item và điều khoản.");
                        }

                        var snapshotJson =
                            SoftwareSupplyContractSnapshotFactory.Serialize(
                                SoftwareSupplyContractSnapshotFactory.Create(
                                    tenantLegalProfile,
                                    customer,
                                    contract,
                                    sourceVersion,
                                    sourceItems,
                                    sourceTerms));

                        var now = DateTime.UtcNow;

                        sourceVersion.SnapshotJson = snapshotJson;
                        sourceVersion.SnapshotHash =
                            CalculateSnapshotHash(snapshotJson);
                        sourceVersion.IsLocked = true;
                        sourceVersion.LockedDate = now;
                        sourceVersion.LockedByEmployeeId = employeeId;

                        var newVersion = new TblContractVersion
                        {
                            ContractId = contract.ContractId,
                            VersionNo = checked(sourceVersion.VersionNo + 1),
                            SourceVersionId = sourceVersion.VersionId,
                            TemplateVersionId =
                                sourceVersion.TemplateVersionId,
                            ChangeNote = changeNote,
                            CurrencyCode = sourceVersion.CurrencyCode,
                            Subtotal = sourceVersion.Subtotal,
                            TotalDiscount =
                                sourceVersion.TotalDiscount,
                            TotalVat = sourceVersion.TotalVat,
                            TotalAmount = sourceVersion.TotalAmount,
                            SnapshotJson = null,
                            SnapshotHash = null,
                            IsLocked = false,
                            LockedDate = null,
                            LockedByEmployeeId = null,
                            CreatedEmployeeId = employeeId,
                            CreatedDate = now
                        };

                        _dbContext.TblContractVersions.Add(newVersion);
                        await _dbContext.SaveChangesAsync();

                        var copiedItems = sourceItems
                            .Select(source => new TblContractItem
                            {
                                ContractId = contract.ContractId,
                                VersionId = newVersion.VersionId,
                                ItemType = source.ItemType,
                                SourceProductId = source.SourceProductId,
                                SourceServiceId = source.SourceServiceId,
                                ItemCode = source.ItemCode,
                                ItemName = source.ItemName,
                                ItemNameEn = source.ItemNameEn,
                                ItemDescription = source.ItemDescription,
                                ItemDescriptionEn =
                                    source.ItemDescriptionEn,
                                UnitName = source.UnitName,
                                UnitNameEn = source.UnitNameEn,
                                Quantity = source.Quantity,
                                UnitPrice = source.UnitPrice,
                                LineSubtotal = source.LineSubtotal,
                                DiscountMode = source.DiscountMode,
                                DiscountPercent =
                                    source.DiscountPercent,
                                FixedDiscountAmount =
                                    source.FixedDiscountAmount,
                                DiscountAmount = source.DiscountAmount,
                                IsTaxable = source.IsTaxable,
                                VatPercent = source.VatPercent,
                                VatAmount = source.VatAmount,
                                LineTotal = source.LineTotal,
                                DisplayOrder = source.DisplayOrder,
                                CreatedEmployeeId = employeeId,
                                CreatedDate = now
                            })
                            .ToList();

                        var copiedTerms = sourceTerms
                            .Select(source => new TblContractTerm
                            {
                                ContractId = contract.ContractId,
                                VersionId = newVersion.VersionId,
                                SourceTemplateTermId =
                                    source.SourceTemplateTermId,
                                TermCode = source.TermCode,
                                TermTitle = source.TermTitle,
                                TermTitleEn = source.TermTitleEn,
                                TermContent = source.TermContent,
                                TermContentEn = source.TermContentEn,
                                IsNegotiable = source.IsNegotiable,
                                DisplayOrder = source.DisplayOrder,
                                CreatedEmployeeId = employeeId,
                                CreatedDate = now
                            })
                            .ToList();

                        _dbContext.TblContractItems.AddRange(copiedItems);
                        _dbContext.TblContractTerms.AddRange(copiedTerms);

                        contract.CurrentVersionId = newVersion.VersionId;
                        contract.UpdatedEmployeeId = employeeId;
                        contract.UpdateDate = now;

                        if (contract.CurrentCustomerAccessLinkId.HasValue)
                        {
                            var sourceLinkId = contract.CurrentCustomerAccessLinkId.Value;
                            await RevokeCustomerLinkStateAsync(
                                sourceLinkId,
                                employeeId,
                                now,
                                "New negotiation round");
                            contract.CurrentCustomerAccessLinkId = null;
                            _contractAuditWriter.StageEmployeeAudits(
                            [
                                new EmployeeContractAuditWriteRequest(
                                contract.ContractId,
                                sourceVersion.VersionId,
                                employeeId,
                                ContractAuditActionTypes.CustomerAccessLinkInvalidated,
                                ContractAuditResults.Succeeded,
                                now,
                                Reason: "New negotiation round",
                                SubjectType: ContractAuditSubjectTypes.CustomerAccessLink,
                                SubjectId: sourceLinkId,
                                NewValues: ContractAuditValues.Create(
                                    ("LinkId", sourceLinkId),
                                    ("CurrentVersionId", sourceVersion.VersionId),
                                    ("LinkState", "Invalidated")))
                            ]);
                        }

                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            newVersion.VersionId,
                            employeeId,
                            ContractAuditActionTypes
                                .NegotiationRoundCreated,
                            ContractAuditResults.Succeeded,
                            now,
                            PreviousContractStatus: contract.Status,
                            NewContractStatus: contract.Status,
                            Reason: changeNote,
                            SubjectType: ContractAuditSubjectTypes.ContractVersion,
                            SubjectId: newVersion.VersionId,
                            PreviousValues: ContractAuditValues.Create(
                                ("SourceVersionId", sourceVersion.VersionId),
                                ("CurrentVersionId", sourceVersion.VersionId),
                                ("SourceVersionLocked", false),
                                ("ItemCount", sourceItems.Count),
                                ("TermCount", sourceTerms.Count),
                                ("TotalAmount", sourceVersion.TotalAmount)),
                            NewValues: ContractAuditValues.Create(
                                ("SourceVersionId", sourceVersion.VersionId),
                                ("NewVersionId", newVersion.VersionId),
                                ("CurrentVersionId", newVersion.VersionId),
                                ("SourceVersionLocked", true),
                                ("ItemCount", copiedItems.Count),
                                ("TermCount", copiedTerms.Count),
                                ("TotalAmount", newVersion.TotalAmount)))
                        ]);

                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return new CreateContractNegotiationRoundResponse
                        {
                            ContractId = contract.ContractId,
                            Status = ContractStatus.Negotiating,
                            RowVersion =
                                EncodeRowVersion(contract.RowVersion),
                            SourceVersion =
                                MapNegotiationRoundVersion(sourceVersion),
                            CurrentVersion =
                                MapNegotiationRoundVersion(newVersion),
                            Totals = new ContractFinancialTotalsResponse
                            {
                                CurrencyCode = newVersion.CurrencyCode,
                                Subtotal = newVersion.Subtotal,
                                TotalDiscount =
                                    newVersion.TotalDiscount,
                                TotalVat = newVersion.TotalVat,
                                TotalPayment = newVersion.TotalAmount
                            }
                        };
                    }
                    catch
                    {
                        try
                        {
                            await transaction.RollbackAsync();
                        }
                        finally
                        {
                            _dbContext.ChangeTracker.Clear();
                        }

                        throw;
                    }
                });
            }
            catch (DbUpdateConcurrencyException exception)
            {
                await PersistContractConcurrencyAuditAsync(
                    contractId,
                    request.CurrentVersionId,
                    employeeId,
                    ContractAuditActionTypes.NegotiationRoundCreated,
                    DateTime.UtcNow);
                throw new DbUpdateConcurrencyException(
                    "Vòng đàm phán không thể tạo vì dữ liệu đã thay đổi.",
                    exception);
            }
        }

        /// <summary>
        /// Lấy tất cả comment gốc của hợp đồng theo thứ tự thời gian.
        /// </summary>
        public async Task<IReadOnlyList<ContractNegotiationCommentResponse>>
            GetRootCommentsAsync(
                int contractId,
                int employeeId,
                bool canReadTenant = false)
        {
            ValidateContractEmployee(contractId, employeeId);
            await EnsureContractCommentReadAccessAsync(
                contractId,
                employeeId,
                canReadTenant);

            var comments = await _dbContext
                .TblContractNegotiationComments
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contractId
                    && x.ParentCommentId == null)
                .OrderBy(x => x.CreatedDate)
                .ThenBy(x => x.CommentId)
                .ToListAsync();

            return await MapCommentResponsesAsync(comments);
        }

        /// <summary>
        /// Lấy các comment con trực tiếp dựa theo ID comment cha.
        /// </summary>
        public async Task<IReadOnlyList<ContractNegotiationCommentResponse>>
            GetCommentRepliesAsync(
                int contractId,
                int parentCommentId,
                int employeeId,
                bool canReadTenant = false)
        {
            ValidateContractEmployee(contractId, employeeId);

            if (parentCommentId <= 0)
            {
                throw new ArgumentException(
                    "ParentCommentId phải lớn hơn 0.");
            }

            await EnsureContractCommentReadAccessAsync(
                contractId,
                employeeId,
                canReadTenant);

            var parentExists = await _dbContext
                .TblContractNegotiationComments
                .AsNoTracking()
                .AnyAsync(x =>
                    x.CommentId == parentCommentId
                    && x.ContractId == contractId
                    && x.ParentCommentId == null);

            if (!parentExists)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy comment cha.");
            }

            var comments = await _dbContext
                .TblContractNegotiationComments
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contractId
                    && x.ParentCommentId == parentCommentId)
                .OrderBy(x => x.CreatedDate)
                .ThenBy(x => x.CommentId)
                .ToListAsync();

            return await MapCommentResponsesAsync(comments);
        }

        /// <summary>
        /// Ghi nhận feedback bên ngoài do nhân viên phụ trách nhập lại.
        /// Comment và event được tạo trong cùng một transaction serializable.
        /// </summary>
        public async Task<ContractNegotiationCommentResponse>
            CreateExternalFeedbackAsync(
                int contractId,
                CreateContractNegotiationCommentRequest request,
                int employeeId)
        {
            ValidateCommentCreateRequest(
                contractId,
                request,
                employeeId);

            var content = request.Content.Trim();
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction =
                        await _dbContext.Database.BeginTransactionAsync(
                            IsolationLevel.Serializable);

                    try
                    {
                        var contract = await _dbContext.TblContracts
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contractId);

                        if (contract == null
                            || contract.EmployeeId != employeeId)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy hợp đồng.");
                        }

                        EnsureNegotiationCommentWriteState(contract);

                        if (!contract.CurrentVersionId.HasValue
                            || contract.CurrentVersionId.Value !=
                            request.CurrentVersionId)
                        {
                            throw new DbUpdateConcurrencyException(
                                "Version hiện hành đã thay đổi.");
                        }

                        var version = await _dbContext.TblContractVersions
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId ==
                                request.CurrentVersionId);

                        if (version == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy version hiện hành.");
                        }

                        if (version.IsLocked)
                        {
                            throw new DbUpdateConcurrencyException(
                                "Version đã bị khóa.");
                        }

                        int? effectiveTermId = request.TermId;

                        if (request.TermId.HasValue)
                        {
                            var term = await _dbContext.TblContractTerms
                                .FirstOrDefaultAsync(x =>
                                    x.ContractId == contract.ContractId
                                    && x.VersionId == version.VersionId
                                    && x.TermId == request.TermId.Value);

                            if (term == null)
                            {
                                throw new InvalidOperationException(
                                    "Term phải thuộc version hiện hành.");
                            }

                            if (!term.IsNegotiable)
                            {
                                throw new InvalidOperationException(
                                    "Term này không cho phép feedback đàm phán.");
                            }
                        }

                        TblContractNegotiationComment? parent = null;

                        if (request.ParentCommentId.HasValue)
                        {
                            parent = await _dbContext
                                .TblContractNegotiationComments
                                .FirstOrDefaultAsync(x =>
                                    x.CommentId ==
                                    request.ParentCommentId.Value);

                            if (parent == null
                                || parent.ContractId != contract.ContractId
                                || parent.VersionId != version.VersionId)
                            {
                                throw new InvalidOperationException(
                                    "Parent comment phải thuộc cùng Contract và Version.");
                            }

                            if (parent.State ==
                                (byte)ContractNegotiationCommentState.Resolved)
                            {
                                throw new InvalidOperationException(
                                    "Comment đã resolved không thể nhận reply.");
                            }

                            if (request.TermId.HasValue
                                && parent.TermId != request.TermId)
                            {
                                throw new InvalidOperationException(
                                    "Reply phải kế thừa Term của parent comment.");
                            }

                            effectiveTermId = parent.TermId;
                        }

                        var now = DateTime.UtcNow;
                        var comment = new TblContractNegotiationComment
                        {
                            ContractId = contract.ContractId,
                            VersionId = version.VersionId,
                            TermId = effectiveTermId,
                            ParentCommentId = request.ParentCommentId,
                            Content = content,
                            Source = "ExternalFeedback",
                            RecordedByEmployeeId = employeeId,
                            State =
                                (byte)ContractNegotiationCommentState.Open,
                            CreatedDate = now
                        };

                        _dbContext.TblContractNegotiationComments
                            .Add(comment);

                        var createdEvent =
                            new TblContractNegotiationCommentEvent
                            {
                                EventType =
                                    (byte)ContractNegotiationCommentEventType
                                        .Created,
                                ActorType = ContractAuditActorTypes.Employee,
                                EmployeeId = employeeId,
                                OccurredAt = now
                            };

                        SetSyntheticCommentRowVersionIfNeeded(comment);
                        await _dbContext.SaveChangesAsync();

                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                                contract.ContractId,
                                version.VersionId,
                                employeeId,
                                request.ParentCommentId.HasValue
                                    ? ContractAuditActionTypes
                                        .NegotiationReplyCreated
                                    : ContractAuditActionTypes
                                        .ExternalFeedbackCreated,
                                ContractAuditResults.Succeeded,
                                now,
                                SubjectType: ContractAuditSubjectTypes.NegotiationComment,
                                SubjectId: comment.CommentId,
                                NewValues: ContractAuditValues.Create(
                                    ("Source", comment.Source),
                                    ("Target", comment.TermId.HasValue ? "Term" : "Contract"),
                                    ("TermId", comment.TermId),
                                    ("ParentCommentId", comment.ParentCommentId),
                                    ("State", "Open")))
                        ]);

                        // Event không có physical FK nên phải gán ID trước khi lưu.
                        createdEvent.CommentId = comment.CommentId;
                        _dbContext.TblContractNegotiationCommentEvents
                            .Add(createdEvent);
                        await _dbContext.SaveChangesAsync();

                        await transaction.CommitAsync();
                        return await MapCommentResponseAsync(comment);
                    }
                    catch (DbUpdateConcurrencyException exception)
                    {
                        await RollbackAndClearAsync(transaction);
                        await PersistNegotiationCommentConflictAuditAsync(
                            contractId,
                            request.CurrentVersionId,
                            employeeId,
                            DateTime.UtcNow,
                            request.ParentCommentId.HasValue
                                ? ContractAuditActionTypes.NegotiationReplyCreated
                                : ContractAuditActionTypes.ExternalFeedbackCreated);

                        throw new DbUpdateConcurrencyException(
                            "Comment không thể ghi vì Contract hoặc Version đã thay đổi.",
                            exception);
                    }
                    catch
                    {
                        await RollbackAndClearAsync(transaction);
                        throw;
                    }
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                throw;
            }
        }

        public Task<ContractNegotiationCommentResponse>
            RecordExternalFeedbackAsync(
                int contractId,
                CreateContractNegotiationCommentRequest request,
                int employeeId)
        {
            return CreateExternalFeedbackAsync(
                contractId,
                request,
                employeeId);
        }

        public async Task<ContractNegotiationCommentResponse>
            ResolveCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId)
        {
            return await ChangeCommentStateAsync(
                contractId,
                commentId,
                request,
                employeeId,
                ContractNegotiationCommentState.Resolved,
                ContractNegotiationCommentEventType.Resolved,
                ContractAuditActionTypes.NegotiationCommentResolved);
        }

        public Task<ContractNegotiationCommentResponse>
            ResolveNegotiationCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId)
        {
            return ResolveCommentAsync(
                contractId,
                commentId,
                request,
                employeeId);
        }

        public async Task<ContractNegotiationCommentResponse>
            ReopenCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId)
        {
            return await ChangeCommentStateAsync(
                contractId,
                commentId,
                request,
                employeeId,
                ContractNegotiationCommentState.Open,
                ContractNegotiationCommentEventType.Reopened,
                ContractAuditActionTypes.NegotiationCommentReopened);
        }

        public Task<ContractNegotiationCommentResponse>
            ReopenNegotiationCommentAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId)
        {
            return ReopenCommentAsync(
                contractId,
                commentId,
                request,
                employeeId);
        }

        public async Task<IReadOnlyList<ContractVersionHistoryResponse>>
            GetVersionHistoryAsync(
                int contractId,
                int employeeId,
                bool canReadTenant = false)
        {
            ValidateContractEmployee(contractId, employeeId);

            var contract = await _dbContext.TblContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ContractId == contractId
                    && (canReadTenant || x.EmployeeId == employeeId));

            if (contract == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng.");
            }

            EnsureNegotiationHistoryReadState(contract);

            return await _dbContext.TblContractVersions
                .AsNoTracking()
                .Where(x => x.ContractId == contractId)
                .OrderBy(x => x.VersionNo)
                .ThenBy(x => x.VersionId)
                .Select(x => new ContractVersionHistoryResponse
                {
                    VersionId = x.VersionId,
                    VersionNo = x.VersionNo,
                    SourceVersionId = x.SourceVersionId,
                    ChangeNote = x.ChangeNote,
                    IsLocked = x.IsLocked,
                    LockedDate = x.LockedDate,
                    LockedByEmployeeId = x.LockedByEmployeeId,
                    CreatedEmployeeId = x.CreatedEmployeeId,
                    CreatedDate = x.CreatedDate,
                    RowVersion = EncodeRowVersion(x.RowVersion)
                })
                .ToListAsync();
        }

        public async Task<ContractVersionDetailResponse>
            GetVersionDetailAsync(
                int contractId,
                int versionId,
                int employeeId,
                bool canReadTenant = false)
        {
            ValidateContractEmployee(contractId, employeeId);

            var contract = await _dbContext.TblContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ContractId == contractId
                    && (canReadTenant || x.EmployeeId == employeeId));

            if (contract == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng.");
            }

            EnsureNegotiationHistoryReadState(contract);

            var version = await _dbContext.TblContractVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ContractId == contractId
                    && x.VersionId == versionId);

            if (version == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy version của hợp đồng.");
            }

            var items = await _dbContext.TblContractItems
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contractId
                    && x.VersionId == versionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.ContractItemId)
                .ToListAsync();

            var terms = await _dbContext.TblContractTerms
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contractId
                    && x.VersionId == versionId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.TermId)
                .ToListAsync();

            return new ContractVersionDetailResponse
            {
                VersionId = version.VersionId,
                VersionNo = version.VersionNo,
                SourceVersionId = version.SourceVersionId,
                TemplateVersionId = version.TemplateVersionId,
                ChangeNote = version.ChangeNote,
                CurrencyCode = version.CurrencyCode,
                Subtotal = version.Subtotal,
                TotalDiscount = version.TotalDiscount,
                TotalVat = version.TotalVat,
                TotalPayment = version.TotalAmount,
                SnapshotHash = version.SnapshotHash,
                IsLocked = version.IsLocked,
                LockedDate = version.LockedDate,
                LockedByEmployeeId = version.LockedByEmployeeId,
                CreatedEmployeeId = version.CreatedEmployeeId,
                CreatedDate = version.CreatedDate,
                RowVersion = EncodeRowVersion(version.RowVersion),
                Items = items.Select(MapItemDetail).ToList(),
                Terms = terms.Select(MapTermDetail).ToList(),
                Comments = await LoadCommentResponsesAsync(
                    contractId,
                    versionId)
            };
        }

        private async Task<ContractNegotiationCommentResponse>
            ChangeCommentStateAsync(
                int contractId,
                int commentId,
                UpdateContractNegotiationCommentStateRequest request,
                int employeeId,
                ContractNegotiationCommentState targetState,
                ContractNegotiationCommentEventType eventType,
                string auditActionType)
        {
            ValidateCommentStateRequest(
                contractId,
                commentId,
                request,
                employeeId);

            var expectedRowVersion = DecodeRowVersion(
                request.RowVersion,
                nameof(request.RowVersion));
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            TblContractNegotiationComment? comment = null;

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    var contract = await _dbContext.TblContracts
                        .FirstOrDefaultAsync(x =>
                            x.ContractId == contractId);

                    if (contract == null)
                    {
                        throw new KeyNotFoundException(
                            "Không tìm thấy hợp đồng.");
                    }

                    comment = await _dbContext
                        .TblContractNegotiationComments
                        .FirstOrDefaultAsync(x =>
                            x.CommentId == commentId
                            && x.ContractId == contractId);

                    if (comment == null)
                    {
                        throw new KeyNotFoundException(
                            "Không tìm thấy comment của hợp đồng.");
                    }

                    if (contract.EmployeeId != employeeId)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Người phụ trách hợp đồng đã thay đổi.");
                    }

                    EnsureNegotiationCommentWriteState(contract);

                    if (!contract.CurrentVersionId.HasValue
                        || contract.CurrentVersionId.Value !=
                        comment.VersionId)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Version nguồn của comment không còn là Current Version.");
                    }

                    var version = await _dbContext.TblContractVersions
                        .FirstOrDefaultAsync(x =>
                            x.ContractId == contractId
                            && x.VersionId == comment!.VersionId);

                    if (version == null)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Không tìm thấy Version nguồn của comment.");
                    }

                    if (version.IsLocked)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Version nguồn của comment đã bị khóa.");
                    }

                    EnsureRowVersionMatches(
                        comment.RowVersion,
                        expectedRowVersion,
                        "Comment");

                    var now = DateTime.UtcNow;
                    var commentsToChange = new List<
                        TblContractNegotiationComment>();

                    if (comment.State != (byte)targetState)
                    {
                        commentsToChange.Add(comment);
                    }

                    if (targetState ==
                        ContractNegotiationCommentState.Resolved)
                    {
                        var commentCandidates = await _dbContext
                            .TblContractNegotiationComments
                            .Where(x =>
                                x.ContractId == contractId
                                && x.VersionId == comment.VersionId
                                && x.CommentId != comment.CommentId)
                            .ToListAsync();
                        var commentsByParent = commentCandidates
                            .Where(x => x.ParentCommentId.HasValue)
                            .ToLookup(x => x.ParentCommentId!.Value);
                        var pendingParentIds = new Queue<int>();
                        var visitedCommentIds = new HashSet<int>
                        {
                            comment.CommentId
                        };
                        pendingParentIds.Enqueue(comment.CommentId);

                        while (pendingParentIds.Count > 0)
                        {
                            var parentCommentId = pendingParentIds.Dequeue();

                            foreach (var child in
                                commentsByParent[parentCommentId])
                            {
                                if (!visitedCommentIds.Add(child.CommentId))
                                {
                                    continue;
                                }

                                pendingParentIds.Enqueue(child.CommentId);

                                if (child.State != (byte)targetState)
                                {
                                    commentsToChange.Add(child);
                                }
                            }
                        }
                    }

                    if (commentsToChange.Count == 0)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Trạng thái comment đã được xử lý bởi request khác.");
                    }

                    var previousStates = commentsToChange.ToDictionary(
                        item => item.CommentId,
                        item => item.State);

                    foreach (var changedComment in commentsToChange)
                    {
                        var changedCommentRowVersion =
                            changedComment.CommentId == comment.CommentId
                                ? expectedRowVersion
                                : changedComment.RowVersion.ToArray();

                        _dbContext.Entry(changedComment)
                            .Property(x => x.RowVersion)
                            .OriginalValue = changedCommentRowVersion;

                        changedComment.State = (byte)targetState;
                        changedComment.UpdatedDate = now;

                        _dbContext.TblContractNegotiationCommentEvents.Add(
                            new TblContractNegotiationCommentEvent
                            {
                                CommentId = changedComment.CommentId,
                                EventType = (byte)eventType,
                                ActorType = ContractAuditActorTypes.Employee,
                                EmployeeId = employeeId,
                                OccurredAt = now
                            });

                        RotateCommentRowVersionForInMemory(
                            changedComment,
                            changedCommentRowVersion);
                    }

                    _contractAuditWriter.StageEmployeeAudits(
                        commentsToChange.Select(changedComment =>
                            new EmployeeContractAuditWriteRequest(
                                contract.ContractId,
                                changedComment.VersionId,
                                employeeId,
                                auditActionType,
                                ContractAuditResults.Succeeded,
                                now,
                                PreviousContractStatus: contract.Status,
                                NewContractStatus: contract.Status,
                                SubjectType: ContractAuditSubjectTypes
                                    .NegotiationComment,
                                SubjectId: changedComment.CommentId,
                                PreviousValues: ContractAuditValues.Create(
                                    ("Source", changedComment.Source),
                                    ("Target", changedComment.TermId.HasValue
                                        ? "Term"
                                        : "Contract"),
                                    ("TermId", changedComment.TermId),
                                    ("ParentCommentId",
                                        changedComment.ParentCommentId),
                                    ("State",
                                        previousStates[changedComment.CommentId]
                                            == 0
                                            ? "Open"
                                            : "Resolved")),
                                NewValues: ContractAuditValues.Create(
                                    ("Source", changedComment.Source),
                                    ("Target", changedComment.TermId.HasValue
                                        ? "Term"
                                        : "Contract"),
                                    ("TermId", changedComment.TermId),
                                    ("ParentCommentId",
                                        changedComment.ParentCommentId),
                                    ("State", targetState ==
                                        ContractNegotiationCommentState.Open
                                        ? "Open"
                                        : "Resolved"))))
                        .ToList());

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return await MapCommentResponseAsync(comment);
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    var versionId = comment?.VersionId;
                    await RollbackAndClearAsync(transaction);
                    await PersistNegotiationCommentConflictAuditAsync(
                        contractId,
                        versionId,
                        employeeId,
                        DateTime.UtcNow,
                        auditActionType);

                    throw new DbUpdateConcurrencyException(
                        "Comment đã được cập nhật hoặc không còn đủ điều kiện lifecycle.",
                        exception);
                }
                catch
                {
                    await RollbackAndClearAsync(transaction);
                    throw;
                }
            });
        }

        private async Task PersistNegotiationCommentConflictAuditAsync(
            int contractId,
            int? versionId,
            int actorEmployeeId,
            DateTime occurredAt,
            string requestedActionType)
        {
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction =
                    await _dbContext.Database.BeginTransactionAsync();

                try
                {
                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contractId,
                            versionId,
                            actorEmployeeId,
                            requestedActionType,
                            ContractAuditResults.ConcurrencyConflict,
                            occurredAt,
                            SubjectType: ContractAuditSubjectTypes.Contract,
                            SubjectId: contractId,
                            FailureCode: ContractAuditFailureCodes.StaleRowVersion)
                    ]);

                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await RollbackAndClearAsync(transaction);
                    throw;
                }
            });
        }

        private async Task RollbackAndClearAsync(
            Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction
                transaction)
        {
            try
            {
                await transaction.RollbackAsync();
            }
            finally
            {
                _dbContext.ChangeTracker.Clear();
            }
        }

        private async Task<List<ContractNegotiationCommentResponse>>
            LoadCommentResponsesAsync(
                int contractId,
                int versionId)
        {
            var comments = await _dbContext
                .TblContractNegotiationComments
                .AsNoTracking()
                .Where(x =>
                    x.ContractId == contractId
                    && x.VersionId == versionId)
                .OrderBy(x => x.CreatedDate)
                .ThenBy(x => x.CommentId)
                .ToListAsync();

            return await MapCommentResponsesAsync(comments);
        }

        private async Task<List<ContractNegotiationCommentResponse>>
            MapCommentResponsesAsync(
                List<TblContractNegotiationComment> comments)
        {

            if (comments.Count == 0)
            {
                return [];
            }

            var commentIds = comments
                .Select(x => x.CommentId)
                .ToList();

            var events = await _dbContext
                .TblContractNegotiationCommentEvents
                .AsNoTracking()
                .Where(x => commentIds.Contains(x.CommentId))
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.CommentEventId)
                .ToListAsync();

            var employeeIds = comments
                .Where(comment => comment.RecordedByEmployeeId.HasValue)
                .Select(comment => comment.RecordedByEmployeeId!.Value)
                .Distinct()
                .ToList();
            var employeeNames = await _dbContext.TblEmployees
                .AsNoTracking()
                .Where(employee => employeeIds.Contains(employee.EmployeeId))
                .ToDictionaryAsync(
                    employee => employee.EmployeeId,
                    employee => employee.EmployeeFullName);

            return comments
                .Select(comment => MapCommentResponse(
                    comment,
                    events.Where(x =>
                        x.CommentId == comment.CommentId),
                    comment.RecordedByEmployeeId.HasValue
                        && employeeNames.TryGetValue(
                            comment.RecordedByEmployeeId.Value,
                            out var displayName)
                            ? displayName
                            : null))
                .ToList();
        }

        private async Task EnsureContractCommentReadAccessAsync(
            int contractId,
            int employeeId,
            bool canReadTenant)
        {
            var contractExists = await _dbContext.TblContracts
                .AsNoTracking()
                .AnyAsync(x =>
                    x.ContractId == contractId
                    && (canReadTenant || x.EmployeeId == employeeId));

            if (!contractExists)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng.");
            }
        }

        private async Task<ContractNegotiationCommentResponse>
            MapCommentResponseAsync(
                TblContractNegotiationComment comment)
        {
            var events = await _dbContext
                .TblContractNegotiationCommentEvents
                .AsNoTracking()
                .Where(x => x.CommentId == comment.CommentId)
                .OrderBy(x => x.OccurredAt)
                .ThenBy(x => x.CommentEventId)
                .ToListAsync();

            var displayName = comment.RecordedByEmployeeId.HasValue
                ? await _dbContext.TblEmployees
                    .AsNoTracking()
                    .Where(employee =>
                        employee.EmployeeId == comment.RecordedByEmployeeId.Value)
                    .Select(employee => employee.EmployeeFullName)
                    .SingleOrDefaultAsync()
                : null;

            return MapCommentResponse(comment, events, displayName);
        }

        private static ContractNegotiationCommentResponse
            MapCommentResponse(
                TblContractNegotiationComment comment,
                IEnumerable<TblContractNegotiationCommentEvent> events,
                string? recordedByDisplayName = null)
        {
            return new ContractNegotiationCommentResponse
            {
                CommentId = comment.CommentId,
                ContractId = comment.ContractId,
                VersionId = comment.VersionId,
                TermId = comment.TermId,
                ParentCommentId = comment.ParentCommentId,
                Content = comment.Content,
                Source = comment.Source,
                ExternalFeedback = comment.ExternalFeedback,
                RecordedByEmployeeId = comment.RecordedByEmployeeId ?? 0,
                RecordedByDisplayName = recordedByDisplayName,
                State = (ContractNegotiationCommentState)comment.State,
                CreatedDate = comment.CreatedDate,
                UpdatedDate = comment.UpdatedDate,
                RowVersion = EncodeRowVersion(comment.RowVersion),
                Events = events
                    .OrderBy(x => x.OccurredAt)
                    .ThenBy(x => x.CommentEventId)
                    .Select(x => new ContractNegotiationCommentEventResponse
                    {
                        CommentEventId = x.CommentEventId,
                        CommentId = x.CommentId,
                        EventType =
                            (ContractNegotiationCommentEventType)x.EventType,
                        EmployeeId = x.EmployeeId ?? 0,
                        OccurredAt = x.OccurredAt
                    })
                    .ToList()
            };
        }

        private static ContractItemDetailResponse MapItemDetail(
            TblContractItem item)
        {
            return new ContractItemDetailResponse
            {
                ContractItemId = item.ContractItemId,
                ItemType = (ContractItemType)item.ItemType,
                SourceProductId = item.SourceProductId,
                SourceServiceId = item.SourceServiceId,
                ItemCode = item.ItemCode,
                ItemName = item.ItemName,
                ItemNameEn = item.ItemNameEn,
                ItemDescription = item.ItemDescription,
                ItemDescriptionEn = item.ItemDescriptionEn,
                UnitName = item.UnitName,
                UnitNameEn = item.UnitNameEn,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineSubtotal = item.LineSubtotal,
                DiscountMode = (ContractItemDiscountMode)item.DiscountMode,
                DiscountPercent = item.DiscountPercent,
                FixedDiscountAmount = item.FixedDiscountAmount,
                DiscountAmount = item.DiscountAmount,
                IsTaxable = item.IsTaxable,
                VatPercent = item.VatPercent,
                VatAmount = item.VatAmount,
                LineTotal = item.LineTotal,
                DisplayOrder = item.DisplayOrder,
                RowVersion = EncodeRowVersion(item.RowVersion)
            };
        }

        private static ContractTermDetailResponse MapTermDetail(
            TblContractTerm term)
        {
            return new ContractTermDetailResponse
            {
                TermId = term.TermId,
                SourceTemplateTermId = term.SourceTemplateTermId,
                TermCode = term.TermCode,
                TermTitle = term.TermTitle,
                TermTitleEn = term.TermTitleEn,
                TermContent = term.TermContent,
                TermContentEn = term.TermContentEn,
                IsNegotiable = term.IsNegotiable,
                DisplayOrder = term.DisplayOrder,
                RowVersion = EncodeRowVersion(term.RowVersion)
            };
        }

        private static void ValidateCommentCreateRequest(
            int contractId,
            CreateContractNegotiationCommentRequest request,
            int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            ArgumentNullException.ThrowIfNull(request);

            if (request.CurrentVersionId <= 0)
            {
                throw new ArgumentException(
                    "CurrentVersionId phải lớn hơn 0.");
            }

            if (request.TermId is <= 0)
            {
                throw new ArgumentException(
                    "TermId phải lớn hơn 0 nếu được truyền.");
            }

            if (request.ParentCommentId is <= 0)
            {
                throw new ArgumentException(
                    "ParentCommentId phải lớn hơn 0 nếu được truyền.");
            }

            var content = request.Content?.Trim();

            if (string.IsNullOrWhiteSpace(content)
                || content.Length > 4000)
            {
                throw new ArgumentException(
                    "Content bắt buộc và không vượt quá 4000 ký tự.");
            }
        }

        private static void ValidateCommentStateRequest(
            int contractId,
            int commentId,
            UpdateContractNegotiationCommentStateRequest request,
            int employeeId)
        {
            if (contractId <= 0 || commentId <= 0)
            {
                throw new ArgumentException(
                    "ContractId và CommentId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            ArgumentNullException.ThrowIfNull(request);
        }

        private static void ValidateContractEmployee(
            int contractId,
            int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }
        }

        private static void EnsureNegotiationCommentWriteState(
            TblContract contract)
        {
            var status = (ContractStatus)contract.Status;

            if (status == ContractStatus.Cancelled
                || status != ContractStatus.Negotiating)
            {
                throw new InvalidOperationException(
                    "Chỉ Contract đang Negotiating mới hỗ trợ comment đàm phán.");
            }
        }

        private static void EnsureNegotiationHistoryReadState(
            TblContract contract)
        {
            var status = (ContractStatus)contract.Status;

            if (status == ContractStatus.Cancelled
                || status != ContractStatus.Negotiating)
            {
                throw new InvalidOperationException(
                    "Chỉ Contract đang Negotiating mới hỗ trợ xem Version history.");
            }
        }

        private void SetSyntheticCommentRowVersionIfNeeded(
            TblContractNegotiationComment comment)
        {
            if (_dbContext.Database.ProviderName ==
                "Microsoft.EntityFrameworkCore.InMemory"
                && comment.RowVersion is not { Length: 8 })
            {
                comment.RowVersion = BitConverter.GetBytes(
                    Interlocked.Increment(
                        ref _syntheticCommentRowVersionSeed));
            }
        }

        private void RotateCommentRowVersionForInMemory(
            TblContractNegotiationComment comment,
            byte[] expectedRowVersion)
        {
            if (_dbContext.Database.ProviderName ==
                "Microsoft.EntityFrameworkCore.InMemory")
            {
                comment.RowVersion = BitConverter.GetBytes(
                    Interlocked.Increment(
                        ref _syntheticCommentRowVersionSeed));
            }
            else
            {
                _dbContext.Entry(comment)
                    .Property(x => x.RowVersion)
                    .OriginalValue = expectedRowVersion;
            }
        }

        /// <summary>
        /// Gửi version hiện hành đi duyệt.
        ///
        /// Khi gửi thành công:
        /// - Contract chuyển sang PendingApproval.
        /// - Version được khóa.
        /// - SnapshotJson và SnapshotHash được tạo.
        /// - Approval request ở trạng thái Pending được tạo.
        /// </summary>
        public async Task<SubmitContractForApprovalResponse>
            SubmitForApprovalAsync(
                int contractId,
                SubmitContractForApprovalRequest request,
                int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đăng nhập.");
            }

            var expectedContractRowVersion = DecodeRowVersion(
                request.RowVersion,
                nameof(request.RowVersion));

            var expectedVersionRowVersion = DecodeRowVersion(
                request.CurrentVersionRowVersion,
                nameof(request.CurrentVersionRowVersion));

            var artifactRenderer = _submissionArtifactRenderer
                ?? throw new InvalidOperationException(
                    "Renderer artifact gửi duyệt chưa được cấu hình.");
            var privateFileStorage = _privateFileStorage
                ?? throw new InvalidOperationException(
                    "Private storage cho artifact gửi duyệt chưa được cấu hình.");
            var tenant = _currentTenant?.GetRequiredTenant()
                ?? throw new InvalidOperationException(
                    "Tenant của request chưa được xác định.");

            var strategy =
                _dbContext.Database.CreateExecutionStrategy();

            try
            {
                return await strategy.ExecuteAsync(async () =>
                {
                    var storedArtifacts = new List<StoredPrivateFile>(2);
                    await using var transaction = await _dbContext.Database
                        .BeginTransactionAsync(IsolationLevel.Serializable);
                    try
                    {
                        var contract = await _dbContext.TblContracts
                            .FirstOrDefaultAsync(x =>
                                x.ContractId == contractId
                                && x.EmployeeId == employeeId);

                        if (contract == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy hợp đồng.");
                        }

                        if (contract.IsLegacy)
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng legacy không hỗ trợ gửi duyệt.");
                        }

                        if (contract.ContractType !=
                            (byte)ContractType.SoftwareSupply)
                        {
                            throw new InvalidOperationException(
                                "Phase 8C chỉ hỗ trợ gửi duyệt hợp đồng cung cấp phần mềm.");
                        }

                        var currentStatus =
                            (ContractStatus)contract.Status;

                        /*
                         * Policy chỉ cho:
                         * Negotiating → PendingApproval.
                         */
                        ContractLifecyclePolicy.EnsureCanTransition(
                            currentStatus,
                            ContractStatus.PendingApproval);

                        if (!contract.CurrentVersionId.HasValue
                            || contract.CurrentVersionId.Value !=
                            request.CurrentVersionId)
                        {
                            throw new DbUpdateConcurrencyException(
                                "Version hiện hành đã thay đổi.");
                        }

                        EnsureRowVersionMatches(
                            contract.RowVersion,
                            expectedContractRowVersion,
                            "Hợp đồng");

                        _dbContext.Entry(contract)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedContractRowVersion;

                        var version = await _dbContext
                            .TblContractVersions
                            .FirstOrDefaultAsync(x =>
                                x.VersionId == request.CurrentVersionId
                                && x.ContractId == contract.ContractId);

                        if (version == null)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy version hiện hành.");
                        }

                        if (version.IsLocked)
                        {
                            throw new InvalidOperationException(
                                "Version đã bị khóa.");
                        }

                        EnsureRowVersionMatches(
                            version.RowVersion,
                            expectedVersionRowVersion,
                            "Version hợp đồng");

                        _dbContext.Entry(version)
                            .Property(x => x.RowVersion)
                            .OriginalValue = expectedVersionRowVersion;

                        /*
                         * Không được tạo hai request Pending
                         * cho cùng một hợp đồng.
                         */
                        var hasPendingRequest = await _dbContext
                            .TblContractApprovalRequests
                            .AnyAsync(x =>
                                x.ContractId == contract.ContractId
                                && x.Status ==
                                (byte)ApprovalRequestStatus.Pending);

                        if (hasPendingRequest)
                        {
                            throw new InvalidOperationException(
                                "Hợp đồng đã có yêu cầu duyệt đang chờ xử lý.");
                        }

                        TblApprovalWorkflow? workflow = null;
                        if (request.WorkflowId.HasValue)
                        {
                            workflow = await _dbContext
                                .TblApprovalWorkflows
                                .AsNoTracking()
                                .SingleOrDefaultAsync(x =>
                                    x.WorkflowId == request.WorkflowId.Value
                                    && x.ObjectType == "Contract"
                                    && x.StepNo == 1
                                    && x.IsActive);

                            if (workflow is null)
                            {
                                throw new KeyNotFoundException(
                                    "Không tìm thấy workflow duyệt hợp lệ.");
                            }
                        }

                        await EnsureEligibleManagerApproverAsync(
                            employeeId,
                            workflow);

                        var items = await _dbContext.TblContractItems
                            .AsNoTracking()
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == version.VersionId)
                            .OrderBy(x => x.DisplayOrder)
                            .ThenBy(x => x.ContractItemId)
                            .ToListAsync();

                        var terms = await _dbContext.TblContractTerms
                            .AsNoTracking()
                            .Where(x =>
                                x.ContractId == contract.ContractId
                                && x.VersionId == version.VersionId)
                            .OrderBy(x => x.DisplayOrder)
                            .ThenBy(x => x.TermId)
                            .ToListAsync();

                        var openCommentCount = await _dbContext
                            .TblContractNegotiationComments
                            .AsNoTracking()
                            .CountAsync(comment =>
                                comment.ContractId == contract.ContractId
                                && comment.VersionId == version.VersionId
                                && comment.State ==
                                (byte)ContractNegotiationCommentState.Open);

                        var approvalReadiness =
                            await GetApprovalReadinessAsync(
                                contract,
                                version,
                                items,
                                terms,
                                openCommentCount);

                        EnsureApprovalReadiness(approvalReadiness);

                        var rendered = await artifactRenderer.RenderAsync(
                            contract.ContractId,
                            employeeId);
                        if (rendered.SnapshotSchemaVersion !=
                            SoftwareSupplyContractSnapshotFactory.CurrentSchemaVersion)
                        {
                            throw new InvalidOperationException(
                                "Renderer không trả snapshot SoftwareSupply schema v4.");
                        }

                        if (version.TemplateVersionId.HasValue
                            && version.TemplateVersionId.Value !=
                            rendered.TemplateVersionId)
                        {
                            throw new DbUpdateConcurrencyException(
                                "TemplateVersion của version đã thay đổi trong lúc gửi duyệt.");
                        }

                        var existingArtifact = await _dbContext.TblFileStorages
                            .AsNoTracking()
                            .AnyAsync(file =>
                                file.ObjectType ==
                                    SubmittedContractArtifactObjectType
                                && file.ObjectId == version.VersionId);
                        if (existingArtifact)
                        {
                            throw new InvalidOperationException(
                                "Version đã có artifact gửi duyệt và không được ghi đè.");
                        }

                        await using var docxStream = new MemoryStream(
                            rendered.DocxContent,
                            writable: false);
                        var storedDocx = await privateFileStorage.SaveAsync(
                            new PrivateFileSaveRequest(
                                docxStream,
                                rendered.DocxFileName,
                                SubmittedDocxContentType,
                                rendered.DocxContent.LongLength,
                                tenant.TenantCode,
                                SubmittedContractArtifactObjectType,
                                version.VersionId,
                                PrivateFileUploadPolicies
                                    .SubmittedContractDocx()));
                        storedArtifacts.Add(storedDocx);

                        await using var pdfStream = new MemoryStream(
                            rendered.PdfContent,
                            writable: false);
                        var storedPdf = await privateFileStorage.SaveAsync(
                            new PrivateFileSaveRequest(
                                pdfStream,
                                rendered.PdfFileName,
                                SubmittedPdfContentType,
                                rendered.PdfContent.LongLength,
                                tenant.TenantCode,
                                SubmittedContractArtifactObjectType,
                                version.VersionId,
                                PrivateFileUploadPolicies
                                    .SubmittedContractPdf()));
                        storedArtifacts.Add(storedPdf);

                        var docxHash = CalculateArtifactHash(
                            rendered.DocxContent);
                        var pdfHash = CalculateArtifactHash(
                            rendered.PdfContent);
                        EnsureStoredArtifactHash(storedDocx, docxHash, "DOCX");
                        EnsureStoredArtifactHash(storedPdf, pdfHash, "PDF");

                        var docxMetadata = CreateSubmittedArtifactMetadata(
                            storedDocx,
                            version.VersionId,
                            employeeId,
                            "docx");
                        var pdfMetadata = CreateSubmittedArtifactMetadata(
                            storedPdf,
                            version.VersionId,
                            employeeId,
                            "pdf");
                        _dbContext.TblFileStorages.AddRange(
                            docxMetadata,
                            pdfMetadata);

                        var snapshotJson = rendered.SnapshotJson;
                        var snapshotHash = CalculateSnapshotHash(snapshotJson);

                        var now = DateTime.UtcNow;

                        version.TemplateVersionId = rendered.TemplateVersionId;
                        version.SnapshotJson = snapshotJson;
                        version.SnapshotHash = snapshotHash;
                        version.IsLocked = true;
                        version.LockedDate = now;
                        version.LockedByEmployeeId = employeeId;

                        var invalidatedAccess =
                            await InvalidateNegotiationAccessAsync(
                                contract,
                                version.VersionId,
                                employeeId,
                                now);

                        contract.Status =
                            (byte)ContractStatus.PendingApproval;

                        contract.UpdatedEmployeeId = employeeId;
                        contract.UpdateDate = now;

                        var approvalRequest =
                            new TblContractApprovalRequest
                            {
                                ContractId = contract.ContractId,
                                VersionId = version.VersionId,
                                WorkflowId = request.WorkflowId,

                                Status =
                                    (byte)ApprovalRequestStatus.Pending,

                                SubmittedByEmployeeId = employeeId,
                                SubmittedDate = now
                            };

                        _dbContext.TblContractApprovalRequests.Add(
                            approvalRequest);

                        // Cấp ID cho request và hai metadata artifact. Transaction
                        // vẫn chưa commit; audit submit ở lần SaveChanges kế tiếp
                        // vẫn nguyên tử với toàn bộ thay đổi nghiệp vụ.
                        await _dbContext.SaveChangesAsync();

                        _contractAuditWriter.StageEmployeeAudits(
                        [
                            new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            version.VersionId,
                            employeeId,
                            ContractAuditActionTypes.ApprovalSubmitted,
                            ContractAuditResults.Succeeded,
                            now,
                            PreviousContractStatus:
                                (byte)currentStatus,
                            NewContractStatus: contract.Status,
                            SubjectType:
                                ContractAuditSubjectTypes.Contract,
                            SubjectId: contract.ContractId,
                            PreviousValues: ContractAuditValues.Create(
                                ("Status", (byte)currentStatus),
                                ("CurrentVersionId", version.VersionId),
                                ("VersionLocked", false)),
                            NewValues: ContractAuditValues.Create(
                                ("Status", contract.Status),
                                ("CurrentVersionId", version.VersionId),
                                ("VersionLocked", version.IsLocked),
                                ("ApprovalRequestId",
                                    approvalRequest.ApprovalRequestId),
                                ("ApprovalStatus", approvalRequest.Status),
                                ("WorkflowId", approvalRequest.WorkflowId),
                                ("SnapshotSchemaVersion",
                                    rendered.SnapshotSchemaVersion),
                                ("TemplateVersionId",
                                    rendered.TemplateVersionId),
                                ("SnapshotHash", snapshotHash),
                                ("DocxFileId", docxMetadata.FileId),
                                ("DocxHash", docxHash),
                                ("PdfFileId", pdfMetadata.FileId),
                                ("PdfHash", pdfHash),
                                ("ArtifactCount", 2),
                                ("InvalidatedLinkCount",
                                    invalidatedAccess.LinkCount),
                                ("RevokedSessionCount",
                                    invalidatedAccess.SessionCount)))
                        ]);
                        await _dbContext.SaveChangesAsync();
                        await transaction.CommitAsync();

                        return new SubmitContractForApprovalResponse
                        {
                            ApprovalRequestId =
                                approvalRequest.ApprovalRequestId,

                            ContractId = contract.ContractId,
                            VersionId = version.VersionId,

                            ContractStatus =
                                ContractStatus.PendingApproval,

                            ApprovalStatus =
                                ApprovalRequestStatus.Pending,

                            SubmittedDate = now,
                            SnapshotHash = snapshotHash,
                            SubmittedDocxFileId = docxMetadata.FileId,
                            SubmittedDocxHash = docxHash,
                            SubmittedPdfFileId = pdfMetadata.FileId,
                            SubmittedPdfHash = pdfHash,

                            ContractRowVersion =
                                EncodeRowVersion(contract.RowVersion),

                            VersionRowVersion =
                                EncodeRowVersion(version.RowVersion)
                        };
                    }
                    catch (Exception exception)
                    {
                        await RollbackAndClearAsync(transaction);
                        try
                        {
                            await DeleteStoredArtifactsAsync(
                                privateFileStorage,
                                storedArtifacts);
                        }
                        catch (Exception cleanupException)
                        {
                            throw new AggregateException(
                                "Submit thất bại và không thể dọn hết private artifact đã lưu dở.",
                                exception,
                                cleanupException);
                        }

                        throw;
                    }
                });
            }
            catch (DbUpdateConcurrencyException exception)
            {
                _dbContext.ChangeTracker.Clear();
                await PersistContractConcurrencyAuditAsync(
                    contractId,
                    request.CurrentVersionId,
                    employeeId,
                    ContractAuditActionTypes.ApprovalSubmitted,
                    DateTime.UtcNow);
                throw new DbUpdateConcurrencyException(
                    "Hợp đồng đã được cập nhật. " +
                    "Vui lòng tải lại dữ liệu trước khi gửi duyệt.",
                    exception);
            }
        }

        /// <summary>
        /// Kiểm tra phòng thủ trong service.
        /// DTO validation vẫn là lớp kiểm tra đầu tiên.
        /// </summary>
        private static void ValidateRequest(
            CreateContractRequest request,
            int createdEmployeeId)
        {
            if (createdEmployeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đang đăng nhập.");
            }

            if (!Enum.IsDefined(
                    typeof(ContractType),
                    request.ContractType))
            {
                throw new ArgumentException(
                    "Loại hợp đồng không hợp lệ.");
            }

            if (!Enum.IsDefined(
                    typeof(ContractLanguageMode),
                    request.LanguageMode))
            {
                throw new ArgumentException(
                    "Chế độ ngôn ngữ không hợp lệ.");
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException(
                    "Hợp đồng phải có ít nhất một sản phẩm hoặc dịch vụ.");
            }

            _ = NormalizeCurrencyCode(request.CurrencyCode);

            foreach (var item in request.Items)
            {
                if (!Enum.IsDefined(
                        typeof(ContractItemType),
                        item.ItemType))
                {
                    throw new ArgumentException(
                        "Loại ContractItem không hợp lệ.");
                }

                ValidateFinanceItem(item);
            }
        }

        private static void ValidateCreateTermsAgainstTemplate(
            CreateContractRequest request,
            IReadOnlyList<TblContractTemplateTerm> templateTerms)
        {
            if (request.Terms is null)
            {
                return;
            }

            if (request.Terms.Count == 0)
            {
                throw new ArgumentException(
                    "Hợp đồng phải có ít nhất một điều khoản.");
            }

            var duplicatedCode = request.Terms
                .GroupBy(
                    term => term.TermCode?.Trim() ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicatedCode is not null)
            {
                throw new ArgumentException(
                    $"Mã điều khoản '{duplicatedCode.Key}' bị trùng.");
            }

            var duplicatedSource = request.Terms
                .Where(term => term.SourceTemplateTermId.HasValue)
                .GroupBy(term => term.SourceTemplateTermId!.Value)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicatedSource is not null)
            {
                throw new ArgumentException(
                    $"Điều khoản nguồn {duplicatedSource.Key} bị sử dụng nhiều lần.");
            }

            var templateById = templateTerms.ToDictionary(
                term => term.TemplateTermId);
            foreach (var term in request.Terms)
            {
                if (string.IsNullOrWhiteSpace(term.TermCode)
                    || string.IsNullOrWhiteSpace(term.TermTitle))
                {
                    throw new ArgumentException(
                        "Mã và tiêu đề điều khoản không được để trống.");
                }

                if (term.SourceTemplateTermId is int sourceId)
                {
                    if (!templateById.TryGetValue(sourceId, out var source))
                    {
                        throw new ArgumentException(
                            $"Điều khoản nguồn {sourceId} không thuộc template đã chọn.");
                    }

                    if (!string.Equals(
                            source.TermCode,
                            term.TermCode.Trim(),
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new ArgumentException(
                            $"Không được thay đổi mã của điều khoản nguồn {sourceId}.");
                    }
                }

                if (request.LanguageMode == ContractLanguageMode.Bilingual)
                {
                    if (string.IsNullOrWhiteSpace(term.TermTitleEn))
                    {
                        throw new ArgumentException(
                            $"Điều khoản '{term.TermCode}' phải có tiêu đề tiếng Anh.");
                    }

                    if (!string.IsNullOrWhiteSpace(term.TermContent)
                        && string.IsNullOrWhiteSpace(term.TermContentEn))
                    {
                        throw new ArgumentException(
                            $"Điều khoản '{term.TermCode}' phải có nội dung tiếng Anh.");
                    }
                }
            }
        }

        private async Task ValidateEmployeeAsync(int employeeId)
        {
            var exists = await _dbContext.TblEmployees
                .AsNoTracking()
                .AnyAsync(x => x.EmployeeId == employeeId);

            if (!exists)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy nhân viên tạo hợp đồng.");
            }
        }

        private async Task ValidateResponsibleEmployeeAsync(
            int employeeId)
        {
            var employee = await _dbContext.TblEmployees
                .AsNoTracking()
                .Where(x => x.EmployeeId == employeeId)
                .Select(x => new
                {
                    x.EmployeeId,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (employee == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy nhân viên phụ trách hợp đồng.");
            }

            // Convention hiện tại của EmployeeService:
            // 1 = Active, 0 = Inactive.
            if (employee.Status != ActiveEmployeeStatus)
            {
                throw new InvalidOperationException(
                    "Nhân viên phụ trách đang inactive.");
            }
        }

        private async Task<CustomerAuditSnapshot> ValidateCustomerAsync(
            int customerId)
        {
            var customer = await _dbContext.TblCustomers
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerCode,
                    x.CustomerFullName,
                    x.CustomerCompany,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy khách hàng.");
            }

            if (customer.Status != 1)
            {
                throw new InvalidOperationException(
                    "Khách hàng đang inactive, không thể tạo hợp đồng.");
            }

            return new CustomerAuditSnapshot(
                customer.CustomerId,
                BuildCustomerAuditDisplayName(
                    customer.CustomerId,
                    customer.CustomerCode,
                    customer.CustomerFullName,
                    customer.CustomerCompany));
        }

        private async Task<CustomerAuditSnapshot>
            GetCustomerAuditSnapshotAsync(int customerId)
        {
            var customer = await _dbContext.TblCustomers
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .Select(x => new
                {
                    x.CustomerId,
                    x.CustomerCode,
                    x.CustomerFullName,
                    x.CustomerCompany
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return new CustomerAuditSnapshot(
                    customerId,
                    $"Khách hàng #{customerId}");
            }

            return new CustomerAuditSnapshot(
                customer.CustomerId,
                BuildCustomerAuditDisplayName(
                    customer.CustomerId,
                    customer.CustomerCode,
                    customer.CustomerFullName,
                    customer.CustomerCompany));
        }

        private async Task ValidateTemplateAsync(
            CreateContractRequest request)
        {
            var templateData = await (
                from version in _dbContext
                    .TblContractTemplateVersions
                    .AsNoTracking()

                join template in _dbContext
                    .TblContractTemplates
                    .AsNoTracking()
                    on version.TemplateId equals template.TemplateId

                where version.TemplateVersionId ==
                      request.TemplateVersionId

                select new
                {
                    VersionStatus = version.Status,
                    template.DocumentType,
                    template.LanguageMode,
                    template.IsActive,
                    template.CurrentPublishedVersionId
                })
                .FirstOrDefaultAsync();

            if (templateData == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy template version.");
            }

            var canBeSelected =
                ContractTemplatePolicy.CanBeSelectedForNewDocument(
                    templateData.IsActive,
                    (TemplateVersionStatus)
                        templateData.VersionStatus);

            if (!canBeSelected)
            {
                throw new InvalidOperationException(
                    "Chỉ được dùng template active và version đã Published.");
            }

            if (templateData.CurrentPublishedVersionId !=
                request.TemplateVersionId)
            {
                throw new InvalidOperationException(
                    "Template version không phải version Published hiện hành.");
            }

            var expectedDocumentType =
                GetExpectedTemplateDocumentType(
                    request.ContractType);

            if (templateData.DocumentType !=
                (byte)expectedDocumentType)
            {
                throw new InvalidOperationException(
                    "Loại template không khớp với loại hợp đồng.");
            }

            if (templateData.LanguageMode !=
                (byte)request.LanguageMode)
            {
                throw new InvalidOperationException(
                    "Ngôn ngữ template không khớp với hợp đồng.");
            }
        }

        private async Task ValidateParentContractAsync(
            CreateContractRequest request,
            int employeeId)
        {
            if (!request.ParentContractId.HasValue)
            {
                return;
            }

            var parentContract = await _dbContext.TblContracts
                .AsNoTracking()
                .Where(x =>
                    x.ContractId ==
                    request.ParentContractId.Value)
                .Select(x => new
                {
                    x.CustomerId,
                    x.EmployeeId,
                    x.ContractType,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (parentContract == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng nguồn.");
            }

            // Người dùng chỉ được chọn hợp đồng mình có quyền xem.
            if (parentContract.EmployeeId != employeeId)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng nguồn hoặc bạn không có quyền xem.");
            }

            if (parentContract.CustomerId != request.CustomerId)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải thuộc cùng khách hàng.");
            }

            if (parentContract.ContractType !=
                (byte)ContractType.SoftwareSupply)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải là hợp đồng cung cấp phần mềm.");
            }

            // Đã ký nhưng chưa hoàn thành vẫn chưa đủ điều kiện.
            if (parentContract.Status !=
                (byte)ContractStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải ở trạng thái Completed.");
            }
        }

        private async Task ValidateCatalogSourcesAsync(
            List<CreateContractItemRequest> items)
        {
            foreach (var item in items)
            {
                if (item.ItemType == ContractItemType.Product
                    && item.SourceServiceId.HasValue)
                {
                    throw new ArgumentException(
                        "Item loại Product không được truyền SourceServiceId.");
                }

                if (item.ItemType == ContractItemType.Service
                    && item.SourceProductId.HasValue)
                {
                    throw new ArgumentException(
                        "Item loại Service không được truyền SourceProductId.");
                }
            }

            var productIds = items
                .Where(x => x.SourceProductId.HasValue)
                .Select(x => x.SourceProductId!.Value)
                .Distinct()
                .ToList();

            if (productIds.Count > 0)
            {
                var activeProducts =
                    await _dbContext.TblProducts
                        .AsNoTracking()
                        .Where(x =>
                            productIds.Contains(x.ProductId) &&
                            x.Status == 1)
                        .Select(x => new
                        {
                            x.ProductId,
                            x.ProductCode
                        })
                        .ToDictionaryAsync(x => x.ProductId);

                if (activeProducts.Count != productIds.Count)
                {
                    throw new InvalidOperationException(
                        "Có Product nguồn không tồn tại hoặc đang inactive.");
                }

                foreach (var item in items.Where(
                             x => x.SourceProductId.HasValue))
                {
                    item.ItemCode = activeProducts[
                        item.SourceProductId!.Value].ProductCode;
                }
            }

            var serviceIds = items
                .Where(x => x.SourceServiceId.HasValue)
                .Select(x => x.SourceServiceId!.Value)
                .Distinct()
                .ToList();

            if (serviceIds.Count > 0)
            {
                var activeServiceIds =
                    await _dbContext.TblServices
                        .AsNoTracking()
                        .Where(x =>
                            serviceIds.Contains(x.ServiceId) &&
                            x.Status == 1)
                        .Select(x => x.ServiceId)
                        .ToListAsync();

                if (activeServiceIds.Count != serviceIds.Count)
                {
                    throw new InvalidOperationException(
                        "Có Service nguồn không tồn tại hoặc đang inactive.");
                }

                foreach (var item in items.Where(
                             x => x.SourceServiceId.HasValue))
                {
                    item.ItemCode = null;
                }
            }
        }

        /// <summary>
        /// Tính tiền cho một dòng:
        /// subtotal → discount → VAT → total.
        /// </summary>
        private static (
            decimal LineSubtotal,
            decimal DiscountAmount,
            decimal VatAmount,
            decimal LineTotal)
            CalculateItemAmounts(
                CreateContractItemRequest item,
                string currencyCode)
        {
            // Ngăn phép nhân decimal bị overflow.
            if (item.UnitPrice > 0 &&
                item.Quantity > MaxMoney / item.UnitPrice)
            {
                throw new InvalidOperationException(
                    $"Giá trị item '{item.ItemName}' vượt giới hạn.");
            }

            var subtotal = RoundMoney(
                item.Quantity * item.UnitPrice,
                currencyCode);

            var discountAmount = item.DiscountMode switch
            {
                ContractItemDiscountMode.None => 0m,
                ContractItemDiscountMode.Percentage => RoundMoney(
                    subtotal * item.DiscountPercent / 100m,
                    currencyCode),
                ContractItemDiscountMode.FixedAmount => RoundMoney(
                    item.FixedDiscountAmount,
                    currencyCode),
                _ => throw new ArgumentException(
                    "DiscountMode không hợp lệ.")
            };

            if (discountAmount > subtotal)
            {
                throw new ArgumentException(
                    "Fixed discount không được vượt số tiền trước giảm giá.");
            }

            var amountAfterDiscount =
                subtotal - discountAmount;

            var vatAmount = item.IsTaxable
                ? RoundMoney(
                    amountAfterDiscount *
                    item.VatPercent /
                    100m,
                    currencyCode)
                : 0m;

            var lineTotal = RoundMoney(
                amountAfterDiscount + vatAmount,
                currencyCode);

            if (lineTotal > MaxMoney)
            {
                throw new InvalidOperationException(
                    $"Tổng tiền item '{item.ItemName}' vượt giới hạn.");
            }

            return (
                subtotal,
                discountAmount,
                vatAmount,
                lineTotal);
        }

        private static decimal RoundMoney(
            decimal value,
            string currencyCode)
        {
            var decimals = currencyCode == "VND" ? 0 : 2;

            return Math.Round(
                value,
                decimals,
                MidpointRounding.AwayFromZero);
        }

        private static string NormalizeCurrencyCode(string? currencyCode)
        {
            var normalized = currencyCode?
                .Trim()
                .ToUpperInvariant();

            if (normalized is not ("VND" or "USD"))
            {
                throw new ArgumentException(
                    "CurrencyCode chỉ hỗ trợ VND hoặc USD.");
            }

            return normalized;
        }

        private static void ValidateFinanceItem(
            CreateContractItemRequest item)
        {
            if (item.Quantity <= 0 || GetDecimalScale(item.Quantity) > 3)
            {
                throw new ArgumentException(
                    "Quantity phải lớn hơn 0 và có tối đa 3 chữ số thập phân.");
            }

            if (item.UnitPrice < 0
                || item.DiscountPercent < 0
                || item.FixedDiscountAmount < 0
                || item.VatPercent < 0)
            {
                throw new ArgumentException(
                    "Dữ liệu tài chính không được âm.");
            }

            if (!Enum.IsDefined(item.DiscountMode))
            {
                throw new ArgumentException(
                    "DiscountMode không hợp lệ.");
            }

            if (item.DiscountPercent > 100
                || GetDecimalScale(item.DiscountPercent) > 2)
            {
                throw new ArgumentException(
                    "DiscountPercent phải từ 0 đến 100 và có tối đa 2 chữ số thập phân.");
            }

            if (item.VatPercent > 100
                || GetDecimalScale(item.VatPercent) > 2)
            {
                throw new ArgumentException(
                    "VatPercent phải từ 0 đến 100 và có tối đa 2 chữ số thập phân.");
            }

            if (item.DiscountMode == ContractItemDiscountMode.None
                && (item.DiscountPercent != 0
                    || item.FixedDiscountAmount != 0))
            {
                throw new ArgumentException(
                    "DiscountMode None không nhận discount rate hoặc fixed amount.");
            }

            if (item.DiscountMode == ContractItemDiscountMode.Percentage
                && item.FixedDiscountAmount != 0)
            {
                throw new ArgumentException(
                    "Percentage discount không được đồng thời có fixed amount.");
            }

            if (item.DiscountMode == ContractItemDiscountMode.FixedAmount
                && item.DiscountPercent != 0)
            {
                throw new ArgumentException(
                    "Fixed discount không được đồng thời có percentage rate.");
            }

            if (!item.IsTaxable && item.VatPercent != 0)
            {
                throw new ArgumentException(
                    "Item Non-taxable phải có VatPercent bằng 0.");
            }
        }

        private static int GetDecimalScale(decimal value)
        {
            var bits = decimal.GetBits(value);
            return (bits[3] >> 16) & 0x7F;
        }

        private static ContractFinancialTotals AddToContractTotals(
            ContractFinancialTotals totals,
            (
                decimal LineSubtotal,
                decimal DiscountAmount,
                decimal VatAmount,
                decimal LineTotal
            ) amounts)
        {
            if (totals.Subtotal > MaxMoney - amounts.LineSubtotal
                || totals.TotalDiscount >
                    MaxMoney - amounts.DiscountAmount
                || totals.TotalVat > MaxMoney - amounts.VatAmount
                || totals.TotalPayment > MaxMoney - amounts.LineTotal)
            {
                throw new InvalidOperationException(
                    "Tổng giá trị hợp đồng vượt quá giới hạn cho phép.");
            }

            return new ContractFinancialTotals(
                totals.Subtotal + amounts.LineSubtotal,
                totals.TotalDiscount + amounts.DiscountAmount,
                totals.TotalVat + amounts.VatAmount,
                totals.TotalPayment + amounts.LineTotal);
        }

        private static void ApplyFinancialTotals(
            TblContract contract,
            TblContractVersion version,
            string currencyCode,
            ContractFinancialTotals totals)
        {
            contract.CurrencyCode = currencyCode;
            contract.Subtotal = totals.Subtotal;
            contract.TotalDiscount = totals.TotalDiscount;
            contract.TotalVat = totals.TotalVat;
            contract.TotalAmount = totals.TotalPayment;

            version.CurrencyCode = currencyCode;
            version.Subtotal = totals.Subtotal;
            version.TotalDiscount = totals.TotalDiscount;
            version.TotalVat = totals.TotalVat;
            version.TotalAmount = totals.TotalPayment;
        }

        private sealed record CustomerAuditSnapshot(
            int CustomerId,
            string DisplayName);

        private readonly record struct ContractFinancialTotals(
            decimal Subtotal,
            decimal TotalDiscount,
            decimal TotalVat,
            decimal TotalPayment)
        {
            public static ContractFinancialTotals Zero =>
                new(0m, 0m, 0m, 0m);
        }

        private static string BuildContractCode(
            DateTime createdDate,
            int contractId)
        {
            return $"HD-{createdDate:yyyyMMdd}-{contractId:D6}";
        }

        private static TemplateDocumentType
            GetExpectedTemplateDocumentType(
                ContractType contractType)
        {
            return contractType switch
            {
                ContractType.SoftwareSupply =>
                    TemplateDocumentType
                        .SoftwareSupplyContract,

                ContractType.SoftwareMaintenance =>
                    TemplateDocumentType
                        .SoftwareMaintenanceContract,

                ContractType.SoftwareUpkeep =>
                    TemplateDocumentType
                        .SoftwareUpkeepContract,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(contractType),
                    contractType,
                    "Loại hợp đồng không hợp lệ.")
            };
        }

        private static string? NormalizeOptional(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string BuildCustomerAuditDisplayName(
            int customerId,
            string? customerCode,
            string? customerFullName,
            string? customerCompany)
        {
            var displayName = NormalizeOptional(customerCompany)
                ?? NormalizeOptional(customerFullName)
                ?? NormalizeOptional(customerCode)
                ?? $"Khách hàng #{customerId}";

            return BuildAuditSafeText(displayName)!;
        }

        private static string? BuildAuditSafeText(string? value)
        {
            var normalized = NormalizeOptional(value);
            if (normalized == null)
            {
                return null;
            }

            return normalized.Length <= MaxAuditSummaryLength
                ? normalized
                : normalized[..MaxAuditSummaryLength];
        }

        private static string? BuildAuditSummary(
            IReadOnlyCollection<string> entries)
        {
            if (entries.Count == 0)
            {
                return null;
            }

            var summary = string.Join("; ", entries);
            return summary.Length <= MaxAuditSummaryLength
                ? summary
                : summary[..(MaxAuditSummaryLength - 1)] + "…";
        }

        private static string BuildItemAuditEntry(
            int? contractItemId,
            string? itemCode,
            string itemName,
            IReadOnlyCollection<string>? changedFields = null)
        {
            var code = NormalizeOptional(itemCode);
            var name = itemName.Trim();
            var identity = code == null
                ? name
                : string.Equals(code, name, StringComparison.OrdinalIgnoreCase)
                    ? code
                    : $"{code} - {name}";
            var idSuffix = contractItemId.HasValue
                ? $" (#{contractItemId.Value})"
                : string.Empty;
            var changes = changedFields is { Count: > 0 }
                ? $" [{string.Join(", ", changedFields)}]"
                : string.Empty;

            return identity + idSuffix + changes;
        }

        private static string BuildTermAuditEntry(
            int? termId,
            string termCode,
            string termTitle,
            IReadOnlyCollection<string>? changedFields = null)
        {
            var code = termCode.Trim();
            var title = termTitle.Trim();
            var identity = string.Equals(
                code,
                title,
                StringComparison.OrdinalIgnoreCase)
                ? code
                : $"{code} - {title}";
            var idSuffix = termId.HasValue
                ? $" (#{termId.Value})"
                : string.Empty;
            var changes = changedFields is { Count: > 0 }
                ? $" [{string.Join(", ", changedFields)}]"
                : string.Empty;

            return identity + idSuffix + changes;
        }

        private static List<string> GetChangedItemFields(
            TblContractItem current,
            CreateContractItemRequest requested,
            int displayOrder)
        {
            var fields = new List<string>();

            AddChangedField(fields, "Mã", current.ItemCode,
                NormalizeOptional(requested.ItemCode));
            AddChangedField(fields, "Tên", current.ItemName,
                requested.ItemName.Trim());
            AddChangedField(fields, "Tên tiếng Anh", current.ItemNameEn,
                NormalizeOptional(requested.ItemNameEn));
            AddChangedField(fields, "Mô tả", current.ItemDescription,
                NormalizeOptional(requested.ItemDescription));
            AddChangedField(fields, "Mô tả tiếng Anh",
                current.ItemDescriptionEn,
                NormalizeOptional(requested.ItemDescriptionEn));
            AddChangedField(fields, "Đơn vị", current.UnitName,
                NormalizeOptional(requested.UnitName));
            AddChangedField(fields, "Đơn vị tiếng Anh", current.UnitNameEn,
                NormalizeOptional(requested.UnitNameEn));
            AddChangedField(fields, "Số lượng", current.Quantity,
                requested.Quantity);
            AddChangedField(fields, "Đơn giá", current.UnitPrice,
                requested.UnitPrice);
            AddChangedField(fields, "Kiểu chiết khấu", current.DiscountMode,
                (byte)requested.DiscountMode);
            AddChangedField(fields, "% chiết khấu",
                current.DiscountPercent,
                requested.DiscountPercent);
            AddChangedField(fields, "Chiết khấu cố định",
                current.FixedDiscountAmount,
                requested.FixedDiscountAmount);
            AddChangedField(fields, "Chịu thuế", current.IsTaxable,
                requested.IsTaxable);
            AddChangedField(fields, "% VAT", current.VatPercent,
                requested.VatPercent);
            AddChangedField(fields, "Thứ tự", current.DisplayOrder,
                displayOrder);

            return fields;
        }

        private static List<string> GetChangedTermFields(
            TblContractTerm current,
            UpdateContractTermRequest requested,
            int displayOrder)
        {
            var fields = new List<string>();

            AddChangedField(fields, "Tiêu đề", current.TermTitle,
                requested.TermTitle.Trim());
            AddChangedField(fields, "Tiêu đề tiếng Anh", current.TermTitleEn,
                NormalizeOptional(requested.TermTitleEn));
            AddChangedField(fields, "Nội dung", current.TermContent,
                NormalizeOptional(requested.TermContent));
            AddChangedField(fields, "Nội dung tiếng Anh",
                current.TermContentEn,
                NormalizeOptional(requested.TermContentEn));
            AddChangedField(fields, "Cho phép đàm phán",
                current.IsNegotiable,
                requested.IsNegotiable);
            AddChangedField(fields, "Thứ tự", current.DisplayOrder,
                displayOrder);

            return fields;
        }

        private static void AddChangedField<T>(
            ICollection<string> fields,
            string label,
            T previousValue,
            T newValue)
        {
            if (!EqualityComparer<T>.Default.Equals(
                    previousValue,
                    newValue))
            {
                fields.Add(label);
            }
        }

        /// <summary>
        /// Chuyển SQL Server rowversion thành chuỗi Base64
        /// để frontend có thể gửi lại khi cập nhật dữ liệu.
        /// </summary>
        private static string EncodeRowVersion(byte[]? rowVersion)
        {
            return rowVersion is { Length: > 0 }
                ? Convert.ToBase64String(rowVersion)
                : string.Empty;
        }

        private static ContractNegotiationRoundVersionResponse
            MapNegotiationRoundVersion(TblContractVersion version)
        {
            return new ContractNegotiationRoundVersionResponse
            {
                VersionId = version.VersionId,
                VersionNo = version.VersionNo,
                SourceVersionId = version.SourceVersionId,
                IsLocked = version.IsLocked,
                LockedDate = version.LockedDate,
                SnapshotHash = version.SnapshotHash,
                RowVersion = EncodeRowVersion(version.RowVersion)
            };
        }

        private static void ValidateUpdateRequest(
    int contractId,
    UpdateContractDraftRequest request,
    int employeeId)
        {
            if (contractId <= 0)
            {
                throw new ArgumentException(
                    "ContractId phải lớn hơn 0.");
            }

            if (employeeId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "Không xác định được nhân viên đang đăng nhập.");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (string.IsNullOrWhiteSpace(request.ContractName))
            {
                throw new ArgumentException(
                    "Tên hợp đồng không được để trống.");
            }

            if (request.Items == null || request.Items.Count == 0)
            {
                throw new ArgumentException(
                    "Hợp đồng phải có ít nhất một item.");
            }

            _ = NormalizeCurrencyCode(request.CurrencyCode);

            if (request.Terms == null || request.Terms.Count == 0)
            {
                throw new ArgumentException(
                    "Hợp đồng phải có ít nhất một điều khoản.");
            }

            if (request.ExpireDate.HasValue
                && request.EffectiveDate.HasValue
                && request.ExpireDate < request.EffectiveDate)
            {
                throw new ArgumentException(
                    "Ngày hết hạn không được trước ngày hiệu lực.");
            }

            foreach (var item in request.Items)
            {
                if (!Enum.IsDefined(
                        typeof(ContractItemType),
                        item.ItemType))
                {
                    throw new ArgumentException(
                        "Loại item không hợp lệ.");
                }

                ValidateFinanceItem(item);

                if (item.ContractItemId.HasValue
                    && string.IsNullOrWhiteSpace(item.RowVersion))
                {
                    throw new ArgumentException(
                        $"Item {item.ContractItemId.Value} thiếu RowVersion.");
                }
            }

            foreach (var term in request.Terms)
            {
                if (string.IsNullOrWhiteSpace(term.TermCode))
                {
                    throw new ArgumentException(
                        "TermCode không được để trống.");
                }

                if (string.IsNullOrWhiteSpace(term.TermTitle))
                {
                    throw new ArgumentException(
                        $"Term {term.TermCode} thiếu tiêu đề.");
                }

                if (term.TermId.HasValue
                    && string.IsNullOrWhiteSpace(term.RowVersion))
                {
                    throw new ArgumentException(
                        $"Term {term.TermId.Value} thiếu RowVersion.");
                }
            }

            var duplicatedItemId = request.Items
                .Where(x => x.ContractItemId.HasValue)
                .GroupBy(x => x.ContractItemId!.Value)
                .FirstOrDefault(x => x.Count() > 1);

            if (duplicatedItemId != null)
            {
                throw new ArgumentException(
                    $"ContractItemId {duplicatedItemId.Key} bị lặp.");
            }

            var duplicatedTermId = request.Terms
                .Where(x => x.TermId.HasValue)
                .GroupBy(x => x.TermId!.Value)
                .FirstOrDefault(x => x.Count() > 1);

            if (duplicatedTermId != null)
            {
                throw new ArgumentException(
                    $"TermId {duplicatedTermId.Key} bị lặp.");
            }

            var duplicatedTermCode = request.Terms
                .GroupBy(
                    x => x.TermCode.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault(x => x.Count() > 1);

            if (duplicatedTermCode != null)
            {
                throw new ArgumentException(
                    $"TermCode '{duplicatedTermCode.Key}' bị trùng.");
            }
        }

        private static void ValidateBilingualUpdate(
            TblContract contract,
            UpdateContractDraftRequest request)
        {
            if (contract.LanguageMode !=
                (byte)ContractLanguageMode.Bilingual)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(request.ContractNameEn))
            {
                throw new InvalidOperationException(
                    "Hợp đồng song ngữ bắt buộc có tên tiếng Anh.");
            }

            for (var index = 0;
                 index < request.Items.Count;
                 index++)
            {
                if (string.IsNullOrWhiteSpace(
                        request.Items[index].ItemNameEn))
                {
                    throw new InvalidOperationException(
                        $"Item thứ {index + 1} phải có tên tiếng Anh.");
                }
            }

            for (var index = 0;
                 index < request.Terms.Count;
                 index++)
            {
                var term = request.Terms[index];

                if (string.IsNullOrWhiteSpace(term.TermTitleEn))
                {
                    throw new InvalidOperationException(
                        $"Term '{term.TermCode}' phải có tiêu đề tiếng Anh.");
                }

                if (!string.IsNullOrWhiteSpace(term.TermContent)
                    && string.IsNullOrWhiteSpace(term.TermContentEn))
                {
                    throw new InvalidOperationException(
                        $"Term '{term.TermCode}' phải có nội dung tiếng Anh.");
                }
            }
        }

        private async Task ValidateParentContractForCustomerAsync(
            int? parentContractId,
            int customerId)
        {
            if (!parentContractId.HasValue)
            {
                return;
            }

            var parent = await _dbContext.TblContracts
                .AsNoTracking()
                .Where(x => x.ContractId == parentContractId.Value)
                .Select(x => new
                {
                    x.CustomerId,
                    x.ContractType,
                    x.Status
                })
                .FirstOrDefaultAsync();

            if (parent == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng nguồn.");
            }

            if (parent.CustomerId != customerId)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải thuộc cùng khách hàng.");
            }

            if (parent.ContractType !=
                (byte)ContractType.SoftwareSupply)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải là hợp đồng cung cấp phần mềm.");
            }

            // Khi sửa Draft cũng phải giữ đúng rule chỉ nhận Completed.
            if (parent.Status !=
                (byte)ContractStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Hợp đồng nguồn phải ở trạng thái Completed.");
            }
        }

        private static void ApplyItemSnapshot(
            TblContractItem target,
            CreateContractItemRequest source,
            (
                decimal LineSubtotal,
                decimal DiscountAmount,
                decimal VatAmount,
                decimal LineTotal
            ) amounts,
            int displayOrder)
        {
            target.ItemType = (byte)source.ItemType;
            target.SourceProductId = source.SourceProductId;
            target.SourceServiceId = source.SourceServiceId;

            target.ItemCode = NormalizeOptional(source.ItemCode);
            target.ItemName = source.ItemName.Trim();
            target.ItemNameEn = NormalizeOptional(source.ItemNameEn);

            target.ItemDescription =
                NormalizeOptional(source.ItemDescription);

            target.ItemDescriptionEn =
                NormalizeOptional(source.ItemDescriptionEn);

            target.UnitName = NormalizeOptional(source.UnitName);
            target.UnitNameEn = NormalizeOptional(source.UnitNameEn);

            target.Quantity = source.Quantity;
            target.UnitPrice = source.UnitPrice;

            target.LineSubtotal = amounts.LineSubtotal;
            target.DiscountMode = (byte)source.DiscountMode;
            target.DiscountPercent = source.DiscountPercent;
            target.FixedDiscountAmount =
                source.FixedDiscountAmount;
            target.DiscountAmount = amounts.DiscountAmount;

            target.IsTaxable = source.IsTaxable;
            target.VatPercent = source.VatPercent;
            target.VatAmount = amounts.VatAmount;
            target.LineTotal = amounts.LineTotal;

            target.DisplayOrder = displayOrder;
        }

        private static byte[] DecodeRowVersion(
            string? rowVersion,
            string fieldName)
        {
            if (string.IsNullOrWhiteSpace(rowVersion))
            {
                throw new ArgumentException(
                    $"{fieldName} không được để trống.");
            }

            try
            {
                var bytes = Convert.FromBase64String(rowVersion);

                // SQL Server rowversion luôn có 8 byte.
                if (bytes.Length != 8)
                {
                    throw new ArgumentException(
                        $"{fieldName} không hợp lệ.");
                }

                return bytes;
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    $"{fieldName} không đúng định dạng Base64.");
            }
        }

        private static void EnsureRowVersionMatches(
            byte[] currentRowVersion,
            byte[] expectedRowVersion,
            string resourceName)
        {
            if (!currentRowVersion
                    .AsSpan()
                    .SequenceEqual(expectedRowVersion))
            {
                throw new DbUpdateConcurrencyException(
                    $"{resourceName} đã được cập nhật bởi request khác.");
            }
        }

        private async Task<ContractApprovalReadinessResponse>
            GetApprovalReadinessAsync(
                TblContract contract,
                TblContractVersion version,
                IReadOnlyCollection<TblContractItem> items,
                IReadOnlyCollection<TblContractTerm> terms,
                int openCommentCount)
        {
            var versionLinks = await _dbContext
                .TblContractCustomerAccessLinks
                .AsNoTracking()
                .Where(link =>
                    link.ContractId == contract.ContractId
                    && link.VersionId == version.VersionId)
                .ToListAsync();

            var hasEverBeenShared = versionLinks.Any(link =>
                link.ActivatedAt.HasValue);
            var now = DateTime.UtcNow;
            var hasActiveCurrentVersionLink =
                contract.CurrentCustomerAccessLinkId.HasValue
                && versionLinks.Any(link =>
                    link.CustomerAccessLinkId ==
                    contract.CurrentCustomerAccessLinkId.Value
                    && link.ActivatedAt.HasValue
                    && !link.RevokedAt.HasValue
                    && link.ExpiresAt > now);

            var blockers = new List<
                ContractApprovalReadinessBlockerResponse>();

            static void AddBlocker(
                List<ContractApprovalReadinessBlockerResponse> target,
                string code,
                string message) => target.Add(new()
                {
                    Code = code,
                    Message = message
                });

            if ((ContractStatus)contract.Status !=
                ContractStatus.Negotiating)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractNotNegotiating,
                    "Hợp đồng phải ở trạng thái Đang đàm phán trước khi gửi duyệt.");
            }

            if (version.IsLocked)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.CurrentVersionLocked,
                    "Version hiện tại đã bị khóa.");
            }

            if (string.IsNullOrWhiteSpace(contract.ContractCode))
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractCodeRequired,
                    "Hợp đồng chưa có mã.");
            }

            if (string.IsNullOrWhiteSpace(contract.ContractName))
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractNameRequired,
                    "Hợp đồng chưa có tên.");
            }

            if (items.Count == 0)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractItemRequired,
                    "Hợp đồng phải có ít nhất một item.");
            }

            if (terms.Count == 0)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractTermRequired,
                    "Hợp đồng phải có ít nhất một điều khoản.");
            }

            if (contract.EffectiveDate.HasValue
                && contract.ExpireDate.HasValue
                && contract.ExpireDate < contract.EffectiveDate)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.InvalidContractDateRange,
                    "Ngày hết hạn không được trước ngày hiệu lực.");
            }

            if (items.Sum(item => item.LineTotal) != contract.TotalAmount)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.ContractTotalMismatch,
                    "Tổng tiền hợp đồng không khớp với các item.");
            }

            if (contract.LanguageMode ==
                (byte)ContractLanguageMode.Bilingual)
            {
                if (string.IsNullOrWhiteSpace(contract.ContractNameEn))
                {
                    AddBlocker(
                        blockers,
                        ContractApprovalReadinessCodes
                            .BilingualContractNameRequired,
                        "Hợp đồng song ngữ thiếu tên tiếng Anh.");
                }

                if (items.Any(item =>
                        string.IsNullOrWhiteSpace(item.ItemNameEn)))
                {
                    AddBlocker(
                        blockers,
                        ContractApprovalReadinessCodes
                            .BilingualItemNameRequired,
                        "Hợp đồng song ngữ có item thiếu tên tiếng Anh.");
                }

                if (terms.Any(term =>
                        string.IsNullOrWhiteSpace(term.TermTitleEn)))
                {
                    AddBlocker(
                        blockers,
                        ContractApprovalReadinessCodes
                            .BilingualTermTitleRequired,
                        "Hợp đồng song ngữ có điều khoản thiếu tiêu đề tiếng Anh.");
                }
            }

            if (!hasEverBeenShared)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes.CurrentVersionNotShared,
                    "Version hiện tại chưa được chia sẻ với khách hàng.");
            }
            else if (!hasActiveCurrentVersionLink)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes
                        .ActiveCustomerAccessLinkRequired,
                    "Version hiện tại cần một link khách hàng đang hoạt động trước khi gửi duyệt.");
            }

            if (openCommentCount > 0)
            {
                AddBlocker(
                    blockers,
                    ContractApprovalReadinessCodes
                        .OpenNegotiationCommentsExist,
                    $"Còn {openCommentCount} trao đổi chưa được xử lý.");
            }

            return new ContractApprovalReadinessResponse
            {
                CanSubmit = blockers.Count == 0,
                HasEverBeenShared = hasEverBeenShared,
                HasActiveCurrentVersionLink =
                    hasActiveCurrentVersionLink,
                OpenCommentCount = openCommentCount,
                Blockers = blockers
            };
        }

        private static void EnsureApprovalReadiness(
            ContractApprovalReadinessResponse readiness)
        {
            var blocker = readiness.Blockers.FirstOrDefault();
            if (blocker is null)
            {
                return;
            }

            var isStateConflict = blocker.Code is
                ContractApprovalReadinessCodes.ContractNotNegotiating
                or ContractApprovalReadinessCodes.CurrentVersionLocked
                or ContractApprovalReadinessCodes.CurrentVersionNotShared
                or ContractApprovalReadinessCodes
                    .ActiveCustomerAccessLinkRequired
                or ContractApprovalReadinessCodes
                    .OpenNegotiationCommentsExist;

            throw new BusinessRuleException(
                isStateConflict
                    ? StatusCodes.Status409Conflict
                    : StatusCodes.Status400BadRequest,
                blocker.Code,
                blocker.Message);
        }

        private async Task EnsureEligibleManagerApproverAsync(
            int submitterEmployeeId,
            TblApprovalWorkflow? workflow)
        {
            if (workflow?.ApproverEmployeeId is int assignedApproverId)
            {
                if (assignedApproverId == submitterEmployeeId)
                {
                    throw new InvalidOperationException(
                        "Người gửi duyệt không được tự duyệt hợp đồng của mình.");
                }

                var assignedManagerExists = await _dbContext.TblEmployees
                    .AsNoTracking()
                    .AnyAsync(employee =>
                        employee.EmployeeId == assignedApproverId
                        && employee.Status == ActiveEmployeeStatus
                        && employee.EmployeeType == (byte)EmployeeType.Manager);
                if (!assignedManagerExists)
                {
                    throw new InvalidOperationException(
                        "Workflow không có Manager đang hoạt động phù hợp để duyệt.");
                }

                return;
            }

            var managerExists = await _dbContext.TblEmployees
                .AsNoTracking()
                .AnyAsync(employee =>
                    employee.EmployeeId != submitterEmployeeId
                    && employee.Status == ActiveEmployeeStatus
                    && employee.EmployeeType == (byte)EmployeeType.Manager);
            if (!managerExists)
            {
                throw new InvalidOperationException(
                    "Không có Manager đang hoạt động khác người gửi để duyệt hợp đồng.");
            }
        }

        private async Task<(int LinkCount, int SessionCount)>
            InvalidateNegotiationAccessAsync(
                TblContract contract,
                int submittedVersionId,
                int employeeId,
                DateTime now)
        {
            const string reason = "Contract submitted for approval";
            var allLinkIds = await _dbContext.TblContractCustomerAccessLinks
                .Where(link => link.ContractId == contract.ContractId)
                .Select(link => link.CustomerAccessLinkId)
                .ToListAsync();
            var activeLinks = await _dbContext.TblContractCustomerAccessLinks
                .Where(link => link.ContractId == contract.ContractId
                    && link.RevokedAt == null)
                .ToListAsync();

            foreach (var link in activeLinks)
            {
                link.RevokedAt = now;
                link.RevokedByEmployeeId = employeeId;
                link.RevocationReason = reason;
            }

            if (allLinkIds.Count > 0)
            {
                var challenges = await _dbContext
                    .TblContractCustomerOtpChallenges
                    .Where(challenge =>
                        allLinkIds.Contains(challenge.LinkId)
                        && challenge.InvalidatedAt == null)
                    .ToListAsync();
                foreach (var challenge in challenges)
                {
                    challenge.InvalidatedAt = now;
                }
            }

            var sessions = await _dbContext.TblContractCustomerAccessSessions
                .Where(session => session.ContractId == contract.ContractId
                    && session.RevokedAt == null)
                .ToListAsync();
            foreach (var session in sessions)
            {
                session.RevokedAt = now;
                session.RevocationReason = reason;
            }

            contract.CurrentCustomerAccessLinkId = null;

            if (activeLinks.Count > 0)
            {
                _contractAuditWriter.StageEmployeeAudits(activeLinks
                    .Select(link => new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        submittedVersionId,
                        employeeId,
                        ContractAuditActionTypes.CustomerAccessLinkInvalidated,
                        ContractAuditResults.Succeeded,
                        now,
                        Reason: reason,
                        SubjectType:
                            ContractAuditSubjectTypes.CustomerAccessLink,
                        SubjectId: link.CustomerAccessLinkId,
                        NewValues: ContractAuditValues.Create(
                            ("LinkId", link.CustomerAccessLinkId),
                            ("CurrentVersionId", submittedVersionId),
                            ("LinkState", "Invalidated"))))
                    .ToArray());
            }

            if (sessions.Count > 0)
            {
                _contractAuditWriter.StageEmployeeAudits(sessions
                    .Select(session => new EmployeeContractAuditWriteRequest(
                        contract.ContractId,
                        session.VersionId,
                        employeeId,
                        ContractAuditActionTypes.CustomerSessionRevoked,
                        ContractAuditResults.Succeeded,
                        now,
                        Reason: reason,
                        SubjectType:
                            ContractAuditSubjectTypes.CustomerAccessSession,
                        SubjectId: session.CustomerAccessSessionId,
                        NewValues: ContractAuditValues.Create(
                            ("CustomerAccessSessionId",
                                session.CustomerAccessSessionId),
                            ("SessionState", "Revoked"),
                            ("RevocationReasonCode", "ApprovalSubmitted"))))
                    .ToArray());
            }

            return (activeLinks.Count, sessions.Count);
        }

        private static TblFileStorage CreateSubmittedArtifactMetadata(
            StoredPrivateFile stored,
            int versionId,
            int employeeId,
            string fileType)
        {
            return new TblFileStorage
            {
                ObjectType = SubmittedContractArtifactObjectType,
                ObjectId = versionId,
                FileName = stored.OriginalFileName,
                // Private artifact không có public/legacy URL.
                FilePath = string.Empty,
                StorageKey = stored.StorageKey,
                ContentType = stored.ContentType,
                Sha256 = stored.Sha256,
                TenantCode = stored.TenantCode,
                FileType = fileType,
                FileSize = stored.FileSize,
                UploadedByUserId = employeeId,
                UploadedDate = stored.CreatedAt
            };
        }

        private static void EnsureStoredArtifactHash(
            StoredPrivateFile stored,
            string expectedHash,
            string artifactName)
        {
            if (!string.Equals(
                    stored.Sha256,
                    expectedHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Hash {artifactName} trong private storage không khớp nội dung đã render.");
            }
        }

        private static string CalculateArtifactHash(byte[] content) =>
            Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        private static async Task DeleteStoredArtifactsAsync(
            IPrivateFileStorage privateFileStorage,
            IReadOnlyCollection<StoredPrivateFile> storedArtifacts)
        {
            List<Exception>? failures = null;
            foreach (var artifact in storedArtifacts)
            {
                try
                {
                    await privateFileStorage.DeleteAsync(
                        artifact.TenantCode,
                        artifact.StorageKey);
                }
                catch (Exception exception)
                {
                    failures ??= [];
                    failures.Add(exception);
                }
            }

            if (failures is { Count: > 0 })
            {
                throw new AggregateException(
                    "Không thể dọn hết private artifact.",
                    failures);
            }
        }

        private static string CalculateSnapshotHash(
            string snapshotJson)
        {
            var contentBytes =
                Encoding.UTF8.GetBytes(snapshotJson);

            var hashBytes =
                SHA256.HashData(contentBytes);

            /*
             * SHA-256 luôn tạo 64 ký tự hexadecimal.
             */
            return Convert
                .ToHexString(hashBytes)
                .ToLowerInvariant();
        }
    }
}
