"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Braces, Check, Copy, Loader2, Search } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import {
  contractTemplateApi,
  TemplatePlaceholderDataKind,
  TemplatePlaceholderMultiplicity,
  type SoftwareSupplyPlaceholderCatalogResponse,
  type SoftwareSupplyPlaceholderDefinition,
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
}: {
  item: SoftwareSupplyPlaceholderDefinition;
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
    <div className="rounded-xl border bg-white p-4">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <code className="break-all rounded-md bg-muted px-2 py-1 text-sm font-semibold text-primary">
              {placeholder}
            </code>
            <Badge variant={item.isRequired ? "destructive" : "secondary"}>
              {item.isRequired ? "Bắt buộc" : "Tùy chọn"}
            </Badge>
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
          <dd className="mt-1">{getDataKindLabel(item.dataKind)}</dd>
        </div>
        <div className="sm:col-span-2">
          <dt className="text-xs text-muted-foreground">Số lần xuất hiện</dt>
          <dd className="mt-1">{getMultiplicityLabel(item.multiplicity)}</dd>
        </div>
      </dl>
    </div>
  );
}

export function ContractTemplatePlaceholderCatalog() {
  const [catalog, setCatalog] =
    useState<SoftwareSupplyPlaceholderCatalogResponse | null>(null);
  const [keyword, setKeyword] = useState("");
  const [isLoading, setIsLoading] = useState(true);

  const fetchCatalog = useCallback(async () => {
    try {
      setIsLoading(true);
      setCatalog(await contractTemplateApi.getPlaceholderCatalog());
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void fetchCatalog();
  }, [fetchCatalog]);

  const filteredItems = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLocaleLowerCase();
    if (!normalizedKeyword) return catalog?.items ?? [];
    return (catalog?.items ?? []).filter((item) =>
      [item.key, item.label, item.dataSource].some((value) =>
        value.toLocaleLowerCase().includes(normalizedKeyword),
      ),
    );
  }, [catalog?.items, keyword]);

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
                Chèn placeholder vào file DOCX; hệ thống sẽ kiểm tra khi upload.
                {catalog?.catalogVersion
                  ? ` Catalog version: ${catalog.catalogVersion}.`
                  : ""}
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
        </CardHeader>
      </Card>

      {isLoading ? (
        <div className="flex justify-center py-14">
          <Loader2 className="size-7 animate-spin text-primary" />
        </div>
      ) : (
        <>
          {requiredItems.length > 0 && (
            <section className="space-y-3">
              <h3 className="font-semibold">
                Bắt buộc ({requiredItems.length})
              </h3>
              <div className="grid gap-3 lg:grid-cols-2">
                {requiredItems.map((item) => (
                  <PlaceholderCard key={item.key} item={item} />
                ))}
              </div>
            </section>
          )}
          {optionalItems.length > 0 && (
            <section className="space-y-3">
              <h3 className="font-semibold">
                Tùy chọn ({optionalItems.length})
              </h3>
              <div className="grid gap-3 lg:grid-cols-2">
                {optionalItems.map((item) => (
                  <PlaceholderCard key={item.key} item={item} />
                ))}
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
    </div>
  );
}
