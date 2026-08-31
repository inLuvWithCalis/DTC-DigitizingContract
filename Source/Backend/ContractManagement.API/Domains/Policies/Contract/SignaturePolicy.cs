using ContractManagement.API.Common.Enums;

namespace ContractManagement.API.Domains.Policies.Contract
{
    /// <summary>
    /// Quy tắc Phase 9: Owner tải một bản scan đã có đủ chữ ký hai bên.
    /// Không có OTP, e-sign hoặc chuỗi ký tuần tự trong MVP.
    /// </summary>
    public static class SignaturePolicy
    {
        public static void EnsureCanUploadInitialEvidence(
            ContractStatus contractStatus,
            int currentVersionId,
            int evidenceVersionId,
            bool versionLocked,
            bool approvedArtifactsExist,
            bool activeEvidenceExists)
        {
            if (contractStatus != ContractStatus.PendingSignature)
            {
                throw new InvalidOperationException(
                    "Chỉ hợp đồng Chờ ký mới được tải bản scan đã ký.");
            }

            EnsureVersionAndArtifacts(
                currentVersionId,
                evidenceVersionId,
                versionLocked,
                approvedArtifactsExist);

            if (activeEvidenceExists)
            {
                throw new InvalidOperationException(
                    "Version đã có bản scan đang hiệu lực.");
            }

            ContractLifecyclePolicy.EnsureCanTransition(
                contractStatus,
                ContractStatus.Signed);
        }

        public static void EnsureCanSupersedeEvidence(
            ContractStatus contractStatus,
            int currentVersionId,
            int evidenceVersionId,
            bool versionLocked,
            bool approvedArtifactsExist,
            bool activeEvidenceExists)
        {
            if (contractStatus != ContractStatus.Signed)
            {
                throw new InvalidOperationException(
                    "Chỉ hợp đồng Đã ký và chưa hoàn thành mới được thay bản scan.");
            }

            EnsureVersionAndArtifacts(
                currentVersionId,
                evidenceVersionId,
                versionLocked,
                approvedArtifactsExist);

            if (!activeEvidenceExists)
            {
                throw new InvalidOperationException(
                    "Không tìm thấy bản scan đang hiệu lực để thay thế.");
            }
        }

        private static void EnsureVersionAndArtifacts(
            int currentVersionId,
            int evidenceVersionId,
            bool versionLocked,
            bool approvedArtifactsExist)
        {
            if (currentVersionId <= 0 || currentVersionId != evidenceVersionId)
            {
                throw new InvalidOperationException(
                    "Bản scan phải gắn với đúng version hiện hành đã duyệt.");
            }

            if (!versionLocked || !approvedArtifactsExist)
            {
                throw new InvalidOperationException(
                    "Version chưa có đầy đủ artifact đã duyệt bất biến.");
            }
        }
    }
}
