using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Models.Contract
{
    /// <summary>
    /// Một nguyên nhân khiến hợp đồng chưa thể chuyển sang Completed.
    /// </summary>
    /// <param name="Code">
    /// Mã nguyên nhân để API hoặc frontend xử lý.
    /// </param>
    /// <param name="Reference">
    /// Thông tin bổ sung liên quan đến nguyên nhân.
    ///
    /// Ví dụ:
    /// Code = RequiredDocumentMissing
    /// Reference = "Biên bản thanh lý"
    /// </param>
    public sealed record ContractCompletionBlocker(
        ContractCompletionBlockerCode Code,
        string? Reference = null);

    /// <summary>
    /// Kết quả đánh giá điều kiện hoàn thành hợp đồng.
    ///
    /// Không chỉ trả về true/false vì người dùng cần biết
    /// chính xác còn thiếu điều kiện nào.
    /// </summary>
    public sealed class ContractCompletionEvaluation
    {
        public ContractCompletionEvaluation(
            IEnumerable<ContractCompletionBlocker> blockers)
        {
            ArgumentNullException.ThrowIfNull(blockers);

            // Sao chép dữ liệu để bên ngoài không thể sửa danh sách
            // sau khi kết quả đã được tạo.
            Blockers = Array.AsReadOnly(blockers.ToArray());
        }

        /// <summary>
        /// Hợp đồng chỉ được hoàn thành khi không còn blocker nào.
        /// </summary>
        public bool CanComplete => Blockers.Count == 0;

        /// <summary>
        /// Danh sách các điều kiện chưa đạt.
        /// </summary>
        public IReadOnlyList<ContractCompletionBlocker> Blockers { get; }
    }
}