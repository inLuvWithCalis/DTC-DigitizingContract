"use client";

import { useState } from "react";
import {
  FileText,
  Plus,
  Edit,
  Trash2,
  Lock,
  Unlock,
  Save,
  ArrowLeft,
  GripVertical,
} from "lucide-react";

// --- FAKE DATA ---
const MOCK_TEMPLATES = [
  {
    id: "TPL-001",
    name: "Hợp đồng Cung cấp Phần mềm (Chuẩn)",
    description:
      "Sử dụng cho các dự án cung cấp phần mềm đóng gói cơ bản, không có custom.",
    lastModified: "10/07/2026",
    terms: [
      {
        id: "term_1",
        isHardTerm: true, // Cứng -> Không cho khách sửa
        titleVn: "ĐIỀU 1: PHẠM VI CÔNG VIỆC",
        titleEn: "ARTICLE 1: SCOPE OF WORK",
        contentVn:
          "Bên A đồng ý cung cấp và Bên B đồng ý sử dụng Phần mềm Quản lý...",
        contentEn:
          "Party A agrees to provide and Party B agrees to use the Software...",
      },
      {
        id: "term_2",
        isHardTerm: false, // Mềm -> Cho khách comment
        titleVn: "ĐIỀU 2: THỜI GIAN BẢO HÀNH & BẢO TRÌ",
        titleEn: "ARTICLE 2: WARRANTY & MAINTENANCE PERIOD",
        contentVn:
          "Bên A cam kết bảo hành miễn phí hệ thống phần mềm trong vòng 12 tháng...",
        contentEn:
          "Party A commits to providing a free warranty for 12 months...",
      },
    ],
  },
  {
    id: "TPL-002",
    name: "Hợp đồng Triển khai & Tích hợp Hệ thống",
    description: "Sử dụng cho các dự án ERP, tích hợp API phức tạp.",
    lastModified: "12/07/2026",
    terms: [],
  },
];

