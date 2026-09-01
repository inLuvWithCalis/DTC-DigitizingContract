"use client";

import { useCallback, useEffect, useState } from "react";
import { CheckCircle2, FileCheck2, Loader2, WalletCards } from "lucide-react";
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
  ContractPaymentStatus,
} from "@/services/contract-completion-api";

interface Props {
  contract: ContractDetailResponse;
  canManage: boolean;
  canComplete: boolean;
  onContractRefetch: () => void | Promise<void>;
}

export function ContractClosing({
  contract,
  canManage,
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
  const editable = canManage && contract.status === ContractStatus.Signed;
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
          ) : editable ? (
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
          {editable && (
            <div className="grid gap-3 rounded-lg border p-4 md:grid-cols-2">
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
                      contractCompletionApi.addPayment(contract.contractId, {
                        evidenceFile: paymentFile,
                        currentVersionId: detail.versionId,
                        contractRowVersion: detail.contractRowVersion,
                        versionRowVersion: detail.versionRowVersion,
                        paymentDate: paymentDate.toISOString(),
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
                    editable && (
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
