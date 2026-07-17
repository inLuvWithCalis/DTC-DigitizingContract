"use client";

import { useState } from "react";
import { Plus, Search, Eye } from "lucide-react";

// Tích hợp Fake Data trực tiếp vào file
const CONTRACT_STATUSES = [
  {
    value: "negotiating",
    label: "Đang đàm phán",
    color: "bg-blue-100 text-blue-700",
  },
  { value: "signing", label: "Đang ký", color: "bg-amber-100 text-amber-700" },
  {
    value: "hardcopy_pending",
    label: "Bản cứng chưa về",
    color: "bg-orange-100 text-orange-700",
  },
  {
    value: "closed",
    label: "Hoàn tất (Closed)",
    color: "bg-emerald-100 text-emerald-700",
  },
  {
    value: "maintenance",
    label: "Bảo trì",
    color: "bg-purple-100 text-purple-700",
  },
];

const MOCK_CONTRACTS = [
  {
    id: "HD-2026-001",
    customer: "Công ty Cổ phần Alpha",
    value: "150,000,000",
    status: "negotiating",
    date: "15/07/2026",
  },
  {
    id: "HD-2026-002",
    customer: "Tập đoàn Xây dựng Beta",
    value: "320,000,000",
    status: "signing",
    date: "10/07/2026",
  },
  {
    id: "HD-2026-003",
    customer: "Công ty TNHH Gamma",
    value: "85,000,000",
    status: "hardcopy_pending",
    date: "05/07/2026",
  },
  {
    id: "HD-2025-102",
    customer: "Hệ thống Bán lẻ Delta",
    value: "450,000,000",
    status: "closed",
    date: "12/12/2025",
  },
  {
    id: "HD-2024-055",
    customer: "Công ty Công nghệ Epsilon",
    value: "50,000,000",
    status: "maintenance",
    date: "01/06/2024",
  },
];

export default function ContractListPage() {
  const [filter, setFilter] = useState("all");

  const filteredContracts =
    filter === "all"
      ? MOCK_CONTRACTS
      : MOCK_CONTRACTS.filter((c) => c.status === filter);

  return (
    <div className="p-6 space-y-6 max-w-7xl mx-auto">
      <div className="flex justify-between items-center">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Quản lý Hợp đồng</h1>
          <p className="text-sm text-gray-500">
            Theo dõi toàn bộ vòng đời hợp đồng của khách hàng.
          </p>
        </div>
        <button className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center gap-2 font-medium">
          <Plus className="w-4 h-4" /> Khởi tạo Hợp đồng
        </button>
      </div>

      <div className="bg-white border rounded-xl shadow-sm overflow-hidden">
        <div className="p-4 border-b flex gap-4 bg-gray-50/50">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-gray-400" />
            <input
              type="text"
              placeholder="Tìm kiếm mã HĐ, tên khách hàng..."
              className="w-full pl-9 pr-4 py-2 border rounded-lg focus:ring-2 focus:ring-blue-500 outline-none"
            />
          </div>
          <select
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
            className="border rounded-lg px-4 py-2 bg-white outline-none focus:ring-2 focus:ring-blue-500"
          >
            <option value="all">Tất cả trạng thái</option>
            {CONTRACT_STATUSES.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>

        <table className="w-full text-left text-sm">
          <thead className="bg-gray-50 text-gray-600">
            <tr>
              <th className="p-4 font-semibold">Mã Hợp đồng</th>
              <th className="p-4 font-semibold">Khách hàng</th>
              <th className="p-4 font-semibold">Giá trị (VNĐ)</th>
              <th className="p-4 font-semibold">Ngày tạo</th>
              <th className="p-4 font-semibold">Trạng thái</th>
              <th className="p-4 font-semibold text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y">
            {filteredContracts.map((contract) => {
              const statusObj = CONTRACT_STATUSES.find(
                (s) => s.value === contract.status,
              );
              return (
                <tr
                  key={contract.id}
                  className="hover:bg-gray-50/50 transition-colors"
                >
                  <td className="p-4 font-medium text-gray-900">
                    {contract.id}
                  </td>
                  <td className="p-4">{contract.customer}</td>
                  <td className="p-4 font-medium">{contract.value}</td>
                  <td className="p-4 text-gray-500">{contract.date}</td>
                  <td className="p-4">
                    <span
                      className={`px-2.5 py-1 rounded-full text-xs font-medium ${statusObj?.color}`}
                    >
                      {statusObj?.label}
                    </span>
                  </td>
                  <td className="p-4 text-right">
                    <button className="text-gray-400 hover:text-blue-600 p-1">
                      <Eye className="w-5 h-5" />
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}
