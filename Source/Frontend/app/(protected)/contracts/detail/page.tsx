"use client";

import { useState } from "react";
import {
  FileText,
  PenTool,
  Paperclip,
  Truck,
  MessageSquare,
  CheckCircle,
  UploadCloud,
  CreditCard, // Thêm icon CreditCard cho Tab Thanh toán
} from "lucide-react";

export default function ContractDetailPage() {
  const [activeTab, setActiveTab] = useState("terms");

  const tabs = [
    { id: "terms", label: "Điều khoản & Đàm phán", icon: FileText },
    { id: "signing", label: "Trạng thái Ký", icon: PenTool },
    { id: "attachments", label: "Hồ sơ đính kèm", icon: Paperclip },
    { id: "hardcopy", label: "Bản cứng", icon: Truck },
    { id: "payment", label: "Thanh toán (Luồng tiền)", icon: CreditCard }, // Bổ sung Tab Thanh toán
  ];

  const attachmentDocs = [
    "Biên bản Bàn giao",
    "Biên bản Nghiệm thu",
    "Thanh lý Hợp đồng",
    "Hóa đơn VAT (PDF)",
  ];

  return (
    <div className="p-6 max-w-6xl mx-auto space-y-6">
      {/* Header Hợp đồng */}
      <div className="flex justify-between items-start bg-white p-6 rounded-xl border shadow-sm">
        <div>
          <div className="flex items-center gap-3 mb-2">
            <h1 className="text-2xl font-bold text-gray-900">HD-2026-001</h1>
            <span className="px-3 py-1 bg-amber-100 text-amber-700 rounded-full text-xs font-bold uppercase tracking-wider">
              Đang ký
            </span>
          </div>
          <p className="text-gray-600 font-medium">
            Khách hàng: Công ty Cổ phần Alpha
          </p>
          <p className="text-sm text-gray-500 mt-1">Giá trị: 150,000,000 VNĐ</p>
        </div>
        <button className="px-4 py-2 bg-gray-900 text-white rounded-lg text-sm font-medium hover:bg-gray-800">
          In bản giấy (PDF)
        </button>
      </div>

      {/* Tabs Navigation */}
      <div className="flex space-x-1 bg-gray-100 p-1 rounded-xl overflow-x-auto">
        {tabs.map((tab) => {
          const Icon = tab.icon;
          const isActive = activeTab === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`flex-1 flex min-w-max items-center justify-center gap-2 px-4 py-2.5 text-sm font-medium rounded-lg transition-all ${
                isActive
                  ? "bg-white text-blue-600 shadow-sm"
                  : "text-gray-600 hover:text-gray-900 hover:bg-gray-200"
              }`}
            >
              <Icon className="w-4 h-4" /> {tab.label}
            </button>
          );
        })}
      </div>

      {/* Tab Content Area */}
      <div className="bg-white border rounded-xl shadow-sm min-h-[400px] p-6">
        {/* TAB 1: ĐIỀU KHOẢN & ĐÀM PHÁN */}
        {activeTab === "terms" && (
          <div className="flex gap-6 h-full">
            <div className="flex-1 border rounded-lg p-6 bg-gray-50 overflow-y-auto max-h-[500px]">
              <h3 className="font-bold text-lg mb-4 text-center">
                HỢP ĐỒNG CUNG CẤP PHẦN MỀM
              </h3>
              <p className="text-sm text-gray-700 leading-relaxed mb-4">
                Điều 1: Khách hàng không được quyền sao chép phần mềm sang máy
                chủ khác...{" "}
                <span className="bg-yellow-200 px-1 rounded cursor-pointer">
                  Điều 3: Thời gian bảo hành là 12 tháng.
                </span>
              </p>
            </div>
            <div className="w-80 border-l pl-6 space-y-4">
              <h4 className="font-semibold flex items-center gap-2">
                <MessageSquare className="w-4 h-4" /> Lịch sử Đề xuất
              </h4>
              <div className="bg-yellow-50 border border-yellow-200 p-3 rounded-lg text-sm">
                <p className="font-semibold text-yellow-800">
                  GĐ. Nguyễn Văn A (Khách hàng)
                </p>
                <p className="text-gray-600 mt-1">
                  "Xin nâng thời gian bảo hành lên 18 tháng."
                </p>
                <div className="mt-2 flex gap-2">
                  <button className="text-xs bg-blue-600 text-white px-2 py-1 rounded">
                    Cập nhật HĐ
                  </button>
                  <button className="text-xs bg-gray-200 text-gray-700 px-2 py-1 rounded">
                    Từ chối
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* TAB 2: TRẠNG THÁI KÝ */}
        {activeTab === "signing" && (
          <div className="max-w-2xl mx-auto py-10 space-y-8">
            <div className="flex items-center justify-between p-6 border rounded-xl bg-green-50 border-green-200">
              <div className="flex items-center gap-4">
                <CheckCircle className="w-8 h-8 text-green-600" />
                <div>
                  <h4 className="font-bold text-green-900">
                    Bên Khách hàng (Bên B)
                  </h4>
                  <p className="text-sm text-green-700">
                    Đã ký bằng OTP lúc 14:30 - 15/07/2026
                  </p>
                </div>
              </div>
              <span className="px-3 py-1 bg-green-200 text-green-800 text-xs font-bold rounded-full">
                Đã xác thực
              </span>
            </div>

            <div className="flex items-center justify-between p-6 border rounded-xl bg-gray-50">
              <div className="flex items-center gap-4">
                <div className="w-8 h-8 rounded-full bg-gray-200 flex items-center justify-center border-2 border-gray-400">
                  <span className="w-2 h-2 rounded-full bg-gray-400"></span>
                </div>
                <div>
                  <h4 className="font-bold text-gray-900">
                    Người Đại Diện Pháp Luật (Bên A)
                  </h4>
                  <p className="text-sm text-gray-500">
                    Chờ xác nhận mã OTP gửi về SĐT ***389
                  </p>
                </div>
              </div>
              <button className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg">
                Ký OTP Ngay
              </button>
            </div>
          </div>
        )}

        {/* TAB 3: HỒ SƠ ĐÍNH KÈM */}
        {activeTab === "attachments" && (
          <div className="space-y-6">
            <div className="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center hover:bg-gray-50 cursor-pointer transition-colors">
              <UploadCloud className="w-10 h-10 text-blue-500 mx-auto mb-3" />
              <p className="font-medium text-gray-700">
                Kéo thả hoặc click để tải lên chứng từ
              </p>
              <p className="text-xs text-gray-500 mt-1">
                Hỗ trợ PDF, DOCX, JPG (Max: 10MB)
              </p>
            </div>
            <div className="grid grid-cols-2 gap-4">
              {attachmentDocs.map((doc, idx) => (
                <div
                  key={idx}
                  className="flex items-center justify-between p-4 border rounded-lg"
                >
                  <div className="flex items-center gap-3">
                    <FileText className="w-5 h-5 text-gray-400" />
                    <span className="font-medium text-sm">{doc}</span>
                  </div>
                  {idx === 0 ? (
                    <span className="text-xs text-green-600 font-medium bg-green-50 px-2 py-1 rounded">
                      Đã upload
                    </span>
                  ) : (
                    <button className="text-xs text-blue-600 font-medium hover:underline">
                      Upload
                    </button>
                  )}
                </div>
              ))}
            </div>
          </div>
        )}

        {/* TAB 4: BẢN CỨNG */}
        {activeTab === "hardcopy" && (
          <div className="max-w-xl py-6 space-y-6">
            <h3 className="font-bold text-gray-900 border-b pb-2">
              Theo dõi Giao nhận Bản cứng
            </h3>
            <div className="space-y-4">
              <label className="flex items-center gap-3 p-4 border rounded-lg bg-gray-50 cursor-pointer">
                <input
                  type="checkbox"
                  defaultChecked
                  className="w-5 h-5 text-blue-600 rounded"
                />
                <span className="font-medium text-gray-700">
                  Đã gửi bản cứng cho Khách hàng qua CPN
                </span>
              </label>
              <label className="flex items-center gap-3 p-4 border rounded-lg bg-gray-50 cursor-pointer">
                <input
                  type="checkbox"
                  className="w-5 h-5 text-blue-600 rounded"
                />
                <span className="font-medium text-gray-700">
                  Khách hàng xác nhận đã nhận được
                </span>
              </label>
              <label className="flex items-center gap-3 p-4 border rounded-lg bg-gray-50 cursor-pointer">
                <input
                  type="checkbox"
                  className="w-5 h-5 text-blue-600 rounded"
                />
                <span className="font-medium text-gray-700">
                  Đã nhận lại 1 bản cứng có chữ ký tươi từ Khách hàng
                </span>
              </label>
            </div>
            <div className="p-4 bg-blue-50 border border-blue-100 rounded-lg text-sm text-blue-800">
              <strong>Lưu ý:</strong> Hợp đồng chỉ chuyển sang trạng thái{" "}
              <b>Closed</b> khi bản cứng đã thu hồi đủ và các chứng từ (Nghiệm
              thu, VAT) đã được đính kèm.
            </div>
          </div>
        )}

        {/* TAB 5: THANH TOÁN (LUỒNG TIỀN) */}
        {activeTab === "payment" && (
          <div className="space-y-6 animate-in fade-in duration-300">
            {/* Tổng quan tài chính */}
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="p-5 border rounded-xl bg-gray-50 shadow-sm">
                <p className="text-sm text-gray-500 mb-1 font-medium">
                  Tổng giá trị hợp đồng
                </p>
                <p className="text-2xl font-bold text-gray-900">
                  150,000,000 ₫
                </p>
              </div>
              <div className="p-5 border rounded-xl bg-green-50 border-green-200 shadow-sm">
                <p className="text-sm text-green-700 mb-1 font-medium">
                  Đã thu tiền (50%)
                </p>
                <p className="text-2xl font-bold text-green-700">
                  75,000,000 ₫
                </p>
              </div>
              <div className="p-5 border rounded-xl bg-red-50 border-red-200 shadow-sm">
                <p className="text-sm text-red-700 mb-1 font-medium">
                  Công nợ còn lại (50%)
                </p>
                <p className="text-2xl font-bold text-red-700">75,000,000 ₫</p>
              </div>
            </div>

            {/* Thanh tiến độ */}
            <div className="bg-white p-5 border rounded-xl shadow-sm space-y-3">
              <div className="flex justify-between text-sm font-semibold text-gray-700">
                <span>Tiến độ hoàn thành luồng tiền</span>
                <span className="text-blue-600">50%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-3">
                <div
                  className="bg-blue-600 h-3 rounded-full transition-all duration-500"
                  style={{ width: "50%" }}
                ></div>
              </div>
              <p className="text-xs text-gray-500 italic text-right">
                Hợp đồng chỉ có thể Closed khi tiến độ đạt 100%.
              </p>
            </div>

            {/* Danh sách các đợt thanh toán */}
            <div className="border rounded-xl shadow-sm overflow-hidden bg-white">
              <div className="flex justify-between items-center p-4 bg-gray-50 border-b">
                <h3 className="font-bold text-gray-900">
                  Chi tiết các đợt thanh toán
                </h3>
                <button className="text-sm bg-blue-600 text-white px-4 py-2 rounded-lg font-medium hover:bg-blue-700 transition-colors">
                  + Thêm đợt thanh toán
                </button>
              </div>

              <div className="divide-y">
                {/* Đợt 1 */}
                <div className="p-5 flex flex-col sm:flex-row sm:items-center justify-between hover:bg-gray-50 transition-colors gap-4">
                  <div>
                    <div className="flex items-center gap-3">
                      <h4 className="font-semibold text-gray-900 text-base">
                        Đợt 1: Tạm ứng 50% sau khi ký
                      </h4>
                      <span className="px-2.5 py-1 bg-green-100 text-green-700 text-xs font-bold rounded-full">
                        Đã thu tiền
                      </span>
                    </div>
                    <div className="text-sm text-gray-500 mt-2 space-y-1">
                      <p>Hạn thanh toán: 20/07/2026</p>
                      <p>
                        Ngày thực thu:{" "}
                        <span className="text-gray-900 font-medium">
                          18/07/2026
                        </span>
                      </p>
                    </div>
                  </div>
                  <div className="text-left sm:text-right">
                    <p className="font-bold text-gray-900 text-lg">
                      75,000,000 ₫
                    </p>
                    <button className="text-sm text-blue-600 hover:text-blue-800 font-medium mt-1 flex items-center gap-1 sm:justify-end">
                      <Paperclip className="w-4 h-4" /> Xem Ủy nhiệm chi
                    </button>
                  </div>
                </div>

                {/* Đợt 2 */}
                <div className="p-5 flex flex-col sm:flex-row sm:items-center justify-between hover:bg-gray-50 transition-colors gap-4">
                  <div>
                    <div className="flex items-center gap-3">
                      <h4 className="font-semibold text-gray-900 text-base">
                        Đợt 2: Thanh toán nốt 50%
                      </h4>
                      <span className="px-2.5 py-1 bg-red-100 text-red-700 text-xs font-bold rounded-full">
                        Công nợ
                      </span>
                    </div>
                    <div className="text-sm text-gray-500 mt-2 space-y-1">
                      <p>
                        Hạn thanh toán:{" "}
                        <span className="font-medium text-gray-900">
                          Sau khi ký Nghiệm thu
                        </span>
                      </p>
                      <p>
                        Ngày thực thu:{" "}
                        <span className="italic">Chưa thanh toán</span>
                      </p>
                    </div>
                  </div>
                  <div className="text-left sm:text-right">
                    <p className="font-bold text-gray-900 text-lg">
                      75,000,000 ₫
                    </p>
                    <button className="text-sm bg-gray-900 text-white px-4 py-2 rounded-lg mt-3 hover:bg-gray-800 transition-colors font-medium">
                      Xác nhận Đã thu tiền
                    </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
