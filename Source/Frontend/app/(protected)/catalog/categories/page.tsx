"use client";

import { useCallback, useEffect, useState, useMemo } from "react";
import { Header } from "@/components/ui/custom/header";
import {
  Eye,
  UserCog,
  Plus,
  Trash2,
  FolderTree,
  Package,
  ChevronRight,
  ChevronDown,
  SortAsc,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "@/components/ui/sonner";
import { ColumnDef } from "@tanstack/react-table";

import { categoryApi, CategoryResponse } from "@/services/catalog/category-api";
import { TreeDataTable } from "@/components/ui/custom/data-tree-table";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import { CategoryFormModal } from "./category-form-modal";
import { showConfirmToast } from "@/components/ui/custom/confirm-toast";
import { usePermission } from "@/hooks/use-permission";
import { RBAC_PERMISSIONS } from "@/lib/rbac";

export type CategoryTreeNode = CategoryResponse;

export default function CategoryListPage() {
  const { can } = usePermission();
  const canManage = can(RBAC_PERMISSIONS.catalogManage);
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [searchTerm, setSearchTerm] = useState("");
  const [pagination, setPagination] = useState({
    pageIndex: 0,
    pageSize: 10,
  });
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<CategoryResponse | null>(null);
  const [viewingItem, setViewingItem] = useState<CategoryResponse | null>(null);
  const [initialParentId, setInitialParentId] = useState<number | null>(null);

  const fetchData = useCallback(async () => {
    setIsLoading(true);
    try {
      const res = await categoryApi.getParents({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchTerm || undefined,
      });
      setCategories(res.items || []);
      setTotalCount(res.totalCount || 0);
    } catch (error) {
      toast.error("Lỗi khi tải danh mục");
    } finally {
      setIsLoading(false);
    }
  }, [pagination.pageIndex, pagination.pageSize, searchTerm]);

  useEffect(() => {
    fetchData();
  }, [fetchData]);

  const handleDeleteConfirm = async () => {
    if (deleteId === null) return;
    setIsDeleting(true);
    try {
      await categoryApi.delete(deleteId);
      toast.success("Đã xóa danh mục thành công");
      setDeleteId(null);
      fetchData();
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        "Không thể xóa danh mục (có thể đang có sản phẩm hoặc danh mục con sử dụng)";
      toast.error(message);
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<ColumnDef<CategoryTreeNode>[]>(
    () => [
      {
        id: "select",
        header: ({ table }) => (
          <div className="flex justify-center w-[40px]">
            <Checkbox
              checked={
                table.getIsAllRowsSelected() ||
                (table.getIsSomeRowsSelected() && "indeterminate")
              }
              onCheckedChange={(value) => table.toggleAllRowsSelected(!!value)}
              className="translate-y-[2px]"
            />
          </div>
        ),
        cell: ({ row }) => (
          <div
            className="flex justify-center w-[40px]"
            onClick={(e) => e.stopPropagation()}
          >
            <Checkbox
              checked={row.getIsSelected()}
              onCheckedChange={(value) => row.toggleSelected(!!value)}
              className="translate-y-[2px]"
            />
          </div>
        ),
      },
      {
        accessorKey: "categoryName",
        header: "Cấu trúc Danh mục",
        cell: ({ row }) => {
          const item = row.original;
          const hasChildren = row.getCanExpand();
          const paddingLeft = row.depth * 24;

          return (
            <div
              className="flex items-center gap-2 w-[400px]"
              style={{ paddingLeft: `${paddingLeft}px` }}
            >
              <div className="w-6 h-6 flex items-center justify-center shrink-0">
                {hasChildren ? (
                  <button
                    onClick={(e) => {
                      e.stopPropagation();
                      row.toggleExpanded();
                    }}
                    className="p-1 hover:bg-muted rounded-md text-muted-foreground transition-colors"
                  >
                    {row.getIsExpanded() ? (
                      <ChevronDown className="w-4 h-4" />
                    ) : (
                      <ChevronRight className="w-4 h-4" />
                    )}
                  </button>
                ) : (
                  <span className="w-4 h-4 block" />
                )}
              </div>
              <FolderTree className="w-4.5 h-4.5 text-primary shrink-0" />
              <div className="flex flex-col truncate">
                <span className="font-semibold text-foreground text-sm truncate">
                  {item.categoryName || "Chưa có tên"}
                </span>
                <span className="text-xs text-muted-foreground truncate">
                  ID: #{item.categoryId}
                </span>
              </div>
            </div>
          );
        },
      },
      {
        accessorKey: "categoryOrder",
        header: () => <div className="text-center">Thứ tự</div>,
        cell: ({ row }) => (
          <div className="text-center text-sm font-medium">
            {row.original.categoryOrder ?? "-"}
          </div>
        ),
      },
      {
        accessorKey: "productCount",
        header: () => <div className="text-center">Sản phẩm</div>,
        cell: ({ row }) => (
          <div className="text-center min-w-[120px]">
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium">
              <Package className="w-3.5 h-3.5 mr-1" />
              {row.original.productCount || 0} SP
            </span>
          </div>
        ),
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <div onClick={(e) => e.stopPropagation()}>
              <SplitActionMenu
                primaryLabel="Chi tiết"
                primaryIcon={<Eye className="w-4 h-4" />}
                onPrimaryClick={() => setViewingItem(item)}
                isLoading={loadingId === item.categoryId}
                menuItems={canManage ? [
                  {
                    label: "Chỉnh sửa",
                    icon: <UserCog className="w-4 h-4" />,
                    onClick: () => {
                      setEditingItem(item);
                      setIsFormOpen(true);
                    },
                  },
                  {
                    label: "Thêm danh mục con",
                    icon: <Plus className="w-4 h-4" />,
                    onClick: () => {
                      setEditingItem(null);
                      setInitialParentId(item.categoryId);
                      setIsFormOpen(true);
                    },
                  },
                  {
                    label: "Xóa danh mục",
                    icon: <Trash2 className="w-4 h-4" />,
                    isDestructive: true,
                    onClick: () => setDeleteId(item.categoryId),
                  },
                ] : []}
              />
            </div>
          );
        },
      },
    ],
    [canManage, loadingId],
  );

  const currentDeleteItem = useMemo(
    () => categories.find((item) => item.categoryId === deleteId),
    [categories, deleteId],
  );

  return (
    <>
      <Header />
      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Danh mục sản phẩm
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Hỗ trợ phân cấp không giới hạn.
            </p>
          </div>
          {canManage && <Button
            className="shadow-sm"
            onClick={() => {
              setEditingItem(null);
              setInitialParentId(null);
              setIsFormOpen(true);
            }}
          >
            <Plus className="w-4 h-4 mr-2" /> Thêm mới
          </Button>}
        </div>

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <TreeDataTable
              columns={columns}
              data={categories}
              defaultExpanded={false}
              getSubRows={(row) =>
                row.items && row.items.length > 0 ? row.items : undefined
              }
              onRowClick={(row) => setViewingItem(row)}
              isLoading={isLoading}
              pageCount={Math.ceil(totalCount / pagination.pageSize)}
              rowCount={totalCount}
              pagination={pagination}
              onPaginationChange={setPagination}
              searchValue={searchTerm}
              onSearchChange={(val) => {
                setSearchTerm(val);
                setPagination((prev) => ({ ...prev, pageIndex: 0 }));
              }}
              bulkActions={canManage ? (selectedItems, resetSelection) => (
                <Button
                  size="sm"
                  variant="destructive"
                  className="h-8 text-xs shadow-sm"
                  disabled={isDeleting}
                  onClick={() => {
                    showConfirmToast({
                      title: "Xác nhận xóa danh mục",
                      description: `Bạn có chắc chắn muốn xóa ${selectedItems.length} danh mục đã chọn?`,
                      confirmLabel: "Xóa",
                      cancelLabel: "Hủy",
                      onConfirm: async () => {
                        setIsDeleting(true);
                        try {
                          await Promise.all(
                            selectedItems.map((item) =>
                              categoryApi.delete(item.categoryId),
                            ),
                          );
                          toast.success(
                            `Đã xóa ${selectedItems.length} danh mục thành công`,
                          );
                          resetSelection();
                          fetchData();
                        } catch (error: any) {
                          toast.error(
                            "Có lỗi khi xóa một số danh mục (có thể đang có sản phẩm hoặc danh mục con sử dụng)",
                          );
                          fetchData();
                        } finally {
                          setIsDeleting(false);
                        }
                      },
                    });
                  }}
                >
                  {isDeleting ? "Đang xóa..." : "Xóa tất cả"}
                </Button>
              ) : undefined}
              searchPlaceholder="Tìm tên danh mục..."
              mobileCardRenderer={(row, { isSelected, actionCell }) => {
                const item = row.original;
                const hasChildren = row.getCanExpand();

                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    } ${row.depth > 0 ? "border-l-2 border-l-primary/50" : ""}`}
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div className="flex items-start gap-2.5 flex-1">
                        {hasChildren ? (
                          <button
                            onClick={(e) => {
                              e.stopPropagation();
                              row.toggleExpanded();
                            }}
                            className="p-1 hover:bg-muted rounded-md text-muted-foreground transition-colors mt-0.5"
                          >
                            {row.getIsExpanded() ? (
                              <ChevronDown className="w-4 h-4" />
                            ) : (
                              <ChevronRight className="w-4 h-4" />
                            )}
                          </button>
                        ) : (
                          <span className="w-4 h-4 block shrink-0 mt-0.5" />
                        )}
                        <div>
                          <div className="flex items-center gap-2">
                            <span className="text-xs font-semibold text-primary uppercase tracking-wider block">
                              ID #{item.categoryId}
                            </span>
                            <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-primary/10 text-primary text-[11px] font-medium shrink-0">
                              <Package className="w-3 h-3 mr-1" />
                              {item.productCount || 0} SP
                            </span>
                            <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-primary/10 text-primary text-[11px] font-medium shrink-0">
                              <SortAsc className="w-3 h-3 mr-1" />
                              STT: {item.categoryOrder || 0}
                            </span>
                          </div>
                          <h3 className="text-base font-bold text-foreground mt-0.5">
                            {item.categoryName || "Chưa có tên"}
                          </h3>
                        </div>
                      </div>
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
          title="Xóa danh mục?"
          description={`Bạn có chắc chắn muốn xóa danh mục "${currentDeleteItem?.categoryName || ""}"? Lưu ý: Không thể xóa nếu danh mục này đang có sản phẩm sử dụng.`}
          confirmText="Xác nhận xóa"
          variant="destructive"
          titleClassName="text-destructive"
        />

        {canManage && <CategoryFormModal
          isOpen={isFormOpen}
          onClose={() => {
            setIsFormOpen(false);
            setEditingItem(null);
            setInitialParentId(null);
          }}
          onSuccess={fetchData}
          item={editingItem}
          initialParentId={initialParentId}
        />}

        <CategoryFormModal
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
