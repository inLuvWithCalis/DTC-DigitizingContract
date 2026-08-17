"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import type { ColumnDef, PaginationState } from "@tanstack/react-table";
import { Eye, Languages, LibraryBig, Plus } from "lucide-react";
import { toast } from "@/components/ui/sonner";

import { PermissionGuard } from "@/components/auth/permission-guard";
import { ContractTemplateFormDialog } from "@/components/contract-templates/contract-template-form-dialog";
import { getContractTemplateErrorMessage } from "@/components/contract-templates/contract-template-utils";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { Header } from "@/components/ui/custom/header";
import { usePermission } from "@/hooks/use-permission";
import { RBAC_PERMISSIONS } from "@/lib/rbac";
import { formatDateTime } from "@/lib/format-date-time";
import { ContractLanguageMode } from "@/services/contract-api";
import {
  contractTemplateApi,
  getTemplateDocumentTypeLabel,
  type ContractTemplateDetailResponse,
  type ContractTemplateResponse,
} from "@/services/contract-template-api";

export default function ContractTemplateListPage() {
  const router = useRouter();
  const { can } = usePermission();
  const canManage = can(RBAC_PERMISSIONS.templateManage);
  const [items, setItems] = useState<ContractTemplateResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState("");
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 20,
  });
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const fetchTemplates = useCallback(async () => {
    if (!canManage) {
      setItems([]);
      setTotalCount(0);
      setIsLoading(false);
      return;
    }

    try {
      setIsLoading(true);
      const response = await contractTemplateApi.getList({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchTerm.trim() || undefined,
      });
      setItems(response.items ?? []);
      setTotalCount(response.totalCount ?? 0);
    } catch (error) {
      toast.error(getContractTemplateErrorMessage(error));
    } finally {
      setIsLoading(false);
    }
  }, [canManage, pagination.pageIndex, pagination.pageSize, searchTerm]);

  useEffect(() => {
    void fetchTemplates();
  }, [fetchTemplates]);

  const openTemplate = useCallback(
    (template: ContractTemplateResponse) => {
      router.push(`/admin/contract-templates/${template.templateId}`);
    },
    [router],
  );

  const columns = useMemo<ColumnDef<ContractTemplateResponse>[]>(
    () => [
      {
        id: "select",
        header: ({ table }) => (
          <div className="flex justify-center">
            <Checkbox
              checked={
                table.getIsAllPageRowsSelected() ||
                (table.getIsSomePageRowsSelected() && "indeterminate")
              }
              onCheckedChange={(value) =>
                table.toggleAllPageRowsSelected(Boolean(value))
              }
              aria-label="Chọn tất cả mẫu hợp đồng trên trang"
            />
          </div>
        ),
        cell: ({ row }) => (
          <div
            className="flex justify-center"
            onClick={(event) => event.stopPropagation()}
          >
            <Checkbox
              checked={row.getIsSelected()}
              onCheckedChange={(value) => row.toggleSelected(Boolean(value))}
              aria-label={`Chọn mẫu ${row.original.templateCode}`}
            />
          </div>
        ),
        enableSorting: false,
        size: 44,
      },
      {
        accessorKey: "templateCode",
        header: "Mã mẫu",
        cell: ({ row }) => (
          <span className="block min-w-36 text-xs font-semibold text-primary">
            {row.original.templateCode}
          </span>
        ),
      },
      {
        accessorKey: "templateName",
        header: "Tên mẫu hợp đồng",
        cell: ({ row }) => (
          <div className="max-w-80 min-w-64">
            <p className="font-semibold text-foreground">
              {row.original.templateName}
            </p>
            {row.original.templateNameEn && (
              <p className="mt-1 truncate text-xs text-muted-foreground">
                {row.original.templateNameEn}
              </p>
            )}
          </div>
        ),
      },
      {
        accessorKey: "documentType",
        header: "Loại tài liệu",
        cell: ({ row }) => (
          <span className="block min-w-48">
            {getTemplateDocumentTypeLabel(row.original.documentType)}
          </span>
        ),
      },
      {
        accessorKey: "languageMode",
        header: "Ngôn ngữ",
        cell: ({ row }) => (
          <span className="flex min-w-28 items-center gap-1.5 text-sm">
            <Languages className="size-4 text-muted-foreground" />
            {row.original.languageMode === ContractLanguageMode.Bilingual
              ? "Song ngữ"
              : "Tiếng Việt"}
          </span>
        ),
      },
      {
        accessorKey: "currentPublishedVersionId",
        header: "Bản hiện hành",
        cell: ({ row }) =>
          row.original.currentPublishedVersionId ? (
            <Badge
              variant="outline"
              className="border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/30 dark:text-emerald-300"
            >
              ID #{row.original.currentPublishedVersionId}
            </Badge>
          ) : (
            <span className="whitespace-nowrap text-sm text-muted-foreground">
              Chưa phát hành
            </span>
          ),
      },
      {
        accessorKey: "updatedDate",
        header: "Cập nhật",
        cell: ({ row }) => (
          <span className="block min-w-36 text-sm text-muted-foreground">
            {formatDateTime(
              row.original.updatedDate || row.original.createdDate,
            )}
          </span>
        ),
      },
      {
        id: "action",
        header: () => <div className="text-right">Thao tác</div>,
        cell: ({ row }) => (
          <div
            className="flex justify-end"
            onClick={(event) => event.stopPropagation()}
          >
            <Button
              size="sm"
              variant="outline"
              onClick={() => openTemplate(row.original)}
            >
              <Eye className="size-4" /> Quản lý
            </Button>
          </div>
        ),
      },
    ],
    [openTemplate],
  );

  const handleCreated = (_template: ContractTemplateDetailResponse) => {
    if (pagination.pageIndex === 0) {
      void fetchTemplates();
      return;
    }
    setPagination((current) => ({ ...current, pageIndex: 0 }));
  };

  return (
    <>
      <Header title="Mẫu hợp đồng" />
      <div className="grow space-y-6 overflow-y-auto p-3 sm:p-6 lg:p-10">
        <div className="mx-auto space-y-6">
          <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-start">
            <div className="flex items-start gap-3">
              <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary">
                <LibraryBig className="size-5" />
              </span>
              <div>
                <h1 className="text-2xl font-bold tracking-tight">
                  Mẫu hợp đồng
                </h1>
                <p className="mt-1 text-sm text-muted-foreground">
                  Quản lý DOCX, điều khoản và vòng đời phát hành của mẫu hợp
                  đồng.
                </p>
              </div>
            </div>
            {canManage && (
              <Button onClick={() => setIsCreateOpen(true)}>
                <Plus className="size-4" /> Tạo mẫu hợp đồng
              </Button>
            )}
          </div>

          <PermissionGuard
            permission={RBAC_PERMISSIONS.templateManage}
            variant="card"
            title="Không có quyền truy cập"
            description="Chỉ Admin Officer đang hoạt động được quản trị mẫu hợp đồng."
          >
            <Card className="flex min-h-[500px] flex-col gap-0 border-border bg-card p-0 shadow-sm">
              <CardContent className="flex flex-1 flex-col justify-between p-4 pb-0">
                <DataTable
                  columns={columns}
                  data={items}
                  pageCount={Math.ceil(totalCount / pagination.pageSize)}
                  rowCount={totalCount}
                  pagination={pagination}
                  onPaginationChange={setPagination}
                  searchValue={searchTerm}
                  onSearchChange={setSearchTerm}
                  searchPlaceholder="Tìm theo mã hoặc tên mẫu..."
                  isLoading={isLoading}
                  onRowClick={openTemplate}
                  mobileCardRenderer={(row, { isSelected, actionCell }) => {
                    const template = row.original;
                    return (
                      <div
                        className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                          isSelected
                            ? "border-primary/40 bg-primary/5"
                            : "border-border"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-3">
                          <div className="min-w-0">
                            <p className="font-mono text-xs font-semibold text-primary">
                              {template.templateCode}
                            </p>
                            <h3 className="mt-1 font-bold text-foreground">
                              {template.templateName}
                            </h3>
                            {template.templateNameEn && (
                              <p className="mt-1 truncate text-xs text-muted-foreground">
                                {template.templateNameEn}
                              </p>
                            )}
                          </div>
                          {template.currentPublishedVersionId ? (
                            <Badge
                              variant="outline"
                              className="shrink-0 border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/30 dark:text-emerald-300"
                            >
                              ID #{template.currentPublishedVersionId}
                            </Badge>
                          ) : (
                            <Badge variant="secondary" className="shrink-0">
                              Chưa phát hành
                            </Badge>
                          )}
                        </div>

                        <div className="my-3 grid grid-cols-2 gap-3 rounded-lg border border-border/50 bg-muted/40 p-3 text-sm">
                          <div>
                            <span className="block text-xs text-muted-foreground">
                              Loại tài liệu
                            </span>
                            <span className="mt-1 block font-medium">
                              {getTemplateDocumentTypeLabel(
                                template.documentType,
                              )}
                            </span>
                          </div>
                          <div>
                            <span className="block text-xs text-muted-foreground">
                              Ngôn ngữ
                            </span>
                            <span className="mt-1 block font-medium">
                              {template.languageMode ===
                              ContractLanguageMode.Bilingual
                                ? "Song ngữ"
                                : "Tiếng Việt"}
                            </span>
                          </div>
                        </div>

                        <p className="text-xs text-muted-foreground">
                          Cập nhật{" "}
                          {formatDateTime(
                            template.updatedDate || template.createdDate,
                          )}
                        </p>
                        {actionCell}
                      </div>
                    );
                  }}
                />
              </CardContent>
            </Card>
          </PermissionGuard>
        </div>
      </div>

      <ContractTemplateFormDialog
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onSuccess={handleCreated}
      />
    </>
  );
}
