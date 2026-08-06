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
import { ContractTermComments } from "./contract-term-comments";

import {
  ContractDetailResponse,
  ContractLanguageMode,
  ContractNegotiationCommentResponse,
  ContractStatus,
} from "@/services/contract-api";

export function ContractTerms({
  contract,
  setContract,
  canEdit,
  onDraftChange,
}: {
  contract: ContractDetailResponse;
  setContract: React.Dispatch<
    React.SetStateAction<ContractDetailResponse | null>
  >;
  canEdit: boolean;
  onDraftChange?: () => void;
}) {
  const isEditable = canEdit;
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
    onDraftChange?.();
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
    onDraftChange?.();
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
    onDraftChange?.();
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

  const handleCommentChanged = (
    changedComment: ContractNegotiationCommentResponse,
  ) => {
    setContract((prev) => {
      if (!prev?.currentVersion) return prev;
      const currentComments = prev.currentVersion.comments || [];
      const commentExists = currentComments.some(
        (comment) => comment.commentId === changedComment.commentId,
      );
      const updatedComments = commentExists
        ? currentComments.map((comment) =>
            comment.commentId === changedComment.commentId
              ? changedComment
              : comment,
          )
        : [...currentComments, changedComment];

      return {
        ...prev,
        currentVersion: {
          ...prev.currentVersion,
          comments: updatedComments,
        },
      };
    });
  };

  return (
    <Card>
      <CardHeader className="flex flex-col gap-3 space-y-0 pb-4 sm:flex-row sm:items-center sm:justify-between">
        <CardTitle className="flex items-center gap-2">
          <ShieldCheck className="size-5 text-primary" />
          Chi tiết các Điều khoản
        </CardTitle>
        {isEditable && (
          <Button
            className="w-full sm:w-auto"
            variant="outline"
            size="sm"
            onClick={handleAddTerm}
          >
            <Plus className="size-4 mr-2" />
            Thêm Điều khoản
          </Button>
        )}
      </CardHeader>
      <CardContent className="space-y-4">
        {paginatedTerms.map((term) => (
          <div
            key={term.termId}
            className="group relative space-y-3 rounded-xl border bg-muted/10 p-4 sm:p-5"
          >
            <div className="flex items-start justify-between gap-2">
              <div className="flex min-w-0 flex-col items-start gap-2 sm:flex-row sm:items-center sm:gap-3">
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
                    className={`flex max-w-full gap-2 rounded-full border px-3 py-1 text-left text-xs font-bold transition-colors cursor-pointer ${
                      term.isNegotiable
                        ? "bg-green-50 text-green-700 border-green-200"
                        : "bg-red-50 text-red-700 border-red-200"
                    }`}
                  >
                    <ArrowRightLeftIcon className="size-4" />
                    {term.isNegotiable
                      ? "Trạng thái: Cho phép đàm phán"
                      : "Trạng thái: Cố định"}
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
                {contract.languageMode === ContractLanguageMode.Bilingual && (
                  <>
                    <div>
                      <label className="text-xs text-muted-foreground mb-1 block">
                        Tiêu đề (Tiếng Anh)
                      </label>
                      <Input
                        value={term.termTitleEn || ""}
                        onChange={(e) =>
                          handleTermChange(
                            term.termId,
                            "termTitleEn",
                            e.target.value,
                          )
                        }
                        className="font-semibold"
                        placeholder="English term title"
                      />
                    </div>
                    <div>
                      <label className="text-xs text-muted-foreground mb-1 block">
                        Nội dung (Tiếng Anh)
                      </label>
                      <Textarea
                        value={term.termContentEn || ""}
                        onChange={(e) =>
                          handleTermChange(
                            term.termId,
                            "termContentEn",
                            e.target.value,
                          )
                        }
                        rows={3}
                        placeholder="English term content"
                      />
                    </div>
                  </>
                )}
              </div>
            ) : (
              <div>
                <p className="text-sm font-semibold mb-1">{term.termTitle}</p>
                <p className="text-sm leading-6 text-muted-foreground">
                  {term.termContent}
                </p>
                {contract.languageMode === ContractLanguageMode.Bilingual && (
                  <div className="mt-3 border-t pt-3">
                    <p className="text-sm font-semibold mb-1">
                      {term.termTitleEn || "Chưa có tiêu đề tiếng Anh"}
                    </p>
                    <p className="text-sm leading-6 text-muted-foreground">
                      {term.termContentEn || "Chưa có nội dung tiếng Anh"}
                    </p>
                  </div>
                )}
              </div>
            )}

            {term.isNegotiable &&
              term.termId > 0 &&
              (contract.status === ContractStatus.Negotiating ||
                (contract.currentVersion.comments || []).some(
                  (comment) => comment.termId === term.termId,
                )) && (
                <ContractTermComments
                  contractId={contract.contractId}
                  versionId={contract.currentVersion.versionId}
                  termId={term.termId}
                  termCode={term.termCode}
                  termTitle={term.termTitle}
                  comments={contract.currentVersion.comments || []}
                  canWrite={
                    contract.status === ContractStatus.Negotiating &&
                    !contract.currentVersion.isLocked
                  }
                  onCommentChanged={handleCommentChanged}
                />
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
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
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
