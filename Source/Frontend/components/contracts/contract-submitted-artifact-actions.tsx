"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Download, Eye, FileText, Loader2 } from "lucide-react";

import { downloadBlob } from "@/components/contract-templates/contract-template-utils";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import { toast } from "@/components/ui/sonner";
import { getBlobApiErrorMessage } from "@/lib/api-error";
import {
  contractApprovalApi,
  type ContractApprovalArtifactResponse,
  type ContractApprovalDetailResponse,
} from "@/services/contract-approval-api";

interface ContractSubmittedArtifactActionsProps {
  contractId: number;
  versionId: number;
  enabled: boolean;
}

export function ContractSubmittedArtifactActions({
  contractId,
  versionId,
  enabled,
}: ContractSubmittedArtifactActionsProps) {
  const [detail, setDetail] = useState<ContractApprovalDetailResponse | null>(
    null,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [downloadingFileId, setDownloadingFileId] = useState<number | null>(
    null,
  );

  const loadArtifacts = useCallback(async () => {
    if (!enabled) {
      setDetail(null);
      return;
    }

    try {
      setIsLoading(true);
      const history = await contractApprovalApi.getContractHistory(contractId);
      const currentVersionRequest = history
        .filter((request) => request.versionId === versionId)
        .sort(
          (left, right) =>
            new Date(right.submittedDate).getTime() -
            new Date(left.submittedDate).getTime(),
        )[0];

      setDetail(
        currentVersionRequest
          ? await contractApprovalApi.getDetail(
              currentVersionRequest.approvalRequestId,
            )
          : null,
      );
    } catch {
      setDetail(null);
    } finally {
      setIsLoading(false);
    }
  }, [contractId, enabled, versionId]);

  useEffect(() => {
    void loadArtifacts();
  }, [loadArtifacts]);

  const pdfArtifact = useMemo(
    () =>
      detail?.artifacts.find(
        (artifact) => artifact.fileType.toLowerCase() === "pdf",
      ),
    [detail],
  );
  const docxArtifact = useMemo(
    () =>
      detail?.artifacts.find(
        (artifact) => artifact.fileType.toLowerCase() === "docx",
      ),
    [detail],
  );

  const downloadArtifact = async (
    artifact: ContractApprovalArtifactResponse,
    openPdf: boolean,
  ) => {
    const previewWindow = openPdf ? window.open("about:blank", "_blank") : null;
    if (openPdf && !previewWindow) {
      toast.error(
        "Trình duyệt đã chặn tab xem tài liệu. Vui lòng cho phép cửa sổ bật lên.",
      );
      return;
    }

    try {
      setDownloadingFileId(artifact.fileId);
      const blob = await contractApprovalApi.downloadArtifact(artifact.fileId);
      if (openPdf && previewWindow) {
        const url = URL.createObjectURL(
          blob.type === "application/pdf"
            ? blob
            : new Blob([blob], { type: "application/pdf" }),
        );
        previewWindow.opener = null;
        previewWindow.location.replace(url);
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      } else {
        previewWindow?.close();
        downloadBlob(blob, artifact.fileName);
      }
    } catch (error) {
      previewWindow?.close();
      toast.error(
        await getBlobApiErrorMessage(
          error,
          "Không thể tải artifact hợp đồng đã gửi duyệt.",
        ),
      );
    } finally {
      setDownloadingFileId(null);
    }
  };

  if (!enabled) return null;

  if (isLoading && !detail) {
    return (
      <SplitActionMenu
        primaryLabel="Đang tải bản đã gửi"
        primaryIcon={<Loader2 className="size-4 animate-spin" />}
        onPrimaryClick={() => undefined}
        disabled
        buttonClassName="h-9"
        menuItems={[]}
      />
    );
  }

  if (!pdfArtifact && !docxArtifact) return null;

  const primaryArtifact = pdfArtifact ?? docxArtifact!;
  const primaryIsPdf = primaryArtifact.fileType.toLowerCase() === "pdf";

  return (
    <SplitActionMenu
      primaryLabel={primaryIsPdf ? "Xem PDF đã gửi" : "Tải bản đã gửi"}
      primaryIcon={
        downloadingFileId === primaryArtifact.fileId ? (
          <Loader2 className="size-4 animate-spin" />
        ) : primaryIsPdf ? (
          <Eye className="size-4" />
        ) : (
          <Download className="size-4" />
        )
      }
      onPrimaryClick={() =>
        void downloadArtifact(primaryArtifact, primaryIsPdf)
      }
      isLoading={downloadingFileId === primaryArtifact.fileId}
      buttonClassName="h-9"
      menuItems={[
        ...(docxArtifact
          ? [
              {
                label: "Tải DOCX đã gửi",
                icon:
                  downloadingFileId === docxArtifact.fileId ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <FileText className="size-4" />
                  ),
                onClick: () => void downloadArtifact(docxArtifact, false),
                disabled: downloadingFileId !== null,
              },
            ]
          : []),
        ...(pdfArtifact
          ? [
              {
                label: "Tải PDF đã gửi",
                icon:
                  downloadingFileId === pdfArtifact.fileId ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Download className="size-4" />
                  ),
                onClick: () => void downloadArtifact(pdfArtifact, false),
                disabled: downloadingFileId !== null,
              },
            ]
          : []),
      ]}
    />
  );
}
