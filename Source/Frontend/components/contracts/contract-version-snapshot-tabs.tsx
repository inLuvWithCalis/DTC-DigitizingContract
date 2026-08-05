"use client";

import { useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  MessageSquareText,
  Package,
  ScrollText,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { formatCurrency } from "@/lib/format-currency";
import {
  ContractItemDiscountMode,
  ContractNegotiationCommentEventType,
  ContractNegotiationCommentState,
  ContractVersionDetailResponse,
  getContractItemTypeLabel,
} from "@/services/contract-api";

const PAGE_SIZE = 5;

const formatDateTime = (value: string) =>
  new Date(value).toLocaleString("vi-VN", {
    hour: "2-digit",
    minute: "2-digit",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });

const getCommentEventLabel = (eventType: ContractNegotiationCommentEventType) => {
  switch (eventType) {
    case ContractNegotiationCommentEventType.Created:
      return "Đã tạo";
    case ContractNegotiationCommentEventType.Resolved:
      return "Đã xử lý";
    case ContractNegotiationCommentEventType.Reopened:
      return "Đã mở lại";
    default:
      return "Cập nhật";
  }
};

function PaginationControls({
  page,
  totalItems,
  label,
  onPageChange,
}: {
  page: number;
  totalItems: number;
  label: string;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / PAGE_SIZE));
  if (totalItems <= PAGE_SIZE) return null;

  return (
    <div className="mt-4 flex items-center justify-between gap-3 border-t pt-4">
      <Button
        variant="outline"
        size="sm"
        onClick={() => onPageChange(Math.max(1, page - 1))}
        disabled={page === 1}
      >
        <ChevronLeft className="mr-1 size-4" />
        Trước
      </Button>
      <div className="text-center text-xs text-muted-foreground">
        <p className="font-medium text-foreground">
          Trang {page} / {totalPages}
        </p>
        <p>
          {totalItems} {label}
        </p>
      </div>
      <Button
        variant="outline"
        size="sm"
        onClick={() => onPageChange(Math.min(totalPages, page + 1))}
        disabled={page === totalPages}
      >
        Sau
        <ChevronRight className="ml-1 size-4" />
      </Button>
    </div>
  );
}

function EmptyState({
  icon,
  children,
}: {
  icon: React.ReactNode;
  children: React.ReactNode;
}) {
  return (
    <div className="rounded-xl border border-dashed p-10 text-center">
      <div className="mx-auto mb-3 w-fit text-muted-foreground">{icon}</div>
      <p className="font-medium">{children}</p>
    </div>
  );
}

