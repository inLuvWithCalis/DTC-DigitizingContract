using ContractManagement.API.Common.Enums;
using ContractManagement.API.Common.Responses;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.API.Domains.Policies.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Policies.ContractTemplate;
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
        private static long _syntheticCommentRowVersionSeed = 1000;

        private readonly DbDtctechContext _dbContext;
        private readonly IContractAuditWriter _contractAuditWriter;
        private readonly ICurrentTenant? _currentTenant;
        private readonly CustomerAccessCryptography? _customerAccessCryptography;

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

        /// <summary>
        /// Lấy danh sách hợp đồng mà nhân viên đăng nhập đang phụ trách.
        /// API danh sách chỉ trả dữ liệu tóm tắt để Frontend tải nhanh.
        /// </summary>
        public async Task<PagedResult<ContractListItemResponse>> GetListAsync(
            ContractFilterRequest filter,
            int employeeId)
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

                // Hiện chưa có RBAC nên nhân viên chỉ xem hợp đồng của mình.
                where contract.EmployeeId == employeeId

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
                int employeeId)
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
                    x.EmployeeId == employeeId
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
                    var contractTerms = templateTerms
                        .Select(templateTerm => new TblContractTerm
                        {
                            ContractId = contract.ContractId,
                            VersionId = contractVersion.VersionId,

                            SourceTemplateTermId =
                                templateTerm.TemplateTermId,

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
                                responsibleEmployeeId),

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
                            Reason: null)
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
                                Reason: reason)
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
                            occurredAt)
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


        /// <summary>
        /// Lấy chi tiết hợp đồng tại version hiện hành.
        /// </summary>
        public async Task<ContractDetailResponse> GetDetailAsync(
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
                      && contracts.EmployeeId == employeeId

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
                }
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

                    /*
                     * Đánh dấu version đã tham gia lần cập nhật này.
                     * SQL Server sẽ sinh RowVersion mới cho version.
                     */
                    versionEntry
                        .Property(x => x.ChangeNote)
                        .IsModified = true;

                    await ValidateCustomerAsync(request.CustomerId);

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
                        }

                        ApplyItemSnapshot(
                            item,
                            requestItem,
                            amounts,
                            requestItem.DisplayOrder > 0
                                ? requestItem.DisplayOrder
                                : index + 1);
                    }

                    /*
                     * Item cũ không còn trong request sẽ bị xóa.
                     */
                    var removedItems = existingItems
                        .Where(x =>
                            !requestedItemIds.Contains(x.ContractItemId))
                        .ToList();

                    _dbContext.TblContractItems.RemoveRange(removedItems);

                    /*
                     * Thêm mới hoặc cập nhật Terms.
                     */
                    for (var index = 0;
                         index < request.Terms.Count;
                         index++)
                    {
                        var requestTerm = request.Terms[index];

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
                        }

                        term.TermTitle = requestTerm.TermTitle.Trim();
                        term.TermTitleEn =
                            NormalizeOptional(requestTerm.TermTitleEn);

                        term.TermContent =
                            NormalizeOptional(requestTerm.TermContent);

                        term.TermContentEn =
                            NormalizeOptional(requestTerm.TermContentEn);

                        term.IsNegotiable = requestTerm.IsNegotiable;

                        term.DisplayOrder =
                            requestTerm.DisplayOrder > 0
                                ? requestTerm.DisplayOrder
                                : index + 1;
                    }

                    /*
                     * Term cũ không còn trong request sẽ bị xóa.
                     */
                    var removedTerms = existingTerms
                        .Where(x => !requestedTermIds.Contains(x.TermId))
                        .ToList();

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
                                now)
                        ]);
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException exception)
            {
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

                    var snapshotJson = BuildSnapshotJson(
                        contract,
                        sourceVersion,
                        customer,
                        sourceItems,
                        sourceTerms);

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
                                now)
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
                            Reason: changeNote)
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
                                now)
                        ]);

                        SetSyntheticCommentRowVersionIfNeeded(comment);
                        await _dbContext.SaveChangesAsync();

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
                            DateTime.UtcNow);

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
                int employeeId)
        {
            ValidateContractEmployee(contractId, employeeId);

            var contract = await _dbContext.TblContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ContractId == contractId
                    && x.EmployeeId == employeeId);

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
                int employeeId)
        {
            ValidateContractEmployee(contractId, employeeId);

            var contract = await _dbContext.TblContracts
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ContractId == contractId
                    && x.EmployeeId == employeeId);

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

                    if (comment.State == (byte)targetState)
                    {
                        throw new DbUpdateConcurrencyException(
                            "Trạng thái comment đã được xử lý bởi request khác.");
                    }

                    _dbContext.Entry(comment)
                        .Property(x => x.RowVersion)
                        .OriginalValue = expectedRowVersion;

                    var now = DateTime.UtcNow;
                    var previousState = comment.State;
                    comment.State = (byte)targetState;
                    comment.UpdatedDate = now;

                    _dbContext.TblContractNegotiationCommentEvents.Add(
                        new TblContractNegotiationCommentEvent
                        {
                            CommentId = comment.CommentId,
                            EventType = (byte)eventType,
                            ActorType = ContractAuditActorTypes.Employee,
                            EmployeeId = employeeId,
                            OccurredAt = now
                        });

                    _contractAuditWriter.StageEmployeeAudits(
                    [
                        new EmployeeContractAuditWriteRequest(
                            contract.ContractId,
                            comment.VersionId,
                            employeeId,
                            auditActionType,
                            ContractAuditResults.Succeeded,
                            now,
                            PreviousContractStatus: contract.Status,
                            NewContractStatus: contract.Status)
                    ]);

                    RotateCommentRowVersionForInMemory(
                        comment,
                        expectedRowVersion);
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _ = previousState;
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
                        DateTime.UtcNow);

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
            DateTime occurredAt)
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
                            ContractAuditActionTypes.ConcurrencyConflict,
                            ContractAuditResults.ConcurrencyConflict,
                            occurredAt)
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

            return comments
                .Select(comment => MapCommentResponse(
                    comment,
                    events.Where(x =>
                        x.CommentId == comment.CommentId)))
                .ToList();
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

            return MapCommentResponse(comment, events);
        }

        private static ContractNegotiationCommentResponse
            MapCommentResponse(
                TblContractNegotiationComment comment,
                IEnumerable<TblContractNegotiationCommentEvent> events)
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

            var strategy =
                _dbContext.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
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

                    /*
                     * Nếu frontend gửi WorkflowId thì kiểm tra workflow.
                     */
                    if (request.WorkflowId.HasValue)
                    {
                        var workflowExists = await _dbContext
                            .TblApprovalWorkflows
                            .AsNoTracking()
                            .AnyAsync(x =>
                                x.WorkflowId == request.WorkflowId.Value
                                && x.ObjectType == "Contract"
                                && x.StepNo == 1
                                && x.IsActive);

                        if (!workflowExists)
                        {
                            throw new KeyNotFoundException(
                                "Không tìm thấy workflow duyệt hợp lệ.");
                        }
                    }

                    var customer = await _dbContext.TblCustomers
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x =>
                            x.CustomerId == contract.CustomerId);

                    if (customer == null)
                    {
                        throw new KeyNotFoundException(
                            "Không tìm thấy khách hàng của hợp đồng.");
                    }

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

                    ValidateApprovalReadiness(
                        contract,
                        items,
                        terms);

                    /*
                     * Tạo bản đóng băng nội dung pháp lý.
                     */
                    var snapshotJson = BuildSnapshotJson(
                        contract,
                        version,
                        customer,
                        items,
                        terms);

                    var snapshotHash =
                        CalculateSnapshotHash(snapshotJson);

                    var now = DateTime.UtcNow;

                    version.SnapshotJson = snapshotJson;
                    version.SnapshotHash = snapshotHash;
                    version.IsLocked = true;
                    version.LockedDate = now;
                    version.LockedByEmployeeId = employeeId;

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

                    /*
                     * SaveChanges tự tạo transaction:
                     * hoặc lưu tất cả, hoặc không lưu gì.
                     */
                    await _dbContext.SaveChangesAsync();

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

                        ContractRowVersion =
                            EncodeRowVersion(contract.RowVersion),

                        VersionRowVersion =
                            EncodeRowVersion(version.RowVersion)
                    };
                }
                catch (DbUpdateConcurrencyException exception)
                {
                    throw new DbUpdateConcurrencyException(
                        "Hợp đồng đã được cập nhật. " +
                        "Vui lòng tải lại dữ liệu trước khi gửi duyệt.",
                        exception);
                }
            });
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

        private async Task ValidateCustomerAsync(int customerId)
        {
            var customer = await _dbContext.TblCustomers
                .AsNoTracking()
                .Where(x => x.CustomerId == customerId)
                .Select(x => new
                {
                    x.CustomerId,
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

        private static void ValidateApprovalReadiness(
    TblContract contract,
    List<TblContractItem> items,
    List<TblContractTerm> terms)
        {
            if (string.IsNullOrWhiteSpace(contract.ContractCode))
            {
                throw new InvalidOperationException(
                    "Hợp đồng chưa có mã.");
            }

            if (string.IsNullOrWhiteSpace(contract.ContractName))
            {
                throw new InvalidOperationException(
                    "Hợp đồng chưa có tên.");
            }

            if (items.Count == 0)
            {
                throw new InvalidOperationException(
                    "Hợp đồng phải có ít nhất một item.");
            }

            if (terms.Count == 0)
            {
                throw new InvalidOperationException(
                    "Hợp đồng phải có ít nhất một điều khoản.");
            }

            if (contract.EffectiveDate.HasValue
                && contract.ExpireDate.HasValue
                && contract.ExpireDate < contract.EffectiveDate)
            {
                throw new InvalidOperationException(
                    "Ngày hết hạn không được trước ngày hiệu lực.");
            }

            var calculatedTotal =
                items.Sum(x => x.LineTotal);

            if (calculatedTotal != contract.TotalAmount)
            {
                throw new InvalidOperationException(
                    "Tổng tiền hợp đồng không khớp với các item.");
            }

            if (contract.LanguageMode !=
                (byte)ContractLanguageMode.Bilingual)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(
                    contract.ContractNameEn))
            {
                throw new InvalidOperationException(
                    "Hợp đồng song ngữ thiếu tên tiếng Anh.");
            }

            if (items.Any(x =>
                    string.IsNullOrWhiteSpace(x.ItemNameEn)))
            {
                throw new InvalidOperationException(
                    "Hợp đồng song ngữ có item thiếu tên tiếng Anh.");
            }

            if (terms.Any(x =>
                    string.IsNullOrWhiteSpace(x.TermTitleEn)))
            {
                throw new InvalidOperationException(
                    "Hợp đồng song ngữ có điều khoản thiếu tiêu đề tiếng Anh.");
            }
        }

        private static string BuildSnapshotJson(
            TblContract contract,
            TblContractVersion version,
            TblCustomer customer,
            List<TblContractItem> items,
            List<TblContractTerm> terms)
        {
            /*
             * Không đưa RowVersion hoặc UpdatedDate vào snapshot
             * vì đây không phải nội dung pháp lý.
             */
            var snapshot = new
            {
                schemaVersion = 1,

                contract = new
                {
                    contract.ContractId,
                    contract.ContractCode,
                    contract.ContractName,
                    contract.ContractNameEn,
                    contract.ContractType,
                    contract.TemplateVersionId,
                    contract.ParentContractId,
                    contract.EffectiveDate,
                    contract.ExpireDate,
                    contract.Subtotal,
                    contract.TotalDiscount,
                    contract.TotalVat,
                    contract.TotalAmount,
                    contract.CurrencyCode,
                    contract.LanguageMode
                },

                customer = new
                {
                    customer.CustomerId,
                    customer.CustomerCode,
                    customer.CustomerFullName,
                    customer.CustomerCompany,
                    customer.CustomerTaxCode,
                    customer.CustomerEmail,
                    customer.CustomerMobile,
                    customer.CustomerAddress
                },

                version = new
                {
                    version.VersionId,
                    version.VersionNo,
                    version.SourceVersionId,
                    version.TemplateVersionId,
                    version.ChangeNote,
                    version.CurrencyCode,
                    version.Subtotal,
                    version.TotalDiscount,
                    version.TotalVat,
                    version.TotalAmount
                },

                items = items.Select(x => new
                {
                    x.ContractItemId,
                    x.ItemType,
                    x.SourceProductId,
                    x.SourceServiceId,
                    x.ItemCode,
                    x.ItemName,
                    x.ItemNameEn,
                    x.ItemDescription,
                    x.ItemDescriptionEn,
                    x.UnitName,
                    x.UnitNameEn,
                    x.Quantity,
                    x.UnitPrice,
                    x.LineSubtotal,
                    x.DiscountMode,
                    x.DiscountPercent,
                    x.FixedDiscountAmount,
                    x.DiscountAmount,
                    x.IsTaxable,
                    x.VatPercent,
                    x.VatAmount,
                    x.LineTotal,
                    x.DisplayOrder
                }),

                terms = terms.Select(x => new
                {
                    x.TermId,
                    x.SourceTemplateTermId,
                    x.TermCode,
                    x.TermTitle,
                    x.TermTitleEn,
                    x.TermContent,
                    x.TermContentEn,
                    x.IsNegotiable,
                    x.DisplayOrder
                })
            };

            return JsonSerializer.Serialize(
                snapshot,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        JsonNamingPolicy.CamelCase,

                    WriteIndented = false
                });
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
