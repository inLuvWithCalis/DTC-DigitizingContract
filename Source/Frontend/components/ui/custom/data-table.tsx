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
import { Loader2, ChevronLeft, ChevronRight, CheckCheck } from "lucide-react";
import { useMediaQuery } from "@/hooks/use-media-query";
import { MobileCardWrapper } from "./mobile-card-wrapper";

interface MobileCardRenderContext {
  isSelectionMode: boolean;
  isSelected: boolean;
}

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  searchKey?: string;
  searchPlaceholder?: string;
  filterSlot?: React.ReactNode;
  isLoading?: boolean;
  onDeleteMany?: (selectedRows: TData[]) => void;
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
  onDeleteMany,
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
      {/* 1. TOP TOOLBAR: Lọc & Tìm kiếm */}
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

      {/* Mobile selection mode toolbar */}
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

      {/* 2. MAIN CONTENT: Mobile Cards or Desktop Table */}
      {isMobile ? (
        /* ────────────── MOBILE CARD VIEW ────────────── */
        <div className="flex flex-col gap-3">
          {isLoading ? (
            <div className="flex items-center justify-center py-16">
              <Loader2 className="w-5 h-5 animate-spin text-muted-foreground" />
            </div>
          ) : table.getRowModel().rows?.length ? (
            table.getRowModel().rows.map((row) => (
              <MobileCardWrapper
                key={row.id}
                row={row}
                isSelectionMode={isSelectionMode}
                onRowClick={onRowClick}
                onLongPress={() => {
                  // Toggle this row and enter selection mode
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
                  })
                ) : (
                  /* Default mobile card */
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
            ))
          ) : (
            <div className="flex items-center justify-center py-16 text-muted-foreground text-sm">
              Không tìm thấy dữ liệu.
            </div>
          )}

          {/* Long-press hint — shown when NOT in selection mode */}
          {!isSelectionMode &&
            table.getRowModel().rows?.length > 0 &&
            onDeleteMany && (
              <p className="text-center text-xs text-muted-foreground/60 mt-1 select-none">
                Nhấn giữ để chọn nhiều dòng
              </p>
            )}
        </div>
      ) : (
        /* ────────────── DESKTOP TABLE VIEW ────────────── */
        <div className="relative w-full overflow-auto rounded-md border border-border">
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
                      <Loader2 className="w-5 h-5 animate-spin mx-auto" />
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

      {/* 3. BULK ACTION (Xóa nhiều) */}
      {selectedRows.length > 0 && onDeleteMany && (
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
            <Button
              size="sm"
              variant="destructive"
              className="h-9 shadow-sm flex-1 sm:flex-none"
              onClick={() => {
                onDeleteMany(selectedRows.map((row) => row.original));
                table.resetRowSelection();
              }}
            >
              Xóa tất cả
            </Button>
          </div>
        </div>
      )}

      {/* 4. PAGINATION */}
      {!isLoading && data.length > 0 && (
        <div className="flex flex-col gap-3 py-4 mt-auto border-t border-transparent flex-1 sm:flex-row sm:items-end sm:justify-between">
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
                data.length,
              )}
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

                  {data.length > 0 &&
                    ![5, 10, 20, 50].includes(data.length) && (
                      <SelectItem value={data.length.toString()}>
                        Tất cả
                      </SelectItem>
                    )}
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
