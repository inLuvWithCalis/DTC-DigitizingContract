"use client";

import type { ReactNode } from "react";
import {
  ArrowDown,
  ArrowUp,
  LockKeyhole,
  Trash2,
  UnlockKeyhole,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { ContractRichTextContent } from "@/components/ui/custom/contract-rich-text-content";
import { ContractRichTextEditor } from "@/components/ui/custom/contract-rich-text-editor";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import { ContractTermKind } from "@/services/contract-template-api";

export type ContractTermEditableField =
  | "termTitle"
  | "termTitleEn"
  | "termContent"
  | "termContentEn"
  | "isNegotiable";

export interface ContractTermCardValue {
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
  termKind?: ContractTermKind;
}

interface ContractTermCardProps {
  term: ContractTermCardValue;
  inputId: string;
  className?: string;
  editable?: boolean;
  isBilingual?: boolean;
  canMoveUp?: boolean;
  canMoveDown?: boolean;
  englishTitlePlaceholder?: string;
  englishContentPlaceholder?: string;
  onChange?: (
    field: ContractTermEditableField,
    value: string | boolean,
  ) => void;
  onKindChange?: (value: ContractTermKind) => void;
  onMove?: (direction: -1 | 1) => void;
  onRemove?: () => void;
  children?: ReactNode;
}

export function ContractTermCard({
  term,
  inputId,
  className,
  editable = false,
  isBilingual = false,
  canMoveUp = false,
  canMoveDown = false,
  englishTitlePlaceholder = "English term title",
  englishContentPlaceholder = "English term content",
  onChange,
  onKindChange,
  onMove,
  onRemove,
  children,
}: ContractTermCardProps) {
  return (
    <div
      className={cn(
        "rounded-2xl border bg-white p-4 shadow-xs transition-[border-color,box-shadow]",
        className,
      )}
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
          {term.termKind === ContractTermKind.Payment && (
            <Badge>Thanh toán theo đợt</Badge>
          )}
        </div>

        {editable && (
          <div className="flex items-center gap-1">
            {onMove && (
              <>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Chuyển điều khoản lên"
                  disabled={!canMoveUp}
                  onClick={() => onMove(-1)}
                >
                  <ArrowUp className="size-4" />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  aria-label="Chuyển điều khoản xuống"
                  disabled={!canMoveDown}
                  onClick={() => onMove(1)}
                >
                  <ArrowDown className="size-4" />
                </Button>
              </>
            )}
            {onRemove && (
              <Button
                type="button"
                variant="ghost"
                size="icon"
                aria-label="Xóa điều khoản"
                className="text-destructive hover:bg-destructive/10 hover:text-destructive"
                onClick={onRemove}
              >
                <Trash2 className="size-4" />
              </Button>
            )}
          </div>
        )}
      </div>

      {editable ? (
        <div className="grid gap-4">
          {onKindChange && (
            <div className="space-y-2">
              <Label htmlFor={`term-kind-${inputId}`}>Loại điều khoản</Label>
              <Select
                value={String(term.termKind ?? ContractTermKind.General)}
                onValueChange={(value) =>
                  onKindChange(Number(value) as ContractTermKind)
                }
              >
                <SelectTrigger id={`term-kind-${inputId}`} className="w-full">
                  <SelectValue placeholder="Chọn loại điều khoản" />
                </SelectTrigger>
                <SelectContent showSearch={false}>
                  <SelectItem value={String(ContractTermKind.General)}>
                    Điều khoản thường
                  </SelectItem>
                  <SelectItem value={String(ContractTermKind.Payment)}>
                    Điều khoản thanh toán
                  </SelectItem>
                </SelectContent>
              </Select>
            </div>
          )}
          <div className="space-y-2">
            <Label htmlFor={`term-title-${inputId}`}>
              Tiêu đề điều khoản <span className="text-destructive">*</span>
            </Label>
            <Input
              id={`term-title-${inputId}`}
              value={term.termTitle}
              onChange={(event) => onChange?.("termTitle", event.target.value)}
            />
          </div>

          <div className="space-y-2">
            <Label htmlFor={`term-content-${inputId}`}>
              Nội dung điều khoản
            </Label>
            <ContractRichTextEditor
              id={`term-content-${inputId}`}
              value={term.termContent ?? ""}
              placeholder="Nhập nội dung điều khoản..."
              ariaLabel="Nội dung điều khoản"
              onChange={(value) => onChange?.("termContent", value)}
            />
          </div>

          {isBilingual && (
            <div className="grid gap-4 rounded-xl border border-dashed p-3">
              <div className="space-y-2">
                <Label htmlFor={`term-title-en-${inputId}`}>
                  Tiêu đề tiếng Anh <span className="text-destructive">*</span>
                </Label>
                <Input
                  id={`term-title-en-${inputId}`}
                  value={term.termTitleEn ?? ""}
                  placeholder={englishTitlePlaceholder}
                  onChange={(event) =>
                    onChange?.("termTitleEn", event.target.value)
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor={`term-content-en-${inputId}`}>
                  Nội dung tiếng Anh
                </Label>
                <ContractRichTextEditor
                  id={`term-content-en-${inputId}`}
                  value={term.termContentEn ?? ""}
                  placeholder={englishContentPlaceholder}
                  ariaLabel="Nội dung điều khoản tiếng Anh"
                  onChange={(value) => onChange?.("termContentEn", value)}
                />
              </div>
            </div>
          )}

          <Button
            variant="link"
            type="button"
            className="flex w-fit items-center gap-2 text-sm font-medium text-muted-foreground transition-colors hover:text-foreground cursor-pointer"
            onClick={() => onChange?.("isNegotiable", !term.isNegotiable)}
          >
            {term.isNegotiable ? (
              <UnlockKeyhole className="size-4 text-emerald-600" />
            ) : (
              <LockKeyhole className="size-4 text-amber-600" />
            )}
            {term.isNegotiable
              ? "Cho phép chỉnh sửa khi đàm phán"
              : "Khóa nội dung khi đàm phán"}
          </Button>
        </div>
      ) : (
        <div>
          <p className="text-sm font-semibold">{term.termTitle}</p>
          <ContractRichTextContent
            value={term.termContent}
            className="mt-1 text-muted-foreground"
          />
          {isBilingual && (
            <div className="mt-4 rounded-xl border border-dashed p-3">
              <p className="text-sm font-semibold">
                {term.termTitleEn || "Chưa có tiêu đề tiếng Anh"}
              </p>
              <ContractRichTextContent
                value={term.termContentEn}
                emptyText="Chưa có nội dung tiếng Anh"
                className="mt-1 text-muted-foreground"
              />
            </div>
          )}
        </div>
      )}

      {children && <div className="mt-4">{children}</div>}
    </div>
  );
}
