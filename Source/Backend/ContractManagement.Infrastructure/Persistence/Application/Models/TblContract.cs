namespace ContractManagement.Infrastructure.Persistence.Application.Models;

public partial class TblContract
{
    public int ContractId { get; set; }

    /// <summary>
    /// Khách hàng ký hợp đồng.
    /// Contract mới bắt buộc phải xác định Customer.
    /// </summary>
    public int CustomerId { get; set; }

    /// <summary>
    /// EmployeeId là nhân viên đang phụ trách Contract.
    /// Khi tạo Contract, giá trị này mặc định là CreatedEmployeeId nhưng có thể
    /// là một employee active khác được chọn hợp lệ.
    /// CreatedEmployeeId vẫn lưu actor đã tạo Contract.
    /// </summary>
    public int EmployeeId { get; set; }

    /// <summary>
    /// Loại hợp đồng, mapping với ContractType.
    /// Entity Infrastructure lưu byte để không reference ngược sang API.
    /// </summary>
    public byte ContractType { get; set; }

    /// <summary>
    /// Template version dùng để tạo hợp đồng.
    /// Nullable để hỗ trợ hợp đồng Legacy được upload từ bên ngoài.
    /// </summary>
    public int? TemplateVersionId { get; set; }

    /// <summary>
    /// Hợp đồng cha, dùng cho hợp đồng bảo trì/duy trì phát sinh
    /// từ hợp đồng cung cấp phần mềm ban đầu.
    ///
    /// Appendix vẫn được quản lý bằng tbl_ContractAppendix,
    /// không dùng ParentContractId.
    /// </summary>
    public int? ParentContractId { get; set; }

    /// <summary>
    /// Version hiện hành của hợp đồng.
    /// Không được tự suy ra bằng MAX(VersionNo).
    /// </summary>
    public int? CurrentVersionId { get; set; }

    /// <summary>
    /// Logical pointers for the currently selected public verification context.
    /// They are protected by Contract.RowVersion and deliberately have no physical FK.
    /// </summary>
    public int? CurrentVerificationPhoneId { get; set; }

    public int? CurrentCustomerAccessLinkId { get; set; }

    public string? ContractCode { get; set; }

    public string ContractName { get; set; } = null!;

    public string? ContractNameEn { get; set; }

    public DateTime? SignDate { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public DateTime? ExpireDate { get; set; }

    /// <summary>
    /// Mapping với ContractStatus.
    /// </summary>
    public byte Status { get; set; }

    /// <summary>
    /// Không sử dụng double/float cho dữ liệu tiền.
    /// </summary>
    public decimal TotalAmount { get; set; }

    public decimal Subtotal { get; set; }

    public decimal TotalDiscount { get; set; }

    public decimal TotalVat { get; set; }

    /// <summary>
    /// Mã tiền tệ ISO 4217, ví dụ VND hoặc USD.
    /// </summary>
    public string CurrencyCode { get; set; } = "VND";

    /// <summary>
    /// Vietnamese hoặc Bilingual.
    /// </summary>
    public byte LanguageMode { get; set; }

    /// <summary>
    /// Hợp đồng cũ chỉ được upload để lưu trữ/tra cứu.
    /// </summary>
    public bool IsLegacy { get; set; }

    public int CreatedEmployeeId { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? UpdateDate { get; set; }

    /// <summary>
    /// SQL Server rowversion dùng để phát hiện hai request
    /// cùng sửa một Contract tại một thời điểm.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}
