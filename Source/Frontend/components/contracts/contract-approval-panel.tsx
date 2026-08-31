"use client";

import { useCallback, useEffect, useState } from "react";
import {
  CheckCircle2,
  Download,
  Eye,
  FileClock,
  FileText,
  Loader2,
  RotateCcw,
  ShieldX,
  Undo2,
} from "lucide-react";

import { downloadBlob } from "@/components/contract-templates/contract-template-utils";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { toast } from "@/components/ui/sonner";
import { useAuthStore } from "@/hooks/use-auth-store";
import {
  getApiErrorMessage,
  getBlobApiErrorMessage,
  isStaleRowVersion,
} from "@/lib/api-error";
import { formatDateTime } from "@/lib/format-date-time";
import { cn } from "@/lib/utils";
import {
  contractApprovalApi,
  type ContractApprovalDetailResponse,
  type ContractApprovalRequestResponse,
} from "@/services/contract-approval-api";
import {
  ApprovalRequestStatus,
  ContractStatus,
  getApprovalRequestStatusLabel,
  type ContractDetailResponse,
} from "@/services/contract-api";

const statusStyles: Record<ApprovalRequestStatus, string> = {
  [ApprovalRequestStatus.Pending]:
    "border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-300",
  [ApprovalRequestStatus.Approved]:
    "border-emerald-500/30 bg-emerald-500/10 text-emerald-700 dark:text-emerald-300",
  [ApprovalRequestStatus.Returned]:
    "border-orange-500/30 bg-orange-500/10 text-orange-700 dark:text-orange-300",
  [ApprovalRequestStatus.Rejected]:
    "border-rose-500/30 bg-rose-500/10 text-rose-700 dark:text-rose-300",
  [ApprovalRequestStatus.Withdrawn]:
    "border-slate-500/30 bg-slate-500/10 text-slate-700 dark:text-slate-300",
};

const statusIcon = (status: ApprovalRequestStatus) => {
  if (status === ApprovalRequestStatus.Approved) return CheckCircle2;
  if (status === ApprovalRequestStatus.Returned) return RotateCcw;
  if (status === ApprovalRequestStatus.Rejected) return ShieldX;
  if (status === ApprovalRequestStatus.Withdrawn) return Undo2;
  return FileClock;
};

interface ContractApprovalPanelProps {
  contract: ContractDetailResponse;
  canManage: boolean;
  onContractRefetch: () => void | Promise<void>;
}

