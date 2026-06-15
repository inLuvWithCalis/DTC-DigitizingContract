"use client";

import { useState } from "react";
import { GoogleSignInButton } from "@/components/google-sign-in-button";

export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);

  const handleGoogleSignIn = async () => {
    setIsLoading(true);
    // TODO: Tích hợp logic Google OAuth
    setTimeout(() => {
      window.location.href = "/dashboard";
    }, 1500);
  };

  return (
    <div className="min-h-screen bg-linear-to-br from-slate-50 via-slate-100 to-indigo-50 flex">
      <div className="hidden lg:flex lg:w-1/2 bg-linear-to-br from-slate-900 via-slate-800 to-indigo-950 flex-col justify-between p-12 relative overflow-hidden">
        <div className="absolute inset-0 bg-[linear-gradient(to_right,#4f4f4f2e_1px,transparent_1px),linear-gradient(to_bottom,#4f4f4f2e_1px,transparent_1px)] bg-size[14px_24px] mask-[radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)]"></div>

        <div className="relative z-10">
          <div className="flex items-center gap-3 mb-12">
            <div className="w-10 h-10 bg-indigo-600 rounded-lg flex items-center justify-center shadow-lg shadow-indigo-500/30">
              <svg
                className="w-6 h-6 text-white"
                fill="none"
                stroke="currentColor"
                strokeWidth="2"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                />
              </svg>
            </div>
            <span className="text-white font-bold text-xl tracking-tight">
              eContract Hub
            </span>
          </div>
          <h1 className="text-4xl lg:text-5xl font-bold text-white mb-4 leading-tight">
            Nền tảng số hóa <br />{" "}
            <span className="text-indigo-400">Hợp đồng toàn diện</span>
          </h1>
          <p className="text-slate-300 text-lg max-w-md leading-relaxed">
            Quản lý tập trung vòng đời chứng từ: Hợp đồng mua, bán, biên bản bàn
            giao và phụ lục với chuẩn bảo mật doanh nghiệp.
          </p>
        </div>

        <div className="mt-12 relative z-10"></div>
      </div>

      <div className="w-full lg:w-1/2 flex items-center justify-center p-4 sm:p-8">
        <div className="w-full max-w-md">
          <div className="bg-white rounded-2xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] p-8 sm:p-10 border border-slate-100">
            <div className="lg:hidden mb-8">
              <div className="flex items-center gap-2 mb-6">
                <div className="w-8 h-8 bg-indigo-600 rounded-lg flex items-center justify-center shadow-md">
                  <svg
                    className="w-5 h-5 text-white"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
                    />
                  </svg>
                </div>
                <span className="text-slate-900 font-bold text-lg">
                  eContract
                </span>
              </div>
            </div>

            <h2 className="text-2xl sm:text-3xl font-bold text-slate-900 mb-2 tracking-tight">
              Đăng nhập hệ thống
            </h2>
            <p className="text-slate-500 mb-8 text-sm sm:text-base">
              Truy cập không gian quản lý tài liệu pháp lý của bạn
            </p>

            <GoogleSignInButton
              isLoading={isLoading}
              onClick={handleGoogleSignIn}
            />

            <div className="my-8 flex items-center gap-3">
              <div className="flex-1 h-px bg-slate-100"></div>
              <span className="text-xs text-slate-400 uppercase tracking-wider font-medium">
                Hoặc đăng nhập bằng Email
              </span>
              <div className="flex-1 h-px bg-slate-100"></div>
            </div>

            <button className="w-full px-4 py-2.5 border border-slate-200 rounded-xl text-slate-700 font-medium hover:bg-slate-50 hover:border-slate-300 transition-all duration-200 focus:ring-2 focus:ring-indigo-100 outline-none">
              Tiếp tục với Email
            </button>

            <p className="text-sm text-slate-500 text-center mt-8">
              Chưa có tài khoản?{" "}
              <a
                href="#"
                className="text-indigo-600 hover:text-indigo-700 hover:underline font-semibold transition-colors"
              >
                Yêu cầu cấp quyền
              </a>
            </p>

            <div className="mt-8 pt-6 border-t border-slate-100">
              <div className="flex items-center justify-center gap-2 text-xs text-slate-500">
                <svg
                  className="w-4 h-4 text-emerald-500"
                  fill="none"
                  stroke="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    strokeWidth="2"
                    d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"
                  />
                </svg>
                <span>Mã hóa đầu cuối & Bảo mật cấp doanh nghiệp</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
