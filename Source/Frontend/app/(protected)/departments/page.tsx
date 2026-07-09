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
  Building2,
  Lock,
  Unlock,
  Building,
  CheckCircle2,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "sonner";
import { ColumnDef, Row } from "@tanstack/react-table";
import { departmentApi, DepartmentResponse } from "@/services/departments-api";
import { formatDateTime } from "@/lib/format-date-time";
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

export default function DepartmentListPage() {
  const router = useRouter();

  const [departments, setDepartments] = useState<DepartmentResponse[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const [loadingId, setLoadingId] = useState<number | null>(null);
  const [toggleStatusId, setToggleStatusId] = useState<number | null>(null);
  const [isToggling, setIsToggling] = useState(false);

  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [dateRange, setDateRange] = useState<{
    from: Date | undefined;
    to: Date | undefined;
  }>({ from: undefined, to: undefined });

  const fetchDepartments = async () => {
    setIsLoading(true);
    try {
      const data = await departmentApi.getAll();
      setDepartments(Array.isArray(data) ? data : (data as any)?.data || []);
    } catch (error) {
      toast.error("Lỗi khi tải danh sách phòng ban");
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchDepartments();
  }, []);

  const filteredData = useMemo(() => {
    return applyTableFilters({
      data: departments,
      statusValue: filterStatus,
      statusKey: "status",
      dateRange: dateRange,
      dateKey: "modifiedDate",
    });
  }, [departments, filterStatus, dateRange]);

  const handleView = (id: number) => {
    setLoadingId(id);
    router.push(`/departments/${id}`);
  };

  const handleToggleStatusConfirm = async () => {
    if (!toggleStatusId) return;
    setIsToggling(true);
    try {
      const targetDept = departments.find(
        (d) => d.departmentId === toggleStatusId,
      );
      const newStatus = targetDept?.status === 1 ? 0 : 1;

      await departmentApi.setStatus(toggleStatusId, newStatus);
      toast.success("Cập nhật trạng thái phòng ban thành công");

      setDepartments((prev) =>
        prev.map((d) =>
          d.departmentId === toggleStatusId ? { ...d, status: newStatus } : d,
        ),
      );
      setToggleStatusId(null);
    } catch (error) {
      toast.error("Không thể cập nhật trạng thái");
    } finally {
      setIsToggling(false);
    }
  };

  const columns = useMemo<ColumnDef<DepartmentResponse>[]>(
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
        accessorKey: "departmentCode",
        header: ({ column }) => (
          <div
            className="flex items-center gap-1.5 select-none cursor-pointer group"
            onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
          >
            Mã Phòng ban
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
            <span className="font-semibold text-foreground">
              {row.original.departmentCode}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "departmentName",
        header: "Tên phòng ban",
        cell: ({ row }) => (
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 rounded-lg bg-primary/10 flex items-center justify-center shrink-0">
              <Building2 className="w-4 h-4 text-primary" />
            </div>
            <span className="font-medium text-foreground text-sm">
              {row.original.departmentName}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "modifiedDate",
        header: "Cập nhật lần cuối",
        cell: ({ row }) => (
          <div className="text-sm text-muted-foreground flex items-center gap-1.5">
            <CalendarDays className="w-3.5 h-3.5" />
            {row.original.modifiedDate
              ? formatDateTime(row.original.modifiedDate)
              : "Chưa cập nhật"}
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => {
          const statusStr = row.original.status === 1 ? "Active" : "Inactive";
          return (
            <div className="text-center">
              <StatusBadge status={statusStr} />
            </div>
          );
        },
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          const isActive = item.status === 1;
          return (
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => handleView(item.departmentId)}
              isLoading={loadingId === item.departmentId}
              menuItems={[
                {
                  label: isActive ? "Khóa phòng ban" : "Mở khóa",
                  icon: isActive ? (
                    <Lock className="w-4 h-4" />
                  ) : (
                    <Unlock className="w-4 h-4" />
                  ),
                  isDestructive: isActive,
                  onClick: () => setToggleStatusId(item.departmentId),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const DEPARTMENT_STATUS_OPTIONS = [
    { label: "Tất cả trạng thái", value: "All" },
    { label: "Đang hoạt động", value: "1" },
    { label: "Tạm khóa", value: "0" },
  ];

  const CustomFilters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={setFilterStatus}
        options={DEPARTMENT_STATUS_OPTIONS}
        placeholder="Trạng thái"
      />
      <DateRangeFilter dateRange={dateRange} onChange={setDateRange} />
    </>
  );

  const totalDepartments = departments.length;
  const activeCount = departments.filter((d) => d.status === 1).length;
  const inactiveCount = totalDepartments - activeCount;

  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng số phòng ban",
      value: totalDepartments,
      icon: <Building className="w-6 h-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Đang hoạt động",
      value: activeCount,
      icon: <CheckCircle2 className="w-6 h-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
    },
    {
      title: "Đang tạm khóa",
      value: inactiveCount,
      icon: <Lock className="w-6 h-6" />,
      iconWrapperClassName: "bg-rose-500/10 text-rose-600 dark:text-rose-500",
    },
  ];

  const currentToggleItem = departments.find(
    (d) => d.departmentId === toggleStatusId,
  );
  const isTogglingToLock = currentToggleItem?.status === 1;

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-6 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Danh sách Phòng ban
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Quản lý danh sách, cấu hình và trạng thái của các phòng ban trong
              hệ thống.
            </p>
          </div>
          <Button className="shadow-sm">
            <Building2 className="w-4 h-4 mr-2" /> Thêm phòng ban
          </Button>
        </div>

        <SummaryCards items={summaryItems} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={filteredData}
              isLoading={isLoading}
              searchKey="departmentName"
              searchPlaceholder="Tìm tên phòng ban..."
              filterSlot={CustomFilters}
              onSelectMany={(rows) =>
                toast.info(`Đang gọi API xử lý ${rows.length} dòng...`)
              }
              onRowClick={(row) => handleView(row.departmentId)}
              mobileCardRenderer={(
                row: Row<DepartmentResponse>,
                { isSelected },
              ) => {
                const item = row.original;
                const statusStr = item.status === 1 ? "Active" : "Inactive";

                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    <div className="flex items-start justify-between gap-3 mb-3">
                      <div className="flex flex-col min-w-0">
                        <span className="font-semibold text-foreground truncate">
                          {item.departmentName}
                        </span>
                        <span className="text-xs font-medium text-primary bg-primary/10 px-2 py-0.5 rounded-md w-fit mt-1.5">
                          {item.departmentCode}
                        </span>
                      </div>
                      <StatusBadge status={statusStr} />
                    </div>

                    <div className="flex items-center justify-between gap-3 pt-3 border-t border-border">
                      <div className="flex items-center gap-2 min-w-0 text-sm text-muted-foreground">
                        <CalendarDays className="w-3.5 h-3.5 shrink-0" />
                        <span className="truncate">
                          Cập nhật:{" "}
                          {item.modifiedDate
                            ? formatDateTime(item.modifiedDate)
                            : "N/A"}
                        </span>
                      </div>
                    </div>
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
          title={isTogglingToLock ? "Khóa phòng ban" : "Mở khóa phòng ban"}
          description={
            isTogglingToLock
              ? `Bạn có chắc chắn muốn khóa phòng ban "${currentToggleItem?.departmentName}"? Các nhân viên thuộc phòng ban này có thể bị ảnh hưởng.`
              : `Xác nhận mở khóa hoạt động cho phòng ban "${currentToggleItem?.departmentName}"?`
          }
          icon={
            isTogglingToLock ? (
              <Lock className="w-5 h-5" />
            ) : (
              <Unlock className="w-5 h-5" />
            )
          }
          confirmText={isTogglingToLock ? "Xác nhận khóa" : "Xác nhận mở"}
          variant={isTogglingToLock ? "destructive" : "default"}
          titleClassName={
            isTogglingToLock ? "text-destructive" : "text-primary"
          }
          isLoading={isToggling}
        />
      </div>
    </>
  );
}
