"use client";

import * as React from "react";
import {
  ColumnDef,
  ColumnFiltersState,
  SortingState,
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  Row,
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
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  MoreHorizontal,
  CheckCheck,
  Search,
} from "lucide-react";
import { useMediaQuery } from "@/hooks/use-media-query";
import { MobileCardWrapper } from "./mobile-card-wrapper";
import {
  TableRowSkeleton,
  MobileCardSkeleton,
  TableHeaderSkeleton,
  TablePaginationSkeleton,
} from "./table-skeleton";

interface MobileCardRenderContext {
  isSelectionMode: boolean;
  isSelected: boolean;
  actionCell?: React.ReactNode;
}

function getPaginationRange(currentPage: number, totalPages: number) {
  if (totalPages <= 5) {
    return Array.from({ length: Math.max(1, totalPages) }, (_, i) => i + 1);
  }
  if (currentPage <= 3) {
    return [1, 2, 3, 4, "...", totalPages];
  }
  if (currentPage >= totalPages - 2) {
    return [
      1,
      "...",
      totalPages - 3,
      totalPages - 2,
      totalPages - 1,
      totalPages,
    ];
  }
  return [
    1,
    "...",
    currentPage - 1,
    currentPage,
    currentPage + 1,
    "...",
    totalPages,
  ];
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  searchKey?: string;
  searchPlaceholder?: string;
  filterSlot?: React.ReactNode;
  isLoading?: boolean;

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

export function DataTable<TData, TValue>({
  columns,
  data,
  searchKey,
  searchPlaceholder = "Tìm kiếm...",
  filterSlot,
  isLoading = false,

  bulkActions,
  onRowClick,
  mobileCardRenderer,
}: DataTableProps<TData, TValue>) {
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    [],
  );
  const [rowSelection, setRowSelection] = React.useState({});
  const isMobile = useMediaQuery("(max-width: 767px)");

  const table = useReactTable({
    data,
    columns,
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    onSortingChange: setSorting,
    onColumnFiltersChange: setColumnFilters,
    onRowSelectionChange: setRowSelection,
    state: {
      sorting,
      columnFilters,
      rowSelection,
    },
  });

  const selectedRows = table.getFilteredSelectedRowModel().rows;
  const isSelectionMode = isMobile && selectedRows.length > 0;

  // Exit selection mode resets selection
  const exitSelectionMode = React.useCallback(() => {
    table.resetRowSelection();
  }, [table]);

  return (
    <div className="flex flex-col h-full w-full flex-1">
      {(!!filterSlot || !!searchKey) && (
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

          {searchKey && (
            <div className="relative w-full md:w-64">
              <Input
                placeholder={searchPlaceholder}
                value={
                  (table.getColumn(searchKey)?.getFilterValue() as string) ?? ""
                }
                onChange={(event) =>
                  table.getColumn(searchKey)?.setFilterValue(event.target.value)
                }
                className="h-9 bg-background"
              />
            </div>
          )}
        </div>
      )}

      {/* Mobile selection mode toolbar */}
      {isMobile && isSelectionMode && (
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
              className="h-8 text-xs"
              onClick={() => table.toggleAllPageRowsSelected(true)}
            >
              Chọn tất cả
            </Button>
            <Button
              variant="ghost"
              size="sm"
              className="h-8 text-xs text-muted-foreground"
              onClick={exitSelectionMode}
            >
              Hủy
            </Button>
            {bulkActions?.(
              selectedRows.map((row) => row.original),
              () => table.resetRowSelection(),
            )}
          </div>
        </div>
      )}

      {/* 2. MAIN CONTENT: Mobile Cards or Desktop Table */}
      {isMobile ? (
        /* ────────────── MOBILE CARD VIEW ────────────── */
        <div className="flex flex-col gap-3">
          {isLoading ? (
            <MobileCardSkeleton count={5} />
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
                  onLongPress={() => {
                    row.toggleSelected(true);
                  }}
                  onTapInSelectionMode={() => {
                    row.toggleSelected(!row.getIsSelected());
                  }}
                >
                  {mobileCardRenderer ? (
                    mobileCardRenderer(row, {
                      isSelectionMode,
                      isSelected: row.getIsSelected(),
                      actionCell: actionNode,
                    })
                  ) : (
                    <div
                      className="rounded-xl border border-border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40"
                      data-state={row.getIsSelected() && "selected"}
                    >
                      <div className="flex flex-col gap-2.5">
                        {row.getVisibleCells().map((cell) => {
                          const header = cell.column.columnDef.header;
                          const columnId = cell.column.id;

                          if (columnId === "select") return null;

                          let label: React.ReactNode = columnId;
                          if (typeof header === "string") {
                            label = header;
                          }

                          if (columnId === "action") {
                            if (isSelectionMode) return null;
                            return (
                              <div
                                key={cell.id}
                                className="pt-2 mt-1 border-t border-border"
                                onClick={(e) => e.stopPropagation()}
                              >
                                {flexRender(
                                  cell.column.columnDef.cell,
                                  cell.getContext(),
                                )}
                              </div>
                            );
                          }

                          return (
                            <div
                              key={cell.id}
                              className="flex items-start justify-between gap-3"
                            >
                              <span className="text-xs font-medium text-muted-foreground shrink-0 pt-0.5 capitalize">
                                {label}
                              </span>
                              <div className="text-sm text-right">
                                {flexRender(
                                  cell.column.columnDef.cell,
                                  cell.getContext(),
                                )}
                              </div>
                            </div>
                          );
                        })}
                      </div>
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

          {/* Long-press hint — shown when NOT in selection mode */}
          {!isSelectionMode &&
            table.getRowModel().rows?.length > 0 &&
            bulkActions && (
              <p className="text-center text-xs text-muted-foreground/60 mt-1 select-none">
                Nhấn giữ để chọn nhiều dòng
              </p>
            )}
        </div>
      ) : (
        /* ────────────── DESKTOP TABLE VIEW ────────────── */
        <div
          className={`relative w-full overflow-auto rounded-md border border-border flex-1 ${
            table.getState().pagination.pageSize <= 5
              ? "min-h-108.5"
              : "min-h-full"
          }`}
        >
          <Table>
            {isLoading ? (
              <TableHeaderSkeleton columnCount={columns.length} />
            ) : (
              <TableHeader className="bg-secondary/50">
                {table.getHeaderGroups().map((headerGroup) => {
                  const hasSelectColumn = headerGroup.headers.some(
                    (h) => h.column.id === "select",
                  );
                  const hasSelectedRows = selectedRows.length > 0;

                  return (
                    <TableRow
                      key={headerGroup.id}
                      className="hover:bg-transparent border-b-border"
                    >
                      {headerGroup.headers.map((header, index) => {
                        if (hasSelectedRows) {
                          if (header.column.id === "select") {
                            return (
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
                            );
                          }

                          const isOverlayPosition = hasSelectColumn
                            ? index === 1
                            : index === 0;

                          if (!isOverlayPosition) return null;

                          const colSpan =
                            headerGroup.headers.length -
                            (hasSelectColumn ? 1 : 0);

                          return (
                            <TableHead
                              key="selection-header-overlay"
                              colSpan={colSpan}
                              className="h-11 font-normal text-foreground py-0"
                            >
                              <div className="flex items-center justify-between gap-3 px-1 animate-in fade-in duration-150">
                                <div className="flex items-center gap-2">
                                  <CheckCheck className="w-4 h-4 text-primary" />
                                  <span className="text-sm font-medium text-foreground">
                                    Đã chọn{" "}
                                    <strong className="text-primary">
                                      {selectedRows.length}
                                    </strong>{" "}
                                    dòng
                                  </span>
                                </div>
                                <div className="flex items-center gap-2">
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-8 text-xs"
                                    onClick={() =>
                                      table.toggleAllPageRowsSelected(true)
                                    }
                                  >
                                    Chọn tất cả
                                  </Button>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    className="h-8 text-xs text-muted-foreground"
                                    onClick={exitSelectionMode}
                                  >
                                    Hủy
                                  </Button>
                                  {bulkActions?.(
                                    selectedRows.map((row) => row.original),
                                    () => table.resetRowSelection(),
                                  )}
                                </div>
                              </div>
                            </TableHead>
                          );
                        }

                        return (
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
                        );
                      })}
                    </TableRow>
                  );
                })}
              </TableHeader>
            )}
            <TableBody>
              {isLoading ? (
                <TableRowSkeleton columnCount={columns.length} rowCount={5} />
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
                    className={`${
                      table.getState().pagination.pageSize <= 5
                        ? "h-86"
                        : "h-172"
                    } text-center text-muted-foreground`}
                  >
                    Không tìm thấy dữ liệu.
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {/* 4. PAGINATION */}
      {isLoading ? (
        <TablePaginationSkeleton />
      ) : (
        <div
          className={`flex flex-col gap-3 py-4 mt-auto border-t border-transparent sm:flex-row sm:items-center sm:justify-between ${isMobile && "justify-end items-center"}`}
        >
          <div className="text-sm text-muted-foreground text-center sm:text-left">
            Hiển thị{" "}
            <span className="font-medium text-foreground">
              {data.length > 0
                ? table.getState().pagination.pageIndex *
                    table.getState().pagination.pageSize +
                  1
                : 0}
            </span>{" "}
            đến{" "}
            <span className="font-medium text-foreground">
              {data.length > 0
                ? Math.min(
                    (table.getState().pagination.pageIndex + 1) *
                      table.getState().pagination.pageSize,
                    data.length,
                  )
                : 0}
            </span>{" "}
            trong{" "}
            <span className="font-medium text-foreground">{data.length}</span>{" "}
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
                disabled={isLoading || data.length <= 0}
              >
                <SelectTrigger className="h-8 w-[75px] bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {[5, 10, 20, 50].map((pageSize) => (
                    <SelectItem key={pageSize} value={pageSize.toString()}>
                      {pageSize}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-1">
              <Button
                variant="outline"
                size="icon"
                onClick={() => table.setPageIndex(0)}
                disabled={
                  isLoading || data.length <= 0 || !table.getCanPreviousPage()
                }
                className="h-8 w-8"
              >
                <ChevronsLeft className="w-4 h-4" />
                <span className="sr-only">Trang đầu</span>
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={() => table.previousPage()}
                disabled={
                  isLoading || data.length <= 0 || !table.getCanPreviousPage()
                }
                className="h-8 w-8"
              >
                <ChevronLeft className="w-4 h-4" />
                <span className="sr-only">Trang trước</span>
              </Button>

              {getPaginationRange(
                data.length > 0 ? table.getState().pagination.pageIndex + 1 : 1,
                data.length > 0 ? table.getPageCount() : 1,
              ).map((page, idx) => {
                if (page === "...") {
                  return (
                    <span
                      key={`gap-${idx}`}
                      className="flex h-8 w-6 items-center justify-center text-xs text-muted-foreground select-none"
                    >
                      <MoreHorizontal className="h-3.5 w-3.5" />
                    </span>
                  );
                }

                const pageNum = page as number;
                const isCurrent =
                  data.length > 0 &&
                  pageNum === table.getState().pagination.pageIndex + 1;

                return (
                  <Button
                    key={pageNum}
                    variant={isCurrent ? "default" : "outline"}
                    size="icon"
                    onClick={() => table.setPageIndex(pageNum - 1)}
                    disabled={isLoading || data.length <= 0}
                    className="h-8 w-8 text-xs font-medium"
                  >
                    {pageNum}
                  </Button>
                );
              })}

              <Button
                variant="outline"
                size="icon"
                onClick={() => table.nextPage()}
                disabled={
                  isLoading || data.length <= 0 || !table.getCanNextPage()
                }
                className="h-8 w-8"
              >
                <ChevronRight className="w-4 h-4" />
                <span className="sr-only">Trang sau</span>
              </Button>
              <Button
                variant="outline"
                size="icon"
                onClick={() =>
                  table.setPageIndex(Math.max(0, table.getPageCount() - 1))
                }
                disabled={
                  isLoading || data.length <= 0 || !table.getCanNextPage()
                }
                className="h-8 w-8"
              >
                <ChevronsRight className="w-4 h-4" />
                <span className="sr-only">Trang cuối</span>
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
