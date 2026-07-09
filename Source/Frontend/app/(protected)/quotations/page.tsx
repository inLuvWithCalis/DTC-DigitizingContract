"use client";

import { useState, useEffect, useMemo } from "react";
import { useRouter } from "next/navigation";
import { Header } from "@/components/ui/custom/header";
import {
  CalendarDays,
  ArrowUp,
  ArrowDown,
  ArrowUpDown,
  Eye,
  Trash2,
  FileText,
  DollarSign,
  Clock,
  Users,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";
import { ColumnDef, Row } from "@tanstack/react-table";
import { quotationApi, QuotationResponseDto } from "@/services/quotations-api";
import { formatDateTime } from "@/lib/format-date-time";
import { formatCurrency } from "@/lib/format-currency";
import { DataTable } from "@/components/ui/custom/data-table";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import { DateRangeFilter } from "@/components/ui/custom/date-range-filter";
import { StatusBadge } from "@/components/ui/custom/status-badge";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { applyTableFilters } from "@/lib/filter-utils";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";

export default function QuotationListPage() {
  const router = useRouter();

  const [quotations, setQuotations] = useState<QuotationResponseDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [dateRange, setDateRange] = useState<{
    from: Date | undefined;
    to: Date | undefined;
  }>({ from: undefined, to: undefined });

  useEffect(() => {
    const fetchQuotations = async () => {
      setIsLoading(true);
      try {
        const data = await quotationApi.getAll();
        setQuotations(Array.isArray(data) ? data : (data as any)?.data || []);
      } catch (error) {
        toast.error("Lỗi khi tải danh sách báo giá");
      } finally {
        setIsLoading(false);
      }
    };
    fetchQuotations();
  }, []);

  const filteredData = useMemo(() => {
    return applyTableFilters({
      data: quotations,
      statusValue: filterStatus,
      statusKey: "quatationStatus",
      dateRange: dateRange,
      dateKey: "quotationDate",
    });
  }, [quotations, filterStatus, dateRange]);

  const handleView = (id: number) => {
    setLoadingId(id);
    router.push(`/quotations/${id}`);
  };

  const handleDeleteConfirm = async () => {
    if (!deleteId) return;
    setIsDeleting(true);
    try {
      await quotationApi.delete(deleteId);
      toast.success("Xóa báo giá thành công");
      setQuotations(quotations.filter((q) => q.quotationId !== deleteId));
      setDeleteId(null);
    } catch (error) {
      toast.error("Không thể xóa báo giá này");
    } finally {
      setIsDeleting(false);
    }
  };

  const columns = useMemo<ColumnDef<QuotationResponseDto>[]>(
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
        accessorKey: "quotationNo",
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
              <StatusBadge status={row.original.quatationStatus} />
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
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => handleView(item.quotationId)}
              isLoading={loadingId === item.quotationId}
              menuItems={[
                {
                  label: "Xóa báo giá",
                  icon: <Trash2 className="w-4 h-4" />,
                  isDestructive: true,
                  onClick: () => setDeleteId(item.quotationId),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const QUOTATION_STATUS_OPTIONS = [
    { label: "Tất cả trạng thái", value: "All" },
    { label: "Đã duyệt", value: "Approved" },
    { label: "Từ chối", value: "Rejected" },
    { label: "Bản nháp", value: "Draft" },
    { label: "Đã gửi", value: "Sent" },
  ];
  const CustomFilters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={setFilterStatus}
        options={QUOTATION_STATUS_OPTIONS}
        placeholder="Trạng thái"
      />

      <DateRangeFilter dateRange={dateRange} onChange={setDateRange} />
    </>
  );

  const totalQuotations = quotations.length;
  const pendingCount = quotations.filter(
    (q) => q.quatationStatus === "Pending",
  ).length;
  const totalValue = quotations.reduce(
    (acc, curr) => acc + (curr.totalAmount || 0),
    0,
  );

  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng báo giá",
      value: totalQuotations,
      icon: <FileText className="w-6 h-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Chờ phê duyệt",
      value: pendingCount,
      icon: <Clock className="w-6 h-6" />,
      iconWrapperClassName:
        "bg-amber-500/10 text-amber-600 dark:text-amber-500",
    },
    {
      title: "Tổng giá trị",
      value: formatCurrency(totalValue),
      icon: <DollarSign className="w-6 h-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
      valueClassName: "text-xl",
    },
  ];

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
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
        <SummaryCards items={summaryItems} />
        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={filteredData}
              isLoading={isLoading}
              searchKey="quotationNo"
              searchPlaceholder="Tìm mã báo giá..."
              filterSlot={CustomFilters}
              onSelectMany={(rows) =>
                toast.info(`Đang gọi API xóa ${rows.length} dòng...`)
              }
              onRowClick={(row) => handleView(row.quotationId)}
              mobileCardRenderer={(
                row: Row<QuotationResponseDto>,
                { isSelected },
              ) => {
                const item = row.original;
                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    {/* Row 1: Mã báo giá + Trạng thái */}
                    <div className="flex items-start justify-between gap-3 mb-3">
                      <div className="flex flex-col min-w-0">
                        <span className="font-semibold text-foreground truncate">
                          {item.quotationNo}
                        </span>
                        <span className="text-xs text-muted-foreground flex items-center gap-1 mt-0.5">
                          <CalendarDays className="w-3 h-3 shrink-0" />
                          {formatDateTime(item.quotationDate)}
                        </span>
                      </div>
                      <StatusBadge status={item.quatationStatus} />
                    </div>

                    {/* Row 2: Khách hàng + Tổng tiền */}
                    <div className="flex items-center justify-between gap-3 pt-3 border-t border-border">
                      <div className="flex items-center gap-2 min-w-0">
                        <div className="w-7 h-7 rounded-full bg-secondary flex items-center justify-center shrink-0">
                          <Users className="w-3.5 h-3.5 text-muted-foreground" />
                        </div>
                        <span className="text-sm text-muted-foreground truncate">
                          KH-{item.customerId}
                        </span>
                      </div>
                      <span className="font-semibold text-primary text-sm whitespace-nowrap">
                        {formatCurrency(item.totalAmount)}
                      </span>
                    </div>
                  </div>
                );
              }}
            />
          </CardContent>
        </Card>

        <ConfirmDialog
          isOpen={!!deleteId}
          onClose={() => setDeleteId(null)}
          onConfirm={handleDeleteConfirm}
          title="Xác nhận xóa báo giá"
          description="Bạn có chắc chắn muốn xóa báo giá này không? Hành động này không thể hoàn tác và toàn bộ dữ liệu sẽ bị xóa khỏi hệ thống."
          icon={<Trash2 className="w-5 h-5" />}
          confirmText="Xóa báo giá"
          variant="destructive"
          titleClassName="text-destructive"
          isLoading={isDeleting}
        />
      </div>
    </>
  );
}
