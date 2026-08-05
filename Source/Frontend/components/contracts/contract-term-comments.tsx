"use client";

import { useEffect, useState } from "react";
import {
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  CornerDownRight,
  Loader2,
  MessageSquareText,
  Reply,
  RotateCcw,
  Send,
} from "lucide-react";
import { toast } from "sonner";

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
import {
  contractApi,
  ContractNegotiationCommentResponse,
  ContractNegotiationCommentState,
} from "@/services/contract-api";

const COMMENTS_PAGE_SIZE = 5;

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

export function ContractTermComments({
  contractId,
  versionId,
  termId,
  termCode,
  termTitle,
  comments,
  canWrite,
  onCommentChanged,
}: {
  contractId: number;
  versionId: number;
  termId: number;
  termCode: string;
  termTitle: string;
  comments: ContractNegotiationCommentResponse[];
  canWrite: boolean;
  onCommentChanged: (comment: ContractNegotiationCommentResponse) => void;
}) {
  const termComments = comments.filter((comment) => comment.termId === termId);
  const openCommentsCount = termComments.filter(
    (comment) => comment.state === ContractNegotiationCommentState.Open,
  ).length;
  const [currentPage, setCurrentPage] = useState(1);
  const [content, setContent] = useState("");
  const [replyTo, setReplyTo] = useState<number | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [processingCommentId, setProcessingCommentId] = useState<number | null>(
    null,
  );

  const totalPages = Math.max(
    1,
    Math.ceil(termComments.length / COMMENTS_PAGE_SIZE),
  );
  const paginatedComments = termComments.slice(
    (currentPage - 1) * COMMENTS_PAGE_SIZE,
    currentPage * COMMENTS_PAGE_SIZE,
  );

  useEffect(() => {
    setCurrentPage((page) => Math.min(Math.max(page, 1), totalPages));
  }, [totalPages]);

  const handleSubmit = async () => {
    const normalizedContent = content.trim();
    if (!normalizedContent) {
      toast.error("Vui lòng nhập nội dung phản hồi.");
      return;
    }

    setIsSubmitting(true);
    try {
      const createdComment = await contractApi.createExternalFeedback(
        contractId,
        {
          currentVersionId: versionId,
          termId,
          parentCommentId: replyTo,
          content: normalizedContent,
        },
      );
      onCommentChanged(createdComment);
      setContent("");
      setReplyTo(null);
      setCurrentPage(
        Math.max(
          1,
          Math.ceil((termComments.length + 1) / COMMENTS_PAGE_SIZE),
        ),
      );
      toast.success(replyTo ? "Đã gửi câu trả lời." : "Đã gửi phản hồi.");
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
      onCommentChanged(updatedComment);
      toast.success(
        action === "resolve"
          ? "Đã đánh dấu phản hồi là đã xử lý."
          : "Đã mở lại phản hồi.",
      );
    } catch (error: any) {
      console.error("Lỗi cập nhật trạng thái comment:", error);
      toast.error(
        getApiErrorMessage(
          error,
          "Không thể cập nhật trạng thái phản hồi.",
        ),
      );
    } finally {
      setProcessingCommentId(null);
    }
  };

  return (
    <Dialog
      onOpenChange={(open) => {
        if (!open) {
          setReplyTo(null);
          setContent("");
        }
      }}
    >
      <DialogTrigger asChild>
        <Button variant="outline" size="sm" className="mt-4 w-fit">
          <MessageSquareText className="mr-2 size-4 text-primary" />
          Trao đổi điều khoản
          <Badge variant="secondary" className="ml-2">
            {termComments.length}
          </Badge>
          {openCommentsCount > 0 && (
            <span className="ml-1 text-xs text-muted-foreground">
              ({openCommentsCount} đang mở)
            </span>
          )}
        </Button>
      </DialogTrigger>

      <DialogContent className="max-h-[90vh] overflow-hidden sm:max-w-3xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <MessageSquareText className="size-5 text-primary" />
            Trao đổi điều khoản
          </DialogTitle>
          <DialogDescription>
            {termCode} · {termTitle}
          </DialogDescription>
        </DialogHeader>

        <ScrollArea className="max-h-[70vh] pr-4">
          <div className="space-y-4 pr-1">
            <div className="flex flex-wrap items-center justify-between gap-2 text-sm">
              <span className="font-medium">
                {termComments.length} phản hồi
              </span>
              {openCommentsCount > 0 && (
                <Badge variant="outline">
                  {openCommentsCount} đang mở
                </Badge>
              )}
            </div>

            <Separator />

      {termComments.length === 0 ? (
        <div className="rounded-lg border border-dashed px-4 py-6 text-center">
          <p className="text-sm font-medium">Chưa có phản hồi</p>
          <p className="mt-1 text-xs text-muted-foreground">
            Các trao đổi về điều khoản này sẽ được lưu theo version hiện hành.
          </p>
        </div>
      ) : (
        <div className="space-y-3">
          {paginatedComments.map((comment) => {
            const isResolved =
              comment.state === ContractNegotiationCommentState.Resolved;
            const isProcessing = processingCommentId === comment.commentId;

            return (
              <div
                key={comment.commentId}
                className={`rounded-lg border p-3 ${
                  comment.parentCommentId ? "ml-4 border-l-2 border-l-primary/40" : ""
                }`}
              >
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p className="flex items-center gap-1.5 text-sm font-medium">
                      {comment.parentCommentId && (
                        <CornerDownRight className="size-3.5 text-muted-foreground" />
                      )}
                      Phản hồi #{comment.commentId}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      Nhân viên #{comment.recordedByEmployeeId} · {" "}
                      {formatDateTime(comment.createdDate)}
                      {comment.parentCommentId
                        ? ` · Trả lời #${comment.parentCommentId}`
                        : ""}
                    </p>
                  </div>
                  <Badge variant={isResolved ? "secondary" : "outline"}>
                    {isResolved ? "Đã xử lý" : "Đang mở"}
                  </Badge>
                </div>

                <p className="mt-3 whitespace-pre-wrap text-sm leading-6">
                  {comment.content}
                </p>

                {canWrite && (
                  <div className="mt-3 flex flex-wrap gap-2 border-t pt-3">
                    {!isResolved && (
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => {
                          setReplyTo(comment.commentId);
                          setContent("");
                        }}
                      >
                        <Reply className="mr-1.5 size-3.5" />
                        Trả lời
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="sm"
                      disabled={isProcessing}
                      onClick={() =>
                        void handleChangeState(
                          comment,
                          isResolved ? "reopen" : "resolve",
                        )
                      }
                    >
                      {isProcessing ? (
                        <Loader2 className="mr-1.5 size-3.5 animate-spin" />
                      ) : isResolved ? (
                        <RotateCcw className="mr-1.5 size-3.5" />
                      ) : (
                        <CheckCircle2 className="mr-1.5 size-3.5" />
                      )}
                      {isResolved ? "Mở lại" : "Đã xử lý"}
                    </Button>
                  </div>
                )}
              </div>
            );
          })}

          {termComments.length > COMMENTS_PAGE_SIZE && (
            <div className="flex items-center justify-between gap-3 border-t pt-3">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setCurrentPage((page) => Math.max(1, page - 1))}
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
                onClick={() =>
                  setCurrentPage((page) => Math.min(totalPages, page + 1))
                }
                disabled={currentPage === totalPages}
              >
                Sau
                <ChevronRight className="ml-1 size-4" />
              </Button>
            </div>
          )}
        </div>
      )}

      {canWrite && (
        <div className="mt-4 space-y-2 border-t pt-4">
          {replyTo && (
            <div className="flex items-center justify-between rounded-lg bg-primary/5 px-3 py-2 text-sm">
              <span>Đang trả lời phản hồi #{replyTo}</span>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setReplyTo(null);
                  setContent("");
                }}
              >
                Hủy trả lời
              </Button>
            </div>
          )}
          <Textarea
            value={content}
            onChange={(event) => setContent(event.target.value)}
            placeholder={
              replyTo
                ? "Nhập nội dung trả lời..."
                : "Nhập phản hồi cho điều khoản này..."
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
              {replyTo ? "Gửi trả lời" : "Gửi phản hồi"}
            </Button>
          </div>
        </div>
      )}
          </div>
        </ScrollArea>
      </DialogContent>
    </Dialog>
  );
}