export function ContractVersionSnapshotTabs({
  version,
}: {
  version: ContractVersionDetailResponse;
}) {
  const [itemsPage, setItemsPage] = useState(1);
  const [termsPage, setTermsPage] = useState(1);
  const [commentsPage, setCommentsPage] = useState(1);

  const comments = version.comments || [];
  const paginatedItems = version.items.slice(
    (itemsPage - 1) * PAGE_SIZE,
    itemsPage * PAGE_SIZE,
  );
  const paginatedTerms = version.terms.slice(
    (termsPage - 1) * PAGE_SIZE,
    termsPage * PAGE_SIZE,
  );
  const paginatedComments = comments.slice(
    (commentsPage - 1) * PAGE_SIZE,
    commentsPage * PAGE_SIZE,
  );

  return (
    <Tabs defaultValue="items" className="space-y-4">
      <TabsList className="grid h-auto w-full grid-cols-3">
        <TabsTrigger value="items">
          <Package className="size-4" />
          <span className="hidden sm:inline">Sản phẩm/Dịch vụ</span>
          <span>({version.items.length})</span>
        </TabsTrigger>
        <TabsTrigger value="terms">
          <ScrollText className="size-4" />
          <span className="hidden sm:inline">Điều khoản</span>
          <span>({version.terms.length})</span>
        </TabsTrigger>
        <TabsTrigger value="comments">
          <MessageSquareText className="size-4" />
          <span className="hidden sm:inline">Bình luận</span>
          <span>({comments.length})</span>
        </TabsTrigger>
      </TabsList>

      <TabsContent value="items" className="space-y-3">
        {version.items.length === 0 ? (
          <EmptyState icon={<Package className="size-8" />}>
            Snapshot không có sản phẩm hoặc dịch vụ
          </EmptyState>
        ) : (
          paginatedItems.map((item) => (
            <div
              key={item.contractItemId}
              className="overflow-hidden rounded-xl border"
            >
              <div className="flex flex-col gap-2 bg-muted/30 p-4 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="font-semibold">{item.itemName}</p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {item.itemCode || "Không có mã"} · {" "}
                    {getContractItemTypeLabel(item.itemType)}
                  </p>
                </div>
                <p className="font-semibold text-primary">
                  {formatCurrency(item.lineTotal, version.currencyCode)}
                </p>
              </div>
              <div className="grid gap-3 p-4 text-sm sm:grid-cols-2 lg:grid-cols-4">
                <div>
                  <p className="text-muted-foreground">Số lượng</p>
                  <p className="mt-1 font-medium">
                    {item.quantity} {item.unitName || ""}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">Đơn giá</p>
                  <p className="mt-1 font-medium">
                    {formatCurrency(item.unitPrice, version.currencyCode)}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">Chiết khấu</p>
                  <p className="mt-1 font-medium text-emerald-600">
                    {item.discountMode === ContractItemDiscountMode.Percentage
                      ? `${item.discountPercent}% (${formatCurrency(
                          item.discountAmount,
                          version.currencyCode,
                        )})`
                      : item.discountMode ===
                          ContractItemDiscountMode.FixedAmount
                        ? formatCurrency(
                            item.discountAmount,
                            version.currencyCode,
                          )
                        : "Không"}
                  </p>
                </div>
                <div>
                  <p className="text-muted-foreground">VAT</p>
                  <p className="mt-1 font-medium">
                    {item.isTaxable
                      ? `${item.vatPercent}% (${formatCurrency(
                          item.vatAmount,
                          version.currencyCode,
                        )})`
                      : "Không thuế"}
                  </p>
                </div>
              </div>
            </div>
          ))
        )}
        <PaginationControls
          page={itemsPage}
          totalItems={version.items.length}
          label="sản phẩm/dịch vụ"
          onPageChange={setItemsPage}
        />
      </TabsContent>

      <TabsContent value="terms" className="space-y-3">
        {version.terms.length === 0 ? (
          <EmptyState icon={<ScrollText className="size-8" />}>
            Snapshot không có điều khoản
          </EmptyState>
        ) : (
          paginatedTerms.map((term) => (
            <div key={term.termId} className="rounded-xl border p-4 sm:p-5">
              <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">
                    {term.termCode} · Điều {term.displayOrder}
                  </p>
                  <h3 className="mt-1 font-semibold">{term.termTitle}</h3>
                </div>
                <Badge variant={term.isNegotiable ? "outline" : "secondary"}>
                  {term.isNegotiable
                    ? "Cho phép đàm phán"
                    : "Điều khoản cứng"}
                </Badge>
              </div>
              <p className="mt-3 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                {term.termContent || "Chưa có nội dung."}
              </p>
              {(term.termTitleEn || term.termContentEn) && (
                <div className="mt-4 border-t pt-4">
                  <p className="font-medium">
                    {term.termTitleEn || "English content"}
                  </p>
                  <p className="mt-2 whitespace-pre-wrap text-sm leading-6 text-muted-foreground">
                    {term.termContentEn || "No content."}
                  </p>
                </div>
              )}
            </div>
          ))
        )}
        <PaginationControls
          page={termsPage}
          totalItems={version.terms.length}
          label="điều khoản"
          onPageChange={setTermsPage}
        />
      </TabsContent>

      <TabsContent value="comments" className="space-y-3">
        {comments.length === 0 ? (
          <EmptyState icon={<MessageSquareText className="size-8" />}>
            Version này chưa có bình luận
          </EmptyState>
        ) : (
          paginatedComments.map((comment) => {
            const relatedTerm = comment.termId
              ? version.terms.find((term) => term.termId === comment.termId)
              : null;

            return (
              <div key={comment.commentId} className="rounded-xl border p-4">
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                  <div>
                    <p className="font-medium">
                      Nhân viên #{comment.recordedByEmployeeId}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {formatDateTime(comment.createdDate)}
                      {comment.parentCommentId
                        ? ` · Trả lời #${comment.parentCommentId}`
                        : ""}
                    </p>
                  </div>
                  <Badge
                    variant={
                      comment.state ===
                      ContractNegotiationCommentState.Resolved
                        ? "secondary"
                        : "outline"
                    }
                  >
                    {comment.state ===
                    ContractNegotiationCommentState.Resolved
                      ? "Đã xử lý"
                      : "Đang mở"}
                  </Badge>
                </div>
                <Badge variant="outline" className="mt-3">
                  {relatedTerm
                    ? `${relatedTerm.termCode} · ${relatedTerm.termTitle}`
                    : "Trao đổi chung"}
                </Badge>
                <p className="mt-3 whitespace-pre-wrap text-sm leading-6">
                  {comment.content}
                </p>
                {comment.events.length > 0 && (
                  <div className="mt-4 flex flex-wrap gap-2 border-t pt-3">
                    {comment.events.map((event) => (
                      <span
                        key={event.commentEventId}
                        className="rounded-full bg-muted px-2.5 py-1 text-xs text-muted-foreground"
                      >
                        {getCommentEventLabel(event.eventType)} · {" "}
                        {formatDateTime(event.occurredAt)}
                      </span>
                    ))}
                  </div>
                )}
              </div>
            );
          })
        )}
        <PaginationControls
          page={commentsPage}
          totalItems={comments.length}
          label="bình luận"
          onPageChange={setCommentsPage}
        />
      </TabsContent>
    </Tabs>
  );
}
