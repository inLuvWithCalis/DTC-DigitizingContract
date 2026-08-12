"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import { Header } from "@/components/ui/custom/header";
import {
  Eye,
  UserCog,
  Plus,
  Trash2,
  Layers,
  FolderTree,
  FileText,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "@/components/ui/sonner";
import { ColumnDef, PaginationState } from "@tanstack/react-table";

import {
  serviceTypeApi,
  ServiceTypeResponse,
} from "@/services/catalog/service-types-api";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { PageHeaderSkeleton } from "@/components/ui/custom/table-skeleton";
import { ServiceTypeFormModal } from "./service-type-form-modal";

function ServiceTypeBulkActions({
  selectedRows,
  resetSelection,
}: {
  selectedRows: ServiceTypeResponse[];
  resetSelection: () => void;
}) {
  const [isLoading, setIsLoading] = useState(false);

  const handleBulkDelete = async () => {
    setIsLoading(true);
    try {
      await Promise.all(
        selectedRows.map((item) => serviceTypeApi.delete(item.serviceTypeId)),
      );
      toast.success(`Đã xóa ${selectedRows.length} loại dịch vụ`);
      resetSelection();
    } catch (error) {
      toast.error("Lỗi hoặc loại dịch vụ đang có dịch vụ con sử dụng");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <SplitActionMenu
      primaryLabel="Xóa loại dịch vụ"
      primaryIcon={<Trash2 className="w-4 h-4" />}
      onPrimaryClick={handleBulkDelete}
      isLoading={isLoading}
      menuItems={[]}
    />
  );
}

export default function ServiceTypeListPage() {
  const [serviceTypes, setServiceTypes] = useState<ServiceTypeResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState("");
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<ServiceTypeResponse | null>(
    null,
  );
  const [viewingItem, setViewingItem] = useState<ServiceTypeResponse | null>(
    null,
  );

  const fetchServiceTypes = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await serviceTypeApi.getList({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchTerm || undefined,
      });

      setServiceTypes(res.items || []);
      setTotalCount(res.totalCount || 0);
    } catch (error) {
      toast.error("Lỗi khi tải danh sách loại dịch vụ");
    } finally {
      setIsLoading(false);
    }
  }, [pagination.pageIndex, pagination.pageSize, searchTerm]);

  useEffect(() => {
    fetchServiceTypes();
  }, [fetchServiceTypes]);

  const handleDeleteConfirm = async () => {
    if (deleteId === null) return;
    setIsDeleting(true);
    try {
      await serviceTypeApi.delete(deleteId);
      toast.success("Đã xóa loại dịch vụ thành công");
      setDeleteId(null);
      fetchServiceTypes();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        "Không thể xóa loại dịch vụ (có thể đang được sử dụng)";
      toast.error(message);
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<ColumnDef<ServiceTypeResponse>[]>(
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
                table.toggleAllPageRowsSelected(!!value)
              }
              aria-label="Select all"
              className="translate-y-[2px]"
            />
          </div>
        ),
        cell: ({ row }) => (
          <div
            className="flex justify-center"
            onClick={(e) => e.stopPropagation()}
          >
            <Checkbox
              checked={row.getIsSelected()}
              onCheckedChange={(value) => row.toggleSelected(!!value)}
              aria-label="Select row"
              className="translate-y-[2px]"
            />
          </div>
        ),
        enableSorting: false,
      },
      {
        accessorKey: "serviceTypeName",
        header: "Tên loại dịch vụ",
        cell: ({ row }) => (
          <div className="max-w-[320px]">
            <span className="font-semibold text-foreground block truncate text-base">
              {row.original.serviceTypeName || "Chưa có tên"}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "serviceCount",
        header: () => <div className="text-center">Dịch vụ sử dụng</div>,
        cell: ({ row }) => (
          <div className="text-center min-w-[120px]">
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium">
              <FileText className="w-3.5 h-3.5 mr-1" />
              {row.original.serviceCount || 0} dịch vụ
            </span>
          </div>
        ),
      },
      {
        accessorKey: "langId",
        header: () => <div className="text-center">Ngôn ngữ</div>,
        cell: ({ row }) => (
          <div className="text-center text-sm text-muted-foreground min-w-[80px]">
            {row.original.langId !== undefined && row.original.langId !== null
              ? `Lang ID #${row.original.langId}`
              : "Mặc định"}
          </div>
        ),
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => setViewingItem(item)}
              isLoading={loadingId === item.serviceTypeId}
              menuItems={[
                {
                  label: "Chỉnh sửa",
                  icon: <UserCog className="w-4 h-4" />,
                  onClick: () => {
                    setEditingItem(item);
                    setIsFormOpen(true);
                  },
                },
                {
                  label: "Xóa loại dịch vụ",
                  icon: <Trash2 className="w-4 h-4" />,
                  isDestructive: true,
                  onClick: () => setDeleteId(item.serviceTypeId),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const totalServiceUsed = useMemo(
    () => serviceTypes.reduce((acc, curr) => acc + (curr.serviceCount || 0), 0),
    [serviceTypes],
  );

  const summaryItems: SummaryCardItem[] = useMemo(
    () => [
      {
        title: "Tổng loại dịch vụ",
        value: totalCount,
        icon: <FolderTree className="w-6 h-6" />,
        iconWrapperClassName: "bg-primary/10 text-primary",
      },
      {
        title: "Dịch vụ con (Trang này)",
        value: totalServiceUsed,
        icon: <Layers className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
      },
    ],
    [totalCount, totalServiceUsed],
  );

  const currentDeleteItem = serviceTypes.find(
    (item) => item.serviceTypeId === deleteId,
  );

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Quản lý Loại dịch vụ
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Quản lý danh mục nhóm / loại dịch vụ trong hệ thống Master Data
            </p>
          </div>

          <Button
            onClick={() => {
              setEditingItem(null);
              setIsFormOpen(true);
            }}
          >
            <Plus className="w-4 h-4 mr-2" /> Thêm mới
          </Button>
        </div>

        <SummaryCards items={summaryItems} isLoading={isLoading} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={serviceTypes}
              pageCount={Math.ceil(totalCount / pagination.pageSize)}
              pagination={pagination}
              rowCount={totalCount}
              onPaginationChange={setPagination}
              isLoading={isLoading}
              searchPlaceholder="Tìm kiếm tên loại dịch vụ..."
              filterSlot={null}
              onRowClick={(row) => setViewingItem(row)}
              searchValue={searchTerm}
              onSearchChange={(value) => setSearchTerm(value)}
              bulkActions={(selectedRows, resetSelection) => (
                <ServiceTypeBulkActions
                  selectedRows={selectedRows}
                  resetSelection={resetSelection}
                />
              )}
              mobileCardRenderer={(row, { isSelected, actionCell }) => {
                const item = row.original;
                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <span className="text-xs font-semibold text-primary uppercase tracking-wider block">
                          ID #{item.serviceTypeId}
                        </span>
                        <h3 className="text-base font-bold text-foreground mt-0.5">
                          {item.serviceTypeName || "Chưa có tên"}
                        </h3>
                      </div>
                      <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium shrink-0">
                        <FileText className="w-3 h-3 mr-1" />
                        {item.serviceCount || 0} dịch vụ
                      </span>
                    </div>

                    <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground bg-muted/40 p-2.5 rounded-lg border border-border/50 my-2">
                      <span>Ngôn ngữ</span>
                      <span className="font-medium text-foreground">
                        {item.langId !== undefined && item.langId !== null
                          ? `Lang ID #${item.langId}`
                          : "Mặc định"}
                      </span>
                    </div>

                    {actionCell}
                  </div>
                );
              }}
            />
          </CardContent>
        </Card>

        <ConfirmDialog
          isOpen={deleteId !== null}
          onClose={() => setDeleteId(null)}
          onConfirm={handleDeleteConfirm}
          isLoading={isDeleting}
          title="Xóa loại dịch vụ?"
          description={`Bạn có chắc chắn muốn xóa loại dịch vụ "${currentDeleteItem?.serviceTypeName}"? Lưu ý: Không thể xóa nếu loại dịch vụ này đang có dịch vụ con sử dụng.`}
          confirmText="Xác nhận xóa"
          variant="destructive"
          titleClassName="text-destructive"
        />

        <ServiceTypeFormModal
          isOpen={isFormOpen}
          onClose={() => {
            setIsFormOpen(false);
            setEditingItem(null);
          }}
          onSuccess={fetchServiceTypes}
          item={editingItem}
        />

        <ServiceTypeFormModal
          isOpen={!!viewingItem}
          onClose={() => setViewingItem(null)}
          onSuccess={() => {}}
          item={viewingItem}
          viewOnly
        />
      </div>
    </>
  );
}
