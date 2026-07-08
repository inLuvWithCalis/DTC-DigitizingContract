namespace ContractManagement.API.Common.Responses
{
    /// <summary>
    /// Response wrapper dùng chung cho API.
    /// Giúp frontend luôn nhận response theo một format thống nhất.
    /// </summary>
    public class ApiResponse<T>
    {
        /// <summary>
        /// API xử lý thành công hay thất bại.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message ngắn gọn trả về cho frontend.
        /// Ví dụ: "Tạo hợp đồng thành công", "Không tìm thấy dữ liệu".
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Dữ liệu chính trả về.
        /// </summary>
        public T? Data { get; set; }

        /// <summary>
        /// Danh sách lỗi chi tiết nếu có.
        /// Ví dụ: lỗi validate input.
        /// </summary>
        public List<string>? Errors { get; set; }

        /// <summary>
        /// Tạo response thành công.
        /// </summary>
        public static ApiResponse<T> Ok(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Message = message,
                Data = data
            };
        }

        /// <summary>
        /// Tạo response thất bại.
        /// </summary>
        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors
            };
        }
    }
}