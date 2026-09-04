"use client";

import { useCallback, useEffect, useState } from "react";
import Link from "next/link";
import {
  CalendarDays,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Eye,
  FileLock2,
  GitBranch,
  Loader2,
  MessageSquareText,
  Package,
  RefreshCw,
  ScrollText,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { formatCurrency } from "@/lib/format-currency";
import { formatDate } from "@/lib/format-date";
import {
  contractApi,
  ContractDetailResponse,
  ContractStatus,
  ContractVersionDetailResponse,
  ContractVersionHistoryResponse,
} from "@/services/contract-api";

const VERSION_HISTORY_PAGE_SIZE = 3;

const getApiErrorMessage = (error: any, fallback: string) => {
  const data = error?.response?.data;
  return data?.errors
    ? Object.values(data.errors).flat().join("; ")
    : data?.message ||
        data?.title ||
        (typeof data === "string" ? data : null) ||
        fallback;
};

export function ContractNegotiation({
  contract,
  setContract,
  hasUnsavedChanges,
  canManage,
  onNegotiationRoundCreated,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
  hasUnsavedChanges: boolean;
  canManage: boolean;
  onNegotiationRoundCreated?: () => void;
}) {
  const [isDialogOpen, setIsDialogOpen] = useState(false);
  const [changeNote, setChangeNote] = useState("");
  const [isCreatingRound, setIsCreatingRound] = useState(false);
  const [versionHistory, setVersionHistory] = useState<
    ContractVersionHistoryResponse[]
  >([]);
  const [selectedVersionId, setSelectedVersionId] = useState<number>(
    contract.currentVersion.versionId,
  );
  const [selectedVersion, setSelectedVersion] =
    useState<ContractVersionDetailResponse | null>(null);
  const [isLoadingHistory, setIsLoadingHistory] = useState(false);
  const [isLoadingVersion, setIsLoadingVersion] = useState(false);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [versionError, setVersionError] = useState<string | null>(null);
  const [historyPage, setHistoryPage] = useState(1);

  const currentVersion = contract.currentVersion;
  const canBranchFromApprovalDecision =
    contract.status === ContractStatus.Negotiating ||
    contract.status === ContractStatus.Rejected;
  const canCreateRound = canManage && canBranchFromApprovalDecision;
  const loadVersionHistory = useCallback(async () => {
    if (
      contract.status !== ContractStatus.Negotiating &&
      contract.status !== ContractStatus.Rejected
    ) {
      setVersionHistory([]);
      setSelectedVersion(null);
      return;
    }

    setIsLoadingHistory(true);
    setHistoryError(null);
    try {
      const versions = await contractApi.getVersionHistory(contract.contractId);
      setVersionHistory(versions);
      setSelectedVersionId((currentSelection) =>
        versions.some((version) => version.versionId === currentSelection)
          ? currentSelection
          : contract.currentVersion.versionId,
      );
    } catch (error: any) {
      console.error("Lỗi tải lịch sử version:", error);
      setHistoryError(
        getApiErrorMessage(error, "Không thể tải lịch sử phiên bản đàm phán."),
      );
    } finally {
      setIsLoadingHistory(false);
    }
  }, [contract.contractId, contract.currentVersion.versionId, contract.status]);

  useEffect(() => {
    void loadVersionHistory();
  }, [loadVersionHistory]);

  const orderedVersionHistory = [...versionHistory].reverse();
  const historyTotalPages = Math.max(
    1,
    Math.ceil(orderedVersionHistory.length / VERSION_HISTORY_PAGE_SIZE),
  );
  const paginatedVersionHistory = orderedVersionHistory.slice(
    (historyPage - 1) * VERSION_HISTORY_PAGE_SIZE,
    historyPage * VERSION_HISTORY_PAGE_SIZE,
  );

  useEffect(() => {
    setHistoryPage((currentPage) =>
      Math.min(Math.max(currentPage, 1), historyTotalPages),
    );
  }, [historyTotalPages]);

  useEffect(() => {
    if (contract.status !== ContractStatus.Negotiating || !selectedVersionId) {
      return;
    }

    let isCancelled = false;
    const loadSelectedVersion = async () => {
      setIsLoadingVersion(true);
      setVersionError(null);
      try {
        const detail = await contractApi.getVersionDetail(
          contract.contractId,
          selectedVersionId,
        );
        if (!isCancelled) {
          setSelectedVersion(detail);
        }
      } catch (error: any) {
        console.error("Lỗi tải chi tiết version:", error);
        if (!isCancelled) {
          setSelectedVersion(null);
          setVersionError(
            getApiErrorMessage(
              error,
              "Không thể tải chi tiết phiên bản đã chọn.",
            ),
          );
        }
      } finally {
        if (!isCancelled) {
          setIsLoadingVersion(false);
        }
      }
    };

    void loadSelectedVersion();
    return () => {
      isCancelled = true;
    };
  }, [contract.contractId, contract.status, selectedVersionId]);

  const handleCreateRound = async () => {
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu các thay đổi trước khi tạo vòng đàm phán mới.");
      return;
    }

    const normalizedChangeNote = changeNote.trim();
    if (!normalizedChangeNote) {
      toast.error("Vui lòng nhập lý do tạo vòng đàm phán mới.");
      return;
    }

    setIsCreatingRound(true);
    try {
      const result = await contractApi.createNegotiationRound(
        contract.contractId,
        {
          currentVersionId: currentVersion.versionId,
          rowVersion: contract.rowVersion,
          currentVersionRowVersion: currentVersion.rowVersion,
          changeNote: normalizedChangeNote,
        },
      );
      onNegotiationRoundCreated?.();
      setChangeNote("");
      setIsDialogOpen(false);

      let updatedContract: ContractDetailResponse;
      try {
        updatedContract = await contractApi.getDetail(contract.contractId);
      } catch {
        toast.warning(
          "Đã tạo vòng đàm phán mới nhưng chưa tải được dữ liệu mới. Vui lòng tải lại trang.",
        );
        return;
      }

      setHistoryPage(1);
      setSelectedVersionId(updatedContract.currentVersion.versionId);
      setContract(updatedContract);
      toast.success(
        `Đã khóa phiên bản ${result.sourceVersion.versionNo} và tạo phiên bản ${result.currentVersion.versionNo}.${
          result.carriedForwardThreadCount > 0
            ? ` Đã chuyển tiếp ${result.carriedForwardThreadCount} luồng trao đổi còn mở (${result.carriedForwardCommentCount} bình luận).`
            : ""
        }`,
      );
    } catch (error: any) {
      console.error("Lỗi tạo vòng đàm phán:", error);
      if (error?.response?.status === 409) {
        try {
          const refreshedContract = await contractApi.getDetail(
            contract.contractId,
          );
          setContract(refreshedContract);
          setSelectedVersionId(refreshedContract.currentVersion.versionId);
          toast.error(
            "Hợp đồng hoặc phiên bản đã thay đổi. Dữ liệu mới nhất đã được tải lại.",
          );
        } catch {
          toast.error(
            "Hợp đồng hoặc phiên bản đã thay đổi. Vui lòng tải lại trang trước khi thử lại.",
          );
        }
        return;
      }

      toast.error(
        getApiErrorMessage(
          error,
          "Không thể tạo vòng đàm phán mới. Vui lòng thử lại.",
        ),
      );
    } finally {
      setIsCreatingRound(false);
    }
  };

  return (
    <div className="space-y-4">
      <Card>
        <CardHeader className="flex flex-col gap-3 space-y-0 sm:flex-row sm:items-start sm:justify-between">
          <div>
            <CardTitle className="flex items-center gap-2">
              <GitBranch className="size-5 text-primary" />
              Vòng đàm phán
            </CardTitle>
            <p className="mt-1 text-sm text-muted-foreground">
              Mỗi vòng mới sẽ lưu bản sao của phiên bản trước khi chỉnh sửa.
            </p>
          </div>
          {canCreateRound && (
            <Button
              className="w-full sm:w-auto"
              onClick={() => setIsDialogOpen(true)}
              disabled={hasUnsavedChanges}
              title={
                hasUnsavedChanges
                  ? "Hãy lưu các thay đổi trước khi tạo vòng mới"
                  : undefined
              }
            >
              <GitBranch className="mr-2 size-4" />
              Tạo vòng mới
            </Button>
          )}
        </CardHeader>
        <CardContent className="space-y-5">
          {contract.status === ContractStatus.Draft && (
            <Alert>
              <MessageSquareText className="size-4" />
              <AlertTitle>Hợp đồng chưa bước vào đàm phán</AlertTitle>
              <AlertDescription>
                Hãy lưu hoàn chỉnh bản nháp rồi chọn “Bắt đầu đàm phán”.
              </AlertDescription>
            </Alert>
          )}

          <div className="rounded-xl border bg-muted/20 p-4 sm:p-5">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
              <div>
                <p className="text-sm text-muted-foreground">
                  Phiên bản hiện hành
                </p>
                <p className="mt-1 text-2xl font-bold">
                  Phiên bản {currentVersion.versionNo}
                </p>
              </div>
              <Badge
                variant={currentVersion.isLocked ? "secondary" : "outline"}
                className="w-fit gap-1.5"
              >
                {currentVersion.isLocked ? (
                  <FileLock2 className="size-3.5" />
                ) : (
                  <CheckCircle2 className="size-3.5" />
                )}
                {currentVersion.isLocked ? "Đã khóa" : "Đang chỉnh sửa"}
              </Badge>
            </div>

            {currentVersion.changeNote && (
              <>
                <Separator className="my-4" />
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    Lý do tạo phiên bản
                  </p>
                  <p className="mt-2 text-sm leading-6">
                    {currentVersion.changeNote}
                  </p>
                </div>
              </>
            )}
          </div>

          {canBranchFromApprovalDecision && (
            <div className="space-y-3">
              <div className="flex items-center justify-between gap-3">
                <div>
                  <h3 className="font-semibold">Lịch sử phiên bản</h3>
                </div>
              </div>

              {historyError ? (
                <Alert variant="destructive">
                  <AlertTitle>Không thể tải lịch sử phiên bản</AlertTitle>
                  <AlertDescription className="space-y-3">
                    <p>{historyError}</p>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => void loadVersionHistory()}
                    >
                      Thử lại
                    </Button>
                  </AlertDescription>
                </Alert>
              ) : isLoadingHistory && versionHistory.length === 0 ? (
                <div className="flex min-h-40 items-center justify-center rounded-xl border border-dashed">
                  <Loader2 className="mr-2 size-5 animate-spin text-primary" />
                  <span className="text-sm text-muted-foreground">
                    Đang tải lịch sử phiên bản...
                  </span>
                </div>
              ) : versionHistory.length === 0 ? (
                <div className="rounded-xl border border-dashed p-6 text-center">
                  <GitBranch className="mx-auto mb-3 size-8 text-muted-foreground" />
                  <p className="font-medium">Chưa có lịch sử phiên bản</p>
                </div>
              ) : (
                <div className="grid gap-4 xl:grid-cols-[280px_minmax(0,1fr)]">
                  <div className="space-y-3 flex flex-col">
                    <div className="space-y-2 flex-1">
                      {paginatedVersionHistory.map((version) => {
                        const isSelected =
                          version.versionId === selectedVersionId;
                        const isCurrent =
                          version.versionId === currentVersion.versionId;

                        return (
                          <button
                            type="button"
                            key={version.versionId}
                            onClick={() =>
                              setSelectedVersionId(version.versionId)
                            }
                            className={`w-full rounded-xl border p-3 text-left transition-colors ${
                              isSelected
                                ? "border-primary bg-primary/5"
                                : "bg-background hover:bg-muted/50"
                            }`}
                          >
                            <div className="flex items-start justify-between gap-2">
                              <div>
                                <p className="font-semibold">
                                  Phiên bản {version.versionNo}
                                </p>
                                <p className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
                                  <CalendarDays className="size-3" />
                                  {formatDate(version.createdDate)}
                                </p>
                              </div>
                              <div className="flex flex-col items-end gap-1">
                                {isCurrent && <Badge>Hiện hành</Badge>}
                                {version.isLocked && (
                                  <Badge variant="secondary">Đã khóa</Badge>
                                )}
                              </div>
                            </div>
                            <p className="mt-3 line-clamp-2 text-sm text-muted-foreground">
                              {version.changeNote || "Khởi tạo hợp đồng."}
                            </p>
                          </button>
                        );
                      })}
                    </div>

                    {versionHistory.length >= 5 && (
                      <div className="flex items-center justify-between gap-2 border-t pt-3">
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() =>
                            setHistoryPage((page) => Math.max(1, page - 1))
                          }
                          disabled={historyPage === 1}
                          aria-label="Trang lịch sử trước"
                        >
                          <ChevronLeft className="size-4" />
                        </Button>
                        <div className="text-center text-xs text-muted-foreground">
                          <p className="font-medium text-foreground">
                            Trang {historyPage} / {historyTotalPages}
                          </p>
                          <p>{versionHistory.length} phiên bản</p>
                        </div>
                        <Button
                          variant="outline"
                          size="icon"
                          onClick={() =>
                            setHistoryPage((page) =>
                              Math.min(historyTotalPages, page + 1),
                            )
                          }
                          disabled={historyPage === historyTotalPages}
                          aria-label="Trang lịch sử sau"
                        >
                          <ChevronRight className="size-4" />
                        </Button>
                      </div>
                    )}
                  </div>

                  <div className="min-h-[260px] rounded-xl border bg-muted/20 p-4 sm:p-5">
                    {isLoadingVersion ? (
                      <div className="flex min-h-[230px] items-center justify-center">
                        <Loader2 className="mr-2 size-5 animate-spin text-primary" />
                        <span className="text-sm text-muted-foreground">
                          Đang tải bản sao lưu...
                        </span>
                      </div>
                    ) : versionError ? (
                      <Alert variant="destructive">
                        <AlertTitle>Không thể tải bản sao lưu</AlertTitle>
                        <AlertDescription>{versionError}</AlertDescription>
                      </Alert>
                    ) : selectedVersion ? (
                      <div className="space-y-5">
                        <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                          <div>
                            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                              Bản sao lưu đã chọn
                            </p>
                            <h4 className="mt-1 text-xl font-bold">
                              Version {selectedVersion.versionNo}
                            </h4>
                          </div>
                          <div className="flex flex-col items-start gap-2 sm:items-end">
                            <Badge
                              variant={
                                selectedVersion.isLocked
                                  ? "secondary"
                                  : "outline"
                              }
                              className="w-fit gap-1"
                            >
                              {selectedVersion.isLocked ? (
                                <FileLock2 className="size-3.5" />
                              ) : (
                                <CheckCircle2 className="size-3.5" />
                              )}
                              {selectedVersion.isLocked
                                ? "Bản sao lưu đã khóa"
                                : "Phiên bản đang chỉnh sửa"}
                            </Badge>
                            {selectedVersion.isLocked &&
                              selectedVersion.versionId !==
                                currentVersion.versionId && (
                                <Button variant="outline" size="sm" asChild>
                                  <Link
                                    href={`/contracts/${contract.contractId}/versions/${selectedVersion.versionId}`}
                                  >
                                    <Eye className="mr-2 size-4" />
                                    Xem chi tiết
                                  </Link>
                                </Button>
                              )}
                          </div>
                        </div>

                        <div className="grid gap-3 sm:grid-cols-3">
                          <div className="rounded-lg border bg-background p-3">
                            <Package className="mb-2 size-4 text-primary" />
                            <p className="text-xl font-bold">
                              {selectedVersion.items.length}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              Sản phẩm / dịch vụ
                            </p>
                          </div>
                          <div className="rounded-lg border bg-background p-3">
                            <ScrollText className="mb-2 size-4 text-primary" />
                            <p className="text-xl font-bold">
                              {selectedVersion.terms.length}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              Điều khoản
                            </p>
                          </div>
                          <div className="rounded-lg border bg-background p-3">
                            <MessageSquareText className="mb-2 size-4 text-primary" />
                            <p className="text-xl font-bold">
                              {selectedVersion.comments?.length || 0}
                            </p>
                            <p className="text-xs text-muted-foreground">
                              Bình luận
                            </p>
                          </div>
                        </div>

                        <div className="grid gap-3 text-sm sm:grid-cols-2">
                          <div>
                            <p className="text-muted-foreground">Ngày tạo</p>
                            <p className="mt-1 font-medium">
                              {formatDate(selectedVersion.createdDate)}
                            </p>
                          </div>
                          <div>
                            <p className="text-muted-foreground">
                              Tổng thanh toán
                            </p>
                            <p className="mt-1 font-semibold text-primary">
                              {formatCurrency(
                                selectedVersion.totalPayment,
                                selectedVersion.currencyCode,
                              )}
                            </p>
                          </div>
                        </div>

                        <Separator />
                        <div>
                          <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                            Lý do thay đổi
                          </p>
                          <p className="mt-2 text-sm leading-6">
                            {selectedVersion.changeNote || "Khởi tạo hợp đồng."}
                          </p>
                        </div>
                      </div>
                    ) : null}
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      <Dialog open={isDialogOpen} onOpenChange={setIsDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tạo vòng đàm phán mới</DialogTitle>
            <DialogDescription>
              {currentVersion.isLocked
                ? `Phiên bản ${currentVersion.versionNo} vẫn được giữ nguyên và hệ thống sẽ tạo phiên bản ${currentVersion.versionNo + 1} để bạn chỉnh sửa.`
                : `Phiên bản ${currentVersion.versionNo} sẽ được khóa và sao lưu. Hệ thống sau đó tạo phiên bản ${currentVersion.versionNo + 1} để bạn tiếp tục chỉnh sửa.`}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-2 py-2">
            <Alert>
              <FileLock2 className="size-4" />
              <AlertTitle>Ảnh hưởng sau khi tạo vòng mới</AlertTitle>
              <AlertDescription>
                Các luồng trao đổi còn mở sẽ được chuyển sang version mới để
                tiếp tục xử lý; các luồng đã đóng chỉ còn trong lịch sử. Link và
                phiên truy cập khách hàng hiện tại sẽ mất hiệu lực; sau khi sửa
                version mới, bạn cần tạo link khác để gửi khách hàng.
              </AlertDescription>
            </Alert>

            <Label htmlFor="negotiation-change-note" className="py-2">
              Lý do thay đổi <span className="text-destructive">*</span>
            </Label>
            <Textarea
              id="negotiation-change-note"
              value={changeNote}
              onChange={(event) => setChangeNote(event.target.value)}
              maxLength={2000}
              rows={5}
              placeholder="Ví dụ: Điều chỉnh phạm vi và điều khoản thanh toán theo phản hồi khách hàng..."
              disabled={isCreatingRound}
            />
            <p className="text-right text-xs text-muted-foreground">
              {changeNote.length}/2000
            </p>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsDialogOpen(false)}
              disabled={isCreatingRound}
            >
              Hủy
            </Button>
            <Button
              onClick={handleCreateRound}
              disabled={
                isCreatingRound || hasUnsavedChanges || !changeNote.trim()
              }
            >
              {isCreatingRound ? (
                <Loader2 className="mr-2 size-4 animate-spin" />
              ) : (
                <GitBranch className="mr-2 size-4" />
              )}
              Tạo version {currentVersion.versionNo + 1}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
