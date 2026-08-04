"use client";

import { useState } from "react";
import {
  FileSignature,
  MessageSquare,
  Lock,
  Send,
  Smartphone,
  X,
  CheckCircle2,
} from "lucide-react";

// --- DỮ LIỆU MẪU (FAKE DATA) ---
const INITIAL_TERMS = [
  {
    id: "term_1",
    isSoftTerm: false, // Điều khoản cứng, không được sửa
    titleVn: "ĐIỀU 1: PHẠM VI CÔNG VIỆC",
    titleEn: "ARTICLE 1: SCOPE OF WORK",
    contentVn:
      "Bên A đồng ý cung cấp và Bên B đồng ý sử dụng Phần mềm Quản lý Hợp đồng theo đúng các tài liệu đặc tả kỹ thuật đính kèm. Bên B không được phép sao chép, dịch ngược hoặc sử dụng phần mềm trên các máy chủ không thuộc quyền sở hữu hợp pháp của Bên B.",
    contentEn:
      "Party A agrees to provide and Party B agrees to use the Contract Management Software in accordance with the attached technical specifications. Party B is not allowed to copy, reverse engineer, or use the software on servers not legally owned by Party B.",
    comments: [],
  },
  {
    id: "term_2",
    isSoftTerm: true, // Điều khoản mềm, được phép comment
    titleVn: "ĐIỀU 2: THỜI GIAN BẢO HÀNH & BẢO TRÌ",
    titleEn: "ARTICLE 2: WARRANTY & MAINTENANCE PERIOD",
    contentVn:
      "Bên A cam kết bảo hành miễn phí hệ thống phần mềm trong vòng 12 tháng kể từ ngày ký Biên bản nghiệm thu tổng thể.",
    contentEn:
      "Party A commits to providing a free warranty for the software system for 12 months from the date of signing the Final Acceptance Minute.",
    comments: [
      {
        id: 1,
        sender: "Nhà cung cấp",
        text: "Gửi anh/chị tham khảo chính sách bảo hành tiêu chuẩn của công ty.",
        time: "10:00 - 15/07/2026",
      },
    ],
  },
  {
    id: "term_3",
    isSoftTerm: true, // Điều khoản mềm
    titleVn: "ĐIỀU 3: ĐIỀU KHOẢN THANH TOÁN",
    titleEn: "ARTICLE 3: PAYMENT TERMS",
    contentVn:
      "Bên B thanh toán cho Bên A thành 02 đợt. Đợt 1: 50% ngay sau khi ký hợp đồng. Đợt 2: 50% sau khi ký Biên bản nghiệm thu và nhận hóa đơn VAT.",
    contentEn:
      "Party B shall pay Party A in 02 installments. 1st: 50% immediately after signing. 2nd: 50% after signing the Acceptance Minute and receiving VAT invoice.",
    comments: [],
  },
];

