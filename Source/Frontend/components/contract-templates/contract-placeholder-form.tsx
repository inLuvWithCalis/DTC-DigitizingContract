"use client";

import { useEffect, useMemo, useState } from "react";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { toast } from "@/components/ui/sonner";
import {
  contractTemplateApi,
  type ContractPlaceholderSourceField,
  type SoftwareSupplyPlaceholderDefinition,
} from "@/services/contract-template-api";
import { getContractTemplateErrorMessage } from "./contract-template-utils";

export const placeholderTypeLabel = (type?: number | null) =>
  ({ 1: "Văn bản", 2: "Số", 3: "Ngày", 4: "Ngày giờ", 5: "Có/Không" })[
    type ?? 1
  ] ?? "Văn bản";

export function ContractPlaceholderForm({
  item,
  onClose,
  onSaved,
}: {
  item: SoftwareSupplyPlaceholderDefinition | null;
  onClose: () => void;
  onSaved: () => Promise<void>;
}) {
  const [fields, setFields] = useState<ContractPlaceholderSourceField[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [saving, setSaving] = useState(false);
  const [key, setKey] = useState(item?.key ?? "");
  const [label, setLabel] = useState(item?.label ?? "");
  const [module, setModule] = useState(item?.moduleKey ?? "");
  const [sourceKey, setSourceKey] = useState(item?.sourceFieldKey ?? "");
  const [format, setFormat] = useState(item?.formatString ?? "");
  const [fallback, setFallback] = useState(item?.defaultValue ?? "");
  const [error, setError] = useState<string | null>(null);
  const [conflict, setConflict] = useState(false);
  useEffect(() => {
    let active = true;
    contractTemplateApi
      .getPlaceholderSourceFields()
      .then((data) => {
        if (active) setFields(data);
      })
      .catch(() => {
        if (active) setLoadError(true);
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);
  const modules = useMemo(
    () => [
      ...new Map(
        fields.map((x) => [
          x.moduleKey,
          { value: x.moduleKey, label: x.moduleLabel },
        ]),
      ).values(),
    ],
    [fields],
  );
  const source = fields.find((x) => x.sourceFieldKey === sourceKey);
  const keyValid =
    key.length <= 100 && /^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)*$/.test(key);
  const changed =
    !item ||
    label !== item.label ||
    sourceKey !== item.sourceFieldKey ||
    format !== (item.formatString ?? "") ||
    fallback !== (item.defaultValue ?? "");
  const valid =
    keyValid &&
    label.trim().length > 0 &&
    source &&
    (!format || source.allowedFormats.includes(format)) &&
    !conflict;

  const save = async () => {
    if (!valid || !changed) return;
    setSaving(true);
    setError(null);
    try {
      const request = {
        placeholderKey: key,
        fieldLabel: label.trim(),
        sourceFieldKey: sourceKey,
        formatString: format || null,
        defaultValue: fallback || null,
        rowVersion: item?.rowVersion,
      };
      if (item?.id)
        await contractTemplateApi.updatePlaceholder(item.id, request);
      else await contractTemplateApi.createPlaceholder(request);
      toast.success(item ? "Đã cập nhật placeholder." : "Đã tạo placeholder.");
      await onSaved();
      onClose();
    } catch (err) {
      setError(getContractTemplateErrorMessage(err));
      const codes = (err as { response?: { data?: { errors?: string[] } } })
        ?.response?.data?.errors;
      if (codes?.includes("PlaceholderConcurrencyConflict")) setConflict(true);
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog
      open
      onOpenChange={(open) => {
        if (!open && !saving) onClose();
      }}
    >
      <DialogContent className="max-h-[90dvh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>
            {item ? "Sửa placeholder" : "Tạo placeholder"}
          </DialogTitle>
          <DialogDescription>
            Dùng chung trong doanh nghiệp. Thay đổi được áp dụng khi tải DOCX
            lên bản nháp; các phiên bản đã phát hành giữ mapping hiện có.
          </DialogDescription>
        </DialogHeader>
        {loading ? (
          <Loader2 className="mx-auto size-6 animate-spin" />
        ) : loadError ? (
          <p role="alert" className="text-sm text-destructive">
            Không tải được danh sách trường. Đóng và mở lại để thử lại.
          </p>
        ) : (
          <div className="grid gap-4">
            <div className="space-y-2">
              <Label htmlFor="placeholder-key">Key placeholder</Label>
              <Input
                id="placeholder-key"
                value={key}
                disabled={!!item || saving}
                maxLength={104}
                onChange={(e) =>
                  setKey(
                    e.target.value
                      .replace(/^\s*\{\{/, "")
                      .replace(/\}\}\s*$/, "")
                      .trim()
                      .toUpperCase(),
                  )
                }
                placeholder="CUSTOMER_CONTACT_NAME"
                aria-invalid={!!key && !keyValid}
              />
              {!!key && !keyValid && (
                <p className="text-xs text-destructive">
                  Bắt đầu bằng chữ; chỉ gồm A–Z, số và dấu gạch dưới, tối đa 100
                  ký tự.
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label htmlFor="placeholder-label">Tên hiển thị</Label>
              <Input
                id="placeholder-label"
                value={label}
                maxLength={300}
                disabled={saving}
                onChange={(e) => setLabel(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label>Module</Label>
              <Select
                value={module}
                disabled={saving}
                onValueChange={(value) => {
                  setModule(value);
                  setSourceKey("");
                  setFormat("");
                }}
              >
                <SelectTrigger className="w-full" aria-label="Module">
                  <SelectValue placeholder="Chọn module" />
                </SelectTrigger>
                <SelectContent showSearch>
                  {modules.map((option) => (
                    <SelectItem key={option.value} value={option.value}>
                      {option.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-2">
              <Label>Trường dữ liệu</Label>
              <Select
                value={sourceKey}
                disabled={!module || saving}
                onValueChange={(value) => {
                  setSourceKey(value);
                  setFormat("");
                }}
              >
                <SelectTrigger className="w-full" aria-label="Trường dữ liệu">
                  <SelectValue placeholder="Chọn trường dữ liệu" />
                </SelectTrigger>
                <SelectContent showSearch>
                  {fields
                    .filter((x) => x.moduleKey === module)
                    .map((field) => (
                      <SelectItem
                        key={field.sourceFieldKey}
                        value={field.sourceFieldKey}
                      >
                        {field.fieldLabel} ·{" "}
                        {placeholderTypeLabel(field.valueType)}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
              {source && (
                <p className="text-xs text-muted-foreground">
                  {placeholderTypeLabel(source.valueType)}
                  {source.isNullable ? " · Có thể trống" : ""}
                </p>
              )}
            </div>
            {source && source.allowedFormats.length > 0 && (
              <div className="space-y-2">
                <Label>Định dạng</Label>
                <Select
                  value={format || "default"}
                  disabled={saving}
                  onValueChange={(v) => setFormat(v === "default" ? "" : v)}
                >
                  <SelectTrigger aria-label="Định dạng">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="default">Mặc định</SelectItem>
                    {source.allowedFormats.map((x) => (
                      <SelectItem key={x} value={x}>
                        {x}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}
            <div className="space-y-2">
              <Label htmlFor="placeholder-default">
                Giá trị khi dữ liệu trống
              </Label>
              <Input
                id="placeholder-default"
                value={fallback}
                maxLength={2000}
                disabled={saving}
                onChange={(e) => setFallback(e.target.value)}
              />
            </div>
            {source && (
              <div className="rounded-md bg-muted p-3 text-sm">
                <p className="mb-1 text-xs text-muted-foreground">
                  Xem trước bằng dữ liệu mẫu
                </p>
                <code>{`{{${key || "KEY"}}}`}</code> →{" "}
                {format ? source.formattedSamples[format] : source.sampleValue}
              </div>
            )}
          </div>
        )}
        {error && (
          <p role="alert" className="text-sm text-destructive">
            {error}
          </p>
        )}
        <DialogFooter>
          <Button variant="outline" disabled={saving} onClick={onClose}>
            Đóng
          </Button>
          {conflict ? (
            <Button
              onClick={async () => {
                await onSaved();
                onClose();
              }}
            >
              Tải lại catalog
            </Button>
          ) : (
            <Button
              disabled={saving || loading || loadError || !valid || !changed}
              onClick={save}
            >
              {saving && <Loader2 className="size-4 animate-spin" />}Lưu
              placeholder
            </Button>
          )}
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
