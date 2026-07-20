using ContractManagement.API.Common.Enums;
using ContractManagement.API.Domains.Models.Contract;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quản lý vòng đời từng khoản thanh toán
    /// và tính tiến độ thanh toán của hợp đồng.
    ///
    /// Policy này không truy cập database.
    /// Service sau này sẽ lấy payment records từ database
    /// rồi truyền vào policy để tính toán.
    /// </summary>
    public static class PaymentPolicy
    {
        /// <summary>
        /// Vòng đời của từng khoản thanh toán.
        /// </summary>
        private static readonly IReadOnlyDictionary<
            PaymentRecordStatus,
            HashSet<PaymentRecordStatus>> AllowedTransitions =
            new Dictionary<
                PaymentRecordStatus,
                HashSet<PaymentRecordStatus>>
            {
                [PaymentRecordStatus.Pending] = new()
                {
                    /*
                     * Kế toán xác nhận khoản tiền là hợp lệ.
                     * Từ thời điểm này khoản tiền được cộng
                     * vào tổng tiền đã nhận.
                     */
                    PaymentRecordStatus.Confirmed,

                    /*
                     * Bản ghi chưa xác nhận nhưng phát hiện nhập sai.
                     * Không xóa vật lý, chỉ chuyển thành Voided.
                     */
                    PaymentRecordStatus.Voided
                },

                [PaymentRecordStatus.Confirmed] = new()
                {
                    /*
                     * Khoản tiền đã xác nhận nhưng sau đó phát hiện sai.
                     * Chuyển thành Voided và bắt buộc lưu lý do/audit.
                     *
                     * Không được hard delete vì sẽ mất lịch sử tài chính.
                     */
                    PaymentRecordStatus.Voided
                },

                // Khoản tiền đã Voided không được kích hoạt lại.
                [PaymentRecordStatus.Voided] = new()
            };

        /// <summary>
        /// Kiểm tra một khoản thanh toán có được chuyển trạng thái hay không.
        /// </summary>
        public static bool CanTransition(
            PaymentRecordStatus currentStatus,
            PaymentRecordStatus targetStatus)
        {
            return AllowedTransitions.TryGetValue(
                       currentStatus,
                       out var allowedTargets)
                   && allowedTargets.Contains(targetStatus);
        }

        /// <summary>
        /// Chặn việc thay đổi trạng thái thanh toán không hợp lệ.
        /// </summary>
        public static void EnsureCanTransition(
            PaymentRecordStatus currentStatus,
            PaymentRecordStatus targetStatus)
        {
            if (CanTransition(currentStatus, targetStatus))
            {
                return;
            }

            throw new InvalidOperationException(
                $"Không thể chuyển khoản thanh toán từ " +
                $"{currentStatus} sang {targetStatus}.");
        }

        /// <summary>
        /// Kiểm tra số tiền trước khi tạo payment record.
        /// </summary>
        public static void EnsureValidAmount(decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Số tiền thanh toán phải lớn hơn 0.");
            }
        }

        /// <summary>
        /// Tính tiến độ thanh toán.
        ///
        /// payments là danh sách gồm:
        /// - Amount: số tiền của payment record;
        /// - Status: trạng thái của payment record.
        ///
        /// Chỉ record có Status = Confirmed mới được cộng tiền.
        /// </summary>
        public static PaymentSummary CalculateSummary(
            decimal requiredAmount,
            IEnumerable<(
                decimal Amount,
                PaymentRecordStatus Status)> payments)
        {
            if (requiredAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredAmount),
                    "Số tiền phải thanh toán phải lớn hơn 0.");
            }

            ArgumentNullException.ThrowIfNull(payments);

            /*
             * Chuyển thành List để:
             * - chỉ duyệt dữ liệu một lần;
             * - tránh query nhiều lần nếu đầu vào là IEnumerable động.
             */
            var paymentList = payments.ToList();

            foreach (var payment in paymentList)
            {
                EnsureValidAmount(payment.Amount);

                if (!Enum.IsDefined(
                        typeof(PaymentRecordStatus),
                        payment.Status))
                {
                    throw new ArgumentException(
                        $"PaymentRecordStatus " +
                        $"{payment.Status} không hợp lệ.");
                }
            }

            /*
             * Pending và Voided không được tính.
             * Chỉ Confirmed mới được cộng vào dòng tiền thực nhận.
             */
            var confirmedAmount = paymentList
                .Where(x =>
                    x.Status == PaymentRecordStatus.Confirmed)
                .Sum(x => x.Amount);

            var progressStatus = CalculateProgressStatus(
                requiredAmount,
                confirmedAmount);

            /*
             * Nếu khách còn thiếu tiền:
             * RequiredAmount - ConfirmedAmount.
             *
             * Nếu đã trả đủ hoặc thừa thì OutstandingAmount = 0.
             */
            var outstandingAmount = Math.Max(
                0m,
                requiredAmount - confirmedAmount);

            /*
             * Nếu khách trả vượt:
             * ConfirmedAmount - RequiredAmount.
             *
             * Nếu chưa vượt thì OverpaidAmount = 0.
             */
            var overpaidAmount = Math.Max(
                0m,
                confirmedAmount - requiredAmount);

            return new PaymentSummary
            {
                RequiredAmount = requiredAmount,
                ConfirmedAmount = confirmedAmount,
                OutstandingAmount = outstandingAmount,
                OverpaidAmount = overpaidAmount,
                ProgressStatus = progressStatus
            };
        }

        /// <summary>
        /// Xác định trạng thái thanh toán từ số tiền yêu cầu
        /// và số tiền đã được xác nhận.
        /// </summary>
        public static PaymentProgressStatus CalculateProgressStatus(
            decimal requiredAmount,
            decimal confirmedAmount)
        {
            if (requiredAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredAmount),
                    "Số tiền phải thanh toán phải lớn hơn 0.");
            }

            if (confirmedAmount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(confirmedAmount),
                    "Tổng tiền đã xác nhận không được nhỏ hơn 0.");
            }

            if (confirmedAmount == 0)
            {
                return PaymentProgressStatus.Pending;
            }

            if (confirmedAmount < requiredAmount)
            {
                return PaymentProgressStatus.PartiallyPaid;
            }

            return PaymentProgressStatus.FullyPaid;
        }

        /// <summary>
        /// Kiểm tra hợp đồng hoặc đợt thanh toán đã đủ tiền hay chưa.
        /// </summary>
        public static bool IsFullyPaid(PaymentSummary summary)
        {
            ArgumentNullException.ThrowIfNull(summary);

            return summary.ProgressStatus
                == PaymentProgressStatus.FullyPaid;
        }

        /// <summary>
        /// Kiểm tra một đợt thanh toán đã quá hạn hay chưa.
        ///
        /// Overdue được tính động từ DueDate,
        /// không cần lưu như trạng thái thanh toán chính.
        /// </summary>
        public static bool IsOverdue(
            DateTime dueDateUtc,
            PaymentProgressStatus progressStatus,
            DateTime utcNow)
        {
            if (progressStatus == PaymentProgressStatus.FullyPaid)
            {
                return false;
            }

            return dueDateUtc < utcNow;
        }

        /// <summary>
        /// Kiểm tra trạng thái payment record đã kết thúc hay chưa.
        ///
        /// Confirmed chưa được coi là terminal vì kế toán
        /// vẫn có thể Void nếu phát hiện nhập sai.
        /// </summary>
        public static bool IsTerminal(PaymentRecordStatus status)
        {
            return status == PaymentRecordStatus.Voided;
        }
    }
}