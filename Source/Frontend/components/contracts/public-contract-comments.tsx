"use client";

import { useMemo, useState } from "react";
import { CheckCircle2, Loader2, MessageSquareText, Reply, Send, X } from "lucide-react";
import { toast } from "sonner";

import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { formatDateTime } from "@/lib/format-date-time";
import { cn } from "@/lib/utils";
import {
  CreateCustomerNegotiationCommentRequest,
  CustomerPublicNegotiationCommentResponse,
} from "@/services/public-contract-api";

const getStatus = (error: any) => error?.response?.status as number | undefined;

const getErrorMessage = (error: any, fallback: string) => {
  const data = error?.response?.data;
  return data?.message || data?.title || (typeof data === "string" ? data : fallback);
};

const getAuthor = (comment: CustomerPublicNegotiationCommentResponse) =>
  comment.source === "Customer" ? "Khách hàng" : "Nhà cung cấp";

function PublicCommentNode({
  comment,
  childrenByParent,
  depth,
  onReply,
  canReply,
}: {
  comment: CustomerPublicNegotiationCommentResponse;
  childrenByParent: Map<number, CustomerPublicNegotiationCommentResponse[]>;
  depth: number;
  onReply: (comment: CustomerPublicNegotiationCommentResponse) => void;
  canReply: boolean;
}) {
  const author = getAuthor(comment);
  const isCustomer = comment.source === "Customer";
  const isOpen = comment.lifecycleState === "Open";
  const replies = childrenByParent.get(comment.commentId) ?? [];

  return (
    <div className={cn("space-y-3", depth > 0 && "ml-4 border-l pl-4 sm:ml-8")}>
      <div className="flex items-start gap-3">
        <Avatar className="size-8 border">
          <AvatarFallback
            className={cn(
              "text-[11px] font-semibold",
              isCustomer
                ? "bg-primary/10 text-primary"
                : "bg-sky-500/10 text-sky-700",
            )}
          >
            {isCustomer ? "KH" : "NCC"}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <span className="text-sm font-semibold">{author}</span>
            <span className="text-xs text-muted-foreground">
              {formatDateTime(comment.createdDate)}
            </span>
            {!isOpen && (
              <Badge variant="secondary" className="h-5">
                <CheckCircle2 /> Đã xử lý
              </Badge>
            )}
          </div>
          <div
            className={cn(
              "mt-1.5 rounded-lg px-3 py-2.5",
              isCustomer ? "bg-primary/5" : "bg-muted",
            )}
          >
            <p className="whitespace-pre-wrap text-sm leading-6">
              {comment.content}
            </p>
          </div>
          {isOpen && canReply && (
            <Button
              type="button"
              variant="ghost"
              size="sm"
              className="mt-1 h-7 px-2 text-xs"
              onClick={() => onReply(comment)}
            >
              <Reply className="mr-1 size-3" /> Trả lời
            </Button>
          )}
        </div>
      </div>

      {replies.map((reply) => (
        <PublicCommentNode
          key={reply.commentId}
          comment={reply}
          childrenByParent={childrenByParent}
          depth={depth + 1}
          onReply={onReply}
          canReply={canReply}
        />
      ))}
    </div>
  );
}

export function PublicContractComments({
  termId,
  comments,
  canCreateRoot,
  onCreate,
}: {
  termId: number | null;
  comments: CustomerPublicNegotiationCommentResponse[];
  canCreateRoot: boolean;
  onCreate: (
    request: CreateCustomerNegotiationCommentRequest,
  ) => Promise<CustomerPublicNegotiationCommentResponse>;
}) {
  const [content, setContent] = useState("");
  const [replyTarget, setReplyTarget] =
    useState<CustomerPublicNegotiationCommentResponse | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const scopedComments = useMemo(
    () => comments.filter((comment) => (comment.termId ?? null) === termId),
    [comments, termId],
  );

  const roots = useMemo(
    () => scopedComments.filter((comment) => !comment.parentCommentId),
    [scopedComments],
  );

  const childrenByParent = useMemo(() => {
    const result = new Map<number, CustomerPublicNegotiationCommentResponse[]>();
    scopedComments.forEach((comment) => {
      if (!comment.parentCommentId) return;
      const children = result.get(comment.parentCommentId) ?? [];
      children.push(comment);
      result.set(comment.parentCommentId, children);
    });
    return result;
  }, [scopedComments]);

  const handleSubmit = async () => {
    const normalizedContent = content.trim();
    if (!normalizedContent) {
      toast.error("Vui lòng nhập nội dung trao đổi.");
      return;
    }

    try {
      setIsSubmitting(true);
      await onCreate({
        termId,
        parentCommentId: replyTarget?.commentId ?? null,
        content: normalizedContent,
      });
      setContent("");
      setReplyTarget(null);
      toast.success(replyTarget ? "Đã gửi câu trả lời." : "Đã gửi trao đổi.");
    } catch (error: any) {
      if (getStatus(error) !== 401) {
        toast.error(getErrorMessage(error, "Không thể gửi trao đổi."));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const showComposer = canCreateRoot || Boolean(replyTarget);

  return (
    <div className="mt-5 space-y-4 border-t pt-5">
      <div className="flex items-center justify-between gap-3">
        <h4 className="flex items-center gap-2 text-sm font-semibold">
          <MessageSquareText className="size-4 text-primary" />
          {termId == null ? "Trao đổi chung" : "Trao đổi điều khoản"}
        </h4>
        <Badge variant="outline">{roots.length} chủ đề</Badge>
      </div>

      {showComposer && (
        <div className="space-y-2 rounded-xl border bg-muted/20 p-3">
          {replyTarget && (
            <div className="flex items-center justify-between gap-3 rounded-lg bg-primary/10 px-3 py-2 text-sm">
              <span className="truncate">
                Đang trả lời <strong>{getAuthor(replyTarget)}</strong>
              </span>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="size-7"
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
              replyTarget
                ? "Nhập nội dung trả lời..."
                : termId == null
                  ? "Nhập trao đổi chung về hợp đồng..."
                  : "Nhập đề xuất hoặc trao đổi về điều khoản..."
            }
            maxLength={4000}
            disabled={isSubmitting}
          />
          <div className="flex justify-end">
            <Button onClick={handleSubmit} disabled={isSubmitting} size="sm">
              {isSubmitting ? (
                <Loader2 className="animate-spin" />
              ) : (
                <Send />
              )}
              Gửi
            </Button>
          </div>
        </div>
      )}

      {roots.length === 0 ? (
        <p className="rounded-lg border border-dashed px-4 py-6 text-center text-sm text-muted-foreground">
          Chưa có trao đổi nào.
        </p>
      ) : (
        <div className="space-y-5">
          {roots.map((comment) => (
            <PublicCommentNode
              key={comment.commentId}
              comment={comment}
              childrenByParent={childrenByParent}
              depth={0}
              onReply={setReplyTarget}
              canReply={canCreateRoot}
            />
          ))}
        </div>
      )}
    </div>
  );
}
