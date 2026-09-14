"use client";

import { useEffect, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  Loader2,
  Plus,
  Save,
  Trash2,
  X,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import {
  contractTemplateApi,
  PaymentDayCountMode,
  PaymentDueAnchor,
  type ContractTemplatePaymentMilestoneResponse,
  type ContractTemplateTermResponse,
  type ContractTemplateVersionDetailResponse,
  type SaveContractTemplatePaymentMilestoneRequest,
} from "@/services/contract-template-api";
import { getContractTemplateErrorMessage } from "./contract-template-utils";

interface Props {
  version: ContractTemplateVersionDetailResponse;
  term: ContractTemplateTermResponse;
  isBilingual: boolean;
  editable: boolean;
  onRefresh: () => Promise<void>;
}

type Draft = Omit<
  SaveContractTemplatePaymentMilestoneRequest,
  "versionRowVersion"
>;

const emptyDraft = (order: number): Draft => ({
  milestoneCode: `PAYMENT_${order}`,
  titleVi: `Đợt ${order}`,
  titleEn: "",
  paymentPercent: 0,
  dueAnchor:
    order === 1
      ? PaymentDueAnchor.ContractSigned
      : PaymentDueAnchor.PreviousMilestonePaid,
  dueOffsetDays: 0,
  dayCountMode: PaymentDayCountMode.CalendarDays,
  conditionVi: "",
  conditionEn: "",
  displayOrder: order,
});

const nextDraft = (rows: ContractTemplatePaymentMilestoneResponse[]): Draft => {
  const order = Math.max(0, ...rows.map((row) => row.displayOrder)) + 1;
  const existingCodes = new Set(
    rows.map((row) => row.milestoneCode.toUpperCase()),
  );
  let codeIndex = 1;
  while (existingCodes.has(`PAYMENT_${codeIndex}`)) codeIndex += 1;
  return {
    ...emptyDraft(order),
    milestoneCode: `PAYMENT_${codeIndex}`,
    titleVi: `Đợt ${rows.length + 1}`,
  };
};

const fromRow = (row: ContractTemplatePaymentMilestoneResponse): Draft => ({
  milestoneCode: row.milestoneCode,
  titleVi: row.titleVi,
  titleEn: row.titleEn ?? "",
  paymentPercent: row.paymentPercent,
  dueAnchor: row.dueAnchor,
  dueOffsetDays: row.dueOffsetDays,
  dayCountMode: row.dayCountMode,
  conditionVi: row.conditionVi ?? "",
  conditionEn: row.conditionEn ?? "",
  displayOrder: row.displayOrder,
});

function Fields({
  value,
  onChange,
  isBilingual,
  disabled = false,
}: {
  value: Draft;
  onChange: (next: Draft) => void;
  isBilingual: boolean;
  disabled?: boolean;
}) {
  const set = <K extends keyof Draft>(key: K, next: Draft[K]) =>
    onChange({ ...value, [key]: next });
  return (
    <div className="grid gap-3 md:grid-cols-2">
      <div className="space-y-1.5">
        <Label>Mã đợt</Label>
        <Input
          value={value.milestoneCode}
          disabled={disabled}
          onChange={(e) => set("milestoneCode", e.target.value.toUpperCase())}
        />
      </div>
      <div className="space-y-1.5">
        <Label>Tên đợt</Label>
        <Input
          value={value.titleVi}
          disabled={disabled}
          onChange={(e) => set("titleVi", e.target.value)}
        />
      </div>
      <div className="space-y-1.5">
        <Label>Tỷ lệ (%)</Label>
        <Input
          type="number"
          min={0.0001}
          max={100}
          step="0.0001"
          value={value.paymentPercent}
          disabled={disabled}
          onChange={(e) => set("paymentPercent", Number(e.target.value))}
        />
      </div>
      <div className="space-y-1.5">
        <Label>Thứ tự</Label>
        <Input type="number" value={value.displayOrder} disabled />
      </div>
      <div className="space-y-1.5">
        <Label>Mốc bắt đầu tính hạn</Label>
        <Select
          value={String(value.dueAnchor)}
          disabled={disabled}
          onValueChange={(val) =>
            set("dueAnchor", Number(val) as PaymentDueAnchor)
          }
        >
          <SelectTrigger className="h-9 w-full">
            <SelectValue placeholder="Chọn mốc bắt đầu" />
          </SelectTrigger>
          <SelectContent showSearch={false}>
            <SelectItem value={String(PaymentDueAnchor.ContractSigned)}>
              Ký hợp đồng
            </SelectItem>
            <SelectItem value={String(PaymentDueAnchor.ContractEffectiveDate)}>
              Hợp đồng có hiệu lực
            </SelectItem>
            <SelectItem value={String(PaymentDueAnchor.AcceptanceCompleted)}>
              Hoàn tất nghiệm thu
            </SelectItem>
            <SelectItem value={String(PaymentDueAnchor.PreviousMilestonePaid)}>
              Thanh toán đủ đợt trước
            </SelectItem>
            <SelectItem value={String(PaymentDueAnchor.ManualDate)}>
              Lịch thủ công (Tự điền ngày)
            </SelectItem>
          </SelectContent>
        </Select>
      </div>
      <div className="grid grid-cols-2 gap-2">
        <div className="space-y-1.5">
          <Label>Số ngày</Label>
          <Input
            type="number"
            min={0}
            value={value.dueOffsetDays}
            disabled={disabled}
            onChange={(e) => set("dueOffsetDays", Number(e.target.value))}
          />
        </div>
        <div className="space-y-1.5">
          <Label>Cách đếm</Label>
          <Select
            value={String(value.dayCountMode)}
            disabled={disabled}
            onValueChange={(val) =>
              set("dayCountMode", Number(val) as PaymentDayCountMode)
            }
          >
            <SelectTrigger className="h-9 w-full">
              <SelectValue placeholder="Chọn cách đếm" />
            </SelectTrigger>
            <SelectContent showSearch={false}>
              <SelectItem value={String(PaymentDayCountMode.CalendarDays)}>
                Ngày lịch
              </SelectItem>
              <SelectItem value={String(PaymentDayCountMode.BusinessDays)}>
                Ngày làm việc
              </SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>
      <div className="space-y-1.5 md:col-span-2">
        <Label>Điều kiện bổ sung</Label>
        <Textarea
          value={value.conditionVi ?? ""}
          disabled={disabled}
          onChange={(e) => set("conditionVi", e.target.value)}
        />
      </div>
      {isBilingual && (
        <>
          <div className="space-y-1.5">
            <Label>Tên đợt tiếng Anh</Label>
            <Input
              value={value.titleEn ?? ""}
              disabled={disabled}
              onChange={(e) => set("titleEn", e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label>Điều kiện tiếng Anh</Label>
            <Input
              value={value.conditionEn ?? ""}
              disabled={disabled}
              onChange={(e) => set("conditionEn", e.target.value)}
            />
          </div>
        </>
      )}
      <p className="rounded-md bg-muted/60 p-2 text-sm md:col-span-2">
        Dữ liệu này chỉ dùng để theo dõi thanh toán và không tự thay đổi nội
        dung điều khoản.
        {value.dueAnchor === PaymentDueAnchor.ManualDate && (
          <> Ngày bắt đầu tính hạn sẽ được nhập riêng khi tạo hợp đồng.</>
        )}
      </p>
    </div>
  );
}

export function ContractTemplatePaymentMilestonesEditor({
  version,
  term,
  isBilingual,
  editable,
  onRefresh,
}: Props) {
  const rows = [...(term.paymentMilestones ?? [])].sort(
    (a, b) => a.displayOrder - b.displayOrder,
  );
  const [drafts, setDrafts] = useState<Record<number, Draft>>({});
  const [adding, setAdding] = useState(false);
  const [newDraft, setNewDraft] = useState<Draft>(() => nextDraft(rows));
  const [busy, setBusy] = useState<number | "new" | "order" | null>(null);
  useEffect(() => {
    setDrafts(
      Object.fromEntries(
        rows.map((row) => [row.templatePaymentMilestoneId, fromRow(row)]),
      ),
    );
    setNewDraft(nextDraft(rows));
  }, [term.paymentMilestones]);
  const total =
    Math.round(
      rows.reduce((sum, row) => sum + row.paymentPercent, 0) * 10_000,
    ) / 10_000;
  const totalValid = total === 100;
  const hasUnsavedDrafts = rows.some(
    (row) =>
      JSON.stringify(drafts[row.templatePaymentMilestoneId] ?? fromRow(row)) !==
      JSON.stringify(fromRow(row)),
  );
  const latestVersionRow = async () =>
    (await contractTemplateApi.getVersion(version.templateVersionId))
      .rowVersion;
  const payload = (draft: Draft, versionRowVersion: string) => ({
    ...draft,
    titleEn: draft.titleEn?.trim() || null,
    conditionVi: draft.conditionVi?.trim() || null,
    conditionEn: draft.conditionEn?.trim() || null,
    versionRowVersion,
  });

  const save = async (row: ContractTemplatePaymentMilestoneResponse) => {
    try {
      setBusy(row.templatePaymentMilestoneId);
      await contractTemplateApi.updatePaymentMilestone(
        version.templateVersionId,
        term.templateTermId,
        row.templatePaymentMilestoneId,
        {
          ...payload(
            drafts[row.templatePaymentMilestoneId],
            await latestVersionRow(),
          ),
          rowVersion: row.rowVersion,
        },
      );
      toast.success("Đã lưu đợt thanh toán.");
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setBusy(null);
    }
  };
  const add = async () => {
    if (
      !newDraft.milestoneCode.trim() ||
      !newDraft.titleVi.trim() ||
      newDraft.paymentPercent <= 0
    )
      return toast.error("Nhập đủ mã, tên và tỷ lệ đợt thanh toán.");
    try {
      setBusy("new");
      await contractTemplateApi.addPaymentMilestone(
        version.templateVersionId,
        term.templateTermId,
        payload(newDraft, await latestVersionRow()),
      );
      toast.success("Đã thêm đợt thanh toán.");
      setAdding(false);
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setBusy(null);
    }
  };
  const remove = async (row: ContractTemplatePaymentMilestoneResponse) => {
    try {
      setBusy(row.templatePaymentMilestoneId);
      await contractTemplateApi.deletePaymentMilestone(
        version.templateVersionId,
        term.templateTermId,
        row.templatePaymentMilestoneId,
        row.rowVersion,
        await latestVersionRow(),
      );
      toast.success("Đã xóa đợt thanh toán.");
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setBusy(null);
    }
  };
  const move = async (index: number, direction: -1 | 1) => {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= rows.length) return;
    if (hasUnsavedDrafts) {
      toast.error("Vui lòng lưu nội dung các đợt trước khi đổi thứ tự.");
      return;
    }
    const ordered = [...rows];
    [ordered[index], ordered[targetIndex]] = [
      ordered[targetIndex],
      ordered[index],
    ];
    try {
      setBusy("order");
      await contractTemplateApi.reorderPaymentMilestones(
        version.templateVersionId,
        term.templateTermId,
        await latestVersionRow(),
        ordered.map((row, orderIndex) => ({
          milestoneId: row.templatePaymentMilestoneId,
          rowVersion: row.rowVersion,
          displayOrder: orderIndex + 1,
        })),
      );
      toast.success("Đã đổi thứ tự đợt thanh toán.");
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setBusy(null);
    }
  };

  return (
    <div className="space-y-3 rounded-xl border bg-muted/20 p-3">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <p className="font-medium">Theo dõi các đợt thanh toán</p>
          <p className="text-sm text-muted-foreground">
            Tổng tỷ lệ:{" "}
            <strong
              className={totalValid ? "text-emerald-600" : "text-destructive"}
            >
              {total}%
            </strong>
          </p>
        </div>
        {editable && (
          <Button size="sm" variant="outline" onClick={() => setAdding(true)}>
            <Plus className="size-4" /> Thêm đợt
          </Button>
        )}
      </div>
      {!totalValid && (
        <Alert variant="destructive">
          <AlertTitle>Chưa thể phát hành</AlertTitle>
          <AlertDescription>
            Tổng tỷ lệ các đợt phải đúng 100%.
          </AlertDescription>
        </Alert>
      )}
      {rows.map((row, index) => (
        <div
          key={row.templatePaymentMilestoneId}
          className="space-y-3 rounded-lg border bg-background p-3"
        >
          <Fields
            value={drafts[row.templatePaymentMilestoneId] ?? fromRow(row)}
            onChange={(next) =>
              setDrafts((current) => ({
                ...current,
                [row.templatePaymentMilestoneId]: next,
              }))
            }
            isBilingual={isBilingual}
            disabled={!editable}
          />
          <div className="flex justify-end gap-2">
            {editable && (
              <>
                <Button
                  type="button"
                  size="icon"
                  variant="outline"
                  aria-label="Chuyển đợt thanh toán lên"
                  onClick={() => move(index, -1)}
                  disabled={busy !== null || index === 0}
                >
                  <ArrowUp className="size-4" />
                </Button>
                <Button
                  type="button"
                  size="icon"
                  variant="outline"
                  aria-label="Chuyển đợt thanh toán xuống"
                  onClick={() => move(index, 1)}
                  disabled={busy !== null || index === rows.length - 1}
                >
                  <ArrowDown className="size-4" />
                </Button>
                <Button
                  size="sm"
                  variant="destructive"
                  onClick={() => remove(row)}
                  disabled={busy !== null}
                >
                  <Trash2 className="size-4" /> Xóa
                </Button>
                <Button
                  size="sm"
                  onClick={() => save(row)}
                  disabled={busy !== null}
                >
                  {busy === row.templatePaymentMilestoneId ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Save className="size-4" />
                  )}{" "}
                  Lưu đợt
                </Button>
              </>
            )}
          </div>
        </div>
      ))}
      {rows.length === 0 && !adding && (
        <p className="py-4 text-center text-sm text-muted-foreground">
          Chưa có đợt thanh toán.
        </p>
      )}
      {adding && (
        <div className="space-y-3 rounded-lg border border-dashed bg-background p-3">
          <Fields
            value={newDraft}
            onChange={setNewDraft}
            isBilingual={isBilingual}
          />
          <div className="flex justify-end gap-2">
            <Button variant="outline" onClick={() => setAdding(false)}>
              <X className="size-4" /> Hủy
            </Button>
            <Button onClick={add} disabled={busy !== null}>
              {busy === "new" && <Loader2 className="size-4 animate-spin" />}{" "}
              Thêm đợt
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