export default function PublicContractView() {
  const [terms, setTerms] = useState(INITIAL_TERMS);
  const [activeCommentId, setActiveCommentId] = useState<string | null>(null);
  const [newComment, setNewComment] = useState("");

  // States cho OTP Modal
  const [isOtpOpen, setIsOtpOpen] = useState(false);
  const [otpCode, setOtpCode] = useState("");
  const [isSigned, setIsSigned] = useState(false);

  // Xử lý gửi comment
  const handleSendComment = (termId: string) => {
    if (!newComment.trim()) return;

    setTerms((prevTerms) =>
      prevTerms.map((term) => {
        if (term.id === termId) {
          return {
            ...term,
            comments: [
              ...term.comments,
              {
                id: Date.now(),
                sender: "Bạn (Khách hàng)",
                text: newComment,
                time: "Vừa xong",
              },
            ],
          };
        }
        return term;
      }),
    );
    setNewComment("");
    setActiveCommentId(null);
  };

  // Xử lý ký hợp đồng
  const handleSignContract = () => {
    if (otpCode.length === 6) {
      setIsSigned(true);
      setIsOtpOpen(false);
    } else {
      alert("Vui lòng nhập đủ 6 số OTP");
    }
  };

  return (
    <div className="h-screen overflow-y-auto bg-gray-50 px-3 py-4 pb-24 sm:px-6 sm:py-8 sm:pb-32">
      <div className="mx-auto max-w-4xl space-y-4 sm:space-y-6">
        {/* HEADER HỢP ĐỒNG */}
        <div className="relative overflow-hidden rounded-xl border border-gray-100 bg-white p-4 text-center shadow-sm sm:p-8">
          <div className="absolute top-0 left-0 w-full h-2 bg-blue-600"></div>
          <h1 className="text-2xl font-bold text-gray-900 uppercase">
            Hợp đồng Cung cấp Phần mềm
          </h1>
          <h2 className="text-lg text-gray-500 italic mt-1">
            Software Provision Contract
          </h2>
          <p className="mt-4 text-sm font-medium text-gray-600">
            Mã Hợp đồng: <span className="text-blue-600">HD-2026-001</span>
          </p>

          {isSigned && (
            <div className="mt-6 inline-flex items-center gap-2 bg-green-50 text-green-700 px-4 py-2 rounded-full font-bold">
              <CheckCircle2 className="w-5 h-5" />
              BẠN ĐÃ KÝ XÁC NHẬN HỢP ĐỒNG NÀY
            </div>
          )}
        </div>

        {/* NỘI DUNG ĐIỀU KHOẢN */}
        <div className="space-y-6">
          {terms.map((term) => (
            <div
              key={term.id}
              className="rounded-xl border border-gray-100 bg-white p-4 shadow-sm transition-all hover:shadow-md sm:p-6"
            >
              <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                <div>
                  <h3 className="font-bold text-gray-900">{term.titleVn}</h3>
                  <h4 className="text-sm font-medium text-gray-500 italic">
                    {term.titleEn}
                  </h4>
                </div>
                {/* Phân biệt điều khoản Cứng/Mềm */}
                {term.isSoftTerm ? (
                  <button
                    onClick={() =>
                      setActiveCommentId(
                        activeCommentId === term.id ? null : term.id,
                      )
                    }
                    className="flex items-center gap-2 text-sm font-medium text-blue-600 bg-blue-50 hover:bg-blue-100 px-3 py-1.5 rounded-lg transition-colors"
                  >
                    <MessageSquare className="w-4 h-4" />
                    {term.comments.length > 0
                      ? `Trao đổi (${term.comments.length})`
                      : "Đề xuất sửa"}
                  </button>
                ) : (
                  <span className="flex items-center gap-1 text-xs font-medium text-gray-400 bg-gray-100 px-2 py-1 rounded">
                    <Lock className="w-3 h-3" /> Cố định
                  </span>
                )}
              </div>

              {/* Văn bản song ngữ xen kẽ */}
              <div className="space-y-2 text-justify">
                <p className="text-gray-800 leading-relaxed">
                  {term.contentVn}
                </p>
                <p className="text-gray-500 italic text-sm leading-relaxed">
                  {term.contentEn}
                </p>
              </div>

              {/* KHU VỰC BÌNH LUẬN (Chỉ hiện khi là điều khoản mềm và có comment hoặc đang mở input) */}
              {(term.comments.length > 0 || activeCommentId === term.id) && (
                <div className="mt-6 border-t pt-4">
                  <div className="space-y-4 mb-4">
                    {term.comments.map((cmt) => (
                      <div
                        key={cmt.id}
                        className={`flex flex-col ${cmt.sender.includes("Bạn") ? "items-end" : "items-start"}`}
                      >
                        <span className="text-xs font-medium text-gray-500 mb-1">
                          {cmt.sender} • {cmt.time}
                        </span>
                        <div
                          className={`px-4 py-2 rounded-2xl max-w-[85%] text-sm ${cmt.sender.includes("Bạn") ? "bg-blue-600 text-white rounded-tr-sm" : "bg-gray-100 text-gray-800 rounded-tl-sm"}`}
                        >
                          {cmt.text}
                        </div>
                      </div>
                    ))}
                  </div>

                  {/* Input nhập comment */}
                  {activeCommentId === term.id && (
                    <div className="flex gap-2 items-end mt-4">
                      <textarea
                        value={newComment}
                        onChange={(e) => setNewComment(e.target.value)}
                        placeholder="Nhập đề xuất thay đổi điều khoản này..."
                        className="w-full border rounded-xl p-3 text-sm focus:ring-2 focus:ring-blue-500 outline-none resize-none bg-gray-50"
                        rows={2}
                      />
                      <button
                        onClick={() => handleSendComment(term.id)}
                        className="p-3 bg-blue-600 text-white rounded-xl hover:bg-blue-700 transition-colors"
                      >
                        <Send className="w-5 h-5" />
                      </button>
                    </div>
                  )}
                </div>
              )}
            </div>
          ))}
        </div>

        {/* BOTTOM ACTION BAR */}
        {!isSigned && (
          <div className="bottom-4 z-10 mt-8 flex flex-col gap-4 rounded-2xl border border-gray-200 bg-white p-4 shadow-lg sm:flex-row sm:items-center sm:justify-between">
            <div className="text-sm">
              <p className="font-semibold text-gray-900">
                Bạn đã đọc kỹ và đồng ý với các điều khoản?
              </p>
              <p className="text-gray-500">
                Sau khi ký, hợp đồng sẽ có giá trị pháp lý.
              </p>
            </div>
            <button
              onClick={() => setIsOtpOpen(true)}
              className="flex w-full items-center justify-center gap-2 rounded-xl bg-green-600 px-6 py-3 font-bold text-white shadow-sm transition-transform hover:bg-green-700 active:scale-95 sm:w-auto sm:px-8"
            >
              <FileSignature className="w-5 h-5" /> KÝ ĐIỆN TỬ
            </button>
          </div>
        )}
      </div>

      {/* --- OTP MODAL --- */}
      {isOtpOpen && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4 animate-in fade-in duration-200">
          <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden animate-in zoom-in-95 duration-200">
            <div className="flex justify-between items-center p-5 border-b">
              <h3 className="font-bold text-lg text-gray-900">
                Xác thực Ký điện tử
              </h3>
              <button
                onClick={() => setIsOtpOpen(false)}
                className="text-gray-400 hover:text-gray-600"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 text-center space-y-6">
              <div className="w-16 h-16 bg-blue-50 rounded-full flex items-center justify-center mx-auto text-blue-600">
                <Smartphone className="w-8 h-8" />
              </div>

              <div>
                <p className="text-gray-600">
                  Mã xác thực (OTP) đã được gửi đến số điện thoại
                </p>
                <p className="text-lg font-bold text-gray-900 tracking-wider mt-1">
                  ******889
                </p>
                <p className="text-xs text-gray-400 mt-1">
                  (Đại diện pháp luật: Nguyễn Văn A)
                </p>
              </div>

              <div className="space-y-2">
                <input
                  type="text"
                  maxLength={6}
                  value={otpCode}
                  onChange={(e) =>
                    setOtpCode(e.target.value.replace(/\D/g, ""))
                  } // Chỉ cho phép nhập số
                  placeholder="Nhập 6 số OTP"
                  className="w-full text-center text-2xl tracking-[0.5em] font-bold border-2 border-gray-200 rounded-xl py-3 focus:border-blue-500 focus:ring-0 outline-none transition-colors"
                />
                <div className="flex justify-between text-sm">
                  <span className="text-gray-500">
                    Mã hết hạn trong:{" "}
                    <strong className="text-red-500">02:59</strong>
                  </span>
                  <button className="text-blue-600 font-medium hover:underline">
                    Gửi lại mã
                  </button>
                </div>
              </div>

              <button
                onClick={handleSignContract}
                className="w-full py-3.5 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl shadow-md transition-colors"
              >
                XÁC NHẬN KÝ
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
