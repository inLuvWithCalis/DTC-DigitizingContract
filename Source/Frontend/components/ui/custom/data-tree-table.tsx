"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  getPaginationRowModel, // Thêm dòng này để xử lý Client Pagination
  useReactTable,
  Row,
  ExpandedState,
} from "@tanstack/react-table";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Loader2,
  CheckCheck,
  Search,
  ChevronLeft,
  ChevronRight,
} from "lucide-react";
import { useMediaQuery } from "@/hooks/use-media-query";
import { MobileCardWrapper } from "./mobile-card-wrapper";

interface MobileCardRenderContext {
  isSelectionMode: boolean;
  isSelected: boolean;
  actionCell: React.ReactNode;
}

interface TreeDataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[]; // Dữ liệu đã được build thành dạng cây
  getSubRows: (row: TData) => TData[] | undefined;
  defaultExpanded?: boolean;

  // Hỗ trợ phân trang Server-side (tùy chọn)
  pageCount?: number;
  rowCount?: number;
  pagination?: { pageIndex: number; pageSize: number };
  onPaginationChange?: (pagination: any) => void;

  searchValue?: string;
  onSearchChange?: (value: string) => void;
  searchPlaceholder?: string;
  filterSlot?: React.ReactNode;
  isLoading?: boolean;
  onSelectMany?: (selectedRows: TData[]) => void;
  bulkActions?: (
    selectedRows: TData[],
    resetSelection: () => void,
  ) => React.ReactNode;
  onRowClick?: (row: TData) => void;
  mobileCardRenderer?: (
    row: Row<TData>,
    context: MobileCardRenderContext,
  ) => React.ReactNode;
}