export function ContractApprovalPanel({
  contract,
  canManage,
  onContractRefetch,
}: ContractApprovalPanelProps) {
  const employeeId = useAuthStore((state) => state.user?.employeeId);
  const [history, setHistory] = useState<ContractApprovalRequestResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [withdrawReason, setWithdrawReason] = useState("");
  const [isWithdrawing, setIsWithdrawing] = useState(false);
  const [artifactDetail, setArtifactDetail] =
    useState<ContractApprovalDetailResponse | null>(null);
  const [isLoadingArtifacts, setIsLoadingArtifacts] = useState(false);
  const [downloadingFileId, setDownloadingFileId] = useState<number | null>(
    null,
  );

  const loadHistory = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      setHistory(
        await contractApprovalApi.getContractHistory(contract.contractId),
      );
    } catch (loadError) {
      setError(
        getApiErrorMessage(loadError, "Không thể tải lịch sử phê duyệt."),
      );
    } finally {
      setIsLoading(false);
    }
  }, [contract.contractId]);

  useEffect(() => {
    void loadHistory();
  }, [loadHistory, contract.status]);

  const pendingRequest = history.find(
    (request) => request.status === ApprovalRequestStatus.Pending,
  );
  const canWithdraw =
    canManage &&
    contract.status === ContractStatus.PendingApproval &&
    pendingRequest?.submittedByEmployeeId === employeeId;

  const withdraw = async () => {
    const reason = withdrawReason.trim();
    if (!pendingRequest || !reason) {
      toast.error("Vui lòng nhập lý do rút yêu cầu duyệt.");
      return;
    }

    try {
      setIsWithdrawing(true);
      await contractApprovalApi.withdraw(pendingRequest.approvalRequestId, {
        rowVersion: pendingRequest.rowVersion,
        reason,
      });
      toast.success("Đã rút yêu cầu duyệt. Hãy tạo version mới để chỉnh sửa.");
      setWithdrawReason("");
      await Promise.all([loadHistory(), Promise.resolve(onContractRefetch())]);
    } catch (withdrawError) {
      if (isStaleRowVersion(withdrawError)) {
        await Promise.all([
          loadHistory(),
          Promise.resolve(onContractRefetch()),
        ]);
        toast.error("Yêu cầu đã thay đổi. Dữ liệu mới nhất đã được tải lại.");
      } else {
        toast.error(
          getApiErrorMessage(withdrawError, "Không thể rút yêu cầu duyệt."),
        );
      }
    } finally {
      setIsWithdrawing(false);
    }
  };

  const openArtifacts = async (approvalRequestId: number) => {
    try {
      setIsLoadingArtifacts(true);
      setArtifactDetail(await contractApprovalApi.getDetail(approvalRequestId));
    } catch (loadError) {
      toast.error(getApiErrorMessage(loadError, "Không thể tải artifact."));
    } finally {
      setIsLoadingArtifacts(false);
    }
  };

  const downloadArtifact = async (
    fileId: number,
    fileName: string,
    openPdf: boolean,
  ) => {
    const previewWindow = openPdf ? window.open("about:blank", "_blank") : null;
    try {
      setDownloadingFileId(fileId);
      const blob = await contractApprovalApi.downloadArtifact(fileId);
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
        downloadBlob(blob, fileName);
      }
    } catch (downloadError) {
      previewWindow?.close();
      toast.error(
        await getBlobApiErrorMessage(downloadError, "Không thể tải artifact."),
      );
    } finally {
      setDownloadingFileId(null);
    }
  };

  return (
    <div className="space-y-4">
      {canWithdraw && pendingRequest && (
        <Alert className="border-amber-500/30 bg-amber-500/5">
          <Undo2 className="size-4 text-amber-600" />
          <AlertTitle>Hợp đồng đang chờ duyệt</AlertTitle>
          <AlertDescription className="mt-3 space-y-3">
            <p>
              Bạn có thể rút hồ sơ trước khi Manager xử lý. Version đã gửi vẫn
              khóa; sau khi rút cần tạo version mới để chỉnh sửa.
            </p>
            <div className="space-y-2">
              <Label htmlFor="withdraw-reason">Lý do rút hồ sơ *</Label>
              <Textarea
                className="bg-white"
                id="withdraw-reason"
                value={withdrawReason}
                maxLength={1000}
                onChange={(event) => setWithdrawReason(event.target.value)}
                placeholder="Nhập lý do cần chỉnh sửa lại hợp đồng..."
              />
            </div>
            <Button
              variant="outline"
              disabled={isWithdrawing}
              onClick={() => void withdraw()}
            >
              {isWithdrawing ? (
                <Loader2 className="size-4 animate-spin" />
              ) : (
                <Undo2 className="size-4" />
              )}
              Rút yêu cầu duyệt
            </Button>
          </AlertDescription>
        </Alert>
      )}

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileClock className="size-5 text-primary" />
            Lịch sử phê duyệt
          </CardTitle>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="flex min-h-32 items-center justify-center">
              <Loader2 className="size-6 animate-spin text-primary" />
            </div>
          ) : error ? (
            <Alert variant="destructive">
              <AlertTitle>Không thể tải dữ liệu</AlertTitle>
              <AlertDescription>{error}</AlertDescription>
            </Alert>
          ) : history.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted-foreground">
              Hợp đồng chưa có lần gửi duyệt nào.
            </p>
          ) : (
            <div className="space-y-3">
              {history.map((request) => {
                const Icon = statusIcon(request.status);
                return (
                  <div
                    key={request.approvalRequestId}
                    className="rounded-xl border p-4"
                  >
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div className="flex items-start gap-3">
                        <div className="rounded-lg bg-muted p-2">
                          <Icon className="size-4" />
                        </div>
                        <div>
                          <div className="flex flex-wrap items-center gap-2">
                            <p className="font-semibold">
                              Lần gửi #{request.approvalRequestId} · Version{" "}
                              {request.versionNo}
                            </p>
                            <Badge
                              variant="outline"
                              className={cn(statusStyles[request.status])}
                            >
                              {getApprovalRequestStatusLabel(request.status)}
                            </Badge>
                          </div>
                          <p className="mt-1 text-xs text-muted-foreground">
                            {request.submittedByEmployeeName || "Nhân viên"} gửi
                            lúc {formatDateTime(request.submittedDate)}
                          </p>
                          {request.resolvedDate && (
                            <p className="mt-1 text-xs text-muted-foreground">
                              {request.resolvedByEmployeeName || "Nhân viên"} xử
                              lý lúc {formatDateTime(request.resolvedDate)}
                            </p>
                          )}
                        </div>
                      </div>
                      <Button
                        size="sm"
                        variant="outline"
                        disabled={isLoadingArtifacts}
                        onClick={() =>
                          void openArtifacts(request.approvalRequestId)
                        }
                      >
                        {isLoadingArtifacts ? (
                          <Loader2 className="size-4 animate-spin" />
                        ) : (
                          <FileText className="size-4" />
                        )}
                        Artifact đã gửi
                      </Button>
                    </div>
                    {request.decisionComment && (
                      <div className="mt-3 rounded-lg bg-muted/50 p-3 text-sm">
                        <span className="font-medium">Ghi chú/lý do: </span>
                        {request.decisionComment}
                      </div>
                    )}
                  </div>
                );
              })}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog
        open={artifactDetail !== null}
        onOpenChange={(open) => !open && setArtifactDetail(null)}
      >
        <DialogContent className="sm:max-w-2xl">
          {artifactDetail && (
            <>
              <DialogHeader>
                <DialogTitle>
                  Artifact Version {artifactDetail.versionNo}
                </DialogTitle>
                <DialogDescription>
                  Đây là bản bất biến được tạo tại thời điểm gửi duyệt.
                </DialogDescription>
              </DialogHeader>
              <div className="grid gap-3 sm:grid-cols-2">
                {artifactDetail.artifacts.map((artifact) => {
                  const isPdf = artifact.fileType.toLowerCase() === "pdf";
                  return (
                    <Button
                      key={artifact.fileId}
                      variant="outline"
                      className="h-auto justify-start p-4"
                      disabled={downloadingFileId === artifact.fileId}
                      onClick={() =>
                        void downloadArtifact(
                          artifact.fileId,
                          artifact.fileName,
                          isPdf,
                        )
                      }
                    >
                      {downloadingFileId === artifact.fileId ? (
                        <Loader2 className="size-5 animate-spin" />
                      ) : isPdf ? (
                        <Eye className="size-5" />
                      ) : (
                        <Download className="size-5" />
                      )}
                      <span className="min-w-0 text-left">
                        <span className="block truncate font-medium">
                          {artifact.fileName}
                        </span>
                        <span className="block text-xs text-muted-foreground">
                          {isPdf ? "Xem PDF" : "Tải DOCX"}
                        </span>
                      </span>
                    </Button>
                  );
                })}
              </div>
            </>
          )}
        </DialogContent>
      </Dialog>
    </div>
  );
}
