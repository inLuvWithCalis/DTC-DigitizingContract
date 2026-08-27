"use client";

import { useMemo, useState } from "react";
import { Plus, ShieldCheck } from "lucide-react";

import {
  ContractTermCard,
  type ContractTermEditableField,
} from "@/components/contracts/contract-term-card";
import { Button } from "@/components/ui/button";
import { CodeInput } from "@/components/ui/custom/code-input";
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
import { toast } from "@/components/ui/sonner";

export interface CreateContractTermDraft {
  clientId: string;
  sourceTemplateTermId?: number | null;
  termCode: string;
  termTitle: string;
  termTitleEn?: string | null;
  termContent?: string | null;
  termContentEn?: string | null;
  isNegotiable: boolean;
  displayOrder: number;
}

interface CreateContractTermsEditorProps {
  terms: CreateContractTermDraft[];
  templateName?: string;
  isBilingual: boolean;
  onChange: (terms: CreateContractTermDraft[]) => void;
}

const reorderTerms = (terms: CreateContractTermDraft[]) =>
  terms.map((term, index) => ({ ...term, displayOrder: index + 1 }));

export function CreateContractTermsEditor({
  terms,
  templateName,
  isBilingual,
  onChange,
}: CreateContractTermsEditorProps) {
  const [dirtyIds, setDirtyIds] = useState<Set<string>>(new Set());
  const [deleteTerm, setDeleteTerm] =
    useState<CreateContractTermDraft | null>(null);
  const [isAddOpen, setIsAddOpen] = useState(false);
  const [newTermCode, setNewTermCode] = useState("");
  const [newTermTitle, setNewTermTitle] = useState("");
  const [newTermTitleEn, setNewTermTitleEn] = useState("");
  const [newTermContent, setNewTermContent] = useState("");
  const [newTermContentEn, setNewTermContentEn] = useState("");

  const existingCodes = useMemo(
    () => new Set(terms.map((term) => term.termCode.trim().toUpperCase())),
    [terms],
  );

  const updateTerm = (
    clientId: string,
    field: ContractTermEditableField,
    value: string | boolean,
  ) => {
    onChange(
      terms.map((term) =>
        term.clientId === clientId ? { ...term, [field]: value } : term,
      ),
    );
    setDirtyIds((current) => new Set(current).add(clientId));
  };

  const moveTerm = (index: number, direction: -1 | 1) => {
    const targetIndex = index + direction;
    if (targetIndex < 0 || targetIndex >= terms.length) return;

    const next = [...terms];
    const affectedIds = [next[index].clientId, next[targetIndex].clientId];
    [next[index], next[targetIndex]] = [next[targetIndex], next[index]];
    onChange(reorderTerms(next));
    setDirtyIds(
      (current) => new Set([...current, ...affectedIds]),
    );
  };

  const openAddDialog = () => {
    let index = 1;
    while (existingCodes.has(`CUSTOM_${index}`)) index += 1;
    setNewTermCode(`CUSTOM_${index}`);
    setNewTermTitle("");
    setNewTermTitleEn("");
    setNewTermContent("");
    setNewTermContentEn("");
    setIsAddOpen(true);
  };

  const addTerm = () => {
    const termCode = newTermCode.trim().toUpperCase();
    if (!termCode || !newTermTitle.trim()) {
      toast.error("Vui lòng nhập mã và tiêu đề điều khoản.");
      return;
    }
    if (existingCodes.has(termCode)) {
      toast.error("Mã điều khoản đã tồn tại trong hợp đồng.");
      return;
    }
    if (isBilingual && !newTermTitleEn.trim()) {
      toast.error("Điều khoản song ngữ phải có tiêu đề tiếng Anh.");
      return;
    }
    if (
      isBilingual &&
      newTermContent.trim() &&
      !newTermContentEn.trim()
    ) {
      toast.error("Điều khoản song ngữ phải có nội dung tiếng Anh.");
      return;
    }

    const clientId = `custom-${Date.now()}-${Math.random().toString(36).slice(2)}`;
    onChange([
      ...terms,
      {
        clientId,
        sourceTemplateTermId: null,
        termCode,
        termTitle: newTermTitle.trim(),
        termTitleEn: newTermTitleEn.trim() || null,
        termContent: newTermContent.trim() || null,
        termContentEn: newTermContentEn.trim() || null,
        isNegotiable: true,
        displayOrder: terms.length + 1,
      },
    ]);
    setDirtyIds((current) => new Set(current).add(clientId));
    setIsAddOpen(false);
  };

  const confirmDelete = () => {
    if (!deleteTerm) return;
    onChange(
      reorderTerms(
        terms.filter((term) => term.clientId !== deleteTerm.clientId),
      ),
    );
    setDirtyIds((current) => {
      const next = new Set(current);
      next.delete(deleteTerm.clientId);
      return next;
    });
    setDeleteTerm(null);
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
      <div className="flex flex-col justify-between gap-3 rounded-xl border bg-muted/20 p-4 sm:flex-row sm:items-center">
        <div>
          <p className="font-semibold">Điều khoản hợp đồng ({terms.length})</p>
          <p className="mt-1 text-sm text-muted-foreground">
            {templateName}. Các thay đổi sẽ được lưu cùng hợp đồng nháp.
          </p>
        </div>
        <Button type="button" onClick={openAddDialog}>
          <Plus className="size-4" /> Thêm điều khoản
        </Button>
      </div>

      {terms.map((term, index) => (
        <ContractTermCard
          key={term.clientId}
          term={term}
          inputId={`create-${term.clientId}`}
          className={
            dirtyIds.has(term.clientId)
              ? "border-destructive ring-1 ring-destructive/40"
              : undefined
          }
          editable
          isBilingual={isBilingual}
          canMoveUp={index > 0}
          canMoveDown={index < terms.length - 1}
          onChange={(field, value) =>
            updateTerm(term.clientId, field, value)
          }
          onMove={(direction) => moveTerm(index, direction)}
          onRemove={() => setDeleteTerm(term)}
        />
      ))}

      {terms.length === 0 && (
        <div className="rounded-xl border border-dashed px-6 py-10 text-center text-sm text-muted-foreground">
          Hợp đồng cần ít nhất một điều khoản. Hãy thêm điều khoản mới để tiếp
          tục.
        </div>
      )}

      <Dialog open={isAddOpen} onOpenChange={setIsAddOpen}>
        <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Thêm điều khoản hợp đồng</DialogTitle>
            <DialogDescription>
              Điều khoản mới sẽ được thêm vào cuối danh sách và không thay đổi
              template gốc.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-2 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="create-new-term-code">
                Mã điều khoản <span className="text-destructive">*</span>
              </Label>
              <CodeInput
                id="create-new-term-code"
                value={newTermCode}
                onValueChange={setNewTermCode}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="create-new-term-title">
                Tiêu đề <span className="text-destructive">*</span>
              </Label>
              <Input
                id="create-new-term-title"
                value={newTermTitle}
                onChange={(event) => setNewTermTitle(event.target.value)}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="create-new-term-content">Nội dung</Label>
              <Textarea
                id="create-new-term-content"
                className="min-h-28"
                value={newTermContent}
                onChange={(event) => setNewTermContent(event.target.value)}
              />
            </div>
            {isBilingual && (
              <>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="create-new-term-title-en">
                    Tiêu đề tiếng Anh{" "}
                    <span className="text-destructive">*</span>
                  </Label>
                  <Input
                    id="create-new-term-title-en"
                    value={newTermTitleEn}
                    onChange={(event) => setNewTermTitleEn(event.target.value)}
                  />
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="create-new-term-content-en">
                    Nội dung tiếng Anh
                  </Label>
                  <Textarea
                    id="create-new-term-content-en"
                    className="min-h-28"
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
              type="button"
              variant="outline"
              onClick={() => setIsAddOpen(false)}
            >
              Hủy
            </Button>
            <Button type="button" onClick={addTerm}>
              <Plus className="size-4" /> Thêm điều khoản
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        isOpen={Boolean(deleteTerm)}
        onClose={() => setDeleteTerm(null)}
        onConfirm={confirmDelete}
        title="Xóa điều khoản?"
        description={`Điều khoản ${deleteTerm?.termCode ?? ""} sẽ không được đưa vào hợp đồng mới. Template gốc không bị thay đổi.`}
        confirmText="Xóa điều khoản"
        variant="destructive"
      />
    </div>
  );
}
