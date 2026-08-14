"use client";

import { useState, useEffect, useMemo, useCallback } from "react";
import { Header } from "@/components/ui/custom/header";
import {
  Users,
  Shield,
  Phone,
  Mail,
  UserCog,
  Lock,
  Unlock,
  Eye,
  CheckCircle2,
  UserPlus,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { toast } from "@/components/ui/sonner";
import { ColumnDef, PaginationState, Row } from "@tanstack/react-table";

import {
  employeeApi,
  EmployeeResponse,
  EmployeeType,
  EmployeeStatus,
  getEmployeeStatusLabel,
  getEmployeeTypeLabel,
} from "@/services/employees-api";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { StatusBadge } from "@/components/ui/custom/status-badge";
import { ConfirmDialog } from "@/components/ui/custom/confirm-dialog";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import {
  DateRangeFilter,
  DateRange,
} from "@/components/ui/custom/date-range-filter";
import { format } from "date-fns";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { PageHeaderSkeleton } from "@/components/ui/custom/table-skeleton";
import { EmployeeFormModal } from "./employee-form-modal";
import { getApiErrorMessage, isStaleRowVersion } from "@/lib/api-error";
import { PermissionGuard } from "@/components/auth/permission-guard";
import { RBAC_PERMISSIONS } from "@/lib/rbac";

function EmployeeBulkActions({
  selectedRows,
  resetSelection,
}: {
  selectedRows: EmployeeResponse[];
  resetSelection: () => void;
}) {
  const [isProcessing, setIsProcessing] = useState(false);

  const handleBulkLock = async () => {
    setIsProcessing(true);
    try {
      toast.info(
        `Đang xử lý khóa ${selectedRows.length} tài khoản nhân viên...`,
      );
      resetSelection();
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <SplitActionMenu
      primaryLabel={`Khóa tài khoản (${selectedRows.length})`}
      primaryIcon={<Lock className="w-4 h-4" />}
      onPrimaryClick={handleBulkLock}
      isLoading={isProcessing}
      variant="default"
      buttonClassName="h-9"
      menuItems={[]}
    />
  );
}

function EmployeeListPageContent() {
  const [employees, setEmployees] = useState<EmployeeResponse[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
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
  const [editingEmployee, setEditingEmployee] =
    useState<EmployeeResponse | null>(null);
  const [viewingEmployee, setViewingEmployee] =
    useState<EmployeeResponse | null>(null);
  const fetchEmployees = useCallback(async () => {
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

      const res = await employeeApi.getList({
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchTerm || undefined,
        status: statusParam,
        fromDate: fromDateParam,
        toDate: toDateParam,
      });

      setEmployees(res.items || []);
      setTotalCount(res.totalCount || 0);
    } catch (error) {
      toast.error("Lỗi khi tải danh sách nhân viên");
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
    fetchEmployees();
  }, [fetchEmployees]);

  const handleView = (employee: EmployeeResponse) => {
    setViewingEmployee(employee);
  };

  const handleToggleStatusConfirm = async () => {
    if (!toggleStatusId) return;
    setIsToggling(true);
    try {
      const targetEmp = employees.find((e) => e.employeeId === toggleStatusId);
      const newStatus =
        targetEmp?.status === EmployeeStatus.Active
          ? EmployeeStatus.Inactive
          : EmployeeStatus.Active;

      if (!targetEmp?.rowVersion) {
        toast.error("Không có rowVersion mới nhất của nhân viên.");
        return;
      }

      await employeeApi.setStatus(toggleStatusId, {
        status: newStatus,
        rowVersion: targetEmp.rowVersion,
      });
      toast.success("Cập nhật trạng thái nhân viên thành công");
      await fetchEmployees();
      setToggleStatusId(null);
    } catch (error) {
      if (isStaleRowVersion(error)) {
        toast.error("Dữ liệu nhân viên đã thay đổi. Danh sách đã được tải lại.");
        await fetchEmployees();
        setToggleStatusId(null);
      } else {
        toast.error(getApiErrorMessage(error, "Không thể cập nhật trạng thái"));
      }
    } finally {
      setIsToggling(false);
    }
  };

  const columns = useMemo<ColumnDef<EmployeeResponse>[]>(
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
        accessorKey: "employeeFullName",
        header: "Nhân viên",
        cell: ({ row }) => (
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-full bg-primary/10 flex items-center justify-center shrink-0">
              <UserCog className="w-4.5 h-4.5 text-primary" />
            </div>
            <div className="flex flex-col min-w-0">
              <span className="font-semibold text-foreground text-sm truncate">
                {row.original.employeeFullName}
              </span>
              <span className="text-xs text-muted-foreground truncate flex items-center gap-1 mt-0.5">
                <Shield className="w-3 h-3" />
                {row.original.employeeCode || "N/A"}
              </span>
            </div>
          </div>
        ),
      },
      {
        accessorKey: "contact",
        header: "Liên hệ",
        cell: ({ row }) => (
          <div className="flex flex-col gap-1 text-sm text-muted-foreground">
            {row.original.employeeMobile && (
              <div className="flex items-center gap-1.5">
                <Phone className="w-3.5 h-3.5" />
                {row.original.employeeMobile}
              </div>
            )}
            {row.original.employeeEmail && (
              <div className="flex items-center gap-1.5 truncate">
                <Mail className="w-3.5 h-3.5" />
                {row.original.employeeEmail}
              </div>
            )}
          </div>
        ),
      },
      {
        accessorKey: "employeeType",
        header: "Vai trò",
        cell: ({ row }) => (
          <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-secondary text-secondary-foreground">
            {getEmployeeTypeLabel(row.original.employeeType)}
          </span>
        ),
      },
      {
        accessorKey: "dateModified",
        header: () => <div className="text-center">Ngày chỉnh sửa</div>,
        cell: ({ row }) => (
          <div className="text-center text-sm text-muted-foreground">
            {row.original.dateModified
              ? format(new Date(row.original.dateModified), "dd/MM/yyyy")
              : "N/A"}
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => {
          const statusStr = getEmployeeStatusLabel(row.original.status);
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
          const isActive = item.status === EmployeeStatus.Active;
          const canMutate = item.employeeType !== EmployeeType.Manager;
          return (
            <SplitActionMenu
              primaryLabel="Hồ sơ"
              primaryIcon={<Eye className="w-4 h-4" />}
              onPrimaryClick={() => handleView(item)}
              isLoading={loadingId === item.employeeId}
              menuItems={
                canMutate
                  ? [
                      {
                        label: "Chỉnh sửa",
                        icon: <UserCog className="w-4 h-4" />,
                        onClick: () => {
                          setEditingEmployee(item);
                          setIsFormOpen(true);
                        },
                      },
                      {
                        label: isActive ? "Khóa tài khoản" : "Mở khóa",
                        icon: isActive ? (
                          <Lock className="w-4 h-4" />
                        ) : (
                          <Unlock className="w-4 h-4" />
                        ),
                        isDestructive: isActive,
                        onClick: () => setToggleStatusId(item.employeeId),
                      },
                    ]
                  : []
              }
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const activeCount = employees.filter(
    (e) => e.status === EmployeeStatus.Active,
  ).length;
  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng nhân sự",
      value: totalCount,
      icon: <Users className="w-6 h-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Nhân sự Quản lý",
      value: employees.filter((e) => e.employeeType === EmployeeType.Manager)
        .length,
      icon: <Shield className="w-6 h-6" />,
      iconWrapperClassName:
        "bg-amber-500/10 text-amber-600 dark:text-amber-500",
    },
    {
      title: "Hoạt động (Trang hiện tại)",
      value: activeCount,
      icon: <CheckCircle2 className="w-6 h-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
    },
  ];

  const EMPLOYEE_STATUS_OPTIONS = [
    { label: "Tất cả trạng thái", value: "All" },
    { label: "Đang hoạt động", value: String(EmployeeStatus.Active) },
    { label: "Tạm khóa", value: String(EmployeeStatus.Inactive) },
  ];

  const CustomFilters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={(val) => {
          setFilterStatus(val);
          setPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }}
        options={EMPLOYEE_STATUS_OPTIONS}
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

  const currentToggleItem = employees.find(
    (e) => e.employeeId === toggleStatusId,
  );
  const isTogglingToLock = currentToggleItem?.status === EmployeeStatus.Active;

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Danh sách Nhân sự
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              Quản lý tài khoản, thông tin liên hệ và phân quyền vai trò cho
              nhân viên.
            </p>
          </div>
          <Button
            className="shadow-sm"
            onClick={() => {
              setEditingEmployee(null);
              setIsFormOpen(true);
            }}
          >
            <UserPlus className="w-4 h-4 mr-2" /> Thêm nhân viên
          </Button>
        </div>

        <SummaryCards items={summaryItems} isLoading={isLoading} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={employees}
              isLoading={isLoading}
              pageCount={Math.ceil(totalCount / pagination.pageSize)}
              rowCount={totalCount}
              pagination={pagination}
              onPaginationChange={setPagination}
              searchValue={searchTerm}
              onSearchChange={(val) => {
                setSearchTerm(val);
                setPagination((prev) => ({ ...prev, pageIndex: 0 }));
              }}
              searchPlaceholder="Tìm tên, mã, số điện thoại..."
              filterSlot={CustomFilters}
              onRowClick={(row) => handleView(row)}
              bulkActions={(selectedRows, resetSelection) => (
                <EmployeeBulkActions
                  selectedRows={selectedRows}
                  resetSelection={resetSelection}
                />
              )}
              mobileCardRenderer={(
                row: Row<EmployeeResponse>,
                { isSelected, actionCell },
              ) => {
                const item = row.original;
                const statusStr = getEmployeeStatusLabel(item.status);

                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    {/* Row 1: Tên + Role + Status */}
                    <div className="flex items-start justify-between gap-3 mb-3">
                      <div className="flex flex-col min-w-0">
                        <span className="font-semibold text-foreground truncate">
                          {item.employeeFullName}
                        </span>
                        <div className="flex items-center gap-2 mt-1.5">
                          <span className="text-xs font-medium text-primary bg-primary/10 px-2 py-0.5 rounded-md w-fit">
                            {item.employeeCode || "No Code"}
                          </span>
                          <span className="text-xs text-muted-foreground">
                            • {getEmployeeTypeLabel(item.employeeType)}
                          </span>
                        </div>
                      </div>
                      <StatusBadge status={statusStr} />
                    </div>

                    {/* Row 2: Liên hệ */}
                    <div className="flex flex-col gap-2 pt-3 border-t border-border text-sm text-muted-foreground">
                      {item.employeeMobile && (
                        <div className="flex items-center gap-2 min-w-0">
                          <Phone className="w-3.5 h-3.5 shrink-0" />
                          <span className="truncate">
                            {item.employeeMobile}
                          </span>
                        </div>
                      )}
                      {item.employeeEmail && (
                        <div className="flex items-center gap-2 min-w-0">
                          <Mail className="w-3.5 h-3.5 shrink-0" />
                          <span className="truncate">{item.employeeEmail}</span>
                        </div>
                      )}
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
          title={isTogglingToLock ? "Khóa tài khoản" : "Mở khóa tài khoản"}
          description={
            isTogglingToLock
              ? `Bạn có chắc chắn muốn khóa tài khoản của "${currentToggleItem?.employeeFullName}"? Người này sẽ không thể đăng nhập vào hệ thống nữa.`
              : `Xác nhận mở khóa để cấp lại quyền truy cập cho "${currentToggleItem?.employeeFullName}"?`
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

        <EmployeeFormModal
          isOpen={isFormOpen}
          onClose={() => {
            setIsFormOpen(false);
            setEditingEmployee(null);
          }}
          onSuccess={fetchEmployees}
          employee={editingEmployee}
        />

        <EmployeeFormModal
          isOpen={!!viewingEmployee}
          onClose={() => setViewingEmployee(null)}
          onSuccess={() => {}}
          employee={viewingEmployee}
          viewOnly
        />
      </div>
    </>
  );
}

export default function EmployeeListPage() {
  return (
    <PermissionGuard permission={RBAC_PERMISSIONS.employeeManage}>
      <EmployeeListPageContent />
    </PermissionGuard>
  );
}
