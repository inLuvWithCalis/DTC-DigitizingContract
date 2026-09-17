"use client";

import { useMemo, useState } from "react";
import { FileStack, Loader2, Plus, Save, Trash2 } from "lucide-react";

import { ContractTermCard } from "@/components/contracts/contract-term-card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { toast } from "@/components/ui/sonner";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";
import {
  contractApi,
  type ContractAppendixResponse,
  type ContractAppendixTermResponse,
  type ContractDetailResponse,
} from "@/services/contract-api";

type DraftTerm = ContractAppendixTermResponse;

export function ContractAppendices({
  contract,
  canEdit,
  onRefetch,
}: {
  contract: ContractDetailResponse;
  canEdit: boolean;
  onRefetch: () => void | Promise<void>;
}) {
  const appendices = useMemo(
    () => [...(contract.currentVersion.appendices ?? [])].sort(
      (a, b) => a.displayOrder - b.displayOrder,
    ),
    [contract.currentVersion.appendices],
  );
  const availableOptions = contract.currentVersion.availableOptionalAppendices ?? [];
  const [drafts, setDrafts] = useState<Record<number, DraftTerm>>(() =>
    Object.fromEntries(
      appendices.flatMap((appendix) =>
        appendix.terms.map((term) => [term.appendixTermId, { ...term }]),
      ),
    ),
  );
  const [busyKey, setBusyKey] = useState<string | null>(null);
  const [addingTo, setAddingTo] = useState<number | null>(null);
  const [newTerm, setNewTerm] = useState({ code: "", title: "" });

  const run = async (key: string, action: () => Promise<unknown>) => {
    try {
      setBusyKey(key);
      await action();
      await Promise.resolve(onRefetch());
    } catch (error) {
      if (isStaleRowVersion(error)) {
        await Promise.resolve(onRefetch());
        toast.error("Dữ liệu đã thay đổi. Danh sách phụ lục đã được tải lại.");
      } else {
        toast.error(getApiErrorMessage(error, "Không thể cập nhật phụ lục."));
      }
    } finally {
      setBusyKey(null);
    }
  };

  const saveTerm = (appendix: ContractAppendixResponse, termId: number) => {
    const term = drafts[termId];
    if (!term?.termCode.trim() || !term.termTitle.trim()) {
      toast.error("Mã và tiêu đề điều khoản là bắt buộc.");
      return;
    }
    void run(`save-${termId}`, async () => {
      await contractApi.updateAppendixTerm(
        contract.contractId,
        contract.currentVersion.versionId,
        appendix.appendixId,
        termId,
        {
          termCode: term.termCode.trim(),
          termTitle: term.termTitle.trim(),
          termTitleEn: term.termTitleEn?.trim() || null,
          termContent: term.termContent?.trim() || null,
          termContentEn: term.termContentEn?.trim() || null,
          displayOrder: term.displayOrder,
          rowVersion: term.rowVersion,
          appendixRowVersion: appendix.rowVersion,
          versionRowVersion: contract.currentVersion.rowVersion,
        },
      );
      toast.success("Đã lưu điều khoản phụ lục.");
    });
  };

  const addTerm = (appendix: ContractAppendixResponse) => {
    if (!newTerm.code.trim() || !newTerm.title.trim()) {
      toast.error("Nhập mã và tiêu đề điều khoản mới.");
      return;
    }
    void run(`add-${appendix.appendixId}`, async () => {
      await contractApi.addAppendixTerm(
        contract.contractId,
        contract.currentVersion.versionId,
        appendix.appendixId,
        {
          termCode: newTerm.code.trim(),
          termTitle: newTerm.title.trim(),
          displayOrder: appendix.terms.length + 1,
          versionRowVersion: contract.currentVersion.rowVersion,
          appendixRowVersion: appendix.rowVersion,
        },
      );
      setAddingTo(null);
      setNewTerm({ code: "", title: "" });
      toast.success("Đã thêm điều khoản phụ lục.");
    });
  };

  const removeTerm = (
    appendix: ContractAppendixResponse,
    term: ContractAppendixTermResponse,
  ) => {
    if (!window.confirm(`Xóa điều khoản “${term.termTitle}”?`)) return;
    void run(`delete-${term.appendixTermId}`, () =>
      contractApi.deleteAppendixTerm(
        contract.contractId,
        contract.currentVersion.versionId,
        appendix.appendixId,
        term.appendixTermId,
        {
          rowVersion: term.rowVersion,
          appendixRowVersion: appendix.rowVersion,
          versionRowVersion: contract.currentVersion.rowVersion,
        },
      ),
    );
  };

  const moveTerm = (
    appendix: ContractAppendixResponse,
    termIndex: number,
    direction: -1 | 1,
  ) => {
    const target = termIndex + direction;
    if (target < 0 || target >= appendix.terms.length) return;
    const ordered = [...appendix.terms];
    [ordered[termIndex], ordered[target]] = [ordered[target], ordered[termIndex]];
    void run(`order-${appendix.appendixId}`, () =>
      contractApi.reorderAppendixTerms(
        contract.contractId,
        contract.currentVersion.versionId,
        appendix.appendixId,
        {
          versionRowVersion: contract.currentVersion.rowVersion,
          appendixRowVersion: appendix.rowVersion,
          terms: ordered.map((term, index) => ({
            appendixTermId: term.appendixTermId,
            rowVersion: term.rowVersion,
            displayOrder: index + 1,
          })),
        },
      ),
    );
  };

  const removeAppendix = (appendix: ContractAppendixResponse) => {
    if (appendix.isRequired || !window.confirm(`Bỏ phụ lục “${appendix.appendixName}”?`)) return;
    void run(`appendix-${appendix.appendixId}`, () =>
      contractApi.deleteAppendix(
        contract.contractId,
        contract.currentVersion.versionId,
        appendix.appendixId,
        appendix.rowVersion,
        contract.currentVersion.rowVersion,
      ),
    );
  };

  const addAppendix = (templateAppendixId: number) => {
    void run(`option-${templateAppendixId}`, async () => {
      await contractApi.addAppendix(
        contract.contractId,
        contract.currentVersion.versionId,
        templateAppendixId,
        contract.currentVersion.rowVersion,
      );
      toast.success("Đã thêm phụ lục tùy chọn.");
    });
  };

  if (appendices.length === 0 && (!canEdit || availableOptions.length === 0)) {
    return (
      <div className="rounded-xl border border-dashed p-10 text-center text-sm text-muted-foreground">
        <FileStack className="mx-auto mb-3 size-8" />
        Version này không có phụ lục đính kèm.
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {canEdit && availableOptions.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Phụ lục tùy chọn có thể thêm</CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {availableOptions.map((option) => (
              <div
                key={option.templateAppendixId}
                className="flex flex-col gap-3 rounded-xl border p-3 sm:flex-row sm:items-center sm:justify-between"
              >
                <div>
                  <p className="font-medium">
                    {option.appendixCode} — {option.appendixName}
                  </p>
                  {option.appendixDescription && (
                    <p className="mt-1 text-sm text-muted-foreground">
                      {option.appendixDescription}
                    </p>
                  )}
                </div>
                <Button
                  size="sm"
                  variant="outline"
                  disabled={busyKey !== null}
                  onClick={() => addAppendix(option.templateAppendixId)}
                >
                  {busyKey === `option-${option.templateAppendixId}` ? (
                    <Loader2 className="size-4 animate-spin" />
                  ) : (
                    <Plus className="size-4" />
                  )}
                  Thêm phụ lục
                </Button>
              </div>
            ))}
          </CardContent>
        </Card>
      )}
      {appendices.map((appendix) => (
        <Card key={appendix.appendixId}>
          <CardHeader className="flex flex-row items-start justify-between gap-3">
            <div className="space-y-1">
              <CardTitle className="text-base">
                {appendix.appendixCode} — {appendix.appendixName}
              </CardTitle>
              <div className="flex items-center gap-2">
                <Badge variant={appendix.isRequired ? "default" : "outline"}>
                  {appendix.isRequired ? "Bắt buộc" : "Tùy chọn"}
                </Badge>
                <span className="text-xs text-muted-foreground">
                  {appendix.terms.length} điều khoản
                </span>
              </div>
            </div>
            {canEdit && !appendix.isRequired && (
              <Button
                variant="ghost"
                size="icon"
                disabled={busyKey !== null}
                aria-label="Bỏ phụ lục"
                onClick={() => removeAppendix(appendix)}
              >
                <Trash2 className="size-4 text-destructive" />
              </Button>
            )}
          </CardHeader>
          <CardContent className="space-y-4">
            {appendix.appendixDescription && (
              <p className="text-sm text-muted-foreground">
                {appendix.appendixDescription}
              </p>
            )}
            {appendix.terms.map((sourceTerm, index) => {
              const term = drafts[sourceTerm.appendixTermId] ?? sourceTerm;
              const busy = busyKey?.endsWith(String(term.appendixTermId));
              return (
                <ContractTermCard
                  key={term.appendixTermId}
                  term={{ ...term, isNegotiable: false }}
                  inputId={`appendix-${appendix.appendixId}-${term.appendixTermId}`}
                  editable={canEdit}
                  isBilingual={Boolean(contract.languageMode === 2)}
                  canMoveUp={index > 0}
                  canMoveDown={index < appendix.terms.length - 1}
                  onChange={(field, value) =>
                    setDrafts((current) => ({
                      ...current,
                      [term.appendixTermId]: { ...term, [field]: value },
                    }))
                  }
                  onMove={(direction) => moveTerm(appendix, index, direction)}
                  onRemove={() => removeTerm(appendix, sourceTerm)}
                >
                  {canEdit && (
                    <Button
                      size="sm"
                      disabled={busyKey !== null}
                      onClick={() => saveTerm(appendix, term.appendixTermId)}
                    >
                      {busy ? <Loader2 className="size-4 animate-spin" /> : <Save className="size-4" />}
                      Lưu điều khoản
                    </Button>
                  )}
                </ContractTermCard>
              );
            })}

            {canEdit && addingTo === appendix.appendixId ? (
              <div className="grid gap-3 rounded-xl border border-dashed p-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label>Mã điều khoản</Label>
                  <Input
                    value={newTerm.code}
                    onChange={(event) => setNewTerm((current) => ({ ...current, code: event.target.value }))}
                  />
                </div>
                <div className="space-y-2">
                  <Label>Tiêu đề</Label>
                  <Input
                    value={newTerm.title}
                    onChange={(event) => setNewTerm((current) => ({ ...current, title: event.target.value }))}
                  />
                </div>
                <div className="flex gap-2 sm:col-span-2">
                  <Button size="sm" onClick={() => addTerm(appendix)}>
                    <Plus className="size-4" /> Thêm
                  </Button>
                  <Button size="sm" variant="outline" onClick={() => setAddingTo(null)}>
                    Hủy
                  </Button>
                </div>
              </div>
            ) : canEdit ? (
              <Button variant="outline" onClick={() => setAddingTo(appendix.appendixId)}>
                <Plus className="size-4" /> Thêm điều khoản
              </Button>
            ) : null}
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
