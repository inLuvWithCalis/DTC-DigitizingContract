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
import { Loader2, ChevronLeft, ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";

interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[];
  data: TData[];
  searchKey?: string; // Khóa của cột để áp dụng ô tìm kiếm (VD: 'quotationNo')
  searchPlaceholder?: string;
  filterSlot?: React.ReactNode; // Khe cắm cho các Filter tùy chỉnh (DateRange, Status...)
  isLoading?: boolean;
  onDeleteMany?: (selectedRows: TData[]) => void; // Hàm xử lý khi bấm Xóa Hàng Loạt
}

export function DataTable<TData, TValue>({
  columns,
  data,
  searchKey,
  searchPlaceholder = "Tìm kiếm...",
  filterSlot,
  isLoading = false,
  onDeleteMany,
}: DataTableProps<TData, TValue>) {
  const [sorting, setSorting] = React.useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>(
    [],
  );
  const [rowSelection, setRowSelection] = React.useState({});

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

  return (
    <div className="flex flex-col h-full w-full">
      {/* 1. TOP TOOLBAR: Lọc & Tìm kiếm */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 w-full mb-4">
        <div className="flex flex-wrap items-center gap-3">
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

      {/* 2. MAIN TABLE */}
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

      {/* 3. BULK ACTION (Xóa nhiều) */}
      {selectedRows.length > 0 && onDeleteMany && (
        <div className="bg-primary/5 border border-primary/20 text-primary px-4 py-3 mt-4 rounded-xl flex items-center justify-between text-sm shadow-sm animate-in fade-in slide-in-from-bottom-2">
          <span>
            Đã chọn:{" "}
            <strong className="font-semibold text-lg">
              {selectedRows.length}
            </strong>{" "}
            dòng
          </span>
          <div className="flex items-center gap-3">
            <Button
              variant="outline"
              size="sm"
              className="h-9 bg-background border-border"
              onClick={() => table.resetRowSelection()}
            >
              Hủy bỏ
            </Button>
            <Button
              size="sm"
              variant="destructive"
              className="h-9 shadow-sm"
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
        <div className="flex flex-col sm:flex-row items-center justify-between py-4 mt-auto border-t border-transparent">
          <div className="text-sm text-muted-foreground">
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

          <div className="flex items-center gap-6 mt-4 sm:mt-0">
            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground whitespace-nowrap">
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
                  <SelectItem value={data.length.toString()}>Tất cả</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
                className="h-8"
              >
                <ChevronLeft className="w-4 h-4 mr-1" /> Trước
              </Button>
              <div className="text-sm font-medium text-foreground px-2 min-w-[80px] text-center">
                Trang {table.getState().pagination.pageIndex + 1} /{" "}
                {table.getPageCount()}
              </div>
              <Button
                variant="outline"
                size="sm"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
                className="h-8"
              >
                Sau <ChevronRight className="w-4 h-4 ml-1" />
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
