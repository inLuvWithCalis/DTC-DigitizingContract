namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Khai báo một placeholder trong DOCX của template version.
///
/// Ví dụ:
/// PlaceholderKey = CONTRACT_CODE
/// Token trong Word = {{CONTRACT_CODE}}
/// DataSource = Contract.ContractCode
/// </summary>
public partial class TblContractTemplateField
{
    public int TemplateFieldId { get; set; }

    /// <summary>
    /// Template version chứa placeholder này.
    /// Đây là logical reference, không tạo foreign key vật lý.
    /// </summary>
    public int TemplateVersionId { get; set; }

    /// <summary>
    /// Khóa placeholder, không chứa {{ }}.
    /// Ví dụ: CUSTOMER_NAME, CONTRACT_CODE.
    /// </summary>
    public string PlaceholderKey { get; set; } = null!;

    /// <summary>
    /// Tên hiển thị trên giao diện.
    /// Ví dụ: Tên khách hàng.
    /// </summary>
    public string FieldLabel { get; set; } = null!;

    /// <summary>
    /// Đường dẫn logic tới nguồn dữ liệu.
    /// Ví dụ: Customer.CustomerFullName.
    ///
    /// Giá trị Manual.PaymentNote thể hiện người dùng phải nhập tay.
    /// </summary>
    public string DataSource { get; set; } = null!;

    /// <summary>
    /// Giá trị mặc định khi nguồn dữ liệu không có giá trị.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Quy tắc định dạng.
    /// Ví dụ: dd/MM/yyyy hoặc N0.
    /// </summary>
    public string? FormatString { get; set; }

    /// <summary>
    /// Nếu true, validation không cho publish khi field không có dữ liệu.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Thứ tự hiển thị khi nhập hoặc cấu hình field.
    /// </summary>
    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Chống hai người cùng ghi đè dữ liệu Draft.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}