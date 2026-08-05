"use client";

import { useEffect, useMemo, useState } from "react";
import {
  ArrowLeft,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Loader2,
  MessageSquareText,
  Reply,
  RotateCcw,
  Send,
  X,
} from "lucide-react";
import { toast } from "sonner";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { ScrollArea } from "@/components/ui/scroll-area";
import { Separator } from "@/components/ui/separator";
import { Textarea } from "@/components/ui/textarea";
import { cn } from "@/lib/utils";
import { employeeApi } from "@/services/employees-api";
import {
  contractApi,
  ContractNegotiationCommentResponse,
  ContractNegotiationCommentState,
} from "@/services/contract-api";

const COMMENTS_PAGE_SIZE = 5;

type ReplyTarget = {
  commentId: number;
  employeeName: string;
};

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });

const getApiErrorMessage = (error: any, fallback: string) => {
  const data = error?.response?.data;
  return data?.errors
    ? Object.values(data.errors).flat().join("; ")
    : data?.message ||
        data?.title ||
        (typeof data === "string" ? data : null) ||
        fallback;
};

const getInitials = (name: string) => {
  const initials = name
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(-2)
    .map((part) => part.charAt(0).toUpperCase())
    .join("");

  return initials || "NV";
};

function CommentPagination({
  currentPage,
  totalPages,
  onPageChange,
}: {
  currentPage: number;
  totalPages: number;
  onPageChange: (page: number) => void;
}) {
  if (totalPages <= 1) return null;

  return (
    <div className="flex items-center justify-between gap-3 border-t pt-3">
      <Button
        variant="outline"
        size="sm"
        onClick={() => onPageChange(Math.max(1, currentPage - 1))}
        disabled={currentPage === 1}
      >
        <ChevronLeft className="mr-1 size-4" />
        Trước
      </Button>
      <span className="text-xs font-medium text-muted-foreground">
        Trang {currentPage} / {totalPages}
      </span>
      <Button
        variant="outline"
        size="sm"
        onClick={() => onPageChange(Math.min(totalPages, currentPage + 1))}
        disabled={currentPage === totalPages}
      >
        Sau
        <ChevronRight className="ml-1 size-4" />
      </Button>
    </div>
  );
}

