"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import {
  Braces,
  Check,
  Copy,
  Loader2,
  Search,
  Plus,
  Pencil,
  Trash2,
} from "lucide-react";
import Link from "next/link";
import { toast } from "@/components/ui/sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import {
  ContractPlaceholderForm,
  placeholderTypeLabel,
} from "./contract-placeholder-form";
import {
  contractTemplateApi,
  TemplatePlaceholderDataKind,
  TemplatePlaceholderMultiplicity,
  type SoftwareSupplyPlaceholderCatalogResponse,
  type SoftwareSupplyPlaceholderDefinition,
  type ContractPlaceholderUsage,
} from "@/services/contract-template-api";
import { getContractTemplateErrorMessage } from "./contract-template-utils";

const getDataKindLabel = (dataKind: TemplatePlaceholderDataKind) =>
  dataKind === TemplatePlaceholderDataKind.DynamicBlock
    ? "Khối dữ liệu động"
    : "Giá trị đơn";

const getMultiplicityLabel = (multiplicity: TemplatePlaceholderMultiplicity) =>
  multiplicity === TemplatePlaceholderMultiplicity.ExactlyOne
    ? "Xuất hiện đúng 1 lần"
    : "Có thể không xuất hiện hoặc xuất hiện 1 lần";

function PlaceholderCard({
  item,
  onEdit,
  onToggle,
  onUsage,
  onDelete,
}: {
  item: SoftwareSupplyPlaceholderDefinition;
  onEdit?: () => void;
  onToggle?: () => void;
  onUsage?: () => void;
  onDelete?: () => void;
}) {
  const [isCopied, setIsCopied] = useState(false);
  const placeholder = `{{${item.key}}}`;

  const copyPlaceholder = async () => {
    try {
      await navigator.clipboard.writeText(placeholder);
      setIsCopied(true);
      toast.success(`Đã sao chép ${placeholder}.`);
      window.setTimeout(() => setIsCopied(false), 1500);
    } catch {
      toast.error("Không thể sao chép placeholder.");
    }
  };

  return (
    <Card className="border p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <code className="break-all rounded-md bg-muted px-2 py-1 text-sm font-semibold text-primary">
              {placeholder}
            </code>
            {/* <Badge variant={item.isRequired ? "destructive" : "secondary"}>
              {item.isRequired ? "Bắt buộc" : "Tùy chọn"}
            </Badge> */}
            <Badge variant="outline">
              {item.isSystem ? "Hệ thống" : "Tùy chỉnh"}
            </Badge>
            {!item.isActive && <Badge variant="secondary">Ngừng dùng</Badge>}
          </div>
          <p className="mt-3 font-medium">{item.label}</p>
        </div>
        <Button
          type="button"
          variant="outline"
          size="icon"
          className="shrink-0"
          onClick={copyPlaceholder}
          aria-label={`Sao chép ${placeholder}`}
        >
          {isCopied ? (
            <Check className="size-4 text-emerald-600" />
          ) : (
            <Copy className="size-4" />
          )}
        </Button>
      </div>
      <dl className="mt-4 grid gap-3 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-xs text-muted-foreground">Nguồn dữ liệu</dt>
          <dd className="mt-1 break-words">{item.dataSource}</dd>
        </div>
        <div>
          <dt className="text-xs text-muted-foreground">Kiểu dữ liệu</dt>
          <dd className="mt-1">
            {item.isSystem
              ? getDataKindLabel(item.dataKind)
              : placeholderTypeLabel(item.valueType)}
          </dd>
        </div>
        <div className="sm:col-span-2">
          <dt className="text-xs text-muted-foreground">Số lần xuất hiện</dt>
          <dd className="mt-1">{getMultiplicityLabel(item.multiplicity)}</dd>
        </div>
      </dl>
      {!item.isSystem && (
        <>
          {(item.defaultValue || item.formatString) && (
            <p className="mt-3 break-words text-xs text-muted-foreground">
              {item.formatString && `Định dạng: ${item.formatString}. `}
              {item.defaultValue && `Khi trống: ${item.defaultValue}`}
            </p>
          )}
          <div className="mt-3 flex flex-wrap gap-2 flex-1 items-end">
            {onEdit && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onEdit}
              >
                <Pencil className="size-3.5" />
                Sửa
              </Button>
            )}
            {onToggle && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={onToggle}
              >
                {item.isActive ? "Ngừng dùng" : "Kích hoạt"}
              </Button>
            )}
            <Button type="button" variant="outline" size="sm" onClick={onUsage}>
              Phiên bản đang dùng
            </Button>
            {onDelete && (
              <Button
                type="button"
                variant="outline"
                size="sm"
                className="text-destructive hover:text-destructive"
                onClick={onDelete}
              >
                <Trash2 className="size-3.5" />
                Xóa
              </Button>
            )}
          </div>
        </>
      )}
    </Card>
  );
}

