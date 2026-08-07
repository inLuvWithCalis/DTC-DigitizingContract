"use client";

import {
  ArrowDown,
  ArrowUp,
  LockKeyhole,
  Plus,
  ShieldCheck,
  Trash2,
  UnlockKeyhole,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { MockContractTerm } from "@/services/contract-templates-mock";

interface CreateContractTermsMockProps {
  terms: MockContractTerm[];
  templateName?: string;
  isBilingual?: boolean;
  onChange: (terms: MockContractTerm[]) => void;
}

function reorderTerms(terms: MockContractTerm[]) {
  return terms.map((term, index) => ({
    ...term,
    displayOrder: index + 1,
  }));
}

export function CreateContractTermsMock({
  terms,
  templateName,
  isBilingual = false,
  onChange,
}: CreateContractTermsMockProps) {
  const updateTerm = (
    id: string,
    field: keyof MockContractTerm,
    value: string | boolean,
  ) => {
    onChange(
      terms.map((term) =>
        term.id === id ? { ...term, [field]: value } : term,
      ),
    );
  };

  const addTerm = () => {
    const nextOrder = terms.length + 1;
    const existingCodes = new Set(
      terms.map((term) => term.termCode.toLocaleUpperCase()),
    );
    let customCodeIndex = 1;

    while (existingCodes.has(`CUSTOM_${customCodeIndex}`)) {
      customCodeIndex += 1;
    }

    onChange([
      ...terms,
      {
        id: `custom-${Date.now()}`,
        termCode: `CUSTOM_${customCodeIndex}`,
        termTitle: `Điều ${nextOrder}. Điều khoản mới`,
        termContent: "",
        isNegotiable: true,
        displayOrder: nextOrder,
      },
    ]);
  };

  const removeTerm = (id: string) => {
    onChange(reorderTerms(terms.filter((term) => term.id !== id)));
  };

  const moveTerm = (index: number, direction: -1 | 1) => {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= terms.length) return;

    const nextTerms = [...terms];
    [nextTerms[index], nextTerms[targetIndex]] = [
      nextTerms[targetIndex],
      nextTerms[index],
    ];
    onChange(reorderTerms(nextTerms));
  };

  if (!templateName) {
    return (
      <div className="rounded-2xl border border-dashed bg-muted/20 px-6 py-12 text-center">
        <ShieldCheck className="mx-auto size-9 text-muted-foreground" />
        <p className="mt-3 font-semibold">Chưa có bộ điều khoản</p>
        <p className="mt-1 text-sm text-muted-foreground">
          Quay lại bước đầu tiên và chọn một mẫu hợp đồng.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 rounded-xl border bg-muted/20 p-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <p className="font-semibold">Bộ điều khoản từ template</p>
          </div>
          <p className="mt-1 text-sm text-muted-foreground">{templateName}</p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={addTerm}>
          <Plus className="mr-2 size-4" />
          Thêm điều khoản
        </Button>
      </div>

      <div className="space-y-3">
        {terms.map((term, index) => (
          <div
            key={term.id}
            className="rounded-2xl border bg-background p-4 shadow-xs"
          >
            <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
              <div className="flex flex-wrap items-center gap-2">
                <Badge variant="outline">Điều {term.displayOrder}</Badge>
                <Badge
                  variant={term.isNegotiable ? "secondary" : "destructive"}
                  className={
                    term.isNegotiable
                      ? "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/30 dark:text-emerald-300"
                      : "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/30 dark:text-amber-300"
                  }
                >
                  {term.isNegotiable ? (
                    <UnlockKeyhole className="mr-1 size-3" />
                  ) : (
                    <LockKeyhole className="mr-1 size-3" />
                  )}
                  {term.isNegotiable ? "Có thể đàm phán" : "Điều khoản cố định"}
                </Badge>
                <span className="font-mono text-xs text-muted-foreground">
                  {term.termCode}
                </span>
              </div>

              <div className="flex items-center gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Chuyển điều khoản lên"
                  disabled={index === 0}
                  onClick={() => moveTerm(index, -1)}
                >
                  <ArrowUp className="size-4" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Chuyển điều khoản xuống"
                  disabled={index === terms.length - 1}
                  onClick={() => moveTerm(index, 1)}
                >
                  <ArrowDown className="size-4" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Xóa điều khoản"
                  className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                  onClick={() => removeTerm(term.id)}
                >
                  <Trash2 className="size-4" />
                </Button>
              </div>
            </div>

            <div className="grid gap-4">
              <div className="space-y-2">
                <Label htmlFor={`term-title-${term.id}`}>
                  Tiêu đề điều khoản
                </Label>
                <Input
                  id={`term-title-${term.id}`}
                  value={term.termTitle}
                  onChange={(event) =>
                    updateTerm(term.id, "termTitle", event.target.value)
                  }
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor={`term-content-${term.id}`}>
                  Nội dung điều khoản
                </Label>
                <Textarea
                  id={`term-content-${term.id}`}
                  value={term.termContent}
                  className="min-h-24 resize-y"
                  placeholder="Nhập nội dung điều khoản..."
                  onChange={(event) =>
                    updateTerm(term.id, "termContent", event.target.value)
                  }
                />
              </div>

              {isBilingual && (
                <div className="grid gap-4 rounded-xl border border-dashed p-3">
                  <div className="space-y-2">
                    <Label htmlFor={`term-title-en-${term.id}`}>
                      Tiêu đề tiếng Anh
                    </Label>
                    <Input
                      id={`term-title-en-${term.id}`}
                      value={term.termTitleEn ?? ""}
                      placeholder="Để trống để giữ tiêu đề từ template"
                      onChange={(event) =>
                        updateTerm(
                          term.id,
                          "termTitleEn",
                          event.target.value,
                        )
                      }
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`term-content-en-${term.id}`}>
                      Nội dung tiếng Anh
                    </Label>
                    <Textarea
                      id={`term-content-en-${term.id}`}
                      value={term.termContentEn ?? ""}
                      className="min-h-24 resize-y"
                      placeholder="Để trống để giữ nội dung từ template"
                      onChange={(event) =>
                        updateTerm(
                          term.id,
                          "termContentEn",
                          event.target.value,
                        )
                      }
                    />
                  </div>
                </div>
              )}

              <button
                type="button"
                className="flex w-fit items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground"
                onClick={() =>
                  updateTerm(term.id, "isNegotiable", !term.isNegotiable)
                }
              >
                {term.isNegotiable ? (
                  <UnlockKeyhole className="size-4 text-emerald-600" />
                ) : (
                  <LockKeyhole className="size-4 text-amber-600" />
                )}
                {term.isNegotiable
                  ? "Cho phép chỉnh sửa khi đàm phán"
                  : "Khóa nội dung khi đàm phán"}
              </button>
            </div>
          </div>
        ))}
      </div>

      {terms.length === 0 && (
        <div className="rounded-xl border border-dashed px-6 py-10 text-center text-sm text-muted-foreground">
          Template hiện chưa có điều khoản. Bạn có thể thêm điều khoản mới.
        </div>
      )}
    </div>
  );
}
