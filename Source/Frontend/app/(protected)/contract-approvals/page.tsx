"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";
import {
  CalendarDays,
  CheckCircle2,
  Clock3,
  Copy,
  Download,
  Eye,
  ExternalLink,
  FileCheck2,
  FileText,
  Loader2,
  RefreshCw,
  RotateCcw,
  ShieldX,
  UserRoundCheck,
} from "lucide-react";

import { PermissionGuard } from "@/components/auth/permission-guard";
import { downloadBlob } from "@/components/contract-templates/contract-template-utils";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { Header } from "@/components/ui/custom/header";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  type SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
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
import { toast } from "@/components/ui/sonner";
import {
  getApiErrorMessage,
  getBlobApiErrorMessage,
  isStaleRowVersion,
} from "@/lib/api-error";
import { formatDateTime, parseApiDate } from "@/lib/format-date-time";
import { RBAC_PERMISSIONS } from "@/lib/rbac";
import {
  contractApprovalApi,
  type ContractApprovalDetailResponse,
  type ContractApprovalRequestResponse,
} from "@/services/contract-approval-api";
import {
  ApprovalRequestStatus,
  getApprovalRequestStatusLabel,
} from "@/services/contract-api";

type Decision = "approve" | "return" | "reject";

const decisionConfig = {
  approve: {
    label: "Duyệt hợp đồng",
    description: "Hợp đồng sẽ chuyển sang trạng thái Chờ ký.",
    buttonClass: "bg-emerald-600 text-white hover:bg-emerald-700",
  },
  return: {
    label: "Yêu cầu sửa lại",
    description:
      "Hợp đồng sẽ quay về Đang đàm phán. Owner phải tạo version mới trước khi chỉnh sửa.",
    buttonClass: "bg-amber-600 text-white hover:bg-amber-700",
  },
  reject: {
    label: "Từ chối",
    description:
      "Version hiện tại bị từ chối và vẫn bất biến. Owner có thể tạo version mới nếu tiếp tục.",
    buttonClass: "bg-rose-600 text-white hover:bg-rose-700",
  },
} as const;

const pendingStatusClassName =
  "border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-300";