export function ContractTermComments({
  contractId,
  versionId,
  termId,
  termCode,
  termTitle,
  comments,
  canWrite,
  onCommentChanged,
  triggerClassName,
}: {
  contractId: number;
  versionId: number;
  termId: number | null;
  termCode?: string;
  termTitle?: string;
  comments: ContractNegotiationCommentResponse[];
  canWrite: boolean;
  onCommentChanged?: (comment: ContractNegotiationCommentResponse) => void;
  triggerClassName?: string;
}) {
  const isGeneralDiscussion = termId == null;
  const discussionLabel = isGeneralDiscussion
    ? "Trao đổi chung"
    : "Trao đổi điều khoản";
  const discussionDescription = isGeneralDiscussion
    ? "Nội dung trao đổi chung của hợp đồng"
    : [termCode, termTitle].filter(Boolean).join(" · ") ||
      "Nội dung trao đổi của điều khoản";
  const matchesTerm = (comment: ContractNegotiationCommentResponse) =>
    (comment.termId ?? null) === termId;

  const initialRootComments = useMemo(
    () =>
      comments.filter(
        (comment) =>
          comment.versionId === versionId &&
          (comment.termId ?? null) === termId &&
          !comment.parentCommentId,
      ),
    [comments, termId, versionId],
  );

  const [isOpen, setIsOpen] = useState(false);
  const [rootComments, setRootComments] = useState(initialRootComments);
  const [childComments, setChildComments] = useState<
    ContractNegotiationCommentResponse[]
  >([]);
  const [selectedParentId, setSelectedParentId] = useState<number | null>(null);
  const [rootPage, setRootPage] = useState(1);
  const [content, setContent] = useState("");
  const [replyTarget, setReplyTarget] = useState<ReplyTarget | null>(null);
  const [employeeNames, setEmployeeNames] = useState<Record<number, string>>(
    {},
  );
  const [isLoadingRoots, setIsLoadingRoots] = useState(false);
  const [isLoadingChildren, setIsLoadingChildren] = useState(false);
  const [rootError, setRootError] = useState<string | null>(null);
  const [childError, setChildError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [processingCommentId, setProcessingCommentId] = useState<number | null>(
    null,
  );

  const termRootComments = rootComments.filter(
    (comment) => comment.versionId === versionId && matchesTerm(comment),
  );
  const selectedParent =
    termRootComments.find(
      (comment) => comment.commentId === selectedParentId,
    ) ?? null;
  const openRootCount = termRootComments.filter(
    (comment) => comment.state === ContractNegotiationCommentState.Open,
  ).length;
  const rootTotalPages = Math.max(
    1,
    Math.ceil(termRootComments.length / COMMENTS_PAGE_SIZE),
  );
  const paginatedRoots = termRootComments.slice(
    (rootPage - 1) * COMMENTS_PAGE_SIZE,
    rootPage * COMMENTS_PAGE_SIZE,
  );

  useEffect(() => {
    if (!isOpen) {
      setRootComments(initialRootComments);
    }
  }, [initialRootComments, isOpen]);

  useEffect(() => {
    setRootPage((page) => Math.min(Math.max(page, 1), rootTotalPages));
  }, [rootTotalPages]);

  const getEmployeeName = (employeeId: number) =>
    employeeNames[employeeId] || `Nhân viên #${employeeId}`;

  const loadEmployeeNames = async (
    targetComments: ContractNegotiationCommentResponse[],
  ) => {
    const employeeIds = Array.from(
      new Set(targetComments.map((comment) => comment.recordedByEmployeeId)),
    );
    if (employeeIds.length === 0) return;

    const entries = await Promise.all(
      employeeIds.map(async (employeeId) => {
        try {
          const employee = await employeeApi.getById(employeeId);
          return [
            employeeId,
            employee.employeeFullName?.trim() || `Nhân viên #${employeeId}`,
          ] as const;
        } catch {
          return [employeeId, `Nhân viên #${employeeId}`] as const;
        }
      }),
    );

    setEmployeeNames((current) => ({
      ...current,
      ...Object.fromEntries(entries),
    }));
  };

  const loadRootComments = async () => {
    setIsLoadingRoots(true);
    setRootError(null);
    try {
      const result = await contractApi.getRootComments(contractId);
      setRootComments(result);
      await loadEmployeeNames(result);
    } catch (error: any) {
      console.error("Lỗi tải comment cha:", error);
      setRootError(
        getApiErrorMessage(
          error,
          "Không thể tải danh sách trao đổi của điều khoản.",
        ),
      );
    } finally {
      setIsLoadingRoots(false);
    }
  };

  const loadChildComments = async (parentCommentId: number) => {
    setIsLoadingChildren(true);
    setChildError(null);
    setChildComments([]);
    try {
      const result = await contractApi.getCommentReplies(
        contractId,
        parentCommentId,
      );
      const termReplies = result.filter(
        (comment) =>
          comment.versionId === versionId && matchesTerm(comment),
      );
      setChildComments(termReplies);
      await loadEmployeeNames(termReplies);
    } catch (error: any) {
      console.error("Lỗi tải comment con:", error);
      setChildError(
        getApiErrorMessage(error, "Không thể tải các câu trả lời."),
      );
    } finally {
      setIsLoadingChildren(false);
    }
  };

  const handleOpenChange = (open: boolean) => {
    setIsOpen(open);
    setContent("");
    setReplyTarget(null);
    setSelectedParentId(null);
    setChildComments([]);
    setRootPage(1);

    if (open) {
      void loadRootComments();
    }
  };

  const handleSelectParent = (
    parentComment: ContractNegotiationCommentResponse,
  ) => {
    setSelectedParentId(parentComment.commentId);
    setContent("");
    setReplyTarget(null);
    void loadChildComments(parentComment.commentId);
  };

  const handleBackToRoots = () => {
    setSelectedParentId(null);
    setChildComments([]);
    setContent("");
    setReplyTarget(null);
    setChildError(null);
  };

  const handleSubmit = async () => {
    const normalizedContent = content.trim();
    if (!normalizedContent) {
      toast.error("Vui lòng nhập nội dung phản hồi.");
      return;
    }

    const submittedContent = replyTarget
      ? `@${replyTarget.employeeName} ${normalizedContent}`
      : normalizedContent;

    setIsSubmitting(true);
    try {
      const createdComment = await contractApi.createExternalFeedback(
        contractId,
        {
          currentVersionId: versionId,
          termId,
          parentCommentId: selectedParent?.commentId ?? null,
          content: submittedContent,
        },
      );

      onCommentChanged?.(createdComment);
      await loadEmployeeNames([createdComment]);
      setContent("");
      setReplyTarget(null);

      if (selectedParent) {
        setChildComments((current) => [...current, createdComment]);
        toast.success("Đã gửi câu trả lời.");
      } else {
        setRootComments((current) => [...current, createdComment]);
        setRootPage(
          Math.max(
            1,
            Math.ceil((termRootComments.length + 1) / COMMENTS_PAGE_SIZE),
          ),
        );
        toast.success("Đã tạo trao đổi mới.");
      }
    } catch (error: any) {
      console.error("Lỗi gửi comment:", error);
      toast.error(
        getApiErrorMessage(error, "Không thể gửi phản hồi. Vui lòng thử lại."),
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleChangeState = async (
    comment: ContractNegotiationCommentResponse,
    action: "resolve" | "reopen",
  ) => {
    setProcessingCommentId(comment.commentId);
    try {
      const updatedComment =
        action === "resolve"
          ? await contractApi.resolveComment(contractId, comment.commentId, {
              rowVersion: comment.rowVersion,
            })
          : await contractApi.reopenComment(contractId, comment.commentId, {
              rowVersion: comment.rowVersion,
            });

      if (updatedComment.parentCommentId) {
        setChildComments((current) =>
          current.map((item) =>
            item.commentId === updatedComment.commentId ? updatedComment : item,
          ),
        );
      } else {
        setRootComments((current) =>
          current.map((item) =>
            item.commentId === updatedComment.commentId ? updatedComment : item,
          ),
        );
      }

      onCommentChanged?.(updatedComment);
      toast.success(
        action === "resolve"
          ? "Đã đánh dấu trao đổi là đã xử lý."
          : "Đã mở lại trao đổi.",
      );
    } catch (error: any) {
      console.error("Lỗi cập nhật trạng thái comment:", error);
      toast.error(
        getApiErrorMessage(error, "Không thể cập nhật trạng thái phản hồi."),
      );
    } finally {
      setProcessingCommentId(null);
    }
  };

  const beginReply = (comment: ContractNegotiationCommentResponse) => {
    setReplyTarget({
      commentId: comment.commentId,
      employeeName: getEmployeeName(comment.recordedByEmployeeId),
    });
  };

  const renderComposer = (isThreadReply: boolean) => {
    const threadIsResolved =
      selectedParent?.state === ContractNegotiationCommentState.Resolved;
    if (!canWrite || (isThreadReply && threadIsResolved)) return null;

    return (
      <div className="space-y-2 rounded-xl border bg-muted/20 p-3">
        {replyTarget && (
          <div className="flex items-center justify-between gap-3 rounded-lg bg-primary/10 px-3 py-2 text-sm">
            <span className="min-w-0 truncate">
              Đang trả lời
              <strong className="ml-1 text-primary">
                @{replyTarget.employeeName}
              </strong>
            </span>
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="size-7 shrink-0"
              onClick={() => setReplyTarget(null)}
              aria-label="Hủy trả lời"
            >
              <X className="size-4" />
            </Button>
          </div>
        )}
        <Textarea
          value={content}
          onChange={(event) => setContent(event.target.value)}
          placeholder={
            isThreadReply
              ? "Viết câu trả lời..."
              : "Bắt đầu một trao đổi mới..."
          }
          rows={3}
          maxLength={4000}
          disabled={isSubmitting}
        />
        <div className="flex items-center justify-between gap-3">
          <span className="text-xs text-muted-foreground">
            {content.length}/4000
          </span>
          <Button
            size="sm"
            onClick={() => void handleSubmit()}
            disabled={isSubmitting || !content.trim()}
          >
            {isSubmitting ? (
              <Loader2 className="mr-2 size-4 animate-spin" />
            ) : (
              <Send className="mr-2 size-4" />
            )}
            {isThreadReply ? "Gửi trả lời" : "Tạo trao đổi"}
          </Button>
        </div>
      </div>
    );
  };

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button
          variant="outline"
          size="sm"
          className={cn("mt-4 w-fit", triggerClassName)}
        >
          <MessageSquareText className="mr-2 size-4 text-primary" />
          {discussionLabel}
          <Badge variant="secondary" className="ml-2">
            {termRootComments.length}
          </Badge>
          {openRootCount > 0 && (
            <span className="ml-1 text-xs text-muted-foreground">
              ({openRootCount} đang mở)
            </span>
          )}
        </Button>
      </DialogTrigger>

      <DialogContent className="max-h-[92vh] overflow-hidden sm:max-w-4xl">
        {selectedParent ? (
          <>
            <DialogHeader>
              <div className="mb-1 flex items-center gap-2">
                <Button
                  variant="ghost"
                  size="sm"
                  className="-ml-2"
                  onClick={handleBackToRoots}
                >
                  <ArrowLeft className="mr-1.5 size-4" />
                  Danh sách trao đổi
                </Button>
              </div>
              <DialogTitle className="pr-8 text-left leading-6">
                {selectedParent.content}
              </DialogTitle>
              <DialogDescription className="text-left">
                {discussionDescription}
              </DialogDescription>
              <div className="space-y-3 text-left">
                <div className="flex items-center gap-2">
                  <Badge
                    variant={
                      selectedParent.state ===
                      ContractNegotiationCommentState.Resolved
                        ? "secondary"
                        : "outline"
                    }
                  >
                    {selectedParent.state ===
                    ContractNegotiationCommentState.Resolved
                      ? "Đã xử lý"
                      : "Đang mở"}
                  </Badge>
                  <span className="text-xs text-muted-foreground">
                    <span className="font-medium text-foreground">
                      {getEmployeeName(selectedParent.recordedByEmployeeId)}
                    </span>{" "}
                    · {formatDateTime(selectedParent.createdDate)}
                  </span>
                </div>
                {canWrite && (
                  <div className="flex flex-wrap gap-2">
                    {selectedParent.state ===
                      ContractNegotiationCommentState.Open && (
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => beginReply(selectedParent)}
                      >
                        <Reply className="mr-1.5 size-3.5" />
                        Trả lời
                      </Button>
                    )}
                    <Button
                      variant="default"
                      size="sm"
                      disabled={
                        processingCommentId === selectedParent.commentId
                      }
                      onClick={() =>
                        void handleChangeState(
                          selectedParent,
                          selectedParent.state ===
                            ContractNegotiationCommentState.Resolved
                            ? "reopen"
                            : "resolve",
                        )
                      }
                    >
                      {processingCommentId === selectedParent.commentId ? (
                        <Loader2 className="mr-1.5 size-3.5 animate-spin" />
                      ) : selectedParent.state ===
                        ContractNegotiationCommentState.Resolved ? (
                        <RotateCcw className="mr-1.5 size-3.5" />
                      ) : (
                        <CheckCircle2 className="mr-1.5 size-3.5" />
                      )}
                      {selectedParent.state ===
                      ContractNegotiationCommentState.Resolved
                        ? "Mở lại"
                        : "Đã xử lý"}
                    </Button>
                  </div>
                )}
              </div>
            </DialogHeader>

            <div className="space-y-4">
              {renderComposer(true)}

              <div className="flex items-center justify-between gap-3">
                <p className="text-sm font-semibold">
                  Câu trả lời ({childComments.length})
                </p>
              </div>
              <Separator />

              <ScrollArea className="h-[34vh] pr-4">
                {isLoadingChildren ? (
                  <div className="flex h-32 items-center justify-center gap-2 text-sm text-muted-foreground">
                    <Loader2 className="size-4 animate-spin" />
                    Đang tải câu trả lời...
                  </div>
                ) : childError ? (
                  <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-center">
                    <p className="text-sm text-destructive">{childError}</p>
                    <Button
                      variant="outline"
                      size="sm"
                      className="mt-3"
                      onClick={() =>
                        void loadChildComments(selectedParent.commentId)
                      }
                    >
                      Thử lại
                    </Button>
                  </div>
                ) : childComments.length === 0 ? (
                  <div className="rounded-lg border border-dashed px-4 py-8 text-center">
                    <MessageSquareText className="mx-auto size-8 text-muted-foreground/50" />
                    <p className="mt-2 text-sm font-medium">
                      Chưa có câu trả lời
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Bạn có thể tiếp tục cuộc trao đổi ở phía trên.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-4">
                    {childComments.map((comment) => {
                      const employeeName = getEmployeeName(
                        comment.recordedByEmployeeId,
                      );
                      const isResolved =
                        comment.state ===
                        ContractNegotiationCommentState.Resolved;
                      const isProcessing =
                        processingCommentId === comment.commentId;

                      return (
                        <div
                          key={comment.commentId}
                          className="flex items-start gap-3"
                        >
                          <Avatar className="size-8 border">
                            <AvatarFallback className="bg-sky-500/10 text-[11px] font-semibold text-sky-700">
                              {getInitials(employeeName)}
                            </AvatarFallback>
                          </Avatar>
                          <div className="min-w-0 flex-1">
                            <div className="flex flex-wrap items-baseline gap-x-2 gap-y-1">
                              <span className="text-sm font-semibold">
                                {employeeName}
                              </span>
                              <span className="text-xs text-muted-foreground">
                                {formatDateTime(comment.createdDate)}
                              </span>
                              {isResolved && (
                                <Badge variant="secondary" className="h-5">
                                  Đã xử lý
                                </Badge>
                              )}
                            </div>
                            <div className="mt-1.5 rounded-lg bg-muted px-3 py-2.5">
                              <p className="whitespace-pre-wrap text-sm leading-6">
                                {comment.content}
                              </p>
                            </div>
                            {canWrite &&
                              selectedParent.state ===
                                ContractNegotiationCommentState.Open && (
                                <div className="mt-1 flex flex-wrap gap-1">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-7 px-2 text-xs"
                                    onClick={() => beginReply(comment)}
                                  >
                                    <Reply className="mr-1 size-3" />
                                    Trả lời
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-7 px-2 text-xs"
                                    disabled={isProcessing}
                                    onClick={() =>
                                      void handleChangeState(
                                        comment,
                                        isResolved ? "reopen" : "resolve",
                                      )
                                    }
                                  >
                                    {isProcessing ? (
                                      <Loader2 className="mr-1 size-3 animate-spin" />
                                    ) : isResolved ? (
                                      <RotateCcw className="mr-1 size-3" />
                                    ) : (
                                      <CheckCircle2 className="mr-1 size-3" />
                                    )}
                                    {isResolved ? "Mở lại" : "Đã xử lý"}
                                  </Button>
                                </div>
                              )}
                          </div>
                        </div>
                      );
                    })}
                  </div>
                )}
              </ScrollArea>
            </div>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle className="flex items-center gap-2">
                <MessageSquareText className="size-5 text-primary" />
                {discussionLabel}
              </DialogTitle>
              <DialogDescription>
                {discussionDescription}
              </DialogDescription>
            </DialogHeader>

            <div className="space-y-4">
              {renderComposer(false)}

              <div className="flex flex-wrap items-center justify-between gap-2">
                <p className="text-sm font-semibold">
                  Danh sách trao đổi ({termRootComments.length})
                </p>
                {openRootCount > 0 && (
                  <Badge variant="outline">{openRootCount} đang mở</Badge>
                )}
              </div>
              <Separator />

              <ScrollArea className="h-[45vh] pr-4">
                {isLoadingRoots ? (
                  <div className="flex h-40 items-center justify-center gap-2 text-sm text-muted-foreground">
                    <Loader2 className="size-4 animate-spin" />
                    Đang tải danh sách trao đổi...
                  </div>
                ) : rootError ? (
                  <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-4 text-center">
                    <p className="text-sm text-destructive">{rootError}</p>
                    <Button
                      variant="outline"
                      size="sm"
                      className="mt-3"
                      onClick={() => void loadRootComments()}
                    >
                      Thử lại
                    </Button>
                  </div>
                ) : termRootComments.length === 0 ? (
                  <div className="rounded-lg border border-dashed px-4 py-10 text-center">
                    <MessageSquareText className="mx-auto size-9 text-muted-foreground/50" />
                    <p className="mt-3 text-sm font-medium">
                      Chưa có trao đổi nào
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Tạo trao đổi đầu tiên cho điều khoản này.
                    </p>
                  </div>
                ) : (
                  <div className="space-y-3">
                    {paginatedRoots.map((comment) => {
                      const employeeName = getEmployeeName(
                        comment.recordedByEmployeeId,
                      );
                      const isResolved =
                        comment.state ===
                        ContractNegotiationCommentState.Resolved;

                      return (
                        <button
                          key={comment.commentId}
                          type="button"
                          onClick={() => handleSelectParent(comment)}
                          className="group w-full rounded-xl border bg-background p-4 text-left transition-colors hover:border-primary/40 hover:bg-muted/30 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                        >
                          <div className="flex items-start gap-3">
                            <Avatar className="size-9 border">
                              <AvatarFallback className="bg-primary/10 text-xs font-semibold text-primary">
                                {getInitials(employeeName)}
                              </AvatarFallback>
                            </Avatar>
                            <div className="min-w-0 flex-1">
                              <div className="flex flex-wrap items-center justify-between gap-2">
                                <div>
                                  <p className="text-sm font-semibold">
                                    {employeeName}
                                  </p>
                                  <p className="text-xs text-muted-foreground">
                                    {formatDateTime(comment.createdDate)}
                                  </p>
                                </div>
                                <Badge
                                  variant={isResolved ? "secondary" : "outline"}
                                >
                                  {isResolved ? "Đã xử lý" : "Đang mở"}
                                </Badge>
                              </div>
                              <p className="mt-3 line-clamp-2 whitespace-pre-wrap text-sm leading-6">
                                {comment.content}
                              </p>
                              <span className="mt-3 inline-flex items-center text-xs font-medium text-primary">
                                Xem cuộc trao đổi
                                <ChevronRight className="ml-1 size-3.5 transition-transform group-hover:translate-x-0.5" />
                              </span>
                            </div>
                          </div>
                        </button>
                      );
                    })}

                    <CommentPagination
                      currentPage={rootPage}
                      totalPages={rootTotalPages}
                      onPageChange={setRootPage}
                    />
                  </div>
                )}
              </ScrollArea>
            </div>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
