import {
  ContractStatus,
  contractApi,
  type ContractDetailResponse,
  type CreateContractNegotiationRoundResponse,
} from "@/services/contract-api";

export interface CreateNegotiationRoundAfterApprovalInput {
  contractId: number;
  sourceVersionId: number;
  changeNote: string;
}

export interface CreateNegotiationRoundAfterApprovalResult {
  created: boolean;
  versionId: number;
  versionNo: number;
}

const isCreatedFromSource = (
  detail: ContractDetailResponse,
  sourceVersionId: number,
) =>
  detail.currentVersion.versionId !== sourceVersionId &&
  detail.currentVersion.sourceVersionId === sourceVersionId;

const mapCreatedRound = (
  result: CreateContractNegotiationRoundResponse,
): CreateNegotiationRoundAfterApprovalResult => ({
  created: true,
  versionId: result.currentVersion.versionId,
  versionNo: result.currentVersion.versionNo,
});

const mapExistingRound = (
  detail: ContractDetailResponse,
): CreateNegotiationRoundAfterApprovalResult => ({
  created: false,
  versionId: detail.currentVersion.versionId,
  versionNo: detail.currentVersion.versionNo,
});

export async function createNegotiationRoundAfterApproval({
  contractId,
  sourceVersionId,
  changeNote,
}: CreateNegotiationRoundAfterApprovalInput): Promise<CreateNegotiationRoundAfterApprovalResult> {
  const normalizedChangeNote = changeNote.trim();
  if (!normalizedChangeNote) {
    throw new Error("Lý do tạo phiên bản chỉnh sửa không được để trống.");
  }

  const detail = await contractApi.getDetail(contractId);
  if (isCreatedFromSource(detail, sourceVersionId)) {
    return mapExistingRound(detail);
  }

  if (detail.currentVersion.versionId !== sourceVersionId) {
    throw new Error(
      "Version hiện hành không còn là version vừa được rút hoặc trả sửa.",
    );
  }

  if (
    detail.status !== ContractStatus.Negotiating &&
    detail.status !== ContractStatus.Rejected
  ) {
    throw new Error(
      "Hợp đồng không còn ở trạng thái cho phép tạo phiên bản chỉnh sửa.",
    );
  }

  try {
    const createdRound = await contractApi.createNegotiationRound(contractId, {
      currentVersionId: detail.currentVersion.versionId,
      rowVersion: detail.rowVersion,
      currentVersionRowVersion: detail.currentVersion.rowVersion,
      changeNote: normalizedChangeNote,
    });
    return mapCreatedRound(createdRound);
  } catch (error) {
    try {
      const latestDetail = await contractApi.getDetail(contractId);
      if (isCreatedFromSource(latestDetail, sourceVersionId)) {
        return mapExistingRound(latestDetail);
      }
    } catch {
      // Giữ lỗi gốc của thao tác tạo round để caller báo đúng bước thất bại.
    }

    throw error;
  }
}