function formatShortDate(value: string) {
  return parseApiDate(value).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

function formatWaitingTime(value?: string, referenceTime?: number | null) {
  if (!value) return "Chưa có dữ liệu";
  if (referenceTime == null) return "Đang tính...";

  const elapsedMilliseconds = Math.max(
    0,
    referenceTime - parseApiDate(value).getTime(),
  );
  const elapsedHours = Math.floor(elapsedMilliseconds / 3_600_000);

  if (elapsedHours < 1) return "Dưới 1 giờ";
  if (elapsedHours < 24) return `${elapsedHours} giờ`;
  return `${Math.floor(elapsedHours / 24)} ngày`;
}

export default function ContractApprovalsPage() {
  const router = useRouter();
  const [data, setData] = useState<ContractApprovalRequestResponse[]>([]);
  const [rowCount, setRowCount] = useState(0);
  const [pageCount, setPageCount] = useState(0);
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  });
  const [search, setSearch] = useState("");
  const [referenceTime, setReferenceTime] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [selected, setSelected] =
    useState<ContractApprovalDetailResponse | null>(null);
  const [isLoadingDetail, setIsLoadingDetail] = useState(false);
  const [decision, setDecision] = useState<Decision>("approve");
  const [comment, setComment] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [downloadingFileId, setDownloadingFileId] = useState<number | null>(
    null,
  );

  const loadInbox = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const result = await contractApprovalApi.getInbox({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: search || undefined,
      });
      setData(result.items);
      setRowCount(result.totalCount);
      setPageCount(result.totalPages);
    } catch (loadError) {
      setData([]);
      setRowCount(0);
      setPageCount(0);
      setError(
        getApiErrorMessage(
          loadError,
          "Không thể tải danh sách hợp đồng chờ duyệt.",
        ),
      );
    } finally {
      setIsLoading(false);
    }
  }, [pagination.pageIndex, pagination.pageSize, search]);

  useEffect(() => {
    void loadInbox();
  }, [loadInbox]);

  useEffect(() => {
    const updateReferenceTime = () => setReferenceTime(Date.now());
    updateReferenceTime();
    const timer = window.setInterval(updateReferenceTime, 60_000);
    return () => window.clearInterval(timer);
  }, []);

  const openRequest = useCallback(async (approvalRequestId: number) => {
    try {
      setIsLoadingDetail(true);
      setComment("");
      setDecision("approve");
      const detail = await contractApprovalApi.getDetail(approvalRequestId);
      setSelected(detail);
    } catch (loadError) {
      toast.error(
        getApiErrorMessage(loadError, "Không thể tải yêu cầu duyệt."),
      );
    } finally {
      setIsLoadingDetail(false);
    }
  }, []);

  const columns = useMemo<ColumnDef<ContractApprovalRequestResponse>[]>(
    () => [
      {
        accessorKey: "contractCode",
        header: "Mã hợp đồng",
        cell: ({ row }) => (
          <div className="flex min-w-40 flex-col pl-1">
            <p className="font-semibold text-foreground">
              {row.original.contractCode || `#${row.original.contractId}`}
            </p>
            <span className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <CalendarDays className="size-3" />
              {formatShortDate(row.original.submittedDate)}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "contractName",
        header: "Thông tin hợp đồng",
        cell: ({ row }) => (
          <div className="max-w-80">
            <p className="truncate font-medium text-foreground">
              {row.original.contractName}
            </p>
            <Badge variant="outline" className="mt-1 text-[10px]">
              Version {row.original.versionNo}
            </Badge>
          </div>
        ),
      },
      {
        accessorKey: "submittedByEmployeeName",
        header: "Người gửi",
        cell: ({ row }) => (
          <div className="flex min-w-40 items-center gap-2">
            <div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-secondary">
              <UserRoundCheck className="size-4 text-muted-foreground" />
            </div>
            <p className="truncate text-sm font-medium">
              {row.original.submittedByEmployeeName ||
                `#${row.original.submittedByEmployeeId}`}
            </p>
          </div>
        ),
      },
      {
        accessorKey: "responsibleEmployeeName",
        header: "Người phụ trách",
        cell: ({ row }) =>
          row.original.responsibleEmployeeName ||
          `#${row.original.responsibleEmployeeId}`,
      },
      {
        id: "waitingTime",
        header: "Thời gian chờ",
        cell: ({ row }) => (
          <div className="flex items-center gap-2 text-sm">
            <Clock3 className="size-4 text-amber-600" />
            <span>
              {formatWaitingTime(row.original.submittedDate, referenceTime)}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => (
          <div className="flex justify-center">
            <Badge variant="outline" className={pendingStatusClassName}>
              {getApprovalRequestStatusLabel(row.original.status)}
            </Badge>
          </div>
        ),
      },
      {
        id: "action",
        header: () => <div className="pr-4 text-right">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <SplitActionMenu
              primaryLabel="Xem và xử lý"
              primaryIcon={<Eye className="size-4" />}
              onPrimaryClick={() => void openRequest(item.approvalRequestId)}
              menuItems={[
                {
                  label: "Mở chi tiết hợp đồng",
                  icon: <ExternalLink className="size-4" />,
                  onClick: () =>
                    router.push(`/contracts/${item.contractId}#approval`),
                },
                {
                  label: "Sao chép mã hợp đồng",
                  icon: <Copy className="size-4" />,
                  disabled: !item.contractCode,
                  onClick: () =>
                    void navigator.clipboard?.writeText(
                      item.contractCode || "",
                    ),
                },
              ]}
            />
          );
        },
      },
    ],
    [openRequest, referenceTime, router],
  );

  const oldestSubmittedDate = useMemo(
    () =>
      data.reduce<string | undefined>((oldest, request) => {
        if (!oldest) return request.submittedDate;
        return parseApiDate(request.submittedDate) < parseApiDate(oldest)
          ? request.submittedDate
          : oldest;
      }, undefined),
    [data],
  );

  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng yêu cầu chờ duyệt",
      value: rowCount,
      icon: <FileCheck2 className="size-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Yêu cầu trên trang",
      value: data.length,
      icon: <FileText className="size-6" />,
      iconWrapperClassName: "bg-blue-500/10 text-blue-600 dark:text-blue-400",
    },
    {
      title: "Người gửi trên trang",
      value: new Set(data.map((request) => request.submittedByEmployeeId)).size,
      icon: <UserRoundCheck className="size-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
    },
    {
      title: "Chờ lâu nhất (trang)",
      value: formatWaitingTime(oldestSubmittedDate, referenceTime),
      icon: <Clock3 className="size-6" />,
      iconWrapperClassName:
        "bg-amber-500/10 text-amber-600 dark:text-amber-400",
      valueClassName: "text-xl",
    },
  ];

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
        previewWindow.opener = null;
        const url = URL.createObjectURL(
          blob.type === "application/pdf"
            ? blob
            : new Blob([blob], { type: "application/pdf" }),
        );
        previewWindow.location.replace(url);
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      } else {
        previewWindow?.close();
        downloadBlob(blob, fileName);
      }
    } catch (downloadError) {
      previewWindow?.close();
      toast.error(
        await getBlobApiErrorMessage(
          downloadError,
          "Không thể tải artifact gửi duyệt.",
        ),
      );
    } finally {
      setDownloadingFileId(null);
    }
  };

  const submitDecision = async () => {
    if (!selected || isSubmitting) return;
    const normalizedComment = comment.trim();
    if (decision !== "approve" && !normalizedComment) {
      toast.error("Vui lòng nhập lý do trước khi xử lý.");
      return;
    }

    try {
      setIsSubmitting(true);
      const payload = {
        rowVersion: selected.rowVersion,
        comment: normalizedComment || null,
      };
      if (decision === "approve") {
        await contractApprovalApi.approve(selected.approvalRequestId, payload);
      } else if (decision === "return") {
        await contractApprovalApi.returnForRevision(
          selected.approvalRequestId,
          payload,
        );
      } else {
        await contractApprovalApi.reject(selected.approvalRequestId, payload);
      }

      toast.success(`${decisionConfig[decision].label} thành công.`);
      setSelected(null);
      await loadInbox();
    } catch (submitError) {
      if (isStaleRowVersion(submitError)) {
        toast.error(
          "Yêu cầu đã được người khác xử lý. Danh sách đã được tải lại.",
        );
        setSelected(null);
        await loadInbox();
      } else {
        toast.error(
          getApiErrorMessage(submitError, "Không thể xử lý yêu cầu duyệt."),
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <>
      <Header />
      <main className="grow space-y-6 overflow-y-auto p-2 lg:p-10">
        <PermissionGuard permission={RBAC_PERMISSIONS.contractApprovalDecide}>
          <div className="space-y-6">
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
              <div>
                <h1 className="text-2xl font-bold tracking-tight text-foreground">
                  Hợp đồng chờ duyệt
                </h1>
                <p className="mt-1 text-sm text-muted-foreground">
                  Kiểm tra version và DOCX/PDF bất biến trước khi duyệt, trả lại
                  hoặc từ chối hợp đồng.
                </p>
              </div>
              <Button
                variant="outline"
                className="shadow-sm"
                disabled={isLoading}
                onClick={() => void loadInbox()}
              >
                <RefreshCw
                  className={`mr-2 size-4 ${isLoading ? "animate-spin" : ""}`}
                />
                Làm mới
              </Button>
            </div>

            {error && (
              <Alert variant="destructive">
                <ShieldX className="size-4" />
                <AlertTitle>Không thể tải dữ liệu</AlertTitle>
                <AlertDescription>{error}</AlertDescription>
              </Alert>
            )}

            <SummaryCards items={summaryItems} isLoading={isLoading} />

            <Card className="flex min-h-[500px] flex-col gap-0 border-border bg-card p-0 shadow-sm">
              <CardContent className="flex flex-1 flex-col justify-between p-4 pb-0">
                <DataTable
                  columns={columns}
                  data={data}
                  pageCount={pageCount}
                  rowCount={rowCount}
                  pagination={pagination}
                  onPaginationChange={setPagination}
                  searchValue={search}
                  onSearchChange={(value) => {
                    setSearch(value);
                    setPagination((current) => ({
                      ...current,
                      pageIndex: 0,
                    }));
                  }}
                  searchPlaceholder="Tìm mã, tên hợp đồng hoặc nhân viên..."
                  isLoading={isLoading}
                  onRowClick={(row) => void openRequest(row.approvalRequestId)}
                  mobileCardRenderer={(row, { isSelected, actionCell }) => {
                    const request = row.original;
                    return (
                      <div
                        className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                          isSelected
                            ? "border-primary/40 bg-primary/5"
                            : "border-border"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="font-semibold text-foreground">
                              {request.contractCode ||
                                `Hợp đồng #${request.contractId}`}
                            </p>
                            <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                              {request.contractName}
                            </p>
                          </div>
                          <Badge
                            variant="outline"
                            className={`${pendingStatusClassName} shrink-0`}
                          >
                            {getApprovalRequestStatusLabel(request.status)}
                          </Badge>
                        </div>

                        <div className="my-3 grid grid-cols-2 gap-3 rounded-lg border border-border/50 bg-muted/40 p-3 text-sm">
                          <div className="min-w-0">
                            <span className="block text-xs text-muted-foreground">
                              Version
                            </span>
                            <span className="mt-1 block font-medium">
                              {request.versionNo}
                            </span>
                          </div>
                          <div className="min-w-0">
                            <span className="block text-xs text-muted-foreground">
                              Thời gian chờ
                            </span>
                            <span className="mt-1 block font-medium text-amber-700 dark:text-amber-300">
                              {formatWaitingTime(
                                request.submittedDate,
                                referenceTime,
                              )}
                            </span>
                          </div>
                          <div className="min-w-0 border-t pt-2">
                            <span className="block text-xs text-muted-foreground">
                              Người gửi
                            </span>
                            <span className="mt-1 block truncate font-medium">
                              {request.submittedByEmployeeName ||
                                `#${request.submittedByEmployeeId}`}
                            </span>
                          </div>
                          <div className="min-w-0 border-t pt-2">
                            <span className="block text-xs text-muted-foreground">
                              Người phụ trách
                            </span>
                            <span className="mt-1 block truncate font-medium">
                              {request.responsibleEmployeeName ||
                                `#${request.responsibleEmployeeId}`}
                            </span>
                          </div>
                        </div>

                        <p className="text-xs text-muted-foreground">
                          Gửi lúc {formatDateTime(request.submittedDate)}
                        </p>
                        {actionCell}
                      </div>
                    );
                  }}
                />
              </CardContent>
            </Card>
          </div>
        </PermissionGuard>
      </main>

      <Dialog
        open={selected !== null || isLoadingDetail}
        onOpenChange={(open) => {
          if (!open && !isSubmitting) setSelected(null);
        }}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-3xl">
          {isLoadingDetail || !selected ? (
            <div className="flex min-h-48 items-center justify-center">
              <DialogTitle className="sr-only">
                Đang tải thông tin...
              </DialogTitle>
              <Loader2 className="size-7 animate-spin text-primary" />
            </div>
          ) : (
            <>
              <DialogHeader>
                <DialogTitle>
                  {selected.contractCode || `Hợp đồng #${selected.contractId}`}
                  {" · "}Version {selected.versionNo}
                </DialogTitle>
                <DialogDescription>
                  Gửi bởi {selected.submittedByEmployeeName || "Nhân viên"} lúc{" "}
                  {formatDateTime(selected.submittedDate)}.
                </DialogDescription>
              </DialogHeader>

              <div className="grid gap-3 sm:grid-cols-2">
                {selected.artifacts.map((artifact) => {
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
                          void downloadArtifact(
                            artifact.fileId,
                            artifact.fileName,
                            isPdf,
                          )
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
                  href={`/contracts/${selected.contractId}#approval`}
                  target="_blank"
                  className="font-medium text-primary hover:underline"
                >
                  Mở trang chi tiết hợp đồng
                </Link>
              </div>

              <div className="space-y-3">
                <Label>Quyết định</Label>
                <div className="grid gap-2 sm:grid-cols-3">
                  {(["approve", "return", "reject"] as const).map((value) => (
                    <Button
                      key={value}
                      type="button"
                      variant={decision === value ? "default" : "outline"}
                      onClick={() => setDecision(value)}
                    >
                      {value === "approve" ? (
                        <CheckCircle2 className="size-4" />
                      ) : value === "return" ? (
                        <RotateCcw className="size-4" />
                      ) : (
                        <ShieldX className="size-4" />
                      )}
                      {decisionConfig[value].label}
                    </Button>
                  ))}
                </div>
                <p className="text-xs text-muted-foreground">
                  {decisionConfig[decision].description}
                </p>
              </div>

              <div className="space-y-2">
                <Label htmlFor="approval-comment">
                  {decision === "approve" ? (
                    "Ghi chú (không bắt buộc)"
                  ) : (
                    <span>
                      Lý do <span className="text-rose-500">*</span>
                    </span>
                  )}
                </Label>
                <Textarea
                  id="approval-comment"
                  value={comment}
                  maxLength={1000}
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
                  onClick={() => setSelected(null)}
                >
                  Đóng
                </Button>
                <Button
                  className={decisionConfig[decision].buttonClass}
                  disabled={isSubmitting}
                  onClick={() => void submitDecision()}
                >
                  {isSubmitting && <Loader2 className="size-4 animate-spin" />}
                  {decisionConfig[decision].label}
                </Button>
              </DialogFooter>
            </>
          )}
        </DialogContent>
      </Dialog>
    </>
  );
}
