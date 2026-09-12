"use client";

import {
  useMemo,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
} from "react";
import { GripVertical, Loader2, Save } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  CONTRACT_TABLE_MIN_COLUMN_BPS,
  isCanonicalColumnWidthsBps,
  normalizeColumnWidthsBps,
} from "@/lib/contract-rich-text";
import { getContractTemplateErrorMessage } from "@/components/contract-templates/contract-template-utils";
import {
  contractTemplateApi,
  type ContractTemplateItemTableColumnLayoutResponse,
  type ContractTemplateVersionDetailResponse,
} from "@/services/contract-template-api";

const COLUMNS = [
  ["No", "STT"],
  ["ItemType", "Loại"],
  ["Description", "Sản phẩm/dịch vụ"],
  ["Quantity", "Số lượng"],
  ["UnitPrice", "Đơn giá"],
  ["Discount", "Chiết khấu"],
  ["Vat", "VAT"],
  ["TotalAmount", "Thành tiền"],
] as const;

interface Props {
  versionId: number;
  versionRowVersion: string;
  layout: ContractTemplateItemTableColumnLayoutResponse[];
  editable: boolean;
  onSaved: (version: ContractTemplateVersionDetailResponse) => void;
}

export function ContractTemplateItemTableLayoutEditor({
  versionId,
  versionRowVersion,
  layout,
  editable,
  onSaved,
}: Props) {
  const canonical = useMemo(() => {
    const ordered = [...layout].sort((left, right) =>
      left.displayOrder - right.displayOrder,
    );
    const widths = ordered.map((item) => item.widthBps);
    const valid =
      ordered.length === COLUMNS.length &&
      ordered.every(
        (item, index) =>
          item.columnKey === COLUMNS[index][0] &&
          item.displayOrder === index,
      ) &&
      isCanonicalColumnWidthsBps(widths, COLUMNS.length);
    return valid ? widths : null;
  }, [layout]);
  const [widths, setWidths] = useState(() => canonical ?? []);
  const [saving, setSaving] = useState(false);
  const tableRef = useRef<HTMLTableElement>(null);

  if (!canonical || widths.length !== COLUMNS.length) {
    return (
      <Card>
        <CardHeader>
          <CardTitle>Bố cục bảng sản phẩm/dịch vụ</CardTitle>
        </CardHeader>
        <CardContent className="text-sm text-destructive">
          ItemTableLayoutInvalid: template version không có đúng tám cột
          canonical.
        </CardContent>
      </Card>
    );
  }

  const startResize = (
    event: ReactPointerEvent<HTMLButtonElement>,
    columnIndex: number,
  ) => {
    if (!editable || columnIndex >= widths.length - 1) return;
    event.preventDefault();
    event.currentTarget.setPointerCapture(event.pointerId);
    const startX = event.clientX;
    const startWidths = [...widths];
    const pairTotal = startWidths[columnIndex] + startWidths[columnIndex + 1];
    const tableWidth = Math.max(1, tableRef.current?.clientWidth ?? 1);

    const move = (moveEvent: PointerEvent) => {
      const delta = Math.round(
        ((moveEvent.clientX - startX) / tableWidth) * 10_000,
      );
      const left = Math.min(
        pairTotal - CONTRACT_TABLE_MIN_COLUMN_BPS,
        Math.max(CONTRACT_TABLE_MIN_COLUMN_BPS, startWidths[columnIndex] + delta),
      );
      const next = [...startWidths];
      next[columnIndex] = left;
      next[columnIndex + 1] = pairTotal - left;
      setWidths(normalizeColumnWidthsBps(next));
    };
    const stop = () => {
      window.removeEventListener("pointermove", move);
      window.removeEventListener("pointerup", stop);
    };
    window.addEventListener("pointermove", move);
    window.addEventListener("pointerup", stop, { once: true });
  };

  const updatePercent = (columnIndex: number, percent: number) => {
    const requestedBps = Math.round(percent * 100);
    if (!Number.isFinite(requestedBps)) return;
    const next = [...widths];
    next[columnIndex] = Math.max(
      CONTRACT_TABLE_MIN_COLUMN_BPS,
      requestedBps,
    );
    setWidths(normalizeColumnWidthsBps(next));
  };

  const save = async () => {
    try {
      setSaving(true);
      const version = await contractTemplateApi.updateItemTableLayout(
        versionId,
        {
          versionRowVersion,
          columnWidthsBps: widths,
        },
      );
      onSaved(version);
      toast.success("Đã lưu bố cục và làm stale preview cũ.");
    } catch (error) {
      toast.error(
        getContractTemplateErrorMessage(
          error,
          "Không thể lưu bố cục bảng sản phẩm/dịch vụ.",
        ),
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <Card>
      <CardHeader className="gap-1">
        <CardTitle>Bố cục bảng sản phẩm/dịch vụ</CardTitle>
        <p className="text-sm font-normal text-muted-foreground">
          Kéo vạch giữa hai cột hoặc nhập tỷ lệ. Tổng luôn được chuẩn hóa về
          100% và dùng chung khi render DOCX/PDF.
        </p>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="overflow-x-auto rounded-md border">
          <table ref={tableRef} className="w-full min-w-[900px] table-fixed text-xs">
            <colgroup>
              {widths.map((width, index) => (
                <col key={COLUMNS[index][0]} style={{ width: `${width / 100}%` }} />
              ))}
            </colgroup>
            <thead>
              <tr className="bg-muted/60">
                {COLUMNS.map(([key, label], index) => (
                  <th key={key} className="relative border-r px-2 py-3 text-left last:border-r-0">
                    <span className="block truncate">{label}</span>
                    <span className="text-[10px] font-normal text-muted-foreground">
                      {(widths[index] / 100).toFixed(2)}%
                    </span>
                    {editable && index < COLUMNS.length - 1 && (
                      <button
                        type="button"
                        aria-label={`Chỉnh độ rộng cột ${label}`}
                        title={`Kéo để chỉnh ${label}`}
                        className="absolute -right-2 top-0 z-10 flex h-full w-4 cursor-col-resize items-center justify-center text-primary hover:bg-primary/10"
                        onPointerDown={(event) => startResize(event, index)}
                      >
                        <GripVertical className="size-3" />
                      </button>
                    )}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              <tr>
                {["1", "Dịch vụ", "Triển khai phần mềm", "1", "10.000.000", "0%", "10%", "11.000.000"].map(
                  (value, index) => (
                    <td key={COLUMNS[index][0]} className="truncate border-r px-2 py-3 last:border-r-0">
                      {value}
                    </td>
                  ),
                )}
              </tr>
            </tbody>
          </table>
        </div>

        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          {COLUMNS.map(([key, label], index) => (
            <div key={key} className="space-y-1.5">
              <Label htmlFor={`item-column-${key}`}>{label} (%)</Label>
              <Input
                id={`item-column-${key}`}
                type="number"
                min={2.5}
                max={100}
                step={0.01}
                disabled={!editable || saving}
                value={(widths[index] / 100).toFixed(2)}
                onChange={(event) =>
                  updatePercent(index, Number(event.target.value))
                }
              />
            </div>
          ))}
        </div>

        <div className="flex items-center justify-between gap-3">
          <span className="text-sm text-muted-foreground">
            Tổng: {(widths.reduce((sum, width) => sum + width, 0) / 100).toFixed(2)}%
          </span>
          {editable && (
            <Button disabled={saving} onClick={() => void save()}>
              {saving ? (
                <Loader2 className="mr-2 size-4 animate-spin" />
              ) : (
                <Save className="mr-2 size-4" />
              )}
              Lưu bố cục
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
