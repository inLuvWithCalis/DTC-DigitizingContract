"use client";

import { useEffect, useMemo, useState } from "react";
import { Loader2, Plus, Save } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import {
  ContractTermCard,
  type ContractTermEditableField,
} from "@/components/contracts/contract-term-card";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  contractTemplateApi,
  TemplateVersionStatus,
  type ContractTemplateTermResponse,
  type ContractTemplateVersionDetailResponse,
} from "@/services/contract-template-api";
import { getContractTemplateErrorMessage } from "./contract-template-utils";

interface ContractTemplateTermsEditorProps {
  version: ContractTemplateVersionDetailResponse;
  isBilingual: boolean;
  onRefresh: () => Promise<void>;
}

const reorderTerms = (terms: ContractTemplateTermResponse[]) =>
  terms.map((term, index) => ({ ...term, displayOrder: index + 1 }));

const getFirstAvailableDisplayOrder = (
  terms: ContractTemplateTermResponse[],
) => {
  const usedOrders = new Set(terms.map((term) => term.displayOrder));
  let displayOrder = 1;
  while (usedOrders.has(displayOrder)) displayOrder += 1;
  return displayOrder;
};

export function ContractTemplateTermsEditor({
  version,
  isBilingual,
  onRefresh,
}: ContractTemplateTermsEditorProps) {
  const isDraft = version.status === TemplateVersionStatus.Draft;
  const [terms, setTerms] = useState<ContractTemplateTermResponse[]>([]);
  const [dirtyIds, setDirtyIds] = useState<Set<number>>(new Set());
  const [isOrderDirty, setIsOrderDirty] = useState(false);
  const [orderDirtyIds, setOrderDirtyIds] = useState<Set<number>>(new Set());
  const [isSavingChanges, setIsSavingChanges] = useState(false);
  const [deleteTerm, setDeleteTerm] =
    useState<ContractTemplateTermResponse | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [isAdding, setIsAdding] = useState(false);
  const [newTermCode, setNewTermCode] = useState("");
  const [newTermTitle, setNewTermTitle] = useState("");
  const [newTermTitleEn, setNewTermTitleEn] = useState("");
  const [newTermContent, setNewTermContent] = useState("");
  const [newTermContentEn, setNewTermContentEn] = useState("");

  useEffect(() => {
    setTerms(
      [...version.terms].sort((a, b) => a.displayOrder - b.displayOrder),
    );
    setDirtyIds(new Set());
    setIsOrderDirty(false);
    setOrderDirtyIds(new Set());
  }, [version]);

  const hasUnsavedContent = dirtyIds.size > 0;
  const hasUnsavedChanges = hasUnsavedContent || isOrderDirty;
  const existingCodes = useMemo(
    () => new Set(terms.map((term) => term.termCode.toLocaleUpperCase())),
    [terms],
  );

  const updateTerm = (
    termId: number,
    field: ContractTermEditableField,
    value: string | boolean,
  ) => {
    if (isOrderDirty) {
      toast.error("Vui lòng lưu thứ tự trước khi sửa nội dung điều khoản.");
      return;
    }
    setTerms((current) =>
      current.map((term) =>
        term.templateTermId === termId ? { ...term, [field]: value } : term,
      ),
    );
    setDirtyIds((current) => new Set(current).add(termId));
  };

  const moveTerm = (index: number, direction: -1 | 1) => {
    if (hasUnsavedContent) {
      toast.error(
        "Vui lòng lưu nội dung điều khoản trước khi thay đổi thứ tự.",
      );
      return;
    }
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= terms.length) return;
    const next = [...terms];
    const movedIds = [
      next[index].templateTermId,
      next[targetIndex].templateTermId,
    ];
    [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
    setTerms(reorderTerms(next));
    setIsOrderDirty(true);
    setOrderDirtyIds((current) => new Set([...current, ...movedIds]));
  };

  const saveDirtyTerms = async () => {
    const pendingTerms = terms.filter((term) =>
      dirtyIds.has(term.templateTermId),
    );
    const invalidTerm = pendingTerms.find(
      (term) => !term.termCode.trim() || !term.termTitle.trim(),
    );
    if (invalidTerm) {
      throw new Error(
        `Mã và tiêu đề điều khoản ${invalidTerm.termCode || "đang chỉnh sửa"} không được để trống.`,
      );
    }

    let latestVersion = await contractTemplateApi.getVersion(
      version.templateVersionId,
    );

    for (const term of pendingTerms) {
      const updatedTerm = await contractTemplateApi.updateTerm(
        version.templateVersionId,
        term.templateTermId,
        {
          termCode: term.termCode.trim(),
          termTitle: term.termTitle.trim(),
          termTitleEn: term.termTitleEn?.trim() || null,
          termContent: term.termContent?.trim() || null,
          termContentEn: term.termContentEn?.trim() || null,
          isNegotiable: term.isNegotiable,
          displayOrder: term.displayOrder,
          rowVersion: term.rowVersion,
          versionRowVersion: latestVersion.rowVersion,
        },
      );

      setTerms((current) =>
        current.map((item) =>
          item.templateTermId === updatedTerm.templateTermId
            ? updatedTerm
            : item,
        ),
      );
      setDirtyIds((current) => {
        const next = new Set(current);
        next.delete(updatedTerm.templateTermId);
        return next;
      });

      latestVersion = await contractTemplateApi.getVersion(
        version.templateVersionId,
      );
    }

    toast.success(
      pendingTerms.length === 1
        ? `Đã lưu điều khoản ${pendingTerms[0].termCode}.`
        : `Đã lưu ${pendingTerms.length} điều khoản.`,
    );
    await onRefresh();
  };

  const saveOrder = async () => {
    if (hasUnsavedContent) {
      toast.error("Vui lòng lưu nội dung các điều khoản trước khi lưu thứ tự.");
      return;
    }
    await contractTemplateApi.reorderTerms(version.templateVersionId, {
      versionRowVersion: version.rowVersion,
      terms: terms.map((term) => ({
        termId: term.templateTermId,
        rowVersion: term.rowVersion,
        displayOrder: term.displayOrder,
      })),
    });
    toast.success("Đã lưu thứ tự điều khoản.");
    await onRefresh();
  };

  const saveChanges = async () => {
    try {
      setIsSavingChanges(true);
      if (isOrderDirty) {
        await saveOrder();
      } else {
        await saveDirtyTerms();
      }
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsSavingChanges(false);
    }
  };

  const openAddDialog = () => {
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu các thay đổi điều khoản trước khi thêm mới.");
      return;
    }
    let index = 1;
    while (existingCodes.has(`TERM_${index}`)) index += 1;
    setNewTermCode(`TERM_${index}`);
    setNewTermTitle("");
    setNewTermTitleEn("");
    setNewTermContent("");
    setNewTermContentEn("");
    setIsAddOpen(true);
  };

  const addTerm = async () => {
    if (!newTermCode.trim() || !newTermTitle.trim()) {
      toast.error("Vui lòng nhập mã và tiêu đề điều khoản.");
      return;
    }
    if (existingCodes.has(newTermCode.trim().toLocaleUpperCase())) {
      toast.error("Mã điều khoản đã tồn tại trong version này.");
      return;
    }
    try {
      setIsAdding(true);
      await contractTemplateApi.addTerm(version.templateVersionId, {
        termCode: newTermCode.trim(),
        termTitle: newTermTitle.trim(),
        termTitleEn: newTermTitleEn.trim() || null,
        termContent: newTermContent.trim() || null,
        termContentEn: newTermContentEn.trim() || null,
        isNegotiable: true,
        displayOrder: getFirstAvailableDisplayOrder(terms),
        versionRowVersion: version.rowVersion,
      });
      toast.success("Đã thêm điều khoản.");
      setIsAddOpen(false);
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setIsAdding(false);
    }
  };

  const confirmDelete = async () => {
    if (!deleteTerm) return;
    try {
      setIsDeleting(true);
      await contractTemplateApi.deleteTerm(
        version.templateVersionId,
        deleteTerm.templateTermId,
        {
          rowVersion: deleteTerm.rowVersion,
          versionRowVersion: version.rowVersion,
        },
      );
      toast.success("Đã xóa điều khoản.");
      setDeleteTerm(null);
      await onRefresh();
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
      await onRefresh();
    } finally {
      setIsDeleting(false);
    }
  };

  const requestDeleteTerm = (term: ContractTemplateTermResponse) => {
    if (hasUnsavedChanges) {
      toast.error("Vui lòng lưu các thay đổi điều khoản trước khi xóa.");
      return;
    }
    setDeleteTerm(term);
  };

  return (
    <div className="space-y-4">
      <div className="flex flex-col justify-between gap-3 rounded-xl border bg-muted/20 p-4 sm:flex-row sm:items-center">
        <div>
          <p className="font-semibold">Điều khoản mềm ({terms.length})</p>
          <p className="mt-1 text-sm text-muted-foreground">
            {isDraft
              ? "Các card có viền đỏ là những thay đổi chưa được lưu."
              : "Version đã khóa và chỉ có thể xem."}
          </p>
        </div>
        {isDraft && (
          <div className="flex flex-wrap gap-2">
            <Button onClick={openAddDialog}>
              <Plus className="size-4" /> Thêm điều khoản
            </Button>
          </div>
        )}
      </div>

      {terms.map((term, index) => (
        <ContractTermCard
          key={term.templateTermId}
          term={term}
          inputId={`template-${term.templateTermId}`}
          className={
            dirtyIds.has(term.templateTermId) ||
            orderDirtyIds.has(term.templateTermId)
              ? "border-destructive ring-1 ring-destructive/40"
              : undefined
          }
          editable={isDraft}
          isBilingual={isBilingual}
          canMoveUp={index > 0}
          canMoveDown={index < terms.length - 1}
          onChange={(field, value) =>
            updateTerm(term.templateTermId, field, value)
          }
          onMove={(direction) => moveTerm(index, direction)}
          onRemove={() => requestDeleteTerm(term)}
        />
      ))}

      {terms.length === 0 && (
        <div className="rounded-xl border border-dashed py-12 text-center text-sm text-muted-foreground">
          Version chưa có điều khoản. Cần ít nhất một điều khoản trước khi dùng
          để tạo hợp đồng.
        </div>
      )}

      <Dialog
        open={isAddOpen}
        onOpenChange={(open) => !open && setIsAddOpen(false)}
      >
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Thêm điều khoản</DialogTitle>
            <DialogDescription>
              Điều khoản sẽ được thêm vào cuối danh sách hiện tại.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-2 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="new-term-code">
                Mã điều khoản <span className="text-destructive">*</span>
              </Label>
              <Input
                id="new-term-code"
                value={newTermCode}
                onChange={(event) => setNewTermCode(event.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="new-term-title">
                Tiêu đề <span className="text-destructive">*</span>
              </Label>
              <Input
                id="new-term-title"
                value={newTermTitle}
                onChange={(event) => setNewTermTitle(event.target.value)}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="new-term-content">Nội dung</Label>
              <Textarea
                id="new-term-content"
                value={newTermContent}
                onChange={(event) => setNewTermContent(event.target.value)}
              />
            </div>
            {isBilingual && (
              <>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="new-term-title-en">Tiêu đề tiếng Anh</Label>
                  <Input
                    id="new-term-title-en"
                    value={newTermTitleEn}
                    onChange={(event) => setNewTermTitleEn(event.target.value)}
                  />
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="new-term-content-en">
                    Nội dung tiếng Anh
                  </Label>
                  <Textarea
                    id="new-term-content-en"
                    value={newTermContentEn}
                    onChange={(event) =>
                      setNewTermContentEn(event.target.value)
                    }
                  />
                </div>
              </>
            )}
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setIsAddOpen(false)}
              disabled={isAdding}
            >
              Hủy
            </Button>
            <Button onClick={addTerm} disabled={isAdding}>
              {isAdding && <Loader2 className="size-4 animate-spin" />} Thêm
              điều khoản
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        isOpen={Boolean(deleteTerm)}
        onClose={() => setDeleteTerm(null)}
        onConfirm={confirmDelete}
        title="Xóa điều khoản?"
        description={`Điều khoản ${deleteTerm?.termCode ?? ""} sẽ bị xóa khỏi bản nháp. Các điều khoản còn lại không tự đổi thứ tự cho đến khi bạn lưu lại thứ tự.`}
        confirmText="Xóa điều khoản"
        variant="destructive"
        isLoading={isDeleting}
      />

      {isDraft && hasUnsavedChanges && (
        <Alert
          className="fixed bottom-6 right-6 z-50 w-[calc(100%-3rem)] max-w-md animate-in fade-in-0 slide-in-from-bottom-8 border-destructive/50 bg-background shadow-lg duration-300"
          aria-live="polite"
        >
          <AlertTitle>Điều khoản có thay đổi chưa được lưu</AlertTitle>
          <AlertDescription className="mt-2 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <span>
              {isOrderDirty
                ? "Thứ tự điều khoản đã thay đổi."
                : `${dirtyIds.size} điều khoản đang có nội dung chỉnh sửa.`}
            </span>
            <Button
              onClick={saveChanges}
              disabled={isSavingChanges}
              size="sm"
              className="shrink-0"
            >
              {isSavingChanges ? (
                <Loader2 className="mr-2 size-4 animate-spin" />
              ) : (
                <Save className="mr-2 size-4" />
              )}
              Lưu thay đổi
            </Button>
          </AlertDescription>
        </Alert>
      )}
    </div>
  );
}
