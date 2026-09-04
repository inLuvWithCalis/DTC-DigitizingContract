"use client";

import { useState } from "react";
import Link from "next/link";
import {
  CheckCircle2,
  Download,
  Eye,
  FileText,
  Loader2,
  RotateCcw,
  ShieldX,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { formatDateTime } from "@/lib/format-date-time";
import type {
  ContractApprovalArtifactResponse,
  ContractApprovalDetailResponse,
  ContractApprovalRequestResponse,
} from "@/services/contract-approval-api";

export type ApprovalDecision = "approve" | "return" | "reject";

export const APPROVAL_DECISION_CONFIG = {
  approve: {
    label: "Duyệt hợp đồng",
    description: "Hợp đồng sẽ chuyển sang trạng thái Chờ ký.",
    buttonClass: "bg-emerald-600 text-white hover:bg-emerald-700",
    icon: CheckCircle2,
  },
  return: {
    label: "Yêu cầu sửa lại",
    description:
      "Hợp đồng sẽ quay về Đang đàm phán. Owner phải tạo version mới trước khi chỉnh sửa.",
    buttonClass: "bg-amber-600 text-white hover:bg-amber-700",
    icon: RotateCcw,
  },
  reject: {
    label: "Từ chối",
    description:
      "Version hiện tại bị từ chối và vẫn bất biến. Owner có thể tạo version mới nếu tiếp tục.",
    buttonClass: "bg-rose-600 text-white hover:bg-rose-700",
    icon: ShieldX,
  },
} as const;

interface ContractApprovalDecisionDialogProps {
  open: boolean;
  mode: "single" | "bulk";
  requests: ContractApprovalRequestResponse[];
  detail?: ContractApprovalDetailResponse | null;
  initialDecision?: ApprovalDecision;
  isLoadingDetail?: boolean;
  isSubmitting?: boolean;
  downloadingFileId?: number | null;
  onOpenChange: (open: boolean) => void;
  onSubmit: (decision: ApprovalDecision, comment: string) => void | Promise<void>;
  onDownloadArtifact?: (
    artifact: ContractApprovalArtifactResponse,
    openPdf: boolean,
  ) => void | Promise<void>;
}

export function ContractApprovalDecisionDialog({
  open,
  mode,
  requests,
  detail,
  initialDecision = "approve",
  isLoadingDetail = false,
  isSubmitting = false,
  downloadingFileId = null,
  onOpenChange,
  onSubmit,
  onDownloadArtifact,
}: ContractApprovalDecisionDialogProps) {
  const [decision, setDecision] = useState<ApprovalDecision>(initialDecision);
  const [comment, setComment] = useState("");

  const normalizedComment = comment.trim();
  const requiresReason = decision !== "approve";
  const canSubmit = requests.length > 0
    && !isLoadingDetail
    && !isSubmitting
    && (!requiresReason || normalizedComment.length > 0);
  const config = APPROVAL_DECISION_CONFIG[decision];

  return (
    <Dialog
      open={open}
      onOpenChange={(nextOpen) => {
        if (!isSubmitting) onOpenChange(nextOpen);
      }}
    >
      <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
        {isLoadingDetail ? (
          <div className="flex min-h-48 items-center justify-center">
            <DialogTitle className="sr-only">
              Đang tải thông tin yêu cầu duyệt
            </DialogTitle>
            <Loader2 className="size-7 animate-spin text-primary" />
          </div>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>
                {mode === "bulk"
                  ? `Xử lý ${requests.length} yêu cầu duyệt`
                  : detail?.contractCode
                    || `Hợp đồng #${detail?.contractId ?? requests[0]?.contractId}`}
                {mode === "single" && (detail || requests[0])
                  ? ` · Version ${(detail ?? requests[0]).versionNo}`
                  : ""}
              </DialogTitle>
              <DialogDescription>
                {mode === "bulk"
                  ? "Một quyết định và ghi chú sẽ được áp dụng cho toàn bộ yêu cầu đã chọn. Các dòng lỗi sẽ được báo riêng."
                  : detail
                    ? `Gửi bởi ${detail.submittedByEmployeeName || "Nhân viên"} lúc ${formatDateTime(detail.submittedDate)}.`
                    : "Kiểm tra yêu cầu trước khi đưa ra quyết định."}
              </DialogDescription>
            </DialogHeader>

            {mode === "bulk" ? (
              <div className="max-h-60 space-y-2 overflow-y-auto rounded-xl border bg-muted/20 p-3">
                {requests.map((request) => (
                  <div
                    key={request.approvalRequestId}
                    className="flex items-start justify-between gap-3 rounded-lg bg-background p-3 text-sm"
                  >
                    <div className="min-w-0">
                      <p className="truncate font-medium">
                        {request.contractCode || `Hợp đồng #${request.contractId}`}
                      </p>
                      <p className="truncate text-xs text-muted-foreground">
                        {request.contractName}
                      </p>
                    </div>
                    <span className="shrink-0 text-xs text-muted-foreground">
                      Version {request.versionNo}
                    </span>
                  </div>
                ))}
              </div>
            ) : detail ? (
              <>
                <div className="grid gap-3 sm:grid-cols-2">
                  {detail.artifacts.map((artifact) => {
                    const isPdf = artifact.fileType.toLowerCase() === "pdf";
                    return (
                      <div
                        key={artifact.fileId}
                        className="rounded-xl border bg-muted/20 p-4"
                      >
                        <div className="flex items-start gap-3">
                          <FileText className="mt-0.5 size-5 text-primary" />
                          <div className="min-w-0 flex-1">
                            <p className="truncate font-medium">
                              {artifact.fileName}
                            </p>
                            <p className="mt-1 truncate font-mono text-[11px] text-muted-foreground">
                              SHA-256: {artifact.sha256}
                            </p>
                          </div>
                        </div>
                        <Button
                          className="mt-3 w-full"
                          variant="outline"
                          disabled={downloadingFileId === artifact.fileId}
                          onClick={() =>
                            void onDownloadArtifact?.(artifact, isPdf)
                          }
                        >
                          {downloadingFileId === artifact.fileId ? (
                            <Loader2 className="size-4 animate-spin" />
                          ) : isPdf ? (
                            <Eye className="size-4" />
                          ) : (
                            <Download className="size-4" />
                          )}
                          {isPdf ? "Xem PDF đã gửi" : "Tải DOCX đã gửi"}
                        </Button>
                      </div>
                    );
                  })}
                </div>

                <div className="rounded-lg border bg-muted/20 p-4 text-sm">
                  <Link
                    href={`/contracts/${detail.contractId}#approval`}
                    target="_blank"
                    className="font-medium text-primary hover:underline"
                  >
                    Mở trang chi tiết hợp đồng
                  </Link>
                </div>
              </>
            ) : null}

            <div className="space-y-3">
              <Label>Quyết định</Label>
              <div className="grid gap-2 sm:grid-cols-3">
                {(Object.keys(APPROVAL_DECISION_CONFIG) as ApprovalDecision[])
                  .map((value) => {
                    const itemConfig = APPROVAL_DECISION_CONFIG[value];
                    const Icon = itemConfig.icon;
                    return (
                      <Button
                        key={value}
                        type="button"
                        variant={decision === value ? "default" : "outline"}
                        onClick={() => setDecision(value)}
                      >
                        <Icon className="size-4" />
                        {itemConfig.label}
                      </Button>
                    );
                  })}
              </div>
              <p className="text-xs text-muted-foreground">
                {config.description}
              </p>
            </div>

            <div className="space-y-2">
              <Label htmlFor="approval-decision-comment">
                {requiresReason ? (
                  <span>
                    Lý do <span className="text-rose-500">*</span>
                  </span>
                ) : (
                  "Ghi chú (không bắt buộc)"
                )}
              </Label>
              <Textarea
                id="approval-decision-comment"
                value={comment}
                maxLength={1000}
                disabled={isSubmitting}
                onChange={(event) => setComment(event.target.value)}
                placeholder={
                  decision === "approve"
                    ? "Ghi chú cho Owner..."
                    : "Nêu rõ nội dung cần sửa hoặc lý do từ chối..."
                }
              />
            </div>

            <DialogFooter>
              <Button
                variant="outline"
                disabled={isSubmitting}
                onClick={() => onOpenChange(false)}
              >
                Đóng
              </Button>
              <Button
                className={config.buttonClass}
                disabled={!canSubmit}
                onClick={() => void onSubmit(decision, normalizedComment)}
              >
                {isSubmitting && <Loader2 className="size-4 animate-spin" />}
                {config.label}
                {mode === "bulk" ? ` (${requests.length})` : ""}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
