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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";

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
          <div className="space-y-2">
            <Label htmlFor={`term-title-${inputId}`}>Tiêu đề điều khoản</Label>
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
            <Textarea
              id={`term-content-${inputId}`}
              value={term.termContent ?? ""}
              className="min-h-24 resize-y"
              placeholder="Nhập nội dung điều khoản..."
              onChange={(event) =>
                onChange?.("termContent", event.target.value)
              }
            />
          </div>

          {isBilingual && (
            <div className="grid gap-4 rounded-xl border border-dashed p-3">
              <div className="space-y-2">
                <Label htmlFor={`term-title-en-${inputId}`}>
                  Tiêu đề tiếng Anh
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
                <Textarea
                  id={`term-content-en-${inputId}`}
                  value={term.termContentEn ?? ""}
                  className="min-h-24 resize-y"
                  placeholder={englishContentPlaceholder}
                  onChange={(event) =>
                    onChange?.("termContentEn", event.target.value)
                  }
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
          <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
            {term.termContent || "Chưa có nội dung"}
          </p>
          {isBilingual && (
            <div className="mt-4 rounded-xl border border-dashed p-3">
              <p className="text-sm font-semibold">
                {term.termTitleEn || "Chưa có tiêu đề tiếng Anh"}
              </p>
              <p className="mt-1 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                {term.termContentEn || "Chưa có nội dung tiếng Anh"}
              </p>
            </div>
          )}
        </div>
      )}

      {children && <div className="mt-4">{children}</div>}
    </div>
  );
}
