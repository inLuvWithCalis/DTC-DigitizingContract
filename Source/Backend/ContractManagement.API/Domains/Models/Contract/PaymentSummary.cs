using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Models.Contract
{
    /// <summary>
    /// Kết quả tính toán tiến độ thanh toán của hợp đồng
    /// hoặc một đợt thanh toán.
    ///
    /// Đây là DOMAIN MODEL, không phải database entity
    /// và cũng không phải HTTP request/response DTO.
    /// </summary>
    public sealed record PaymentSummary
    {
        /// <summary>
        /// Tổng số tiền khách hàng phải thanh toán.
        /// </summary>
        public decimal RequiredAmount { get; init; }

        /// <summary>
        /// Tổng các khoản thanh toán đã được xác nhận.
        /// </summary>
        public decimal ConfirmedAmount { get; init; }

        /// <summary>
        /// Số tiền khách hàng còn phải thanh toán.
        /// Không bao giờ nhỏ hơn 0.
        /// </summary>
        public decimal OutstandingAmount { get; init; }

        /// <summary>
        /// Số tiền khách hàng thanh toán vượt quá yêu cầu.
        /// Không bao giờ nhỏ hơn 0.
        /// </summary>
        public decimal OverpaidAmount { get; init; }

        /// <summary>
        /// Tiến độ thanh toán được hệ thống tính toán.
        /// </summary>
        public PaymentProgressStatus ProgressStatus { get; init; }
    }
}