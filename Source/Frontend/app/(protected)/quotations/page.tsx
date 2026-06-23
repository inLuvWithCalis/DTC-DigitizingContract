"use client";

import { useState, useEffect, useMemo } from "react";
import { useRouter } from "next/navigation";
import { Sidebar } from "@/components/sidebar";
import { Header } from "@/components/ui/custom/header"; // Thay đường dẫn đúng với project của bạn
import {
  Search,
  Loader2,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  ArrowUp,
  ArrowDown,
  ArrowUpDown,
  Eye,
  Delete,
  Trash2,
  FileText,
  DollarSign,
  Clock,
  CheckCircle2,
  Users,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Card, CardHeader, CardContent } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";

import { endOfDay, format, isWithinInterval, startOfDay } from "date-fns";
import { cn } from "@/lib/utils";
import { toast } from "sonner";
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getPaginationRowModel,
  flexRender,
  ColumnDef,
  SortingState,
} from "@tanstack/react-table";

// --- Import API Service của bạn ---
import { quotationApi, QuotationResponseDto } from "@/services/quotations-api";
import { formatDateTime } from "@/lib/format-date-time";
import { formatCurrency } from "@/lib/format-currency";

export default function QuotationListPage() {
  const router = useRouter();

  // State Dữ liệu
  const [quotations, setQuotations] = useState<QuotationResponseDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  // State Hành động (Action)
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  // State Lọc & Tìm kiếm
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [dateRange, setDateRange] = useState<{
    from: Date | undefined;
    to: Date | undefined;
  }>({ from: undefined, to: undefined });

  // State TanStack Table
  const [sorting, setSorting] = useState<SortingState>([]);
  const [rowSelection, setRowSelection] = useState({});
  const [pagination, setPagination] = useState({
    pageIndex: 0,
    pageSize: 10,
  });

  // ==========================================
  // FETCH DATA
  // ==========================================
  useEffect(() => {
    const fetchQuotations = async () => {
      setIsLoading(true);
      try {
        const data = await quotationApi.getAll();
        setQuotations(data);
      } catch (error) {
        toast.error("Lỗi khi tải danh sách báo giá");
      } finally {
        setIsLoading(false);
      }
    };
    fetchQuotations();
  }, []);

  // Reset pagination khi đổi filter
  useEffect(() => {
    setPagination((prev) => ({ ...prev, pageIndex: 0 }));
  }, [searchTerm, filterStatus, dateRange]);

  // ==========================================
  // XỬ LÝ LỌC (FILTER) DỮ LIỆU
  // ==========================================
  const filteredData = useMemo(() => {
    return quotations.filter((item) => {
      // 1. Lọc theo trạng thái
      let matchesStatus =
        filterStatus !== "All" ? item.quatationStatus === filterStatus : true;

      // 2. Lọc theo Search (Mã báo giá)
      const searchLower = searchTerm.toLowerCase();
      const matchesSearch = item.quotationNo
        ?.toLowerCase()
        .includes(searchLower);

      // 3. Lọc theo Ngày
      let matchesDate = true;
      if (dateRange.from || dateRange.to) {
        if (!item.quotationDate) {
          matchesDate = false;
        } else {
          const itemDate = new Date(item.quotationDate);
          const fromDate = dateRange.from
            ? startOfDay(dateRange.from)
            : new Date(2000, 0, 1);
          const toDate = dateRange.to
            ? endOfDay(dateRange.to)
            : new Date(2100, 0, 1);
          matchesDate = isWithinInterval(itemDate, {
            start: fromDate,
            end: toDate,
          });
        }
      }

      return matchesStatus && matchesSearch && matchesDate;
    });
  }, [quotations, filterStatus, searchTerm, dateRange]);

  // ==========================================
  // HÀM TIỆN ÍCH & XỬ LÝ LOGIC
  // ==========================================
  const handleView = (id: number) => {
    setLoadingId(id);
    router.push(`/dashboard/quotations/${id}`);
  };

  const handleDeleteConfirm = async () => {
    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await quotationApi.delete(deleteId);
      toast.success("Xóa báo giá thành công");
      setQuotations(quotations.filter((q) => q.quotationId !== deleteId));
      setDeleteId(null);
      setRowSelection({}); // Clear select nếu có
    } catch (error) {
      toast.error("Không thể xóa báo giá này");
    } finally {
      setIsDeleting(false);
    }
  };

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "Pending":
      case "Draft":
        return "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20";
      case "Approved":
        return "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20";
      case "Rejected":
        return "bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-500/10 dark:text-rose-400 dark:border-rose-500/20";
      default:
        return "bg-secondary text-secondary-foreground border-border";
    }
  };

  // ==========================================
  // CẤU HÌNH CỘT (COLUMNS) CHO TANSTACK TABLE
  // ==========================================
  const columns = useMemo<ColumnDef<QuotationResponseDto>[]>(
    () => [
      {
        id: "select",
        header: ({ table }) => (
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
        ),
        cell: ({ row }) => (
          <Checkbox
            checked={row.getIsSelected()}
            onCheckedChange={(value) => row.toggleSelected(!!value)}
            aria-label="Select row"
            className="translate-y-[2px]"
          />
        ),
        enableSorting: false,
      },
      {
        accessorKey: "quotationDate",
        header: ({ column }) => (
          <div
            className="flex items-center gap-1.5 select-none cursor-pointer group"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Mã Báo giá & Ngày
            {{
              asc: <ArrowUp className="w-3.5 h-3.5 text-primary" />,
              desc: <ArrowDown className="w-3.5 h-3.5 text-primary" />,
            }[column.getIsSorted() as string] ?? (
              <ArrowUpDown className="w-3.5 h-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
            )}
          </div>
        ),
        cell: ({ row }) => (
          <div className="flex flex-col pl-1">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-foreground">
                {row.original.quotationNo}
              </span>
            </div>
            <span className="text-xs text-muted-foreground flex items-center gap-1 mt-1">
              <CalendarDays className="w-3 h-3" />
              {formatDateTime(row.original.quotationDate)}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "customerId",
        header: "Đối tác (KH)",
        cell: ({ row }) => (
          <div className="flex items-center gap-2">
            <div className="w-8 h-8 rounded-full bg-secondary flex items-center justify-center">
              <Users className="w-4 h-4 text-muted-foreground" />
            </div>
            <div className="flex flex-col">
              <span className="font-medium text-foreground text-sm">
                KH-{row.original.customerId}
              </span>
              <span className="text-xs text-muted-foreground">
                ID Khách hàng
              </span>
            </div>
          </div>
        ),
      },
      {
        accessorKey: "totalAmount",
        header: ({ column }) => (
          <div
            className="flex items-center justify-end gap-1.5 select-none cursor-pointer group"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Tổng tiền
            {{
              asc: <ArrowUp className="w-3.5 h-3.5 text-primary" />,
              desc: <ArrowDown className="w-3.5 h-3.5 text-primary" />,
            }[column.getIsSorted() as string] ?? (
              <ArrowUpDown className="w-3.5 h-3.5 text-muted-foreground opacity-0 group-hover:opacity-100 transition-opacity" />
            )}
          </div>
        ),
        cell: ({ row }) => (
          <div className="text-right font-semibold text-primary">
            {formatCurrency(row.original.totalAmount)}
          </div>
        ),
      },
      {
        accessorKey: "quatationStatus",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => {
          const status = row.original.quatationStatus;
          return (
            <div className="text-center">
              <Badge variant="outline" className={getStatusBadge(status)}>
                {status === "Pending"
                  ? "Chờ duyệt"
                  : status === "Approved"
                    ? "Đã duyệt"
                    : status === "Rejected"
                      ? "Từ chối"
                      : status}
              </Badge>
            </div>
          );
        },
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <div className="flex items-center justify-end gap-2 pr-4">
              <Button
                size="sm"
                variant="outline"
                onClick={(e) => {
                  e.stopPropagation();
                  handleView(item.quotationId);
                }}
                disabled={loadingId === item.quotationId}
                className="h-8 w-8 p-0 text-primary border-primary/20 hover:bg-primary/10"
              >
                {loadingId === item.quotationId ? (
                  <Loader2 className="w-4 h-4 animate-spin" />
                ) : (
                  <Eye className="w-4 h-4" />
                )}
              </Button>
              <Button
                size="sm"
                variant="outline"
                onClick={(e) => {
                  e.stopPropagation();
                  setDeleteId(item.quotationId); // Mở Modal Xóa
                }}
                className="h-8 w-8 p-0 text-rose-600 border-rose-200 hover:bg-rose-50 hover:text-rose-700 dark:border-rose-500/20 dark:hover:bg-rose-500/10"
              >
                <Trash2 className="w-4 h-4" />
              </Button>
            </div>
          );
        },
      },
    ],
    [loadingId],
  );

  // ==========================================
  // KHỞI TẠO TANSTACK TABLE
  // ==========================================
  const table = useReactTable({
    data: filteredData,
    columns,
    state: { sorting, rowSelection, pagination },
    onSortingChange: setSorting,
    onRowSelectionChange: setRowSelection,
    onPaginationChange: setPagination,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: getSortedRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
  });

  const selectedRows = table.getSelectedRowModel().rows;

  // Thống kê nhanh
  const totalQuotations = quotations.length;
  const pendingCount = quotations.filter(
    (q) => q.quatationStatus === "Pending",
  ).length;
  const totalValue = quotations.reduce(
    (acc, curr) => acc + (curr.totalAmount || 0),
    0,
  );

  return (
    <div className="flex flex-row h-screen w-screen overflow-hidden bg-background">
      <Sidebar />
      <main className="grow flex flex-col overflow-hidden relative z-10">
        <Header title="Quản lý Hợp đồng & Báo giá" />

        <div className="grow overflow-y-auto p-6 lg:p-10 space-y-6">
          {/* Header & Mô tả */}
          <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl font-bold tracking-tight text-foreground">
                Danh sách Báo giá
              </h1>
              <p className="text-sm text-muted-foreground mt-1">
                Quản lý, theo dõi trạng thái và tạo các báo giá gửi cho đối tác.
              </p>
            </div>
            <Button className="shadow-sm">
              <FileText className="w-4 h-4 mr-2" /> Tạo báo giá mới
            </Button>
          </div>

          {/* Cards Thống kê */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card className="bg-card border-border shadow-sm">
              <CardContent className="p-4 flex items-center gap-4">
                <div className="p-3 bg-primary/10 text-primary rounded-lg">
                  <FileText className="w-6 h-6" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground font-medium">
                    Tổng báo giá
                  </p>
                  <h3 className="text-2xl font-bold text-foreground">
                    {totalQuotations}
                  </h3>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-card border-border shadow-sm">
              <CardContent className="p-4 flex items-center gap-4">
                <div className="p-3 bg-amber-500/10 text-amber-600 rounded-lg dark:text-amber-500">
                  <Clock className="w-6 h-6" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground font-medium">
                    Chờ phê duyệt
                  </p>
                  <h3 className="text-2xl font-bold text-foreground">
                    {pendingCount}
                  </h3>
                </div>
              </CardContent>
            </Card>

            <Card className="bg-card border-border shadow-sm">
              <CardContent className="p-4 flex items-center gap-4">
                <div className="p-3 bg-emerald-500/10 text-emerald-600 rounded-lg dark:text-emerald-500">
                  <DollarSign className="w-6 h-6" />
                </div>
                <div>
                  <p className="text-sm text-muted-foreground font-medium">
                    Tổng giá trị
                  </p>
                  <h3 className="text-xl font-bold text-foreground">
                    {formatCurrency(totalValue)}
                  </h3>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Khu vực Bảng Dữ liệu */}
          <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 pb-0">
            <CardHeader className="border-b border-border pb-4">
              <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 w-full">
                {/* Bộ lọc */}
                <div className="flex flex-wrap items-center gap-3">
                  <span className="text-sm font-medium text-muted-foreground hidden md:block">
                    Bộ lọc:
                  </span>

                  <Select value={filterStatus} onValueChange={setFilterStatus}>
                    <SelectTrigger className="w-[160px] bg-background border-border shadow-sm h-9 cursor-pointer">
                      <SelectValue placeholder="Trạng thái" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="All">Tất cả trạng thái</SelectItem>
                      <SelectItem value="Pending">Chờ duyệt</SelectItem>
                      <SelectItem value="Approved">Đã duyệt</SelectItem>
                      <SelectItem value="Rejected">Từ chối</SelectItem>
                    </SelectContent>
                  </Select>

                  <div className="flex items-center gap-2">
                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "justify-start text-left font-normal h-9 bg-background shadow-sm",
                            !dateRange.from && "text-muted-foreground",
                          )}
                        >
                          <CalendarDays className="mr-2 h-4 w-4" />
                          {dateRange.from ? (
                            format(dateRange.from, "dd/MM/yyyy")
                          ) : (
                            <span>Từ ngày</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0" align="start">
                        <Calendar
                          mode="single"
                          selected={dateRange.from}
                          onSelect={(date) =>
                            setDateRange((prev) => ({ ...prev, from: date }))
                          }
                          initialFocus
                        />
                      </PopoverContent>
                    </Popover>

                    <span className="text-muted-foreground">-</span>

                    <Popover>
                      <PopoverTrigger asChild>
                        <Button
                          variant="outline"
                          className={cn(
                            "justify-start text-left font-normal h-9 bg-background shadow-sm",
                            !dateRange.to && "text-muted-foreground",
                          )}
                        >
                          <CalendarDays className="mr-2 h-4 w-4" />
                          {dateRange.to ? (
                            format(dateRange.to, "dd/MM/yyyy")
                          ) : (
                            <span>Đến ngày</span>
                          )}
                        </Button>
                      </PopoverTrigger>
                      <PopoverContent className="w-auto p-0" align="start">
                        <Calendar
                          mode="single"
                          selected={dateRange.to}
                          onSelect={(date) =>
                            setDateRange((prev) => ({ ...prev, to: date }))
                          }
                          initialFocus
                          disabled={(date) =>
                            dateRange.from ? date < dateRange.from : false
                          }
                        />
                      </PopoverContent>
                    </Popover>

                    {(dateRange.from || dateRange.to) && (
                      <Button
                        variant="ghost"
                        size="sm"
                        className="h-8 text-xs text-muted-foreground px-2 hover:bg-destructive/10 hover:text-destructive"
                        onClick={() =>
                          setDateRange({ from: undefined, to: undefined })
                        }
                      >
                        <Delete className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>

                {/* Tìm kiếm */}
                <div className="relative w-full md:w-64">
                  <Input
                    placeholder="Tìm mã báo giá..."
                    className="h-9 bg-background"
                    maxLength={50}
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                  />
                </div>
              </div>
            </CardHeader>

            <CardContent className="p-0 flex flex-col justify-between flex-1">
              {/* Table */}
              <div className="relative w-full overflow-auto">
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
                          // onClick={() => handleView(row.original.quotationId)}
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
                            "Không tìm thấy dữ liệu báo giá."
                          )}
                        </TableCell>
                      </TableRow>
                    )}
                  </TableBody>
                </Table>
              </div>

              {/* Bulk Action (Xóa hàng loạt) */}
              {selectedRows.length > 0 && (
                <div className="bg-primary/5 border border-primary/20 text-primary px-4 py-3 mx-4 mt-4 rounded-xl flex items-center justify-between text-sm shadow-sm animate-in fade-in slide-in-from-bottom-2">
                  <span>
                    Đã chọn:{" "}
                    <strong className="font-semibold text-lg">
                      {selectedRows.length}
                    </strong>{" "}
                    báo giá
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
                      onClick={() =>
                        toast.info(
                          "Chức năng xóa nhiều dòng đang được phát triển.",
                        )
                      }
                    >
                      Xóa tất cả
                    </Button>
                  </div>
                </div>
              )}

              {/* Phân trang */}
              {!isLoading && filteredData.length > 0 && (
                <div className="flex flex-col sm:flex-row items-center justify-between px-6 py-4 border-t border-border bg-secondary/20 mt-auto">
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
                        filteredData.length,
                      )}
                    </span>{" "}
                    trong{" "}
                    <span className="font-medium text-foreground">
                      {filteredData.length}
                    </span>{" "}
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
                            <SelectItem
                              key={pageSize}
                              value={pageSize.toString()}
                            >
                              {pageSize}
                            </SelectItem>
                          ))}
                          <SelectItem value={filteredData.length.toString()}>
                            Tất cả
                          </SelectItem>
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
            </CardContent>
          </Card>
        </div>
      </main>

      {/* Dialog Xác nhận Xóa */}
      <Dialog
        open={!!deleteId}
        onOpenChange={(open) => !open && setDeleteId(null)}
      >
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle className="text-rose-600 flex items-center gap-2">
              <Trash2 className="w-5 h-5" /> Xác nhận xóa báo giá
            </DialogTitle>
            <DialogDescription className="pt-2">
              Bạn có chắc chắn muốn xóa báo giá này không? Hành động này không
              thể hoàn tác và toàn bộ dữ liệu (bao gồm cả sản phẩm chi tiết) sẽ
              bị xóa khỏi hệ thống.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0 mt-4">
            <Button
              variant="outline"
              onClick={() => setDeleteId(null)}
              disabled={isDeleting}
            >
              Hủy bỏ
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeleteConfirm}
              disabled={isDeleting}
            >
              {isDeleting && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
              Xóa báo giá
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
