"use client";

import { AlertCircle, Wrench } from "lucide-react";

const MAINTENANCE_CONTRACTS = [
  {
    id: "HD-2024-055",
    customer: "Công ty Công nghệ Epsilon",
    value: "50.000.000",
    date: "01/06/2024",
    maintenanceFee: "7.500.000",
  },
  {
    id: "HD-2024-012",
    customer: "Tập đoàn Vận tải Sigma",
    value: "120.000.000",
    date: "15/05/2024",
    maintenanceFee: "18.000.000",
  },
];

export default function MaintenanceContractPage() {
  return (
    <div className="mx-auto max-w-7xl space-y-4 p-3 sm:space-y-6 sm:p-6">
      <div className="flex flex-col gap-4 rounded-xl border border-purple-100 bg-purple-50 p-4 sm:p-6 lg:flex-row lg:items-start lg:justify-between">
        <div className="min-w-0">
          <h1 className="flex items-start gap-2 text-xl font-bold text-purple-900 sm:items-center sm:text-2xl">
            <Wrench className="mt-0.5 size-6 shrink-0 sm:mt-0" />
            Quản lý Hợp đồng Bảo trì
          </h1>
          <p className="mt-2 max-w-2xl text-sm text-purple-700">
            Danh sách các hợp đồng gốc đã hết hạn bảo hành miễn phí (12 tháng).
            Hệ thống tự động phát sinh luồng Hợp đồng Bảo trì (phí 15-20%).
          </p>
        </div>
        <button className="w-full rounded-lg bg-purple-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-purple-700 sm:w-fit">
          Thiết lập tỷ lệ phí bảo trì
        </button>
      </div>

      <div className="space-y-3 md:hidden">
        {MAINTENANCE_CONTRACTS.map((contract) => (
          <article
            key={contract.id}
            className="rounded-xl border bg-white p-4 shadow-sm"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="min-w-0">
                <p className="font-semibold text-purple-700">{contract.id}</p>
                <p className="mt-1 break-words text-sm font-medium">
                  {contract.customer}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-1 text-xs font-medium text-red-600">
                <AlertCircle className="size-4" />
                {contract.date}
              </div>
            </div>
            <dl className="mt-4 grid grid-cols-2 gap-3 border-t pt-4 text-sm">
              <div>
                <dt className="text-xs text-muted-foreground">Trị giá phần mềm</dt>
                <dd className="mt-1 font-medium">{contract.value} VNĐ</dd>
              </div>
              <div>
                <dt className="text-xs text-muted-foreground">Phí bảo trì dự kiến</dt>
                <dd className="mt-1 font-bold">{contract.maintenanceFee} VNĐ</dd>
              </div>
            </dl>
            <button className="mt-4 w-full rounded-md border border-purple-200 px-3 py-2 text-sm font-semibold text-purple-700 hover:bg-purple-50">
              Khởi tạo HĐ Bảo trì
            </button>
          </article>
        ))}
      </div>

      <div className="hidden overflow-x-auto rounded-xl border bg-white shadow-sm md:block">
        <table className="w-full min-w-[900px] text-left text-sm">
          <thead className="bg-gray-50 text-gray-600">
            <tr>
              <th className="p-4 font-semibold">Mã HĐ Gốc</th>
              <th className="p-4 font-semibold">Khách hàng</th>
              <th className="p-4 font-semibold">Ngày hết hạn BH</th>
              <th className="p-4 font-semibold">Trị giá phần mềm</th>
              <th className="p-4 font-semibold">Phí bảo trì ước tính</th>
              <th className="p-4 text-right font-semibold">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {MAINTENANCE_CONTRACTS.map((contract) => (
              <tr key={contract.id} className="transition-colors hover:bg-gray-50">
                <td className="p-4 font-medium text-purple-700">{contract.id}</td>
                <td className="p-4">{contract.customer}</td>
                <td className="p-4">
                  <span className="flex items-center gap-1 font-medium text-red-600">
                    <AlertCircle className="size-4" /> {contract.date}
                  </span>
                </td>
                <td className="p-4 text-gray-500">{contract.value} VNĐ</td>
                <td className="p-4 font-bold text-gray-900">
                  {contract.maintenanceFee} VNĐ
                </td>
                <td className="p-4 text-right">
                  <button className="rounded border border-purple-200 px-3 py-1.5 text-xs font-semibold text-purple-700 hover:bg-purple-50">
                    Khởi tạo HĐ Bảo trì
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
