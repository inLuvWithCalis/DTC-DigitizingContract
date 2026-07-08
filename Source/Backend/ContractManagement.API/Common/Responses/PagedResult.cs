namespace ContractManagement.API.Common.Responses
{
    /// <summary>
    /// Kết quả phân trang dùng chung cho các API dạng danh sách.
    /// Ví dụ: danh sách hợp đồng, khách hàng, nhân viên, sản phẩm.
    /// </summary>
    public class PagedResult<T>
    {
        /// <summary>
        /// Danh sách dữ liệu của trang hiện tại.
        /// </summary>
        public List<T> Items { get; set; } = new();

        /// <summary>
        /// Tổng số bản ghi trong database sau khi filter.
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Trang hiện tại.
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Số bản ghi mỗi trang.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Tổng số trang.
        /// </summary>
        public int TotalPages =>
            PageSize <= 0
                ? 0
                : (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}