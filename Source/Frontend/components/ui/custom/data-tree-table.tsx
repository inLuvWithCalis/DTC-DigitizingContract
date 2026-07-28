"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ColumnDef,
  flexRender,
  getCoreRowModel,
  getExpandedRowModel,
  getPaginationRowModel,
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
import { CheckCheck, Search, ChevronLeft, ChevronRight } from "lucide-react";
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
  actionCell: React.ReactNode;
}

interface TreeDataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  getSubRows: (row: TData) => TData[] | undefined;
  defaultExpanded?: boolean;

  pageCount?: number;
  rowCount?: number;
  pagination?: { pageIndex: number; pageSize: number };
  onPaginationChange?: (pagination: any) => void;

  searchValue?: string;
  onSearchChange?: (value: string) => void;
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

  bulkActions,
  onRowClick,
  mobileCardRenderer,
}: TreeDataTableProps<TData, TValue>) {
  const [rowSelection, setRowSelection] = useState({});
  const [expanded, setExpanded] = useState<ExpandedState>(
    defaultExpanded ? true : {},
  );
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

  const selectedRows = table.getFilteredSelectedRowModel().flatRows;
  const isSelectionMode = isMobile && selectedRows.length > 0;
  const totalRowsCount = isServerSide
    ? (rowCount ?? data.length)
    : table.getPrePaginationRowModel().rows.length;

  const exitSelectionMode = useCallback(() => {
    table.resetRowSelection();
  }, [table]);

  return (
    <div className="flex flex-col h-full w-full flex-1">
      {(!!filterSlot || !!onSearchChange) && (
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
      )}

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

      {isMobile ? (
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

              const nameCell = row
                .getVisibleCells()
                .find((c) => c.column.id === "categoryName");

              const isSelected = row.getIsSelected();

              return (
                <MobileCardWrapper
                  key={row.id}
                  row={row}
                  isSelectionMode={isSelectionMode}
                  onRowClick={onRowClick}
                  onLongPress={() => row.toggleSelected(true)}
                  onTapInSelectionMode={() => row.toggleSelected(!isSelected)}
                >
                  {mobileCardRenderer ? (
                    mobileCardRenderer(row, {
                      isSelectionMode,
                      isSelected,
                      actionCell: actionNode,
                    })
                  ) : (
                    <div
                      className={`rounded-xl border p-4 shadow-sm transition-all relative ${
                        isSelected
                          ? "bg-primary/10 border-primary shadow-inner"
                          : "bg-card border-border"
                      } ${row.depth > 0 ? "border-l-4" : ""}`}
                      data-state={isSelected ? "selected" : undefined}
                    >
                      <div className="flex items-start gap-2 mb-3 pb-3 border-b border-border/50">
                        <div className="flex-1">
                          {nameCell
                            ? flexRender(
                                nameCell.column.columnDef.cell,
                                nameCell.getContext(),
                              )
                            : null}
                        </div>
                      </div>

                      <div className="flex flex-col gap-2.5">
                        {row.getVisibleCells().map((cell) => {
                          const columnId = cell.column.id;
                          if (
                            columnId === "select" ||
                            columnId === "action" ||
                            columnId === "categoryName"
                          )
                            return null;

                          return (
                            <div
                              key={cell.id}
                              className="flex items-start justify-between gap-3"
                            >
                              <span className="text-xs font-medium text-muted-foreground shrink-0 pt-0.5 capitalize">
                                {typeof cell.column.columnDef.header ===
                                "string"
                                  ? cell.column.columnDef.header
                                  : columnId}
                              </span>
                              <div className="text-sm text-right font-medium">
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
        </div>
      ) : (
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
      {isLoading ? (
        <TablePaginationSkeleton />
      ) : (
        <div
          className={`flex flex-col gap-3 py-4 mt-auto border-t border-transparent sm:flex-row sm:items-center sm:justify-between ${
            isMobile && "justify-end items-center"
          }`}
        >
          <div className="text-sm text-muted-foreground text-center sm:text-left">
            Hiển thị{" "}
            <span className="font-medium text-foreground">
              {totalRowsCount > 0
                ? table.getState().pagination.pageIndex *
                    table.getState().pagination.pageSize +
                  1
                : 0}
            </span>{" "}
            đến{" "}
            <span className="font-medium text-foreground">
              {totalRowsCount > 0
                ? Math.min(
                    (table.getState().pagination.pageIndex + 1) *
                      table.getState().pagination.pageSize,
                    totalRowsCount,
                  )
                : 0}
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
                disabled={isLoading || totalRowsCount <= 0}
              >
                <SelectTrigger className="h-8 w-[75px] bg-background border-border">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent
                  showSearch={false}
                  className="min-w-[--radix-select-trigger-width] w-[--radix-select-trigger-width]"
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
                disabled={
                  isLoading ||
                  totalRowsCount <= 0 ||
                  !table.getCanPreviousPage()
                }
                className="h-8 px-2 sm:px-3"
              >
                <ChevronLeft className="w-4 h-4 sm:mr-1" />
                <span className="hidden sm:inline">Trước</span>
              </Button>
              <div className="text-sm font-medium text-foreground px-1.5 sm:px-2 min-w-[60px] sm:min-w-[80px] text-center">
                {totalRowsCount > 0
                  ? table.getState().pagination.pageIndex + 1
                  : 0}{" "}
                / {totalRowsCount > 0 ? table.getPageCount() : 0}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.nextPage()}
                disabled={
                  isLoading || totalRowsCount <= 0 || !table.getCanNextPage()
                }
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