export default function TemplateManagementPage() {
  // viewState: 'list' | 'edit'
  const [viewState, setViewState] = useState<"list" | "edit">("list");
  const [templates, setTemplates] = useState(MOCK_TEMPLATES);
  const [editingTemplate, setEditingTemplate] = useState<any>(null);

  // Mở màn hình Edit
  const handleEdit = (template: any) => {
    setEditingTemplate(JSON.parse(JSON.stringify(template))); // Clone data để edit
    setViewState("edit");
  };

  // Quay lại danh sách
  const handleBack = () => {
    setEditingTemplate(null);
    setViewState("list");
  };

  // Thêm điều khoản mới vào Template đang sửa
  const handleAddTerm = () => {
    const newTerm = {
      id: `term_${Date.now()}`,
      isHardTerm: true,
      titleVn: "",
      titleEn: "",
      contentVn: "",
      contentEn: "",
    };
    setEditingTemplate({
      ...editingTemplate,
      terms: [...editingTemplate.terms, newTerm],
    });
  };

  // Xóa điều khoản
  const handleRemoveTerm = (termId: string) => {
    setEditingTemplate({
      ...editingTemplate,
      terms: editingTemplate.terms.filter((t: any) => t.id !== termId),
    });
  };

  // Cập nhật giá trị điều khoản
  const handleTermChange = (termId: string, field: string, value: any) => {
    const updatedTerms = editingTemplate.terms.map((t: any) => {
      if (t.id === termId) {
        return { ...t, [field]: value };
      }
      return t;
    });
    setEditingTemplate({ ...editingTemplate, terms: updatedTerms });
  };

  // Lưu Template
  const handleSave = () => {
    const updatedTemplates = templates.map((t) =>
      t.id === editingTemplate.id ? editingTemplate : t,
    );
    setTemplates(updatedTemplates);
    setViewState("list");
    alert("Đã lưu Template thành công!");
  };

  return (
    <div className="mx-auto max-w-6xl space-y-4 p-3 sm:space-y-6 sm:p-6">
      {/* ========================================== */}
      {/* GIAO DIỆN DANH SÁCH TEMPLATE               */}
      {/* ========================================== */}
      {viewState === "list" && (
        <div className="space-y-6 animate-in fade-in duration-300">
          <div className="flex flex-col gap-4 rounded-xl border border-blue-100 bg-blue-50 p-4 sm:p-6 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <h1 className="text-2xl font-bold text-blue-900 flex items-center gap-2">
                <FileText className="w-6 h-6" /> Quản lý Template Hợp đồng
              </h1>
              <p className="text-sm text-blue-700 mt-2 max-w-2xl">
                Khởi tạo và cấu hình các mẫu hợp đồng chuẩn. Cấu hình song ngữ
                và thiết lập phân quyền sửa đổi (Điều khoản cứng / Điều khoản
                mềm) cho khách hàng.
              </p>
            </div>
            <button className="flex w-full items-center justify-center gap-2 rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 sm:w-fit">
              <Plus className="w-4 h-4" /> Tạo Template mới
            </button>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {templates.map((tpl) => (
              <div
                key={tpl.id}
                className="bg-white border rounded-xl p-5 hover:shadow-md transition-shadow"
              >
                <div className="flex justify-between items-start mb-3">
                  <h3 className="font-bold text-lg text-gray-900">
                    {tpl.name}
                  </h3>
                  <span className="text-xs bg-gray-100 text-gray-600 px-2 py-1 rounded font-medium">
                    {tpl.id}
                  </span>
                </div>
                <p className="text-sm text-gray-500 line-clamp-2 mb-4 h-10">
                  {tpl.description}
                </p>
                <div className="flex flex-col gap-3 border-t pt-4 sm:flex-row sm:items-center sm:justify-between">
                  <span className="text-xs text-gray-400">
                    Cập nhật: {tpl.lastModified}
                  </span>
                  <div className="flex gap-2">
                    <button className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded transition-colors">
                      <Trash2 className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => handleEdit(tpl)}
                      className="flex items-center gap-1 px-3 py-1.5 bg-blue-50 text-blue-700 hover:bg-blue-100 font-medium text-sm rounded transition-colors"
                    >
                      <Edit className="w-4 h-4" /> Chỉnh sửa
                    </button>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* ========================================== */}
      {/* GIAO DIỆN CHỈNH SỬA (BUILDER)              */}
      {/* ========================================== */}
      {viewState === "edit" && editingTemplate && (
        <div className="space-y-6 animate-in slide-in-from-right-8 duration-300">
          {/* Action Bar */}
          <div className="sticky top-0 z-10 flex flex-col gap-3 border-b bg-white pb-4 pt-2 sm:flex-row sm:items-center sm:justify-between">
            <button
              onClick={handleBack}
              className="flex items-center gap-2 text-gray-600 hover:text-gray-900 font-medium"
            >
              <ArrowLeft className="w-4 h-4" /> Quay lại
            </button>
            <div className="grid w-full grid-cols-2 gap-2 sm:flex sm:w-auto sm:gap-3">
              <button
                onClick={handleAddTerm}
                className="flex items-center justify-center gap-2 rounded-lg border border-blue-200 bg-blue-50 px-3 py-2 text-sm font-medium text-blue-700 hover:bg-blue-100 sm:px-4 sm:text-base"
              >
                <Plus className="w-4 h-4" /> Thêm Điều khoản
              </button>
              <button
                onClick={handleSave}
                className="flex items-center justify-center gap-2 rounded-lg bg-blue-600 px-3 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 sm:px-4 sm:text-base"
              >
                <Save className="w-4 h-4" /> Lưu Template
              </button>
            </div>
          </div>

          {/* Template Info */}
          <div className="bg-white p-6 border rounded-xl shadow-sm space-y-4">
            <div>
              <label className="block text-sm font-bold text-gray-700 mb-1">
                Tên Template
              </label>
              <input
                type="text"
                value={editingTemplate.name}
                onChange={(e) =>
                  setEditingTemplate({
                    ...editingTemplate,
                    name: e.target.value,
                  })
                }
                className="w-full border rounded-lg p-2.5 font-bold text-lg focus:ring-2 focus:ring-blue-500 outline-none"
              />
            </div>
            <div>
              <label className="block text-sm font-bold text-gray-700 mb-1">
                Mô tả sử dụng
              </label>
              <textarea
                value={editingTemplate.description}
                onChange={(e) =>
                  setEditingTemplate({
                    ...editingTemplate,
                    description: e.target.value,
                  })
                }
                className="w-full border rounded-lg p-2.5 text-sm focus:ring-2 focus:ring-blue-500 outline-none"
                rows={2}
              />
            </div>
          </div>

          {/* Terms Builder List */}
          <div className="space-y-4">
            <h3 className="font-bold text-lg text-gray-900 flex items-center gap-2">
              Danh sách Điều khoản ({editingTemplate.terms.length})
            </h3>

            {editingTemplate.terms.map((term: any, index: number) => (
              <div
                key={term.id}
                className="group flex overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm"
              >
                {/* Drag Handle (Trực quan) */}
                <div className="hidden w-10 cursor-move items-center justify-center border-r bg-gray-50 text-gray-400 transition-colors group-hover:bg-gray-100 sm:flex">
                  <GripVertical className="w-5 h-5" />
                </div>

                <div className="min-w-0 flex-1 space-y-5 p-4 sm:p-5">
                  {/* Header của một Điều khoản: Tùy chọn Cứng/Mềm & Xóa */}
                  <div className="flex flex-col gap-3 border-b pb-3 sm:flex-row sm:items-center sm:justify-between">
                    <div className="flex min-w-0 flex-col gap-2 sm:flex-row sm:items-center sm:gap-3">
                      <span className="font-bold bg-gray-100 text-gray-700 px-3 py-1 rounded">
                        Mục {index + 1}
                      </span>

                      {/* TOGGLE ĐIỀU KHOẢN CỨNG/MỀM */}
                      <button
                        onClick={() =>
                          handleTermChange(
                            term.id,
                            "isHardTerm",
                            !term.isHardTerm,
                          )
                        }
                        className={`flex w-fit max-w-full items-center gap-2 rounded-full px-3 py-1.5 text-left text-xs font-bold transition-colors sm:text-sm ${
                          term.isHardTerm
                            ? "bg-red-50 text-red-700 border border-red-200"
                            : "bg-green-50 text-green-700 border border-green-200"
                        }`}
                      >
                        {term.isHardTerm ? (
                          <Lock className="w-4 h-4" />
                        ) : (
                          <Unlock className="w-4 h-4" />
                        )}
                        {term.isHardTerm
                          ? "Điều khoản Cứng (Read-only)"
                          : "Điều khoản Mềm (Cho phép Comment)"}
                      </button>
                    </div>

                    <button
                      onClick={() => handleRemoveTerm(term.id)}
                      className="text-gray-400 hover:text-red-600 transition-colors"
                      title="Xóa điều khoản này"
                    >
                      <Trash2 className="w-5 h-5" />
                    </button>
                  </div>

                  {/* Form Tiêu đề (Song ngữ) */}
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div>
                      <label className="block text-xs font-bold text-gray-500 mb-1 uppercase">
                        Tiêu đề (Tiếng Việt)
                      </label>
                      <input
                        value={term.titleVn}
                        onChange={(e) =>
                          handleTermChange(term.id, "titleVn", e.target.value)
                        }
                        placeholder="VD: ĐIỀU 1: PHẠM VI CÔNG VIỆC"
                        className="w-full border rounded p-2 text-sm font-semibold focus:ring-1 focus:ring-blue-500 outline-none"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-bold text-gray-500 mb-1 uppercase">
                        Tiêu đề (Tiếng Anh)
                      </label>
                      <input
                        value={term.titleEn}
                        onChange={(e) =>
                          handleTermChange(term.id, "titleEn", e.target.value)
                        }
                        placeholder="Ex: ARTICLE 1: SCOPE OF WORK"
                        className="w-full border rounded p-2 text-sm italic focus:ring-1 focus:ring-blue-500 outline-none bg-gray-50"
                      />
                    </div>
                  </div>

                  {/* Form Nội dung (Song ngữ) */}
                  <div className="grid grid-cols-1 gap-4 md:grid-cols-2">
                    <div>
                      <label className="block text-xs font-bold text-gray-500 mb-1 uppercase">
                        Nội dung (Tiếng Việt)
                      </label>
                      <textarea
                        value={term.contentVn}
                        onChange={(e) =>
                          handleTermChange(term.id, "contentVn", e.target.value)
                        }
                        placeholder="Nội dung điều khoản bằng Tiếng Việt..."
                        rows={4}
                        className="w-full border rounded p-2 text-sm focus:ring-1 focus:ring-blue-500 outline-none resize-y"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-bold text-gray-500 mb-1 uppercase">
                        Nội dung (Tiếng Anh)
                      </label>
                      <textarea
                        value={term.contentEn}
                        onChange={(e) =>
                          handleTermChange(term.id, "contentEn", e.target.value)
                        }
                        placeholder="English content..."
                        rows={4}
                        className="w-full border rounded p-2 text-sm italic focus:ring-1 focus:ring-blue-500 outline-none bg-gray-50 resize-y"
                      />
                    </div>
                  </div>
                </div>
              </div>
            ))}

            {editingTemplate.terms.length === 0 && (
              <div className="text-center py-10 border-2 border-dashed rounded-xl text-gray-500 bg-gray-50">
                Chưa có điều khoản nào. Hãy thêm điều khoản mới.
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
