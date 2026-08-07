"use client";

import { useState, useEffect } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Plus,
  ShieldCheck,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ContractTermComments } from "./contract-term-comments";
import {
  ContractTermCard,
  type ContractTermEditableField,
} from "./contract-term-card";

import {
  ContractDetailResponse,
  ContractLanguageMode,
  ContractNegotiationCommentResponse,
  ContractStatus,
  ContractTermDetailResponse,
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

  const handleTermChange = (
    termId: number,
    field: ContractTermEditableField,
    value: string | boolean,
  ) => {
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
      const existingCodes = new Set(
        prev.currentVersion.terms.map((term) =>
          term.termCode.toLocaleUpperCase(),
        ),
      );
      let termCodeIndex = 1;

      while (existingCodes.has(`TERM_${termCodeIndex}`)) {
        termCodeIndex += 1;
      }

      const newTerm: ContractTermDetailResponse = {
        termId: -(Math.floor(Math.random() * 100000) + 1),
        sourceTemplateTermId: null,
        termCode: `TERM_${termCodeIndex}`,
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

  const handleMoveTerm = (termId: number, direction: -1 | 1) => {
    onDraftChange?.();
    setContract((prev) => {
      if (!prev?.currentVersion) return prev;

      const currentIndex = prev.currentVersion.terms.findIndex(
        (term) => term.termId === termId,
      );
      const targetIndex = currentIndex + direction;

      if (
        currentIndex < 0 ||
        targetIndex < 0 ||
        targetIndex >= prev.currentVersion.terms.length
      ) {
        return prev;
      }

      const updatedTerms = [...prev.currentVersion.terms];
      [updatedTerms[currentIndex], updatedTerms[targetIndex]] = [
        updatedTerms[targetIndex],
        updatedTerms[currentIndex],
      ];

      return {
        ...prev,
        currentVersion: {
          ...prev.currentVersion,
          terms: updatedTerms.map((term, index) => ({
            ...term,
            displayOrder: index + 1,
          })),
        },
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
        {paginatedTerms.map((term, pageIndex) => {
          const termIndex =
            (currentPage - 1) * itemsPerPage + pageIndex;

          return (
            <ContractTermCard
              key={term.termId}
              term={term}
              inputId={String(term.termId)}
              editable={isEditable}
              isBilingual={
                contract.languageMode === ContractLanguageMode.Bilingual
              }
              canMoveUp={termIndex > 0}
              canMoveDown={termIndex < terms.length - 1}
              onChange={(field, value) =>
                handleTermChange(term.termId, field, value)
              }
              onMove={(direction) =>
                handleMoveTerm(term.termId, direction)
              }
              onRemove={() => handleRemoveTerm(term.termId)}
            >
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
            </ContractTermCard>
          );
        })}

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
