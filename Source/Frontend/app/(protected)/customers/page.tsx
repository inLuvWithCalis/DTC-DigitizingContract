"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import { Header } from "@/components/ui/custom/header";
import {
  CheckCircle2,
  Lock,
  Unlock,
  Eye,
  UserCog,
  Plus,
  Users,
  Building2,
  Mail,
  Phone,
  MapPin,
  FileText,
  Shield,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "@/components/ui/sonner";
import { ColumnDef, PaginationState } from "@tanstack/react-table";
import { format } from "date-fns";

import {
  customerApi,
  CustomerResponse,
  CustomerStatus,
  getCustomerStatusLabel,
} from "@/services/customers-api";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { StatusBadge } from "@/components/ui/custom/status-badge";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import {
  DateRangeFilter,
  DateRange,
} from "@/components/ui/custom/date-range-filter";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { PageHeaderSkeleton } from "@/components/ui/custom/table-skeleton";
import { CustomerFormModal } from "./customer-form-modal";
import { useRouter } from "next/navigation";

function CustomerBulkActions({
  selectedRows,
  resetSelection,
}: {
  selectedRows: CustomerResponse[];
  resetSelection: () => void;
}) {
  const [isLoading, setIsLoading] = useState(false);

  const handleBulkSetStatus = async (status: CustomerStatus) => {
    setIsLoading(true);
    try {
      await Promise.all(
        selectedRows.map((customer) =>
          customerApi.setStatus(customer.customerId, status),
        ),
      );
      toast.success(
        `Đã cập nhật trạng thái cho ${selectedRows.length} khách hàng`,
      );
      resetSelection();
    } catch (error) {
      toast.error("Lỗi khi cập nhật trạng thái hàng loạt");
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <SplitActionMenu
      primaryLabel="Ngừng hoạt động"
      primaryIcon={<Lock className="w-4 h-4" />}
      onPrimaryClick={() => handleBulkSetStatus(CustomerStatus.Inactive)}
      isLoading={isLoading}
      menuItems={[
        {
          label: "Đang hoạt động",
          icon: <Unlock className="w-4 h-4" />,
          onClick: () => handleBulkSetStatus(CustomerStatus.Active),
        },
      ]}
    />
  );
}

const CUSTOMER_STATUS_OPTIONS = [
  { label: "Tất cả trạng thái", value: "All" },
  { label: "Đang hoạt động", value: String(CustomerStatus.Active) },
  { label: "Ngừng hoạt động", value: String(CustomerStatus.Inactive) },
];

export default function CustomerListPage() {
  const [customers, setCustomers] = useState<CustomerResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(false);
  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  });
  const [searchTerm, setSearchTerm] = useState("");
  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [dateRange, setDateRange] = useState<DateRange>({
    from: undefined,
    to: undefined,
  });
  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [toggleStatusId, setToggleStatusId] = useState<number | null>(null);
  const [isToggling, setIsToggling] = useState(false);
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<CustomerResponse | null>(null);
  const [viewingItem, setViewingItem] = useState<CustomerResponse | null>(null);

  const router = useRouter();

  const fetchCustomers = useCallback(async () => {
    setIsLoading(true);
    try {
      const statusParam =
        filterStatus !== "All" && filterStatus !== ""
          ? Number(filterStatus)
          : undefined;
      const fromDateParam = dateRange.from
        ? format(dateRange.from, "yyyy-MM-dd")
        : undefined;
      const toDateParam = dateRange.to
        ? format(dateRange.to, "yyyy-MM-dd")
        : undefined;

      const res = await customerApi.getList({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchTerm || undefined,
        status: statusParam,
        fromDate: fromDateParam,
        toDate: toDateParam,
      });

      setCustomers(res.items || []);
      setTotalCount(res.totalCount || 0);
    } catch (error) {
      toast.error("Lỗi khi tải danh sách khách hàng");
    } finally {
      setIsLoading(false);
    }
  }, [
    pagination.pageIndex,
    pagination.pageSize,
    searchTerm,
    filterStatus,
    dateRange.from,
    dateRange.to,
  ]);

  useEffect(() => {
    fetchCustomers();
  }, [fetchCustomers]);

  const handleToggleStatusConfirm = async () => {
    if (!toggleStatusId) return;
    setIsToggling(true);
    try {
      const targetCust = customers.find((c) => c.customerId === toggleStatusId);
      const newStatus =
        targetCust?.status === CustomerStatus.Active
          ? CustomerStatus.Inactive
          : CustomerStatus.Active;

      await customerApi.setStatus(toggleStatusId, newStatus);
      toast.success(
        newStatus === CustomerStatus.Active
          ? "Đã chuyển sang đang hoạt động"
          : "Đã ngừng hoạt động khách hàng",
      );
      setToggleStatusId(null);
      fetchCustomers();
    } catch (error) {
      toast.error("Không thể cập nhật trạng thái");
    } finally {
      setIsToggling(false);
    }
  };

  const columns = useMemo<ColumnDef<CustomerResponse>[]>(
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
        accessorKey: "customerCode",
        header: "Mã KH",
        cell: ({ row }) => (
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
              <UserCog className="w-4.5 h-4.5 text-primary" />
            </div>
            <div className="flex flex-col min-w-0">
              <span className="font-semibold text-foreground text-sm truncate">
                {row.original.customerFullName}
              </span>
              <span className="text-xs text-muted-foreground truncate flex items-center gap-1 mt-0.5">
                {row.original.customerCode || "N/A"}
              </span>
            </div>
          </div>
        ),
      },
      {
        accessorKey: "customerFullName",
        header: "Khách hàng / Đối tác",
        cell: ({ row }) => (
          <div className="max-w-[280px]">
            <span className="font-medium text-foreground block truncate">
              {row.original.customerFullName || "Chưa có tên"}
            </span>
            {row.original.customerCompany && (
              <span className="text-xs text-muted-foreground flex items-center gap-1 truncate mt-0.5">
                <Building2 className="w-3 h-3 shrink-0" />
                {row.original.customerCompany}
              </span>
            )}
          </div>
        ),
      },
      {
        id: "contact",
        header: "Liên hệ",
        cell: ({ row }) => {
          const item = row.original;
          const phone = item.customerMobile || item.customerPhone;
          return (
            <div className="flex flex-col gap-1 text-sm min-w-[160px]">
              {item.customerEmail ? (
                <div className="flex items-center gap-1.5 text-foreground truncate max-w-[200px]">
                  <Mail className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
                  <span className="truncate">{item.customerEmail}</span>
                </div>
              ) : null}
              {phone ? (
                <div className="flex items-center gap-1.5 text-muted-foreground">
                  <Phone className="w-3.5 h-3.5 shrink-0" />
                  <span>{phone}</span>
                </div>
              ) : null}
              {!item.customerEmail && !phone && (
                <span className="text-muted-foreground italic text-xs">
                  Chưa có thông tin
                </span>
              )}
            </div>
          );
        },
      },
      {
        id: "location",
        header: "Khu vực",
        cell: ({ row }) => {
          const city = row.original.customerCity;
          const country = row.original.customerCountry;
          const locationText = [city, country].filter(Boolean).join(", ");
          return (
            <div className="flex items-center gap-1.5 min-w-[120px] max-w-[180px]">
              <MapPin className="w-3.5 h-3.5 text-muted-foreground shrink-0" />
              <span className="text-sm truncate">
                {locationText || "Chưa xác định"}
              </span>
            </div>
          );
        },
      },
      {
        accessorKey: "totalContracts",
        header: () => <div className="text-center">Hợp đồng</div>,
        cell: ({ row }) => (
          <div className="text-center font-semibold min-w-[80px]">
            <span className="inline-flex items-center px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs">
              <FileText className="w-3 h-3 mr-1" />
              {row.original.totalContracts || 0}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => {
          const statusVal = row.original.status;
          return (
            <div className="flex justify-center min-w-[130px]">
              <StatusBadge status={getCustomerStatusLabel(statusVal)} />
            </div>
          );
        },
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          const isActive = item.status === CustomerStatus.Active;
          return (
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => {
                router.push(`/customers/${item.customerId}`);
              }}
              isLoading={loadingId === item.customerId}
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
                  label: isActive ? "Ngừng hoạt động" : "Đang hoạt động",
                  icon: isActive ? (
                    <Lock className="w-4 h-4" />
                  ) : (
                    <Unlock className="w-4 h-4" />
                  ),
                  isDestructive: isActive,
                  onClick: () => setToggleStatusId(item.customerId),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const activeCount = useMemo(
    () => customers.filter((c) => c.status === CustomerStatus.Active).length,
    [customers],
  );

  const summaryItems: SummaryCardItem[] = useMemo(
    () => [
      {
        title: "Tổng khách hàng",
        value: totalCount,
        icon: <Users className="w-6 h-6" />,
        iconWrapperClassName: "bg-primary/10 text-primary",
      },
      {
        title: "Đang hoạt động (Trang này)",
        value: activeCount,
        icon: <CheckCircle2 className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
      },
      {
        title: "Ngừng hoạt động (Trang này)",
        value: customers.length - activeCount,
        icon: <Lock className="w-6 h-6" />,
        iconWrapperClassName:
          "bg-amber-500/10 text-amber-600 dark:text-amber-500",
      },
    ],
    [totalCount, activeCount, customers.length],
  );

  const CustomFilters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={(val) => {
          setFilterStatus(val);
          setPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }}
        options={CUSTOMER_STATUS_OPTIONS}
        placeholder="Trạng thái"
      />
      <DateRangeFilter
        dateRange={dateRange}
        onChange={(range) => {
          setDateRange(range);
          setPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }}
      />
    </>
  );

  const currentToggleItem = customers.find(
    (c) => c.customerId === toggleStatusId,
  );
  const isTogglingToLock = currentToggleItem?.status === CustomerStatus.Active;

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Danh sách Khách hàng
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Quản lý danh sách Khách hàng / Đối tác trong hệ thống CRM
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
              data={customers}
              pageCount={Math.ceil(totalCount / pagination.pageSize)}
              pagination={pagination}
              rowCount={totalCount}
              onPaginationChange={setPagination}
              isLoading={isLoading}
              searchPlaceholder="Tìm kiếm tên, mã KH, công ty, email, số ĐT..."
              filterSlot={CustomFilters}
              onRowClick={(row) => router.push(`/customers/${row.customerId}`)}
              searchValue={searchTerm}
              onSearchChange={(value) => setSearchTerm(value)}
              bulkActions={(selectedRows, resetSelection) => (
                <CustomerBulkActions
                  selectedRows={selectedRows}
                  resetSelection={resetSelection}
                />
              )}
              mobileCardRenderer={(row, { isSelected, actionCell }) => {
                const item = row.original;
                const phone = item.customerMobile || item.customerPhone;
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
                        <span className="text-xs font-semibold text-muted-foreground uppercase tracking-wider block">
                          {item.customerCode || "CHƯA CÓ MÃ"}
                        </span>
                        <h3 className="text-base font-bold text-foreground mt-0.5">
                          {item.customerFullName || "Chưa có tên"}
                        </h3>
                      </div>
                      <StatusBadge
                        className="m-2"
                        status={getCustomerStatusLabel(item.status)}
                      />
                    </div>

                    {item.customerCompany && (
                      <div className="flex items-center gap-1.5 text-xs text-muted-foreground mt-1">
                        <Building2 className="w-3.5 h-3.5 shrink-0" />
                        <span className="font-medium truncate">
                          {item.customerCompany}
                        </span>
                      </div>
                    )}

                    <div className="grid grid-cols-2 gap-2 text-sm text-muted-foreground bg-muted/40 p-2.5 rounded-lg border border-border/50 my-2">
                      <div>
                        <span className="text-xs text-muted-foreground block">
                          Email / SĐT
                        </span>
                        <span className="font-medium text-foreground block truncate text-xs">
                          {item.customerEmail || phone || "N/A"}
                        </span>
                      </div>
                      <div>
                        <span className="text-xs text-muted-foreground block">
                          Hợp đồng
                        </span>
                        <span className="font-medium text-primary block">
                          {item.totalContracts || 0} hợp đồng
                        </span>
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
          isOpen={!!toggleStatusId}
          onClose={() => setToggleStatusId(null)}
          onConfirm={handleToggleStatusConfirm}
          isLoading={isToggling}
          title={
            isTogglingToLock
              ? "Ngừng hoạt động khách hàng?"
              : "Mở lại hoạt động khách hàng?"
          }
          description={
            isTogglingToLock
              ? `Bạn có chắc chắn muốn ngừng hoạt động khách hàng "${currentToggleItem?.customerFullName}"? Khách hàng này sẽ không thể tạo mới báo giá hoặc hợp đồng.`
              : `Bạn có chắc chắn muốn mở lại hoạt động cho khách hàng "${currentToggleItem?.customerFullName}"?`
          }
          confirmText={isTogglingToLock ? "Xác nhận ngừng" : "Xác nhận mở lại"}
          variant={isTogglingToLock ? "destructive" : "default"}
          titleClassName={
            isTogglingToLock ? "text-destructive" : "text-foreground"
          }
        />

        <CustomerFormModal
          isOpen={isFormOpen}
          onClose={() => {
            setIsFormOpen(false);
            setEditingItem(null);
          }}
          onSuccess={fetchCustomers}
          item={editingItem}
        />

        {/* <CustomerFormModal
          isOpen={!!viewingItem}
          onClose={() => setViewingItem(null)}
          onSuccess={() => {}}
          item={viewingItem}
          viewOnly
        /> */}
      </div>
    </>
  );
}
