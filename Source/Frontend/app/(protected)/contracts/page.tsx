"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ColumnDef, PaginationState, Row } from "@tanstack/react-table";
import {
  CalendarDays,
  CheckCircle2,
  Clock,
  Eye,
  FileSignature,
  Link2,
  Plus,
  Users,
  WalletCards,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Checkbox } from "@/components/ui/checkbox";
import { Badge } from "@/components/ui/badge";
import { DataTable } from "@/components/ui/custom/data-table-server";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { formatCurrency } from "@/lib/format-currency";
import {
  contractApi,
  ContractFilterRequest,
  ContractListItemResponse,
  ContractStatus,
  ContractType,
  getContractStatusLabel,
  getContractTypeLabel,
  statusClasses,
} from "@/services/contract-api";

function ContractStatusBadge({ status }: { status: ContractStatus }) {
  return (
    <Badge variant="outline" className={statusClasses[status] || ""}>
      {getContractStatusLabel(status)}
    </Badge>
  );
}

function formatShortDate(value?: string | null) {
  if (!value) return "Chưa cập nhật";
  return new Date(value).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

const STATUS_FILTER_OPTIONS = [
  { value: "All", label: "Tất cả trạng thái" },
  { value: String(ContractStatus.Draft), label: getContractStatusLabel(ContractStatus.Draft) },
  { value: String(ContractStatus.Negotiating), label: getContractStatusLabel(ContractStatus.Negotiating) },
  { value: String(ContractStatus.PendingApproval), label: getContractStatusLabel(ContractStatus.PendingApproval) },
  { value: String(ContractStatus.PendingSignature), label: getContractStatusLabel(ContractStatus.PendingSignature) },
  { value: String(ContractStatus.Signed), label: getContractStatusLabel(ContractStatus.Signed) },
  { value: String(ContractStatus.Completed), label: getContractStatusLabel(ContractStatus.Completed) },
  { value: String(ContractStatus.Cancelled), label: getContractStatusLabel(ContractStatus.Cancelled) },
  { value: String(ContractStatus.Rejected), label: getContractStatusLabel(ContractStatus.Rejected) },
];

const CONTRACT_TYPE_FILTER_OPTIONS = [
  { value: "All", label: "Tất cả loại HĐ" },
  { value: String(ContractType.SoftwareSupply), label: getContractTypeLabel(ContractType.SoftwareSupply) },
  { value: String(ContractType.SoftwareMaintenance), label: getContractTypeLabel(ContractType.SoftwareMaintenance) },
  { value: String(ContractType.SoftwareUpkeep), label: getContractTypeLabel(ContractType.SoftwareUpkeep) },
];

export default function ContractListPage() {
  const router = useRouter();

  const [data, setData] = useState<ContractListItemResponse[]>([]);
  const [rowCount, setRowCount] = useState<number>(0);
  const [pageCount, setPageCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [loadingId, setLoadingId] = useState<number | null>(null);

  const [pagination, setPagination] = useState<PaginationState>({
    pageIndex: 0,
    pageSize: 10,
  });
  const [searchValue, setSearchValue] = useState<string>("");
  const [filterStatus, setFilterStatus] = useState<string>("All");
  const [filterContractType, setFilterContractType] = useState<string>("All");

  const fetchContracts = useCallback(async () => {
    setIsLoading(true);
    try {
      const params: ContractFilterRequest = {
        page: pagination.pageIndex + 1,
        pageSize: pagination.pageSize,
        keyword: searchValue || undefined,
        status: filterStatus !== "All" ? (Number(filterStatus) as ContractStatus) : undefined,
        contractType: filterContractType !== "All" ? (Number(filterContractType) as ContractType) : undefined,
      };
      const res = await contractApi.getList(params);
      if (res) {
        setData(res.items || []);
        setRowCount(res.totalCount || 0);
        setPageCount(res.totalPages || Math.ceil((res.totalCount || 0) / pagination.pageSize));
      }
    } catch (error) {
      console.error("Lỗi khi tải danh sách hợp đồng:", error);
      setData([]);
      setRowCount(0);
      setPageCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [pagination.pageIndex, pagination.pageSize, searchValue, filterStatus, filterContractType]);

  useEffect(() => {
    fetchContracts();
  }, [fetchContracts]);

  const handleView = (id: number) => {
    setLoadingId(id);
    router.push(`/contracts/${id}`);
  };

  const columns = useMemo<ColumnDef<ContractListItemResponse>[]>(
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
            onClick={(event) => event.stopPropagation()}
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
        accessorKey: "contractCode",
        header: "Mã hợp đồng",
        cell: ({ row }) => (
          <div className="flex flex-col pl-1">
            <span className="font-semibold text-foreground">
              {row.original.contractCode || "Chưa cấp mã"}
            </span>
            <span className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <CalendarDays className="h-3 w-3" />
              {formatShortDate(row.original.createdDate)}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "contractName",
        header: "Thông tin hợp đồng",
        cell: ({ row }) => (
          <div className="max-w-[320px]">
            <p className="truncate font-medium text-foreground">
              {row.original.contractName}
            </p>
            <p className="mt-1 truncate text-xs text-muted-foreground">
              {getContractTypeLabel(row.original.contractType)}
            </p>
          </div>
        ),
      },
      {
        accessorKey: "customerName",
        header: "Khách hàng",
        cell: ({ row }) => (
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-secondary">
              <Users className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">
                {row.original.customerName || row.original.customerCompany || "Chưa cập nhật"}
              </p>
              {row.original.customerCompany && (
                <p className="truncate text-xs text-muted-foreground">
                  {row.original.customerCompany}
                </p>
              )}
            </div>
          </div>
        ),
      },
      {
        accessorKey: "totalAmount",
        header: () => <div className="text-right">Giá trị</div>,
        cell: ({ row }) => (
          <div className="text-right font-semibold text-primary">
            {formatCurrency(row.original.totalAmount)}
          </div>
        ),
      },
      {
        accessorKey: "status",
        header: () => <div className="text-center">Trạng thái</div>,
        cell: ({ row }) => (
          <div className="flex justify-center">
            <ContractStatusBadge status={row.original.status} />
          </div>
        ),
      },
      {
        id: "action",
        header: () => <div className="text-right pr-4">Thao tác</div>,
        cell: ({ row }) => {
          const item = row.original;
          return (
            <SplitActionMenu
              primaryLabel="Chi tiết"
              primaryIcon={<Eye className="h-4 w-4" />}
              onPrimaryClick={() => handleView(item.contractId)}
              isLoading={loadingId === item.contractId}
              menuItems={[
                {
                  label: "Sao chép mã hợp đồng",
                  icon: <Link2 className="h-4 w-4" />,
                  onClick: () =>
                    navigator.clipboard?.writeText(item.contractCode || ""),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const totalValue = useMemo(
    () => data.reduce((sum, contract) => sum + (contract.totalAmount || 0), 0),
    [data],
  );

  const signedCount = useMemo(
    () =>
      data.filter((c) =>
        [ContractStatus.Signed, ContractStatus.Completed].includes(c.status),
      ).length,
    [data],
  );

  const negotiatingCount = useMemo(
    () =>
      data.filter((c) => c.status === ContractStatus.Negotiating).length,
    [data],
  );

  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng hợp đồng",
      value: rowCount,
      icon: <FileSignature className="h-6 w-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Đã ký kết",
      value: signedCount,
      icon: <CheckCircle2 className="h-6 w-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
    },
    {
      title: "Đang đàm phán",
      value: negotiatingCount,
      icon: <Clock className="h-6 w-6" />,
      iconWrapperClassName:
        "bg-amber-500/10 text-amber-600 dark:text-amber-500",
    },
    {
      title: "Tổng giá trị (trang)",
      value: formatCurrency(totalValue),
      icon: <WalletCards className="h-6 w-6" />,
      iconWrapperClassName: "bg-blue-500/10 text-blue-600 dark:text-blue-500",
      valueClassName: "text-xl",
    },
  ];

  const filters = (
    <>
      <SelectFilter
        value={filterStatus}
        onChange={(val) => {
          setFilterStatus(val);
          setPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }}
        options={STATUS_FILTER_OPTIONS}
        placeholder="Trạng thái"
      />
      <SelectFilter
        value={filterContractType}
        onChange={(val) => {
          setFilterContractType(val);
          setPagination((prev) => ({ ...prev, pageIndex: 0 }));
        }}
        options={CONTRACT_TYPE_FILTER_OPTIONS}
        placeholder="Loại hợp đồng"
      />
    </>
  );

  return (
    <>
      <Header />

      <div className="grow overflow-y-auto p-2 lg:p-10 space-y-6">
        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Danh sách Hợp đồng
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Quản lý và theo dõi vòng đời hợp đồng: nháp, đàm phán, trình duyệt, ký và hoàn thành.
            </p>
          </div>
          <Button
            className="shadow-sm"
            onClick={() => router.push("/contracts/create")}
          >
            <Plus className="mr-2 h-4 w-4" /> Tạo hợp đồng nháp
          </Button>
        </div>

        <SummaryCards items={summaryItems} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={data}
              pageCount={pageCount}
              rowCount={rowCount}
              pagination={pagination}
              onPaginationChange={setPagination}
              searchValue={searchValue}
              onSearchChange={(val) => {
                setSearchValue(val);
                setPagination((prev) => ({ ...prev, pageIndex: 0 }));
              }}
              searchPlaceholder="Tìm mã hoặc tên hợp đồng..."
              filterSlot={filters}
              isLoading={isLoading}
              onRowClick={(row) => handleView(row.contractId)}
              mobileCardRenderer={(row: Row<ContractListItemResponse>, { isSelected, actionCell }) => {
                const item = row.original;
                return (
                  <div
                    className={`rounded-xl border bg-card p-4 shadow-sm transition-colors active:bg-secondary/40 ${
                      isSelected
                        ? "border-primary/40 bg-primary/5"
                        : "border-border"
                    }`}
                  >
                    <div className="mb-3 flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="font-semibold text-foreground">
                          {item.contractCode || "Chưa cấp mã"}
                        </p>
                        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                          {item.contractName}
                        </p>
                      </div>
                      <ContractStatusBadge status={item.status} />
                    </div>

                    <div className="grid grid-cols-2 gap-2 rounded-lg border bg-muted/40 p-2.5 text-sm">
                      <div>
                        <p className="text-xs text-muted-foreground">
                          Khách hàng
                        </p>
                        <p className="truncate font-medium">
                          {item.customerName || item.customerCompany || "Chưa cập nhật"}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">Giá trị</p>
                        <p className="truncate font-semibold text-primary">
                          {formatCurrency(item.totalAmount)}
                        </p>
                      </div>
                    </div>
                    {actionCell}
                  </div>
                );
              }}
            />
          </CardContent>
        </Card>
      </div>
    </>
  );
}