export function TreeDataTable<TData, TValue>({
  columns,
  data,
  getSubRows,
  defaultExpanded = true,
  pageCount,
  rowCount,
  pagination,
  onPaginationChange,
  searchValue = "",
  onSearchChange,
  searchPlaceholder = "Tìm kiếm...",
  filterSlot,
  isLoading = false,
  onSelectMany,
  bulkActions,
  onRowClick,
  mobileCardRenderer,
}: TreeDataTableProps<TData, TValue>) {
  const [rowSelection, setRowSelection] = useState({});
  const [expanded, setExpanded] = useState<ExpandedState>(
    defaultExpanded ? true : {},
  );
  // Khởi tạo Pagination State (Client-side fallback)
  const [internalPagination, setInternalPagination] = useState({
    pageIndex: 0,
    pageSize: 10,
  });

  const isServerSide =
    pagination !== undefined && onPaginationChange !== undefined;
  const currentPagination = isServerSide ? pagination : internalPagination;
  const handlePaginationChange = isServerSide
    ? onPaginationChange
    : setInternalPagination;

  const isMobile = useMediaQuery("(max-width: 767px)");
  const [localSearch, setLocalSearch] = useState(searchValue);

  const triggerSearch = useCallback(
    (newVal?: string) => {
      const targetSearch = newVal !== undefined ? newVal : localSearch;
      if (onSearchChange && targetSearch !== searchValue) {
        onSearchChange(targetSearch);
        if (!isServerSide) {
          setInternalPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }
      }
    },
    [localSearch, onSearchChange, searchValue, isServerSide],
  );

  useEffect(() => {
    const timer = setTimeout(() => triggerSearch(), 500);
    return () => clearTimeout(timer);
  }, [triggerSearch]);

  useEffect(() => {
    setLocalSearch(searchValue);
  }, [searchValue]);

  // Cấu hình Table
  const table = useReactTable({
    data,
    columns,
    state: {
      rowSelection,
      expanded,
      pagination: currentPagination,
    },
    manualPagination: isServerSide,
    pageCount: isServerSide ? (pageCount ?? -1) : undefined,
    getSubRows,
    onExpandedChange: setExpanded,
    getExpandedRowModel: getExpandedRowModel(),
    onRowSelectionChange: setRowSelection,
    onPaginationChange: handlePaginationChange,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: isServerSide ? undefined : getPaginationRowModel(),
  });

  const selectedRows = table.getFilteredSelectedRowModel().rows;
  const isSelectionMode = isMobile && selectedRows.length > 0;
  const totalRowsCount = isServerSide
    ? (rowCount ?? data.length)
    : table.getPrePaginationRowModel().rows.length;

  const exitSelectionMode = useCallback(() => {
    table.resetRowSelection();
  }, [table]);

  return (
    <div className="flex flex-col h-full w-full flex-1">
      {/* ------------------ TOP BAR ------------------ */}
      <div className="flex flex-col gap-3 w-full mb-4 md:flex-row md:items-center md:justify-between md:gap-4">
        <div className="flex flex-wrap items-center gap-2 md:gap-3">
          {filterSlot && (
            <>
              <span className="text-sm font-medium text-muted-foreground hidden md:block">
                Bộ lọc:
              </span>
              {filterSlot}
            </>
          )}
        </div>

        {onSearchChange && (
          <div className="relative w-full md:w-96 flex items-center">
            <Input
              placeholder={searchPlaceholder}
              value={localSearch}
              onChange={(event) => setLocalSearch(event.target.value)}
              onClear={() => {
                setLocalSearch("");
                triggerSearch("");
              }}
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  triggerSearch();
                }
              }}
              className="h-9 bg-background pr-9"
            />

            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="ml-2"
              onClick={() => triggerSearch()}
            >
              <Search className="w-4 h-4" />
            </Button>
          </div>
        )}
      </div>

      {/* ------------------ MOBILE SELECTION HEADER ------------------ */}
      {isSelectionMode && (
        <div className="flex items-center justify-between gap-3 mb-3 px-1 animate-in fade-in slide-in-from-top-1 duration-200">
          <div className="flex items-center gap-2">
            <CheckCheck className="w-4 h-4 text-primary" />
            <span className="text-sm font-medium text-foreground">
              Đã chọn{" "}
              <strong className="text-primary">{selectedRows.length}</strong>{" "}
              dòng
            </span>
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              className="h-8 text-xs text-muted-foreground"
              onClick={exitSelectionMode}
            >
              Hủy
            </Button>
          </div>
        </div>
      )}

      {isMobile ? (
        /* ------------------ MOBILE CARD VIEW ------------------ */
        <div className="flex flex-col gap-3">
          {isLoading ? (
            <div className="flex items-center justify-center py-16">
              <Loader2 className="w-5 h-5 animate-spin text-primary" />
            </div>
          ) : table.getRowModel().rows?.length ? (
            table.getRowModel().rows.map((row) => {
              const actionColumn = row
                .getVisibleCells()
                .find((c) => c.column.id === "action");
              const actionNode =
                !isSelectionMode && actionColumn ? (
                  <div
                    className="pt-3 mt-3 border-t border-border flex justify-end w-full"
                    onClick={(e) => e.stopPropagation()}
                  >
                    {flexRender(
                      actionColumn.column.columnDef.cell,
                      actionColumn.getContext(),
                    )}
                  </div>
                ) : null;

              return (
                <MobileCardWrapper
                  key={row.id}
                  row={row}
                  isSelectionMode={isSelectionMode}
                  onRowClick={onRowClick}
                  onLongPress={() => row.toggleSelected(true)}
                  onTapInSelectionMode={() =>
                    row.toggleSelected(!row.getIsSelected())
                  }
                >
                  {mobileCardRenderer ? (
                    mobileCardRenderer(row, {
                      isSelectionMode,
                      isSelected: row.getIsSelected(),
                      actionCell: actionNode,
                    })
                  ) : (
                    <div
                      className="rounded-xl border bg-card p-4 shadow-sm"
                      data-state={row.getIsSelected() && "selected"}
                    >
                      Mobile view needs mobileCardRenderer prop
                    </div>
                  )}
                </MobileCardWrapper>
              );
            })
          ) : (
            <div className="flex items-center justify-center py-16 text-muted-foreground text-sm">
              Không tìm thấy dữ liệu.
            </div>
          )}
        </div>
      ) : (
        /* ------------------ DESKTOP TREE TABLE VIEW ------------------ */
        <div className="relative w-full overflow-auto rounded-md border border-border flex-1">
          <Table>
            <TableHeader className="bg-secondary/50">
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow
                  key={headerGroup.id}
                  className="hover:bg-transparent border-b-border"
                >
                  {headerGroup.headers.map((header) => (
                    <TableHead
                      key={header.id}
                      className="h-11 font-semibold text-muted-foreground"
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext(),
                          )}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {isLoading ? (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="h-32 text-center text-muted-foreground"
                  >
                    <Loader2 className="w-5 h-5 animate-spin mx-auto text-primary" />
                  </TableCell>
                </TableRow>
              ) : table.getRowModel().rows?.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow
                    key={row.id}
                    data-state={row.getIsSelected() && "selected"}
                    className="hover:bg-secondary/40 border-b-border transition-colors cursor-pointer"
                    onClick={() => onRowClick && onRowClick(row.original)}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <TableCell key={cell.id} className="py-3">
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext(),
                        )}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="h-32 text-center text-muted-foreground"
                  >
                    Không tìm thấy dữ liệu.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {/* ------------------ BULK ACTIONS BANNER (PC) ------------------ */}
      {selectedRows.length > 0 && bulkActions && !isSelectionMode && (
        <div className="bg-primary/5 border border-primary/20 text-primary px-3 py-2.5 mt-4 rounded-xl flex flex-col gap-3 text-sm shadow-sm animate-in fade-in slide-in-from-bottom-2 sm:flex-row sm:items-center sm:justify-between sm:px-4 sm:py-3">
          <span>
            Đã chọn:{" "}
            <strong className="font-semibold text-lg">
              {selectedRows.length}
            </strong>{" "}
            dòng
          </span>
          <div className="flex items-center gap-2 sm:gap-3">
            <Button
              variant="outline"
              size="sm"
              className="h-9 bg-background border-border flex-1 sm:flex-none"
              onClick={() => table.resetRowSelection()}
            >
              Hủy bỏ
            </Button>
            {bulkActions(
              selectedRows.map((row) => row.original),
              () => table.resetRowSelection(),
            )}
          </div>
        </div>
      )}

      {/* ------------------ PAGINATION FOOTER ------------------ */}
      {totalRowsCount > 0 && (
        <div
          className={`flex flex-col gap-3 py-4 mt-auto border-t border-transparent sm:flex-row sm:items-center sm:justify-between ${
            isMobile && "justify-end items-center"
          }`}
        >
          <div className="text-sm text-muted-foreground text-center sm:text-left">
            Hiển thị{" "}
            <span className="font-medium text-foreground">
              {table.getState().pagination.pageIndex *
                table.getState().pagination.pageSize +
                1}
            </span>{" "}
            đến{" "}
            <span className="font-medium text-foreground">
              {Math.min(
                (table.getState().pagination.pageIndex + 1) *
                  table.getState().pagination.pageSize,
                totalRowsCount,
              )}
            </span>{" "}
            trong{" "}
            <span className="font-medium text-foreground">
              {totalRowsCount}
            </span>{" "}
            kết quả
          </div>

          <div className="flex items-center justify-between gap-3 sm:gap-6">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground whitespace-nowrap hidden sm:inline">
                Số dòng:
              </span>
              <Select
                value={table.getState().pagination.pageSize.toString()}
                onValueChange={(val) => table.setPageSize(Number(val))}
              >
                <SelectTrigger className="h-8 w-[75px] bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent
                  showSearch={false}
                  className="min-w-[var(--radix-select-trigger-width)] w-[var(--radix-select-trigger-width)]"
                >
                  {[5, 10, 20, 50, 100].map((pageSize) => (
                    <SelectItem key={pageSize} value={pageSize.toString()}>
                      {pageSize}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-1.5 sm:gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
                className="h-8 px-2 sm:px-3"
              >
                <ChevronLeft className="w-4 h-4 sm:mr-1" />
                <span className="hidden sm:inline">Trước</span>
              </Button>
              <div className="text-sm font-medium text-foreground px-1.5 sm:px-2 min-w-[60px] sm:min-w-[80px] text-center">
                {table.getState().pagination.pageIndex + 1} /{" "}
                {table.getPageCount()}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
                className="h-8 px-2 sm:px-3"
              >
                <span className="hidden sm:inline">Sau</span>
                <ChevronRight className="w-4 h-4 sm:ml-1" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
