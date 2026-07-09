"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ColumnDef,
  SortingState,
  flexRender,
  getCoreRowModel,
  useReactTable,
  Row,
  PaginationState,
  OnChangeFn,
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
  ChevronLeft,
  ChevronRight,
  CheckCheck,
  Search,
} from "lucide-react";
import { useMediaQuery } from "@/hooks/use-media-query";
import { MobileCardWrapper } from "./mobile-card-wrapper";

interface MobileCardRenderContext {
  isSelectionMode: boolean;
  isSelected: boolean;
  actionCell: React.ReactNode;
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  pageCount: number;
  rowCount: number;
  pagination: PaginationState;
  onPaginationChange: OnChangeFn<PaginationState>;
  sorting?: SortingState;
  onSortingChange?: OnChangeFn<SortingState>;
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

export function DataTable<TData, TValue>({
  columns,
  data,
  pageCount,
  rowCount,
  pagination,
  onPaginationChange,
  sorting = [],
  onSortingChange,
  searchValue = "",
  onSearchChange,
  searchPlaceholder = "Tìm kiếm...",
  filterSlot,
  isLoading = false,
  onSelectMany,
  bulkActions,
  onRowClick,
  mobileCardRenderer,
}: DataTableProps<TData, TValue>) {
  const [rowSelection, setRowSelection] = useState({});
  const isMobile = useMediaQuery("(max-width: 767px)");

  const [localSearch, setLocalSearch] = useState(searchValue);

  useEffect(() => {
    const timer = setTimeout(() => {
      if (onSearchChange && localSearch !== searchValue) {
        onSearchChange(localSearch);
      }
    }, 500);
    return () => clearTimeout(timer);
  }, [localSearch, onSearchChange, searchValue]);

  useEffect(() => {
    setLocalSearch(searchValue);
  }, [searchValue]);

  const table = useReactTable({
    data,
    columns,
    pageCount,
    rowCount,
    state: {
      pagination,
      sorting,
      rowSelection,
    },
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
    onPaginationChange,
    onSortingChange,
    onRowSelectionChange: setRowSelection,
    getCoreRowModel: getCoreRowModel(),
  });

  const selectedRows = table.getFilteredSelectedRowModel().rows;
  const isSelectionMode = isMobile && selectedRows.length > 0;

  const exitSelectionMode = useCallback(() => {
    table.resetRowSelection();
  }, [table]);

  return (
    <div className="flex flex-col h-full w-full flex-1">
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
              onKeyDown={(event) => {
                if (event.key === "Enter") {
                  event.preventDefault();
                  if (onSearchChange && localSearch !== searchValue) {
                    onSearchChange(localSearch);
                  }
                }
              }}
              className="h-9 bg-background pr-9"
            />

            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="ml-2"
              onClick={() => {
                if (onSearchChange && localSearch !== searchValue) {
                  onSearchChange(localSearch);
                }
              }}
            >
              <Search className="w-4 h-4" />
            </Button>
          </div>
        )}
      </div>

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
          </div>
        </div>
      )}

      {isMobile ? (
        /* ────────────── MOBILE CARD VIEW ────────────── */
        <div className="flex flex-col gap-3">
          {isLoading ? (
            <div className="flex items-center justify-center py-16">
              <Loader2 className="w-5 h-5 animate-spin text-primary" />
            </div>
          ) : table.getRowModel().rows?.length ? (
            table.getRowModel().rows.map((row) => {
              // TỰ ĐỘNG TÌM VÀ RENDER CỘT ACTION
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
                    // Truyền actionNode xuống cho Page sử dụng
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

                          if (columnId === "select" || columnId === "action")
                            return null;

                          let label: React.ReactNode = columnId;
                          if (typeof header === "string") {
                            label = header;
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

                      {actionNode}
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

          {!isSelectionMode &&
            table.getRowModel().rows?.length > 0 &&
            (bulkActions || onSelectMany) && (
              <p className="text-center text-xs text-muted-foreground/60 mt-1 select-none">
                Nhấn giữ để chọn nhiều dòng
              </p>
            )}
        </div>
      ) : (
        /* ────────────── DESKTOP TABLE VIEW ────────────── */
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
              {table.getRowModel().rows?.length ? (
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
                    {isLoading ? (
                      <Loader2 className="w-5 h-5 animate-spin mx-auto text-primary" />
                    ) : (
                      "Không tìm thấy dữ liệu."
                    )}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
      )}

      {selectedRows.length > 0 && (bulkActions || onSelectMany) && (
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
            {bulkActions ? (
              bulkActions(
                selectedRows.map((row) => row.original),
                () => table.resetRowSelection(),
              )
            ) : (
              <Button
                size="sm"
                variant="destructive"
                className="h-9 shadow-sm flex-1 sm:flex-none"
                onClick={() => {
                  onSelectMany?.(selectedRows.map((row) => row.original));
                  table.resetRowSelection();
                }}
              >
                Xóa tất cả
              </Button>
            )}
          </div>
        </div>
      )}

      {rowCount > 0 && (
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
                rowCount,
              )}
            </span>{" "}
            trong{" "}
            <span className="font-medium text-foreground">{rowCount}</span> kết
            quả
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
                <SelectContent>
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
