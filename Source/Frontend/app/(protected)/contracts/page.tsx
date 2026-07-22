"use client";

import { useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { ColumnDef, Row } from "@tanstack/react-table";
import {
  CalendarDays,
  CheckCircle2,
  Clock,
  Eye,
  FileSignature,
  FileText,
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
import { Progress } from "@/components/ui/progress";
import { DataTable } from "@/components/ui/custom/data-table";
import { SelectFilter } from "@/components/ui/custom/select-filter";
import { SplitActionMenu } from "@/components/ui/custom/split-action-menu";
import {
  SummaryCardItem,
  SummaryCards,
} from "@/components/ui/custom/summary-cards";
import { formatCurrency } from "@/lib/format-currency";
import {
  CONTRACT_STATUS_LABELS,
  CONTRACT_STATUS_OPTIONS,
  CONTRACT_TYPE_LABELS,
  ContractMock,
  ContractStatus,
  mockContracts,
} from "@/services/contracts-mock";

const statusClasses: Record<ContractStatus, string> = {
  Draft:
    "bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-500/10 dark:text-amber-400 dark:border-amber-500/20",
  Negotiating:
    "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-500/10 dark:text-blue-400 dark:border-blue-500/20",
  Approved:
    "bg-indigo-50 text-indigo-700 border-indigo-200 dark:bg-indigo-500/10 dark:text-indigo-400 dark:border-indigo-500/20",
  Signed:
    "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-500/10 dark:text-emerald-400 dark:border-emerald-500/20",
  Closing:
    "bg-orange-50 text-orange-700 border-orange-200 dark:bg-orange-500/10 dark:text-orange-400 dark:border-orange-500/20",
  Closed:
    "bg-slate-50 text-slate-700 border-slate-200 dark:bg-slate-500/10 dark:text-slate-400 dark:border-slate-500/20",
};

function ContractStatusBadge({ status }: { status: ContractStatus }) {
  return (
    <Badge variant="outline" className={statusClasses[status]}>
      {CONTRACT_STATUS_LABELS[status]}
    </Badge>
  );
}

function formatShortDate(value: string) {
  return new Date(value).toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

export default function ContractListPage() {
  const router = useRouter();
  const [filterStatus, setFilterStatus] = useState("All");
  const [loadingId, setLoadingId] = useState<number | null>(null);

  const filteredContracts = useMemo(() => {
    if (filterStatus === "All") return mockContracts;
    return mockContracts.filter((contract) => contract.status === filterStatus);
  }, [filterStatus]);

  const handleView = (id: number) => {
    setLoadingId(id);
    router.push(`/contracts/${id}`);
  };

  const columns = useMemo<ColumnDef<ContractMock>[]>(
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
        accessorKey: "contractNo",
        header: "Mã hợp đồng",
        cell: ({ row }) => (
          <div className="flex flex-col pl-1">
            <span className="font-semibold text-foreground">
              {row.original.contractNo}
            </span>
            <span className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
              <CalendarDays className="h-3 w-3" />
              {formatShortDate(row.original.createdAt)}
            </span>
          </div>
        ),
      },
      {
        accessorKey: "title",
        header: "Thông tin hợp đồng",
        cell: ({ row }) => (
          <div className="max-w-[320px]">
            <p className="truncate font-medium text-foreground">
              {row.original.title}
            </p>
            <p className="mt-1 truncate text-xs text-muted-foreground">
              {CONTRACT_TYPE_LABELS[row.original.type]}
            </p>
          </div>
        ),
      },
      {
        accessorKey: "customerCompany",
        header: "Khách hàng",
        cell: ({ row }) => (
          <div className="flex items-center gap-2">
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-secondary">
              <Users className="h-4 w-4 text-muted-foreground" />
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium">
                {row.original.customerName}
              </p>
              <p className="truncate text-xs text-muted-foreground">
                {row.original.customerCompany}
              </p>
            </div>
          </div>
        ),
      },
      {
        accessorKey: "value",
        header: () => <div className="text-right">Giá trị</div>,
        cell: ({ row }) => (
          <div className="text-right font-semibold text-primary">
            {formatCurrency(row.original.value)}
          </div>
        ),
      },
      {
        accessorKey: "paymentProgress",
        header: "Thanh toán",
        cell: ({ row }) => (
          <div className="min-w-[130px]">
            <div className="mb-1 flex items-center justify-between text-xs">
              <span className="text-muted-foreground">Tiến độ</span>
              <span className="font-medium">
                {row.original.paymentProgress}%
              </span>
            </div>
            <Progress value={row.original.paymentProgress} className="h-2" />
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
              onPrimaryClick={() => handleView(item.id)}
              isLoading={loadingId === item.id}
              menuItems={[
                {
                  label: "Copy link khách xem",
                  icon: <Link2 className="h-4 w-4" />,
                  onClick: () => navigator.clipboard?.writeText(item.publicLink),
                },
              ]}
            />
          );
        },
      },
    ],
    [loadingId],
  );

  const totalValue = mockContracts.reduce(
    (sum, contract) => sum + contract.value,
    0,
  );
  const closingCount = mockContracts.filter(
    (contract) => contract.status === "Closing",
  ).length;
  const signedCount = mockContracts.filter((contract) =>
    ["Signed", "Closing", "Closed"].includes(contract.status),
  ).length;

  const summaryItems: SummaryCardItem[] = [
    {
      title: "Tổng hợp đồng",
      value: mockContracts.length,
      icon: <FileSignature className="h-6 w-6" />,
      iconWrapperClassName: "bg-primary/10 text-primary",
    },
    {
      title: "Đã ký điện tử",
      value: signedCount,
      icon: <CheckCircle2 className="h-6 w-6" />,
      iconWrapperClassName:
        "bg-emerald-500/10 text-emerald-600 dark:text-emerald-500",
    },
    {
      title: "Cần hoàn thiện hồ sơ",
      value: closingCount,
      icon: <Clock className="h-6 w-6" />,
      iconWrapperClassName:
        "bg-amber-500/10 text-amber-600 dark:text-amber-500",
    },
    {
      title: "Tổng giá trị",
      value: formatCurrency(totalValue),
      icon: <WalletCards className="h-6 w-6" />,
      iconWrapperClassName:
        "bg-blue-500/10 text-blue-600 dark:text-blue-500",
      valueClassName: "text-xl",
    },
  ];

  const filters = (
    <SelectFilter
      value={filterStatus}
      onChange={setFilterStatus}
      options={CONTRACT_STATUS_OPTIONS}
      placeholder="Trạng thái"
    />
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
              Mock UI theo dõi vòng đời hợp đồng: nháp, đàm phán, ký điện tử và
              hoàn thiện hồ sơ.
            </p>
          </div>
          <Button className="shadow-sm" onClick={() => router.push("/contracts/create")}>
            <Plus className="mr-2 h-4 w-4" /> Tạo hợp đồng nháp
          </Button>
        </div>

        <SummaryCards items={summaryItems} />

        <Card className="border-border shadow-sm bg-card min-h-[500px] flex flex-col gap-0 p-0">
          <CardContent className="p-4 flex flex-col justify-between flex-1 pb-0">
            <DataTable
              columns={columns}
              data={filteredContracts}
              searchKey="contractNo"
              searchPlaceholder="Tìm mã hợp đồng..."
              filterSlot={filters}
              onRowClick={(row) => handleView(row.id)}
              mobileCardRenderer={(
                row: Row<ContractMock>,
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
                    <div className="mb-3 flex items-start justify-between gap-3">
                      <div className="min-w-0">
                        <p className="font-semibold text-foreground">
                          {item.contractNo}
                        </p>
                        <p className="mt-1 line-clamp-2 text-sm text-muted-foreground">
                          {item.title}
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
                          {item.customerCompany}
                        </p>
                      </div>
                      <div>
                        <p className="text-xs text-muted-foreground">Giá trị</p>
                        <p className="truncate font-semibold text-primary">
                          {formatCurrency(item.value)}
                        </p>
                      </div>
                    </div>

                    <div className="mt-3">
                      <div className="mb-1 flex justify-between text-xs">
                        <span className="text-muted-foreground">
                          Thanh toán
                        </span>
                        <span className="font-medium">
                          {item.paymentProgress}%
                        </span>
                      </div>
                      <Progress value={item.paymentProgress} className="h-2" />
                    </div>
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