export function ContractTemplatePlaceholderCatalog() {
  const [catalog, setCatalog] =
    useState<SoftwareSupplyPlaceholderCatalogResponse | null>(null);
  const [keyword, setKeyword] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [filter, setFilter] = useState("all");
  const [form, setForm] = useState<{
    item: SoftwareSupplyPlaceholderDefinition | null;
  } | null>(null);
  const [toggleItem, setToggleItem] =
    useState<SoftwareSupplyPlaceholderDefinition | null>(null);
  const [toggling, setToggling] = useState(false);
  const [deleteItem, setDeleteItem] =
    useState<SoftwareSupplyPlaceholderDefinition | null>(null);
  const [deleting, setDeleting] = useState(false);
  const [usage, setUsage] = useState<{
    key: string;
    items: ContractPlaceholderUsage[];
    loading: boolean;
    error?: boolean;
  } | null>(null);

  const fetchCatalog = useCallback(async () => {
    try {
      setCatalog(await contractTemplateApi.getPlaceholderCatalog());
      setLoadError(false);
    } catch (error) {
      setLoadError(true);
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    let active = true;
    contractTemplateApi
      .getPlaceholderCatalog()
      .then((data) => {
        if (active) setCatalog(data);
      })
      .catch((error) => {
        if (active) {
          setLoadError(true);
          toast.error(getContractTemplateErrorMessage(error));
        }
      })
      .finally(() => {
        if (active) setIsLoading(false);
      });
    return () => {
      active = false;
    };
  }, []);

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLocaleLowerCase();
    const items = (catalog?.items ?? []).filter(
      (item) =>
        filter === "all" ||
        (filter === "system" && item.isSystem) ||
        (filter === "custom" && !item.isSystem && item.isActive) ||
        (filter === "inactive" && !item.isActive),
    );
    if (!normalizedKeyword) return items;
    return items.filter((item) =>
      [item.key, item.label, item.dataSource].some((value) =>
        value.toLocaleLowerCase().includes(normalizedKeyword),
      ),
    );
  }, [catalog?.items, keyword, filter]);

  const showUsage = async (item: SoftwareSupplyPlaceholderDefinition) => {
    if (!item.id) return;
    setUsage({ key: item.key, items: [], loading: true });
    try {
      const items = await contractTemplateApi.getPlaceholderUsage(item.id);
      setUsage((current) =>
        current?.key === item.key
          ? { key: item.key, items, loading: false }
          : current,
      );
    } catch {
      setUsage((current) =>
        current?.key === item.key
          ? { ...current, loading: false, error: true }
          : current,
      );
    }
  };
  const toggleActive = async () => {
    if (!toggleItem?.id || !toggleItem.rowVersion) return;
    setToggling(true);
    try {
      await contractTemplateApi.setPlaceholderActive(
        toggleItem.id,
        !toggleItem.isActive,
        toggleItem.rowVersion,
      );
      toast.success("Đã cập nhật trạng thái placeholder.");
      setToggleItem(null);
      await fetchCatalog();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await fetchCatalog();
      setToggleItem(null);
    } finally {
      setToggling(false);
    }
  };
  const deletePlaceholder = async () => {
    if (!deleteItem?.id || !deleteItem.rowVersion) return;
    setDeleting(true);
    try {
      await contractTemplateApi.deletePlaceholder(
        deleteItem.id,
        deleteItem.rowVersion,
      );
      toast.success(`Đã xóa {{${deleteItem.key}}}.`);
      setDeleteItem(null);
      await fetchCatalog();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await fetchCatalog();
      setDeleteItem(null);
    } finally {
      setDeleting(false);
    }
  };
  const renderItem = (item: SoftwareSupplyPlaceholderDefinition) => (
    <PlaceholderCard
      key={item.key}
      item={item}
      onEdit={catalog?.customEnabled ? () => setForm({ item }) : undefined}
      onToggle={catalog?.customEnabled ? () => setToggleItem(item) : undefined}
      onUsage={() => void showUsage(item)}
      onDelete={catalog?.customEnabled ? () => setDeleteItem(item) : undefined}
    />
  );

  const requiredItems = filteredItems.filter((item) => item.isRequired);
  const optionalItems = filteredItems.filter((item) => !item.isRequired);

  return (
    <div className="space-y-5">
      <Card className="pb-0">
        <CardHeader className="pb-3">
          <div className="flex flex-col justify-between gap-3 sm:flex-row sm:items-center">
            <div>
              <CardTitle className="flex items-center gap-2 text-lg">
                <Braces className="size-5 text-primary" /> Catalog placeholder
              </CardTitle>
              <p className="mt-1 text-sm text-muted-foreground">
                Dùng chung trong doanh nghiệp. Sao chép placeholder vào DOCX rồi
                tải lên bản nháp để áp dụng.
              </p>
            </div>
            <div className="relative w-full sm:w-72">
              <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
              <Input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Tìm key, nhãn, nguồn dữ liệu..."
                className="pl-9"
              />
            </div>
          </div>
          <div className="mt-3 flex flex-wrap items-center gap-2 justify-between">
            <Select value={filter} onValueChange={setFilter}>
              <SelectTrigger className="w-44" aria-label="Lọc placeholder">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tất cả</SelectItem>
                <SelectItem value="system">Hệ thống</SelectItem>
                <SelectItem value="custom">Tùy chỉnh</SelectItem>
                <SelectItem value="inactive">Ngừng dùng</SelectItem>
              </SelectContent>
            </Select>
            {catalog?.customEnabled && (
              <Button onClick={() => setForm({ item: null })}>
                <Plus className="size-4" />
                Tạo placeholder
              </Button>
            )}
          </div>
        </CardHeader>
      </Card>

      {isLoading ? (
        <div className="flex justify-center py-14">
          <Loader2 className="size-7 animate-spin text-primary" />
        </div>
      ) : loadError ? (
        <div role="alert" className="rounded-xl border p-4 text-sm">
          Không tải được catalog.{" "}
          <Button variant="link" onClick={() => void fetchCatalog()}>
            Thử lại
          </Button>
        </div>
      ) : (
        <>
          {requiredItems.length > 0 && (
            <section className="space-y-3">
              <h3 className="font-semibold">
                Bắt buộc ({requiredItems.length})
              </h3>
              <div className="grid gap-3 lg:grid-cols-2">
                {requiredItems.map(renderItem)}
              </div>
            </section>
          )}
          {optionalItems.length > 0 && (
            <section className="space-y-3">
              <div className="grid gap-3 lg:grid-cols-2">
                {optionalItems.map(renderItem)}
              </div>
            </section>
          )}
          {filteredItems.length === 0 && (
            <div className="rounded-xl border border-dashed py-12 text-center text-sm text-muted-foreground">
              Không tìm thấy placeholder phù hợp.
            </div>
          )}
        </>
      )}
      {form && (
        <ContractPlaceholderForm
          item={form.item}
          onClose={() => setForm(null)}
          onSaved={fetchCatalog}
        />
      )}
      <ConfirmDialog
        isOpen={!!toggleItem}
        onClose={() => !toggling && setToggleItem(null)}
        onConfirm={toggleActive}
        title={
          toggleItem?.isActive
            ? "Ngừng dùng placeholder?"
            : "Kích hoạt placeholder?"
        }
        description="Các phiên bản đã tải DOCX giữ mapping hiện có. Trạng thái mới áp dụng cho những lần upload tiếp theo."
        confirmText={toggleItem?.isActive ? "Ngừng dùng" : "Kích hoạt"}
        isLoading={toggling}
      />
      <ConfirmDialog
        isOpen={!!deleteItem}
        onClose={() => !deleting && setDeleteItem(null)}
        onConfirm={deletePlaceholder}
        title="Xóa vĩnh viễn placeholder?"
        description="Chỉ placeholder chưa từng được template sử dụng mới có thể xóa. Audit tạo/sửa vẫn được giữ lại."
        confirmText="Xóa vĩnh viễn"
        isLoading={deleting}
        variant="destructive"
        icon={<Trash2 className="size-5 text-destructive" />}
        titleClassName="text-destructive"
      />
      <Dialog
        open={!!usage}
        onOpenChange={(open) => {
          if (!open) setUsage(null);
        }}
      >
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Phiên bản dùng {usage?.key}</DialogTitle>
            <DialogDescription>
              Mapping được lưu riêng cho từng phiên bản mẫu.
            </DialogDescription>
          </DialogHeader>
          <div className="max-h-80 overflow-y-auto">
            {usage?.loading ? (
              <Loader2 className="size-5 animate-spin" />
            ) : usage?.error ? (
              <p role="alert">Không tải được danh sách phiên bản.</p>
            ) : usage?.items.length === 0 ? (
              <p className="text-sm text-muted-foreground">
                Chưa có phiên bản sử dụng placeholder này.
              </p>
            ) : (
              usage?.items.map((x) => (
                <Link
                  key={x.templateVersionId}
                  className="block rounded-md p-2 text-sm text-primary hover:bg-muted"
                  href={`/admin/contract-templates/${x.templateId}/versions/${x.templateVersionId}`}
                >
                  {x.templateCode} · Version {x.versionNo}
                </Link>
              ))
            )}
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
