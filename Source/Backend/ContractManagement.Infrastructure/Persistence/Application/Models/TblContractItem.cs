namespace ContractManagement.Infrastructure.Persistence.Application.Models;

/// <summary>
/// Product/Service snapshot thuộc một phiên bản hợp đồng.
///
/// Khi Product hoặc Service trong catalog thay đổi tên, giá...
/// dữ liệu của hợp đồng cũ vẫn giữ nguyên trong bảng này.
/// </summary>
public partial class TblContractItem
{
    /// <summary>
    /// Khóa chính của dòng sản phẩm/dịch vụ.
    /// </summary>
    public int ContractItemId { get; set; }

    /// <summary>
    /// Hợp đồng sở hữu item.
    ///
    /// ContractId được lưu thêm để hỗ trợ truy vấn
    /// và kiểm tra quyền xem dữ liệu theo người phụ trách.
    /// </summary>
    public int ContractId { get; set; }

    /// <summary>
    /// Phiên bản hợp đồng chứa item.
    ///
    /// Mỗi ContractVersion phải có các item snapshot riêng.
    /// </summary>
    public int VersionId { get; set; }

    /// <summary>
    /// Loại item:
    /// 1 = Product
    /// 2 = Service
    ///
    /// Entity Infrastructure dùng byte để không phụ thuộc enum bên API.
    /// </summary>
    public byte ItemType { get; set; }

    /// <summary>
    /// Product nguồn trong catalog.
    ///
    /// Có thể null nếu người dùng nhập Product ngoài catalog.
    /// </summary>
    public int? SourceProductId { get; set; }

    /// <summary>
    /// Service nguồn trong catalog.
    ///
    /// Có thể null nếu người dùng nhập Service ngoài catalog.
    /// </summary>
    public int? SourceServiceId { get; set; }

    /// <summary>
    /// Mã Product/Service tại thời điểm tạo snapshot.
    /// </summary>
    public string? ItemCode { get; set; }

    /// <summary>
    /// Tên tiếng Việt được đóng băng trong hợp đồng.
    ///
    /// Không lấy lại tên từ catalog khi đọc hợp đồng.
    /// </summary>
    public string ItemName { get; set; } = null!;

    /// <summary>
    /// Tên tiếng Anh, dùng cho hợp đồng song ngữ.
    /// </summary>
    public string? ItemNameEn { get; set; }

    /// <summary>
    /// Mô tả tiếng Việt của Product/Service trong hợp đồng.
    /// </summary>
    public string? ItemDescription { get; set; }

    /// <summary>
    /// Mô tả tiếng Anh, dùng cho hợp đồng song ngữ.
    /// </summary>
    public string? ItemDescriptionEn { get; set; }

    /// <summary>
    /// Đơn vị tính, ví dụ: Gói, License, Tháng.
    /// </summary>
    public string? UnitName { get; set; }

    /// <summary>
    /// Đơn vị tính tiếng Anh.
    /// </summary>
    public string? UnitNameEn { get; set; }

    /// <summary>
    /// Số lượng. Dùng decimal để hỗ trợ số lượng lẻ.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Đơn giá snapshot.
    ///
    /// Đơn vị tiền tệ lấy từ TblContract.CurrencyCode.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thành tiền trước giảm giá và VAT:
    /// Quantity * UnitPrice.
    /// </summary>
    public decimal LineSubtotal { get; set; }

    /// <summary>
    /// Phần trăm chiết khấu, từ 0 đến 100.
    /// </summary>
    public decimal DiscountPercent { get; set; }

    /// <summary>
    /// Số tiền được chiết khấu.
    /// </summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>
    /// Phần trăm VAT, từ 0 đến 100.
    /// </summary>
    public decimal VatPercent { get; set; }

    /// <summary>
    /// Số tiền VAT sau khi đã trừ chiết khấu.
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Tổng cuối cùng của item:
    /// LineSubtotal - DiscountAmount + VatAmount.
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong hợp đồng.
    /// </summary>
    public int DisplayOrder { get; set; }

    public int CreatedEmployeeId { get; set; }

    public DateTime CreatedDate { get; set; }

    public int? UpdatedEmployeeId { get; set; }

    public DateTime? UpdatedDate { get; set; }

    /// <summary>
    /// Chống hai request cùng ghi đè item
    /// khi ContractVersion vẫn chưa khóa.
    /// </summary>
    public byte[] RowVersion { get; set; } = null!;
}