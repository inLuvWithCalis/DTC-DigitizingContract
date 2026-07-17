"use client";

import { Save, Link as LinkIcon, FileText, CheckCircle2 } from "lucide-react";

export default function CreateContractPage() {
  const standardTerms = [
    "Điều khoản thanh toán (50% tạm ứng, 50% nghiệm thu)",
    "Nghĩa vụ bảo mật thông tin (NDA cơ bản)",
    "Chính sách bảo hành, bảo trì 12 tháng miễn phí",
    "Điều khoản bồi thường thiệt hại (Không vượt quá 100% GTHĐ)",
  ];

  return (
    <div className="p-6 max-w-4xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">
          Khởi tạo Hợp đồng (Draft)
        </h1>
        <p className="text-sm text-gray-500">
          Tạo bản nháp từ template và chọn các điều khoản chuẩn.
        </p>
      </div>

      <div className="bg-white border rounded-xl shadow-sm p-6 space-y-8">
        {/* Thông tin chung */}
        <section className="space-y-4">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <FileText className="w-5 h-5 text-blue-600" /> Thông tin chung
          </h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium mb-1">
                Khách hàng (Bên B)
              </label>
              <input
                type="text"
                className="w-full border rounded-lg p-2.5"
                placeholder="Tên công ty khách hàng..."
              />
            </div>
            <div>
              <label className="block text-sm font-medium mb-1">
                Mã số thuế
              </label>
              <input
                type="text"
                className="w-full border rounded-lg p-2.5"
                placeholder="MST..."
              />
            </div>
            <div className="col-span-2">
              <label className="block text-sm font-medium mb-1">
                Template Hợp đồng
              </label>
              <select className="w-full border rounded-lg p-2.5 bg-white">
                <option>Hợp đồng Cung cấp Phần mềm (Chuẩn)</option>
                <option>Hợp đồng Triển khai & Tích hợp Hệ thống</option>
              </select>
            </div>
          </div>
        </section>

        <hr />

        {/* Điều khoản chuẩn */}
        <section className="space-y-4">
          <h2 className="text-lg font-semibold flex items-center gap-2">
            <CheckCircle2 className="w-5 h-5 text-green-600" /> Điều khoản chuẩn
            (Public sẵn)
          </h2>
          <div className="space-y-3">
            {standardTerms.map((term, idx) => (
              <label
                key={idx}
                className="flex items-start gap-3 p-3 border rounded-lg cursor-pointer hover:bg-gray-50"
              >
                <input
                  type="checkbox"
                  defaultChecked
                  className="mt-1 rounded text-blue-600 focus:ring-blue-500"
                />
                <span className="text-sm font-medium text-gray-700">
                  {term}
                </span>
              </label>
            ))}
          </div>
        </section>

        {/* Action Buttons */}
        <div className="flex justify-end gap-3 pt-4">
          <button className="px-5 py-2.5 border rounded-lg font-medium text-gray-700 hover:bg-gray-50 flex items-center gap-2">
            <Save className="w-4 h-4" /> Lưu Nháp
          </button>
          <button className="px-5 py-2.5 bg-blue-600 text-white rounded-lg font-medium hover:bg-blue-700 flex items-center gap-2 shadow-sm">
            <LinkIcon className="w-4 h-4" /> Sinh Link & Gửi Khách Hàng
          </button>
        </div>
      </div>
    </div>
  );
}
