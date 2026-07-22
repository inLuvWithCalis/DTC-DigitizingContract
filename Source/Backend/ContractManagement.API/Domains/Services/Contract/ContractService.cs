using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.DTOs.Requests.Contract;
using ContractManagement.API.Domains.DTOs.Responses.Contract;
using ContractManagement.Common.Enums;
using ContractManagement.Domains.Interfaces.Contract;
using ContractManagement.Domains.Policies.ContractTemplate;
using ContractManagement.Infrastructure.Persistence.Application;
using ContractManagement.Infrastructure.Persistence.Application.Models;
using Microsoft.EntityFrameworkCore;

namespace ContractManagement.Domains.Services.Contract
{
    /// <summary>
    /// Service xử lý nghiệp vụ chính của hợp đồng.
    /// </summary>
    public class ContractService : IContractService
    {
        private const decimal MaxMoney = 9999999999999999.99m;

        private readonly DbDtctechContext _dbContext;

        public ContractService(DbDtctechContext dbContext)
        {
            _dbContext = dbContext;
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
                    await ValidateCustomerAsync(request.CustomerId);
                    await ValidateTemplateAsync(request);
                    await ValidateParentContractAsync(request);
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

                    /*
                     * Bước 1: Tạo Contract trước để SQL Server sinh ContractId.
                     * CurrentVersionId tạm thời để null vì Version 1 chưa tồn tại.
                     */
                    var contract = new TblContract
                    {
                        CustomerId = request.CustomerId,

                        // Người tạo mặc định là người phụ trách.
                        EmployeeId = createdEmployeeId,
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

                        CurrencyCode = request.CurrencyCode
                            .Trim()
                            .ToUpperInvariant(),

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
                    decimal totalAmount = 0m;

                    for (var index = 0;
                         index < request.Items.Count;
                         index++)
                    {
                        var requestItem = request.Items[index];

                        var amounts = CalculateItemAmounts(requestItem);

                        if (totalAmount > MaxMoney - amounts.LineTotal)
                        {
                            throw new InvalidOperationException(
                                "Tổng giá trị hợp đồng vượt quá giới hạn cho phép.");
                        }

                        totalAmount += amounts.LineTotal;

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

                            DiscountPercent =
                                requestItem.DiscountPercent,

                            DiscountAmount =
                                amounts.DiscountAmount,

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

                    contract.TotalAmount = totalAmount;

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
                        CurrencyCode = contract.CurrencyCode,
                        LanguageMode = request.LanguageMode,

                        EmployeeId = createdEmployeeId,
                        CreatedDate = contract.CreatedDate,

                        ItemCount = contractItems.Count,
                        TermCount = contractTerms.Count
                    };
                }
                catch
                {
                    // Không để lại dữ liệu dở dang nếu bất kỳ bước nào thất bại.
                    await transaction.RollbackAsync();
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

                            DiscountPercent =
                                item.DiscountPercent,

                            DiscountAmount = item.DiscountAmount,
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
                        .ToList()
                }
            };
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

            foreach (var item in request.Items)
            {
                if (!Enum.IsDefined(
                        typeof(ContractItemType),
                        item.ItemType))
                {
                    throw new ArgumentException(
                        "Loại ContractItem không hợp lệ.");
                }

                if (item.Quantity <= 0)
                {
                    throw new ArgumentException(
                        "Số lượng item phải lớn hơn 0.");
                }

                if (item.UnitPrice < 0)
                {
                    throw new ArgumentException(
                        "Đơn giá item không được âm.");
                }

                if (item.DiscountPercent is < 0 or > 100)
                {
                    throw new ArgumentException(
                        "Phần trăm chiết khấu phải từ 0 đến 100.");
                }

                if (item.VatPercent is < 0 or > 100)
                {
                    throw new ArgumentException(
                        "Phần trăm VAT phải từ 0 đến 100.");
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
            CreateContractRequest request)
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
                    x.ContractType
                })
                .FirstOrDefaultAsync();

            if (parentContract == null)
            {
                throw new KeyNotFoundException(
                    "Không tìm thấy hợp đồng nguồn.");
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
        }

        private async Task ValidateCatalogSourcesAsync(
            List<CreateContractItemRequest> items)
        {
            var productIds = items
                .Where(x => x.SourceProductId.HasValue)
                .Select(x => x.SourceProductId!.Value)
                .Distinct()
                .ToList();

            if (productIds.Count > 0)
            {
                var activeProductIds =
                    await _dbContext.TblProducts
                        .AsNoTracking()
                        .Where(x =>
                            productIds.Contains(x.ProductId) &&
                            x.Status == 1)
                        .Select(x => x.ProductId)
                        .ToListAsync();

                if (activeProductIds.Count != productIds.Count)
                {
                    throw new InvalidOperationException(
                        "Có Product nguồn không tồn tại hoặc đang inactive.");
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
                CreateContractItemRequest item)
        {
            // Ngăn phép nhân decimal bị overflow.
            if (item.UnitPrice > 0 &&
                item.Quantity > MaxMoney / item.UnitPrice)
            {
                throw new InvalidOperationException(
                    $"Giá trị item '{item.ItemName}' vượt giới hạn.");
            }

            var subtotal = RoundMoney(
                item.Quantity * item.UnitPrice);

            var discountAmount = RoundMoney(
                subtotal * item.DiscountPercent / 100m);

            var amountAfterDiscount =
                subtotal - discountAmount;

            var vatAmount = RoundMoney(
                amountAfterDiscount *
                item.VatPercent /
                100m);

            var lineTotal = RoundMoney(
                amountAfterDiscount + vatAmount);

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

        private static decimal RoundMoney(decimal value)
        {
            return Math.Round(
                value,
                2,
                MidpointRounding.AwayFromZero);
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
    }
}