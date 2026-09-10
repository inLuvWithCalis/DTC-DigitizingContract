"use client";

import { useCallback, useEffect, useState } from "react";
import { format } from "date-fns";
import {
  CalendarClock,
  CheckCircle2,
  FileCheck2,
  Loader2,
  WalletCards,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
  ContractPaymentDueAnchor,
  ContractPaymentStatus,
} from "@/services/contract-completion-api";

interface Props {
  contract: ContractDetailResponse;
  canManageAcceptance: boolean;
  canManagePayment: boolean;
  canComplete: boolean;
  onContractRefetch: () => void | Promise<void>;
}

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
  const [paymentMilestoneId, setPaymentMilestoneId] = useState<number>();
  const [manualMilestoneId, setManualMilestoneId] = useState<number>();
  const [manualAnchorDate, setManualAnchorDate] = useState<Date>();
  const [manualAnchorReason, setManualAnchorReason] = useState("");
  const [method, setMethod] = useState("");
  const [reference, setReference] = useState("");
  const [paymentFile, setPaymentFile] = useState<File>();
  const [voidingId, setVoidingId] = useState<number>();
  const [voidReason, setVoidReason] = useState("");
  const [confirmComplete, setConfirmComplete] = useState(false);

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setDetail(await contractCompletionApi.get(contract.contractId));
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Không thể tải hồ sơ hoàn tất."));
    } finally {
      setLoading(false);
    }
  }, [contract.contractId]);
  useEffect(() => {
    void load();
  }, [load]);

  const mutate = async (action: () => Promise<unknown>, success: string) => {
    try {
      setBusy(true);
      await action();
      toast.success(success);
      await load();
      await onContractRefetch();
    } catch (error) {
      toast.error(getApiErrorMessage(error, "Thao tác không thành công."));
    } finally {
      setBusy(false);
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
  const checks = [
    detail.readiness.signed,
    detail.readiness.acceptanceEvidenceAvailable,
    detail.readiness.remainingAmount === 0,
  ];
  const completed = checks.filter(Boolean).length;
  const selectedPaymentMilestone = detail.paymentMilestones.find(
    (item) => item.paymentMilestoneId === paymentMilestoneId,
  );
  const manualMilestone = detail.paymentMilestones.find(
    (item) => item.paymentMilestoneId === manualMilestoneId,
  );

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
          {canComplete && contract.status === ContractStatus.Signed && (
            <Button
              disabled={!detail.readiness.ready || busy}
              onClick={() => setConfirmComplete(true)}
            >
              Đánh dấu hoàn tất
            </Button>
          )}
          {contract.status === ContractStatus.Completed && (
            <Badge className="bg-emerald-600">Hợp đồng đã hoàn tất</Badge>
          )}
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
          {detail.paymentMilestones?.length > 0 && (
            <div className="grid gap-3 md:grid-cols-2">
              {detail.paymentMilestones.map((milestone) => (
                <div
                  key={milestone.paymentMilestoneId}
                  className="rounded-lg border p-3 text-sm"
                >
                  <div className="flex justify-between gap-2">
                    <b>
                      {milestone.titleVi} · {milestone.paymentPercent}%
                    </b>
                    <Badge
                      variant={
                        milestone.status === "Paid"
                          ? "default"
                          : milestone.status === "Overdue"
                            ? "destructive"
                            : "secondary"
                      }
                    >
                      {milestone.status}
                    </Badge>
                  </div>
                  <p className="mt-1 text-muted-foreground">
                    {formatCurrency(
                      milestone.paidAmount,
                      detail.readiness.currencyCode,
                    )}{" "}
                    /{` `}
                    {formatCurrency(
                      milestone.amount,
                      detail.readiness.currencyCode,
                    )}
                  </p>
                  <p className="text-muted-foreground">
                    {milestone.dueDate
                      ? `Hạn ${new Date(milestone.dueDate).toLocaleDateString("vi-VN")}`
                      : "Chưa phát sinh mốc tính hạn"}
                  </p>
                  {milestone.conditionVi && (
                    <p className="mt-1 text-muted-foreground">
                      {milestone.conditionVi}
                    </p>
                  )}
                  {milestone.dayCountMode ===
                    ContractPaymentDayCountMode.BusinessDays && (
                    <p className="mt-1 text-xs text-muted-foreground">
                      Ngày làm việc hiện chỉ loại trừ thứ Bảy và Chủ nhật.
                    </p>
                  )}
                  {paymentEditable &&
                    milestone.dueAnchor === ContractPaymentDueAnchor.Manual && (
                      <Button
                        type="button"
                        variant="outline"
                        size="sm"
                        className="mt-3"
                        onClick={() => {
                          setManualMilestoneId(milestone.paymentMilestoneId);
                          setManualAnchorDate(
                            milestone.anchorDate
                              ? new Date(milestone.anchorDate)
                              : undefined,
                          );
                          setManualAnchorReason("");
                        }}
                      >
                        <CalendarClock className="mr-2 size-4" />
                        {milestone.anchorDate
                          ? "Đổi ngày kích hoạt"
                          : "Đặt ngày kích hoạt"}
                      </Button>
                    )}
                </div>
              ))}
            </div>
          )}
          {paymentEditable && manualMilestone && (
            <div className="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
              <div className="space-y-2">
                <Label>Ngày kích hoạt thủ công</Label>
                <DateFilter
                  date={manualAnchorDate}
                  onChange={setManualAnchorDate}
                  placeholder="Chọn ngày"
                />
              </div>
              <div className="space-y-2 md:col-span-2">
                <Label>Lý do thiết lập</Label>
                <Textarea
                  value={manualAnchorReason}
                  onChange={(event) => setManualAnchorReason(event.target.value)}
                  maxLength={1000}
                  placeholder="Ghi lại sự kiện hoặc chứng từ làm mốc tính hạn"
                />
              </div>
              <div className="flex gap-2 md:col-span-2">
                <Button
                  type="button"
                  disabled={!manualAnchorDate || !manualAnchorReason.trim() || busy}
                  onClick={() =>
                    manualAnchorDate &&
                    mutate(
                      () =>
                        contractCompletionApi.setPaymentMilestoneManualAnchor(
                          contract.contractId,
                          detail.versionId,
                          manualMilestone.paymentMilestoneId,
                          {
                            currentVersionId: detail.versionId,
                            contractRowVersion: detail.contractRowVersion,
                            versionRowVersion: detail.versionRowVersion,
                            milestoneRowVersion: manualMilestone.rowVersion,
                            anchorDate: format(manualAnchorDate, "yyyy-MM-dd"),
                            reason: manualAnchorReason,
                          },
                        ),
                      "Đã thiết lập ngày kích hoạt đợt thanh toán.",
                    )
                  }
                >
                  Lưu ngày kích hoạt
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setManualMilestoneId(undefined)}
                >
                  Hủy
                </Button>
              </div>
            </div>
          )}
          {paymentEditable && (
            <div className="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
              {detail.paymentMilestones?.length > 0 && <div className="space-y-2 md:col-span-2"><Label>Đợt thanh toán <span className="text-destructive">*</span></Label><select className="h-9 w-full rounded-md border bg-background px-3 text-sm" value={paymentMilestoneId ?? ""} onChange={(e) => { const id = Number(e.target.value); setPaymentMilestoneId(id || undefined); const selected = detail.paymentMilestones.find((item) => item.paymentMilestoneId === id); if (selected) setAmount(selected.remainingAmount); }}><option value="">Chọn đợt thanh toán</option>{detail.paymentMilestones.filter((item) => item.remainingAmount > 0).map((item) => <option key={item.paymentMilestoneId} value={item.paymentMilestoneId}>{item.titleVi} — còn {formatCurrency(item.remainingAmount, detail.readiness.currencyCode)}</option>)}</select></div>}
              <div className="space-y-2">
                <Label>
                  Ngày thanh toán <span className="text-destructive">*</span>
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
                  max={
                    selectedPaymentMilestone?.remainingAmount ??
                    detail.readiness.remainingAmount
                  }
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
                  (detail.paymentMilestones?.length > 0 && !paymentMilestoneId) ||
                  amount <= 0 ||
                  !method.trim() ||
                  !reference.trim() ||
                  busy
                }
                onClick={() =>
                  paymentDate &&
                  mutate(
                    () =>
                      contractCompletionApi.addPayment(contract.contractId, {
                        evidenceFile: paymentFile,
                        currentVersionId: detail.versionId,
                        contractRowVersion: detail.contractRowVersion,
                        versionRowVersion: detail.versionRowVersion,
                        paymentMilestoneId,
                        paymentDate: format(paymentDate, "yyyy-MM-dd"),
                        amount,
                        currencyCode: detail.readiness.currencyCode,
                        paymentMethod: method,
                        referenceCode: reference,
                      }),
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
                        {formatCurrency(payment.amount, payment.currencyCode)}
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
                        onClick={() => setVoidingId(payment.contractPaymentId)}
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
        </CardContent>
      </Card>

      <ConfirmDialog
        isOpen={Boolean(voidingId)}
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
