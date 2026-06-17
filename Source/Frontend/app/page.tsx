"use client";

import { useState } from "react";
import { Loader2, FileText, ShieldCheck, Eye, EyeOff } from "lucide-react";
import { toast } from "sonner";
import { ThemeToggle } from "@/components/theme-toggle";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

export default function LoginPage() {
  const [isLoading, setIsLoading] = useState(false);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);

  const handleEmailSignIn = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!email.trim()) {
      toast.error("Vui lòng nhập địa chỉ email");
      return;
    }
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.trim())) {
      toast.error("Địa chỉ email không hợp lệ");
      return;
    }
    if (!password) {
      toast.error("Vui lòng nhập mật khẩu");
      return;
    }
    if (password.length < 6) {
      toast.error("Mật khẩu phải chứa ít nhất 6 ký tự");
      return;
    }

    setIsLoading(true);
    setTimeout(() => {
      window.location.href = "/dashboard";
    }, 1500);
  };

  return (
    <div className="min-h-screen bg-linear-to-br from-background via-secondary/50 to-primary/5 flex">
      <div className="hidden lg:flex lg:w-1/2 bg-linear-to-br from-slate-900 via-slate-800 to-primary/20 dark:from-black dark:via-slate-950 dark:to-primary/10 flex-col justify-between p-12 relative overflow-hidden">
        <div className="absolute inset-0 bg-[linear-gradient(to_right,#4f4f4f2e_1px,transparent_1px),linear-gradient(to_bottom,#4f4f4f2e_1px,transparent_1px)] bg-[size:14px_24px] [mask-image:radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)]"></div>

        <div className="relative z-10">
          <div className="flex items-center gap-3 mb-12">
            <div className="w-10 h-10 bg-primary rounded-lg flex items-center justify-center shadow-lg shadow-primary/30">
              <FileText className="w-6 h-6 text-primary-foreground" />
            </div>
            <span className="text-white font-bold text-xl tracking-tight">
              eContract Hub
            </span>
          </div>
          <h1 className="text-4xl lg:text-5xl font-bold text-white mb-4 leading-tight">
            Nền tảng số hóa <br />{" "}
            <span className="text-primary/80">Hợp đồng toàn diện</span>
          </h1>
          <p className="text-slate-400 text-lg max-w-md leading-relaxed">
            Quản lý tập trung vòng đời chứng từ: Hợp đồng mua, bán, biên bản bàn
            giao và phụ lục với chuẩn bảo mật doanh nghiệp.
          </p>
        </div>
      </div>

      <div className="w-full lg:w-1/2 flex items-center justify-center p-4 sm:p-8 relative">
        <div className="absolute top-4 right-4 sm:top-6 sm:right-6">
          <ThemeToggle />
        </div>
        <div className="w-full max-w-md">
          <Card className="rounded-2xl shadow-[0_8px_30px_rgb(0,0,0,0.04)] dark:shadow-[0_8px_30px_rgb(0,0,0,0.3)] border-border overflow-hidden">
            <CardHeader className="p-8 pb-0 sm:p-10 sm:pb-0 space-y-8">
              <div className="lg:hidden">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 bg-primary rounded-lg flex items-center justify-center shadow-md">
                    <FileText className="w-5 h-5 text-primary-foreground" />
                  </div>
                  <span className="text-foreground font-bold text-lg">
                    eContract
                  </span>
                </div>
              </div>

              <div className="space-y-2">
                <CardTitle className="text-2xl sm:text-3xl font-bold text-foreground tracking-tight">
                  Đăng nhập hệ thống
                </CardTitle>
                <CardDescription className="text-muted-foreground text-sm sm:text-base">
                  Nhập email và mật khẩu để truy cập không gian làm việc
                </CardDescription>
              </div>
            </CardHeader>
            <CardContent className="p-8 sm:p-10">
              <form onSubmit={handleEmailSignIn} className="space-y-5">
                <div className="space-y-2">
                  <Label
                    htmlFor="email"
                    className="text-foreground/80 font-semibold"
                  >
                    Địa chỉ Email
                  </Label>
                  <Input
                    id="email"
                    type="email"
                    value={email}
                    maxLength={255}
                    onChange={(e) => setEmail(e.target.value)}
                    className="h-11 rounded-xl"
                  />
                </div>

                <div className="space-y-2">
                  <div className="flex items-center justify-between">
                    <Label
                      htmlFor="password"
                      className="text-foreground/80 font-semibold"
                    >
                      Mật khẩu
                    </Label>
                    <a
                      href="#"
                      className="text-sm font-medium text-primary hover:text-primary/80 transition-colors"
                    >
                      Quên mật khẩu?
                    </a>
                  </div>
                  <div className="relative">
                    <Input
                      id="password"
                      type={showPassword ? "text" : "password"}
                      value={password}
                      maxLength={64}
                      onChange={(e) => setPassword(e.target.value)}
                      className="h-11 rounded-xl pr-24"
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-14 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors focus:outline-none"
                      aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
                    >
                      {showPassword ? (
                        <EyeOff className="w-4 h-4" />
                      ) : (
                        <Eye className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                </div>

                <div className="flex items-center space-x-2.5">
                  <Checkbox id="remember-me" />
                  <Label
                    htmlFor="remember-me"
                    className="text-sm font-medium leading-none cursor-pointer text-muted-foreground peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                  >
                    Ghi nhớ đăng nhập
                  </Label>
                </div>

                <Button
                  type="submit"
                  disabled={isLoading}
                  className="w-full h-11 mt-2 rounded-xl font-semibold shadow-sm transition-all"
                >
                  {isLoading && (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  )}
                  {isLoading ? "Đang xử lý..." : "Đăng nhập"}
                </Button>
              </form>
            </CardContent>

            <CardFooter className="p-8 pt-0 sm:p-10 sm:pt-0">
              <div className="w-full pt-6 border-t border-border">
                <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground">
                  <ShieldCheck className="w-4 h-4 text-emerald-500" />
                  <span>Dữ liệu được mã hóa & Bảo mật cấp doanh nghiệp</span>
                </div>
              </div>
            </CardFooter>
          </Card>
        </div>
      </div>
    </div>
  );
}
