"use client";

import { Plus, ShieldCheck } from "lucide-react";

import { Button } from "@/components/ui/button";
import {
  ContractTermCard,
  type ContractTermEditableField,
} from "@/components/contracts/contract-term-card";
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
    field: ContractTermEditableField,
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
          <ContractTermCard
            key={term.id}
            term={term}
            inputId={term.id}
            editable
            isBilingual={isBilingual}
            canMoveUp={index > 0}
            canMoveDown={index < terms.length - 1}
            englishTitlePlaceholder="Để trống để giữ tiêu đề từ template"
            englishContentPlaceholder="Để trống để giữ nội dung từ template"
            onChange={(field, value) => updateTerm(term.id, field, value)}
            onMove={(direction) => moveTerm(index, direction)}
            onRemove={() => removeTerm(term.id)}
          />
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
