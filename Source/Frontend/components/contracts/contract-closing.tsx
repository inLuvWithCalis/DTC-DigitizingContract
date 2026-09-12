"use client";

import { useCallback, useEffect, useState } from "react";
import { format } from "date-fns";
import {
  AlertTriangle,
  CheckCircle2,
  Eye,
  FileCheck2,
  Loader2,
  RotateCcw,
  WalletCards,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Progress } from "@/components/ui/progress";
import { Textarea } from "@/components/ui/textarea";
import { DateFilter } from "@/components/ui/custom/date-filter";
import { DecimalInput } from "@/components/ui/custom/decimal-input";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { formatCurrency } from "@/lib/format-currency";
import { getApiErrorMessage } from "@/lib/api-error";
import {
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";
import {
  contractCompletionApi,
  ContractCompletionDetailResponse,
  ContractPaymentDayCountMode,
  ContractPaymentMilestoneStatus,
  ContractPaymentStatus,
} from "@/services/contract-completion-api";
import { formatDateTime } from "@/lib/format-date-time";

interface Props {
  contract: ContractDetailResponse;
  canManageAcceptance: boolean;
  canManagePayment: boolean;
  canComplete: boolean;
  onContractRefetch: () => void | Promise<void>;
}

const MAX_EVIDENCE_SIZE = 20 * 1024 * 1024;
const EVIDENCE_EXTENSIONS = ["pdf", "jpg", "jpeg", "png"];

const evidenceContentType = (fileName: string) => {
  const extension = fileName.split(".").pop()?.toLowerCase();
  if (extension === "pdf") return "application/pdf";
  if (extension === "png") return "image/png";
  return "image/jpeg";
};

export function ContractClosing({
  contract,
  canManageAcceptance,
  canManagePayment,
  canComplete,
  onContractRefetch,
}: Props) {
  const [detail, setDetail] = useState<ContractCompletionDetailResponse | null>(
    null,
  );
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [acceptanceFile, setAcceptanceFile] = useState<File>();
  const [paymentDate, setPaymentDate] = useState<Date>();
  const [amount, setAmount] = useState(0);
  const [method, setMethod] = useState("");
  const [reference, setReference] = useState("");
  const [paymentFile, setPaymentFile] = useState<File>();
  const [voidingId, setVoidingId] = useState<number>();
  const [voidReason, setVoidReason] = useState("");
  const [completingMilestoneId, setCompletingMilestoneId] = useState<number>();
  const [milestonePaymentDate, setMilestonePaymentDate] = useState<Date>();
  const [milestonePaymentMethod, setMilestonePaymentMethod] = useState("");
  const [milestoneReference, setMilestoneReference] = useState("");
  const [milestoneEvidence, setMilestoneEvidence] = useState<File>();
  const [evidenceConfirmed, setEvidenceConfirmed] = useState(false);
  const [reopeningMilestoneId, setReopeningMilestoneId] = useState<number>();
  const [reopenReason, setReopenReason] = useState("");
  const [updatingMilestoneId, setUpdatingMilestoneId] = useState<number>();
  const [openingEvidenceFileId, setOpeningEvidenceFileId] = useState<number>();
  const [confirmComplete, setConfirmComplete] = useState(false);

  const load = useCallback(
    async (showSpinner = true) => {
      try {
        if (showSpinner) setLoading(true);
        setDetail(await contractCompletionApi.get(contract.contractId));
      } catch (error) {
        toast.error(getApiErrorMessage(error, "Không thể tải hồ sơ hoàn tất."));
      } finally {
        setLoading(false);
      }
    },
    [contract.contractId],
  );
  useEffect(() => {
    void load();
  }, [load]);

  const mutate = async (action: () => Promise<unknown>, success: string) => {
    try {
      setBusy(true);
      await action();
      toast.success(success);
      await load(false);
      await onContractRefetch();
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Thao tác không thành công."));
    } finally {
      setBusy(false);
    }
  };

  const resetCompleteMilestone = () => {
    setCompletingMilestoneId(undefined);
    setMilestonePaymentDate(undefined);
    setMilestonePaymentMethod("");
    setMilestoneReference("");
    setMilestoneEvidence(undefined);
    setEvidenceConfirmed(false);
  };

  const openCompleteMilestone = (milestoneId: number) => {
    setCompletingMilestoneId(milestoneId);
    setMilestonePaymentDate(new Date());
    setMilestonePaymentMethod("");
    setMilestoneReference("");
    setMilestoneEvidence(undefined);
    setEvidenceConfirmed(false);
  };

  const completeMilestone = async () => {
    if (
      !detail ||
      !completingMilestoneId ||
      !milestoneEvidence ||
      !milestonePaymentDate
    )
      return;
    const milestone = detail.paymentMilestones.find(
      (item) => item.paymentMilestoneId === completingMilestoneId,
    );
    if (!milestone) return;
    try {
      setUpdatingMilestoneId(milestone.paymentMilestoneId);
      await contractCompletionApi.completePaymentMilestone(
        contract.contractId,
        detail.versionId,
        milestone.paymentMilestoneId,
        {
          evidenceFile: milestoneEvidence,
          currentVersionId: detail.versionId,
          contractRowVersion: detail.contractRowVersion,
          versionRowVersion: detail.versionRowVersion,
          milestoneRowVersion: milestone.rowVersion,
          paymentDate: format(milestonePaymentDate, "yyyy-MM-dd"),
          paymentMethod: milestonePaymentMethod.trim(),
          referenceCode: milestoneReference.trim(),
        },
      );
      toast.success(`Đã hoàn tất ${milestone.titleVi} và lưu chứng từ.`);
      resetCompleteMilestone();
      await load(false);
      await onContractRefetch();
    } catch (error) {
      toast.error(
        getApiErrorMessage(error, "Không thể hoàn tất đợt thanh toán."),
      );
    } finally {
      setUpdatingMilestoneId(undefined);
    }
  };

  const reopenMilestone = async () => {
    if (!detail || !reopeningMilestoneId || !reopenReason.trim()) return;
    const milestone = detail.paymentMilestones.find(
      (item) => item.paymentMilestoneId === reopeningMilestoneId,
    );
    if (!milestone?.activePayment) return;
    try {
      setUpdatingMilestoneId(milestone.paymentMilestoneId);
      await contractCompletionApi.reopenPaymentMilestone(
        contract.contractId,
        detail.versionId,
        milestone.paymentMilestoneId,
        {
          currentVersionId: detail.versionId,
          contractRowVersion: detail.contractRowVersion,
          versionRowVersion: detail.versionRowVersion,
          milestoneRowVersion: milestone.rowVersion,
          paymentRowVersion: milestone.activePayment.rowVersion,
          reason: reopenReason.trim(),
        },
      );
      toast.success(`Đã chuyển ${milestone.titleVi} về chưa nộp.`);
      setReopeningMilestoneId(undefined);
      setReopenReason("");
      await load(false);
      await onContractRefetch();
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể hoàn tác thanh toán."));
    } finally {
      setUpdatingMilestoneId(undefined);
    }
  };

  const openEvidence = async (fileId: number, fileName: string) => {
    const previewWindow = window.open("about:blank", "_blank");
    if (!previewWindow) {
      toast.error("Trình duyệt đã chặn tab xem chứng từ.");
      return;
    }
    previewWindow.opener = null;
    previewWindow.document.body.textContent = "Đang tải chứng từ...";
    try {
      setOpeningEvidenceFileId(fileId);
      const source = await contractCompletionApi.downloadEvidence(fileId);
      const blob = new Blob([source], { type: evidenceContentType(fileName) });
      const url = URL.createObjectURL(blob);
      previewWindow.location.replace(url);
      window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
    } catch (error) {
      previewWindow.close();
      toast.error(getApiErrorMessage(error, "Không thể mở chứng từ."));
    } finally {
      setOpeningEvidenceFileId(undefined);
    }
  };

  if (loading || !detail)
    return (
      <Card>
        <CardContent className="flex min-h-48 items-center justify-center">
          <Loader2 className="size-6 animate-spin" />
        </CardContent>
      </Card>
    );
  const acceptanceEditable =
    canManageAcceptance && contract.status === ContractStatus.Signed;
  const paymentEditable =
    canManagePayment && contract.status === ContractStatus.Signed;
  const hasStructuredPayment = detail.paymentMilestones.length > 0;
  const completingMilestone = detail.paymentMilestones.find(
    (item) => item.paymentMilestoneId === completingMilestoneId,
  );
  const reopeningMilestone = detail.paymentMilestones.find(
    (item) => item.paymentMilestoneId === reopeningMilestoneId,
  );
  const checks = [
    detail.readiness.signed,
    detail.readiness.acceptanceEvidenceAvailable,
    detail.readiness.remainingAmount === 0,
  ];
  const completed = checks.filter(Boolean).length;

  return (
    <div className="space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Checklist hoàn tất hợp đồng</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex justify-between text-sm">
            <span>Tiến độ</span>
            <b>{completed}/3</b>
          </div>
          <Progress value={(completed / 3) * 100} />
          {[
            ["Hợp đồng có bản scan đã ký hợp lệ", checks[0]],
            ["Đã tải biên bản nghiệm thu", checks[1]],
            ["Đã thanh toán đủ giá trị hợp đồng", checks[2]],
          ].map(([label, ok]) => (
            <div
              key={String(label)}
              className="flex items-center gap-3 rounded-lg border p-3"
            >
              <CheckCircle2
                className={
                  ok
                    ? "size-5 text-emerald-600"
                    : "size-5 text-muted-foreground"
                }
              />
              <span className="text-sm">{label}</span>
            </div>
          ))}
          {detail.readiness.blockers.length > 0 && (
            <Alert>
              <AlertTitle>Chưa thể hoàn tất</AlertTitle>
              <AlertDescription>
                <ul className="list-disc pl-5">
                  {detail.readiness.blockers.map((x) => (
                    <li key={x.code}>{x.message}</li>
                  ))}
                </ul>
              </AlertDescription>
            </Alert>
          )}
          <div className="grid gap-3 sm:grid-cols-3">
            <div className="rounded-lg bg-muted p-3">
              <p className="text-xs text-muted-foreground">Giá trị</p>
              <b>
                {formatCurrency(
                  detail.readiness.totalAmount,
                  detail.readiness.currencyCode,
                )}
              </b>
            </div>
            <div className="rounded-lg bg-muted p-3">
              <p className="text-xs text-muted-foreground">Đã thanh toán</p>
              <b className="text-emerald-600">
                {formatCurrency(
                  detail.readiness.paidAmount,
                  detail.readiness.currencyCode,
                )}
              </b>
            </div>
            <div className="rounded-lg bg-muted p-3">
              <p className="text-xs text-muted-foreground">Còn lại</p>
              <b className="text-amber-600">
                {formatCurrency(
                  detail.readiness.remainingAmount,
                  detail.readiness.currencyCode,
                )}
              </b>
            </div>
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <FileCheck2 className="size-5" />
            Biên bản nghiệm thu
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-3">
          {detail.acceptanceEvidence ? (
            <div className="rounded-lg border p-3 text-sm">
              <b>{detail.acceptanceEvidence.fileName}</b>
              <p className="text-muted-foreground">
                Tải bởi{" "}
                {detail.acceptanceEvidence.uploadedByEmployeeName ??
                  `#${detail.acceptanceEvidence.uploadedByEmployeeId}`}{" "}
                ·{" "}
                {new Date(detail.acceptanceEvidence.uploadedAt).toLocaleString(
                  "vi-VN",
                )}
              </p>
            </div>
          ) : acceptanceEditable ? (
            <div className="flex flex-col gap-3 sm:flex-row">
              <Input
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                onChange={(e) => setAcceptanceFile(e.target.files?.[0])}
              />
              <Button
                disabled={!acceptanceFile || busy}
                onClick={() =>
                  acceptanceFile &&
                  mutate(
                    () =>
                      contractCompletionApi.uploadAcceptance(
                        contract.contractId,
                        {
                          file: acceptanceFile,
                          currentVersionId: detail.versionId,
                          contractRowVersion: detail.contractRowVersion,
                          versionRowVersion: detail.versionRowVersion,
                        },
                      ),
                    "Đã tải biên bản nghiệm thu.",
                  )
                }
              >
                Tải lên
              </Button>
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">
              Chưa có biên bản nghiệm thu.
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <WalletCards className="size-5" />
            Thanh toán
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-5">
          {hasStructuredPayment ? (
            <div className="grid gap-3 md:grid-cols-2">
              {detail.paymentMilestones.map((milestone) => {
                const isPaid =
                  milestone.paymentStatus ===
                  ContractPaymentMilestoneStatus.Paid;
                const isUpdating =
                  updatingMilestoneId === milestone.paymentMilestoneId;
                return (
                  <div
                    key={milestone.paymentMilestoneId}
                    className="flex flex-col rounded-lg border p-4 text-sm"
                  >
                    <div className="flex flex-wrap items-start justify-between gap-2">
                      <div>
                        <b>
                          {milestone.titleVi} · {milestone.paymentPercent}%
                        </b>
                        {milestone.titleEn && (
                          <p className="text-muted-foreground">
                            {milestone.titleEn}
                          </p>
                        )}
                      </div>
                      <div className="flex flex-wrap justify-end gap-2">
                        <Badge variant={isPaid ? "default" : "secondary"}>
                          {isPaid ? "Đã nộp" : "Chưa nộp"}
                        </Badge>
                        {milestone.isOverdue && (
                          <Badge variant="destructive">
                            <AlertTriangle className="mr-1 size-3" />
                            Quá hạn
                          </Badge>
                        )}
                      </div>
                    </div>

                    <div className="mt-3 space-y-1 text-muted-foreground">
                      <p>
                        Số tiền dự kiến:{" "}
                        <span className="font-medium text-foreground">
                          {formatCurrency(
                            milestone.amount,
                            detail.readiness.currencyCode,
                          )}
                        </span>
                      </p>
                      <p>
                        {milestone.dueDate
                          ? `Hạn ${new Date(milestone.dueDate).toLocaleDateString("vi-VN")}`
                          : "Chưa phát sinh mốc tính hạn"}
                      </p>
                      {milestone.conditionVi && <p>{milestone.conditionVi}</p>}
                      {milestone.dayCountMode ===
                        ContractPaymentDayCountMode.BusinessDays && (
                        <p className="text-xs">
                          Ngày làm việc hiện chỉ loại trừ thứ Bảy và Chủ nhật.
                        </p>
                      )}
                      {isPaid && milestone.activePayment ? (
                        <div className="mt-3 space-y-1 rounded-md border bg-muted/40 p-3 text-xs">
                          <p>
                            Thanh toán ngày{" "}
                            {new Date(
                              milestone.activePayment.paymentDate,
                            ).toLocaleDateString("vi-VN")}
                          </p>
                          <p>
                            {milestone.activePayment.paymentMethod} ·{" "}
                            {milestone.activePayment.referenceCode}
                          </p>
                          <p>
                            Ghi nhận bởi{" "}
                            {milestone.activePayment.createdByEmployeeName ??
                              `#${milestone.activePayment.createdByEmployeeId}`}{" "}
                            lúc{" "}
                            {formatDateTime(milestone.activePayment.createdAt)}
                          </p>
                        </div>
                      ) : isPaid ? (
                        <p className="mt-3 text-xs text-destructive">
                          Dữ liệu không hợp lệ: thiếu khoản thanh toán hoặc
                          chứng từ hiệu lực.
                        </p>
                      ) : null}
                    </div>

                    <div className="mt-4 flex flex-wrap gap-2">
                      {isPaid && milestone.activePayment?.evidenceFileId && (
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          disabled={
                            openingEvidenceFileId ===
                            milestone.activePayment.evidenceFileId
                          }
                          onClick={() =>
                            void openEvidence(
                              milestone.activePayment!.evidenceFileId!,
                              milestone.activePayment!.evidenceFileName ??
                                "chung-tu.pdf",
                            )
                          }
                        >
                          {openingEvidenceFileId ===
                          milestone.activePayment.evidenceFileId ? (
                            <Loader2 className="mr-2 size-4 animate-spin" />
                          ) : (
                            <Eye className="mr-2 size-4" />
                          )}
                          Xem chứng từ
                        </Button>
                      )}
                      {paymentEditable && (
                        <Button
                          type="button"
                          size="sm"
                          variant={isPaid ? "outline" : "default"}
                          disabled={
                            Boolean(updatingMilestoneId) ||
                            busy ||
                            (isPaid && !milestone.activePayment)
                          }
                          onClick={() => {
                            if (isPaid) {
                              setReopeningMilestoneId(
                                milestone.paymentMilestoneId,
                              );
                              setReopenReason("");
                              return;
                            }
                            openCompleteMilestone(milestone.paymentMilestoneId);
                          }}
                        >
                          {isUpdating ? (
                            <Loader2 className="mr-2 size-4 animate-spin" />
                          ) : isPaid ? (
                            <RotateCcw className="mr-2 size-4" />
                          ) : (
                            <CheckCircle2 className="mr-2 size-4" />
                          )}
                          {isPaid ? "Chuyển về chưa nộp" : "Đánh dấu đã nộp"}
                        </Button>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            <>
              {paymentEditable && (
                <div className="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <Label>
                      Ngày thanh toán{" "}
                      <span className="text-destructive">*</span>
                    </Label>
                    <DateFilter
                      date={paymentDate}
                      onChange={setPaymentDate}
                      placeholder="Chọn ngày"
                      className="flex-1"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>
                      Số tiền <span className="text-destructive">*</span>
                    </Label>
                    <DecimalInput
                      value={amount}
                      onValueChange={setAmount}
                      max={detail.readiness.remainingAmount}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>
                      Phương thức <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      value={method}
                      onChange={(e) => setMethod(e.target.value)}
                      placeholder="Chuyển khoản"
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>
                      Mã tham chiếu <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      value={reference}
                      onChange={(e) => setReference(e.target.value)}
                    />
                  </div>
                  <div className="space-y-2 md:col-span-2">
                    <Label>Chứng từ (không bắt buộc)</Label>
                    <Input
                      type="file"
                      accept=".pdf,.jpg,.jpeg,.png"
                      onChange={(e) => setPaymentFile(e.target.files?.[0])}
                    />
                  </div>
                  <Button
                    className="md:col-span-2 md:justify-self-start"
                    disabled={
                      !paymentDate ||
                      amount <= 0 ||
                      !method.trim() ||
                      !reference.trim() ||
                      busy
                    }
                    onClick={() =>
                      paymentDate &&
                      mutate(
                        () =>
                          contractCompletionApi.addPayment(
                            contract.contractId,
                            {
                              evidenceFile: paymentFile,
                              currentVersionId: detail.versionId,
                              contractRowVersion: detail.contractRowVersion,
                              versionRowVersion: detail.versionRowVersion,
                              paymentDate: format(paymentDate, "yyyy-MM-dd"),
                              amount,
                              currencyCode: detail.readiness.currencyCode,
                              paymentMethod: method,
                              referenceCode: reference,
                            },
                          ),
                        "Đã ghi nhận khoản thanh toán.",
                      )
                    }
                  >
                    Thêm khoản thanh toán
                  </Button>
                </div>
              )}
              <div className="space-y-3">
                {detail.payments.length === 0 ? (
                  <p className="text-sm text-muted-foreground">
                    Chưa có khoản thanh toán.
                  </p>
                ) : (
                  detail.payments.map((payment) => (
                    <div
                      key={payment.contractPaymentId}
                      className="rounded-lg border p-4"
                    >
                      <div className="flex flex-wrap items-start justify-between gap-3">
                        <div>
                          <b>
                            {formatCurrency(
                              payment.amount,
                              payment.currencyCode,
                            )}
                          </b>
                          <p className="text-sm text-muted-foreground">
                            {new Date(payment.paymentDate).toLocaleDateString(
                              "vi-VN",
                            )}{" "}
                            · {payment.paymentMethod} · {payment.referenceCode}
                          </p>
                        </div>
                        <Badge
                          variant={
                            payment.status === ContractPaymentStatus.Active
                              ? "default"
                              : "destructive"
                          }
                        >
                          {payment.status === ContractPaymentStatus.Active
                            ? "Hiệu lực"
                            : "Đã hủy"}
                        </Badge>
                      </div>
                      {payment.status === ContractPaymentStatus.Active &&
                        paymentEditable && (
                          <Button
                            className="mt-3"
                            size="sm"
                            variant="outline"
                            onClick={() =>
                              setVoidingId(payment.contractPaymentId)
                            }
                          >
                            Hủy khoản này
                          </Button>
                        )}
                      {payment.voidReason && (
                        <p className="mt-2 text-sm text-destructive">
                          Lý do: {payment.voidReason}
                        </p>
                      )}
                    </div>
                  ))
                )}
              </div>
            </>
          )}
        </CardContent>
      </Card>
      <Card className="border-primary/20 bg-primary/5">
        <CardContent className="flex flex-col gap-4 p-5 sm:flex-row sm:items-center sm:justify-between sm:p-6">
          <div className="flex min-w-0 items-center gap-3">
            <div className="rounded-xl bg-primary/10 p-2.5 text-primary">
              <CheckCircle2 className="size-5" />
            </div>
            <div>
              <p className="font-semibold text-foreground">
                {contract.status === ContractStatus.Completed
                  ? "Hợp đồng đã được hoàn thành!"
                  : !canComplete
                    ? "Bạn không có quyền thực hiện thao tác hoàn tất hợp đồng."
                    : contract.status !== ContractStatus.Signed
                      ? "Hợp đồng cần ở trạng thái Đã ký để có thể hoàn tất."
                      : detail.readiness.ready
                        ? "Tất cả điều kiện đã được thỏa mãn. Bạn có thể xác nhận đóng hợp đồng ngay."
                        : "Cần hoàn tất đủ 3 điều kiện (chữ ký scan, nghiệm thu, thanh toán) để đóng hợp đồng."}
              </p>
            </div>
          </div>
          <div className="flex shrink-0 items-center">
            {contract.status === ContractStatus.Completed ? (
              <Badge className="bg-emerald-600 px-3 py-1.5 text-sm font-medium">
                <CheckCircle2 className="mr-1.5 size-4" />
                Hợp đồng đã hoàn tất
              </Badge>
            ) : (
              <Button
                size="lg"
                disabled={
                  !canComplete ||
                  contract.status !== ContractStatus.Signed ||
                  !detail.readiness.ready ||
                  busy
                }
                onClick={() => setConfirmComplete(true)}
                className="gap-2 shadow-sm"
              >
                <CheckCircle2 className="size-4" />
                Đánh dấu hoàn tất
              </Button>
            )}
          </div>
        </CardContent>
      </Card>

      <Dialog
        open={Boolean(completingMilestone)}
        onOpenChange={(open) => !open && resetCompleteMilestone()}
      >
        <DialogContent className="sm:max-w-xl">
          <DialogHeader>
            <DialogTitle>Hoàn tất đợt thanh toán</DialogTitle>
            <DialogDescription>
              {completingMilestone?.titleVi}. Số tiền được lấy cố định từ đợt
              thanh toán và không thể chỉnh sửa.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="milestone-payment-date">
                Ngày thanh toán <span className="text-destructive">*</span>
              </Label>
              <DateFilter
                id="milestone-payment-date"
                date={milestonePaymentDate}
                onChange={setMilestonePaymentDate}
                placeholder="Chọn ngày thanh toán"
                className="flex-1"
              />
            </div>
            <div className="space-y-2">
              <Label>Số tiền</Label>
              <Input
                disabled
                value={
                  completingMilestone
                    ? formatCurrency(
                        completingMilestone.amount,
                        detail.readiness.currencyCode,
                      )
                    : ""
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="milestone-payment-method">
                Phương thức thanh toán{" "}
                <span className="text-destructive">*</span>
              </Label>
              <Input
                id="milestone-payment-method"
                maxLength={100}
                value={milestonePaymentMethod}
                onChange={(event) =>
                  setMilestonePaymentMethod(event.target.value)
                }
                placeholder="Chuyển khoản"
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="milestone-reference">
                Mã giao dịch <span className="text-destructive">*</span>
              </Label>
              <Input
                id="milestone-reference"
                maxLength={100}
                value={milestoneReference}
                onChange={(event) => setMilestoneReference(event.target.value)}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="milestone-evidence">
                Chứng từ <span className="text-destructive">*</span>
              </Label>
              <Input
                id="milestone-evidence"
                type="file"
                accept=".pdf,.jpg,.jpeg,.png"
                onChange={(event) => {
                  const file = event.target.files?.[0];
                  if (!file) {
                    setMilestoneEvidence(undefined);
                    return;
                  }
                  const extension = file.name.split(".").pop()?.toLowerCase();
                  if (
                    !extension ||
                    !EVIDENCE_EXTENSIONS.includes(extension) ||
                    file.size <= 0 ||
                    file.size > MAX_EVIDENCE_SIZE
                  ) {
                    event.target.value = "";
                    setMilestoneEvidence(undefined);
                    toast.error(
                      "Chứng từ phải là PDF/JPG/JPEG/PNG và không quá 20 MiB.",
                    );
                    return;
                  }
                  setMilestoneEvidence(file);
                }}
              />
              <p className="text-xs text-muted-foreground">
                PDF, JPG, JPEG hoặc PNG; tối đa 20 MiB.
              </p>
            </div>
            <label className="flex items-start gap-2 text-sm sm:col-span-2">
              <Checkbox
                checked={evidenceConfirmed}
                onCheckedChange={(checked) =>
                  setEvidenceConfirmed(checked === true)
                }
              />
              <span>Tôi xác nhận chứng từ thuộc đúng đợt thanh toán này.</span>
            </label>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              disabled={Boolean(updatingMilestoneId)}
              onClick={resetCompleteMilestone}
            >
              Hủy
            </Button>
            <Button
              disabled={
                Boolean(updatingMilestoneId) ||
                !milestonePaymentDate ||
                !milestonePaymentMethod.trim() ||
                !milestoneReference.trim() ||
                !milestoneEvidence ||
                !evidenceConfirmed
              }
              onClick={() => void completeMilestone()}
            >
              {updatingMilestoneId === completingMilestoneId && (
                <Loader2 className="mr-2 size-4 animate-spin" />
              )}
              Hoàn tất đợt
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <Dialog
        open={Boolean(reopeningMilestone)}
        onOpenChange={(open) => {
          if (!open) {
            setReopeningMilestoneId(undefined);
            setReopenReason("");
          }
        }}
      >
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>Chuyển về chưa nộp?</DialogTitle>
            <DialogDescription>
              Khoản thanh toán sẽ chuyển sang trạng thái đã hủy. Chứng từ và
              lịch sử audit vẫn được giữ nguyên.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor="reopen-payment-reason">Lý do hoàn tác *</Label>
            <Textarea
              id="reopen-payment-reason"
              maxLength={1000}
              value={reopenReason}
              onChange={(event) => setReopenReason(event.target.value)}
              placeholder="Nhập lý do chuyển đợt thanh toán về chưa nộp..."
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              disabled={Boolean(updatingMilestoneId)}
              onClick={() => {
                setReopeningMilestoneId(undefined);
                setReopenReason("");
              }}
            >
              Hủy
            </Button>
            <Button
              variant="destructive"
              disabled={Boolean(updatingMilestoneId) || !reopenReason.trim()}
              onClick={() => void reopenMilestone()}
            >
              {updatingMilestoneId === reopeningMilestoneId && (
                <Loader2 className="mr-2 size-4 animate-spin" />
              )}
              Chuyển về chưa nộp
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
      <ConfirmDialog
        isOpen={!hasStructuredPayment && Boolean(voidingId)}
        onClose={() => {
          setVoidingId(undefined);
          setVoidReason("");
        }}
        onConfirm={() => {
          const payment = detail.payments.find(
            (x) => x.contractPaymentId === voidingId,
          );
          if (!payment || !voidReason.trim()) return;
          void mutate(
            () =>
              contractCompletionApi.voidPayment(
                contract.contractId,
                payment.contractPaymentId,
                {
                  contractRowVersion: detail.contractRowVersion,
                  versionRowVersion: detail.versionRowVersion,
                  paymentRowVersion: payment.rowVersion,
                  reason: voidReason,
                },
              ),
            "Đã hủy khoản thanh toán.",
          ).then(() => {
            setVoidingId(undefined);
            setVoidReason("");
          });
        }}
        title="Hủy khoản thanh toán"
        description={
          <div className="space-y-2">
            <p>Khoản bị hủy vẫn được giữ lại để audit.</p>
            <Textarea
              value={voidReason}
              onChange={(e) => setVoidReason(e.target.value)}
              placeholder="Nhập lý do bắt buộc"
            />
          </div>
        }
        confirmText="Hủy khoản"
        variant="destructive"
        isLoading={busy}
      />
      <ConfirmDialog
        isOpen={confirmComplete}
        onClose={() => setConfirmComplete(false)}
        onConfirm={() =>
          void mutate(
            () =>
              contractCompletionApi.complete(contract.contractId, {
                currentVersionId: detail.versionId,
                contractRowVersion: detail.contractRowVersion,
                versionRowVersion: detail.versionRowVersion,
              }),
            "Hợp đồng đã hoàn tất.",
          ).then(() => setConfirmComplete(false))
        }
        title="Hoàn tất hợp đồng?"
        description="Sau khi hoàn tất, hồ sơ nghiệm thu và thanh toán sẽ không thể thay đổi."
        confirmText="Hoàn tất"
        isLoading={busy}
      />
    </div>
  );
}
