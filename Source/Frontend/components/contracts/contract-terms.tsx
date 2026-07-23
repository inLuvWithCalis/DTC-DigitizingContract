"use client";

import { useState, useEffect } from "react";
import {
  ArrowRightLeftIcon,
  ChevronLeft,
  ChevronRight,
  Plus,
  ShieldCheck,
  Trash2,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";

import {
  ContractDetailResponse,
  ContractStatus,
} from "@/services/contract-api";

export function ContractTerms({
  contract,
  setContract,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
}) {
  const isEditable = contract.status === ContractStatus.Draft || contract.status === ContractStatus.Negotiating;
  const terms = contract.currentVersion?.terms || [];

  const [currentPage, setCurrentPage] = useState(1);
  const itemsPerPage = 5;
  const totalPages = Math.ceil(terms.length / itemsPerPage);

  useEffect(() => {
    if (currentPage > totalPages && totalPages > 0) {
      setCurrentPage(totalPages);
    }
  }, [totalPages, currentPage]);

  const paginatedTerms = terms.slice(
    (currentPage - 1) * itemsPerPage,
    currentPage * itemsPerPage,
  );

  const handleTermChange = (termId: number, field: string, value: any) => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const updatedTerms = prev.currentVersion.terms.map((t) =>
        t.termId === termId ? { ...t, [field]: value } : t,
      );
      return {
        ...prev,
        currentVersion: { ...prev.currentVersion, terms: updatedTerms },
      };
    });
  };

  const handleAddTerm = () => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const newDisplayOrder = prev.currentVersion.terms.length + 1;
      const newTerm: any = {
        termId: -(Math.floor(Math.random() * 100000) + 1),
        sourceTemplateTermId: null,
        termCode: `TERM_${newDisplayOrder}`,
        termTitle: "Điều khoản mới",
        termTitleEn: null,
        termContent: "",
        termContentEn: null,
        isNegotiable: true,
        displayOrder: newDisplayOrder,
        rowVersion: "",
      };
      return {
        ...prev,
        currentVersion: {
          ...prev.currentVersion,
          terms: [...prev.currentVersion.terms, newTerm],
        },
      };
    });
    const newTotalPages = Math.ceil((terms.length + 1) / itemsPerPage);
    setCurrentPage(newTotalPages);
  };

  const handleRemoveTerm = (termId: number) => {
    setContract((prev) => {
      if (!prev || !prev.currentVersion) return prev;
      const updatedTerms = prev.currentVersion.terms.filter(
        (t) => t.termId !== termId,
      );
      const reorderedTerms = updatedTerms.map((t, i) => ({
        ...t,
        displayOrder: i + 1,
      }));
      return {
        ...prev,
        currentVersion: { ...prev.currentVersion, terms: reorderedTerms },
      };
    });
  };

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-4">
        <CardTitle className="flex items-center gap-2">
          <ShieldCheck className="size-5 text-primary" />
          Chi tiết các Điều khoản
        </CardTitle>
        {isEditable && (
          <Button variant="outline" size="sm" onClick={handleAddTerm}>
            <Plus className="size-4 mr-2" />
            Thêm Điều khoản
          </Button>
        )}
      </CardHeader>
      <CardContent className="space-y-4">
        {paginatedTerms.map((term) => (
          <div
            key={term.termId}
            className="rounded-xl border bg-muted/10 p-5 space-y-3 relative group"
          >
            <div className="flex items-start justify-between">
              <div className="flex items-center gap-3">
                <span className="text-xs font-bold text-muted-foreground uppercase bg-muted px-2 py-1 rounded">
                  Mục {term.displayOrder}: {term.termCode}
                </span>

                {/* Nút Toggle Cứng / Mềm */}
                {isEditable ? (
                  <button
                    onClick={() =>
                      handleTermChange(
                        term.termId,
                        "isNegotiable",
                        !term.isNegotiable,
                      )
                    }
                    className={`text-xs font-bold px-3 py-1 rounded-full border transition-colors flex gap-2 ${
                      term.isNegotiable
                        ? "bg-green-50 text-green-700 border-green-200"
                        : "bg-red-50 text-red-700 border-red-200"
                    }`}
                  >
                    <ArrowRightLeftIcon className="size-4" />
                    {term.isNegotiable
                      ? "Điều khoản Mềm (Cho đàm phán)"
                      : "Điều khoản Cứng (Cố định)"}
                  </button>
                ) : (
                  <span
                    className={`text-xs font-bold px-3 py-1 rounded-full ${term.isNegotiable ? "text-green-700 bg-green-50" : "text-red-700 bg-red-50"}`}
                  >
                    {term.isNegotiable ? "Cho phép đàm phán" : "Cố định"}
                  </span>
                )}
              </div>

              {isEditable && (
                <Button
                  variant="ghost"
                  size="icon"
                  className="text-destructive hover:text-destructive hover:bg-destructive/10 -mt-2 -mr-2"
                  onClick={() => handleRemoveTerm(term.termId)}
                >
                  <Trash2 className="size-4" />
                </Button>
              )}
            </div>

            {isEditable ? (
              <div className="space-y-3">
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">
                    Tiêu đề (Tiếng Việt)
                  </label>
                  <Input
                    value={term.termTitle}
                    onChange={(e) =>
                      handleTermChange(term.termId, "termTitle", e.target.value)
                    }
                    className="font-semibold"
                  />
                </div>
                <div>
                  <label className="text-xs text-muted-foreground mb-1 block">
                    Nội dung (Tiếng Việt)
                  </label>
                  <Textarea
                    value={term.termContent || ""}
                    onChange={(e) =>
                      handleTermChange(
                        term.termId,
                        "termContent",
                        e.target.value,
                      )
                    }
                    rows={3}
                  />
                </div>
              </div>
            ) : (
              <div>
                <p className="text-sm font-semibold mb-1">{term.termTitle}</p>
                <p className="text-sm leading-6 text-muted-foreground">
                  {term.termContent}
                </p>
              </div>
            )}
          </div>
        ))}

        {totalPages > 1 && (
          <div className="flex items-center justify-center gap-2 mt-4 pt-4 border-t">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              disabled={currentPage === 1}
            >
              <ChevronLeft className="size-4 mr-1" /> Trước
            </Button>
            <span className="text-sm font-medium">
              Trang {currentPage} / {totalPages}
            </span>
            <Button
              variant="outline"
              size="sm"
              onClick={() =>
                setCurrentPage((p) => Math.min(totalPages, p + 1))
              }
              disabled={currentPage === totalPages}
            >
              Sau <ChevronRight className="size-4 ml-1" />
            </Button>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
