"use client";

import { Wrench, AlertCircle } from "lucide-react";

// Tích hợp Fake Data chuyên cho Hợp đồng bảo trì
const MAINTENANCE_CONTRACTS = [
  {
    id: "HD-2024-055",
    customer: "Công ty Công nghệ Epsilon",
    value: "50,000,000",
    date: "01/06/2024",
    maintenanceFee: "7,500,000",
  },
  {
    id: "HD-2024-012",
    customer: "Tập đoàn Vận tải Sigma",
    value: "120,000,000",
    date: "15/05/2024",
    maintenanceFee: "18,000,000",
  },
];

export default function MaintenanceContractPage() {
  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      <div className="flex items-start justify-between bg-purple-50 p-6 rounded-xl border border-purple-100">
        <div>
          <h1 className="text-2xl font-bold text-purple-900 flex items-center gap-2">
            <Wrench className="w-6 h-6" /> Quản lý Hợp đồng Bảo trì
          </h1>
          <p className="text-sm text-purple-700 mt-2 max-w-2xl">
            Danh sách các hợp đồng gốc đã hết hạn bảo hành miễn phí (12 tháng).
            Hệ thống tự động phát sinh luồng Hợp đồng Bảo trì (Phí 15-20%).
          </p>
        </div>
        <button className="bg-purple-600 hover:bg-purple-700 text-white px-4 py-2 rounded-lg text-sm font-medium shadow-sm">
          Thiết lập Tỷ lệ phí Bảo trì
        </button>
      </div>

      <div className="bg-white border rounded-xl shadow-sm overflow-hidden">
        <table className="w-full text-left text-sm">
          <thead className="bg-gray-50 text-gray-600">
            <tr>
              <th className="p-4 font-semibold">Mã HĐ Gốc</th>
              <th className="p-4 font-semibold">Khách hàng</th>
              <th className="p-4 font-semibold">Ngày hết hạn BH</th>
              <th className="p-4 font-semibold">Trị giá phần mềm</th>
              <th className="p-4 font-semibold">Phí bảo trì ước tính</th>
              <th className="p-4 font-semibold text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {MAINTENANCE_CONTRACTS.map((contract) => (
              <tr
                key={contract.id}
                className="hover:bg-gray-50 transition-colors"
              >
                <td className="p-4 font-medium text-purple-700">
                  {contract.id}
                </td>
                <td className="p-4">{contract.customer}</td>
                <td className="p-4 text-red-600 font-medium flex items-center gap-1">
                  <AlertCircle className="w-4 h-4" /> {contract.date}
                </td>
                <td className="p-4 text-gray-500">{contract.value} VNĐ</td>
                <td className="p-4 font-bold text-gray-900">
                  {contract.maintenanceFee} VNĐ
                </td>
                <td className="p-4 text-right">
                  <button className="bg-white border border-purple-200 text-purple-700 px-3 py-1.5 rounded hover:bg-purple-50 text-xs font-semibold">
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
