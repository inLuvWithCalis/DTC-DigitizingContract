"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import {
  Loader2,
  FileText,
  ShieldCheck,
  Eye,
  EyeOff,
  Building,
} from "lucide-react";
import { toast } from "@/components/ui/sonner";
import { ThemeToggle } from "@/components/theme-toggle";
import { authApi } from "@/services/auth-api";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useAuthStore } from "@/hooks/use-auth-store";

export default function LoginPage() {
  const router = useRouter();
  const [isLoading, setIsLoading] = useState(true);
  const [tenantCode, setTenantCode] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const { user, isAuthenticated, setUser } = useAuthStore();

  const [errors, setErrors] = useState<{
    tenantCode?: string;
    email?: string;
    password?: string;
  }>({});
  const [touched, setTouched] = useState<{
    tenantCode?: boolean;
    email?: boolean;
    password?: boolean;
  }>({});

  const validateTenantCode = (value: string): string | undefined => {
    if (!value.trim()) return "Vui lòng nhập Mã công ty (Tenant Code)";
    return undefined;
  };

  useEffect(() => {
    if (typeof window !== "undefined") {
      const urlParams = new URLSearchParams(window.location.search);
      const error = urlParams.get("error");

      if (error === "session_expired" || error === "employee_inactive") {
        const timer = setTimeout(() => {
          toast.error(
            error === "employee_inactive"
              ? "Tài khoản nhân viên đã bị khóa. Vui lòng liên hệ quản lý."
              : "Phiên đăng nhập đã hết hạn hoặc không tồn tại, vui lòng đăng nhập lại.",
          );
          window.history.replaceState(null, "", "/");
        }, 100);
        return () => clearTimeout(timer);
      }
    }
  }, []);

  useEffect(() => {
    const checkAlreadyLoggedIn = async () => {
      if (isAuthenticated && user) {
        router.push("/dashboard");
        return;
      }

      try {
        await authApi.getMe();
        router.push("/dashboard");
      } catch (error) {
        setIsLoading(false);
      }
    };

    checkAlreadyLoggedIn();
  }, [isAuthenticated, user, router, setUser]);

  const validateEmail = (value: string): string | undefined => {
    if (!value.trim()) return "Vui lòng nhập tên tài khoản";
    // if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value.trim()))
    //   return "Địa chỉ email không hợp lệ";
    return undefined;
  };

  const validatePassword = (value: string): string | undefined => {
    if (!value) return "Vui lòng nhập mật khẩu";
    return undefined;
  };

  const handleEmailChange = (value: string) => {
    setEmail(value);
    if (touched.email) {
      setErrors((prev) => ({ ...prev, email: validateEmail(value) }));
    }
  };

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    if (touched.password) {
      setErrors((prev) => ({ ...prev, password: validatePassword(value) }));
    }
  };

  const handleTenantCodeChange = (value: string) => {
    setTenantCode(value);
    if (touched.tenantCode) {
      setErrors((prev) => ({ ...prev, tenantCode: validateTenantCode(value) }));
    }
  };

  const handleBlur = (field: "tenantCode" | "email" | "password") => {
    setTouched((prev) => ({ ...prev, [field]: true }));
    let value = "";
    let validator;

    if (field === "tenantCode") {
      value = tenantCode;
      validator = validateTenantCode;
    } else if (field === "email") {
      value = email;
      validator = validateEmail;
    } else {
      value = password;
      validator = validatePassword;
    }

    setErrors((prev) => ({ ...prev, [field]: validator(value) }));
  };

  const handleEmailSignIn = async (e: React.FormEvent) => {
    e.preventDefault();

    const tenantError = validateTenantCode(tenantCode);
    const emailError = validateEmail(email);
    const passwordError = validatePassword(password);

    setTouched({ tenantCode: true, email: true, password: true });
    setErrors({
      tenantCode: tenantError,
      email: emailError,
      password: passwordError,
    });

    if (tenantError || emailError || passwordError) {
      toast.error(tenantError || emailError || passwordError);
      return;
    }

    setIsLoading(true);

    try {
      const data = await authApi.login(
        {
          accountName: email,
          password,
        },
        tenantCode.trim(),
      );

      toast.success(data.message || "Đăng nhập thành công!");
      router.push("/dashboard");
    } catch (error: any) {
      const message =
        error?.response?.data?.message ||
        error?.message ||
        "Đăng nhập thất bại, vui lòng thử lại.";
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-linear-to-br from-background via-secondary/50 to-primary/5 flex">
      <div className="hidden lg:flex lg:w-1/2 bg-linear-to-br from-slate-900 via-slate-800 to-primary/20 dark:from-black dark:via-slate-950 dark:to-primary/10 flex-col justify-between p-12 relative overflow-hidden">
        <div className="absolute inset-0 bg-[linear-gradient(to_right,#4f4f4f2e_1px,transparent_1px),linear-gradient(to_bottom,#4f4f4f2e_1px,transparent_1px)] bg-size-[14px_24px] mask-[radial-gradient(ellipse_60%_50%_at_50%_0%,#000_70%,transparent_100%)]"></div>

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
            <CardHeader className="space-y-8 p-5 pb-0 sm:p-10 sm:pb-0">
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
                  Nhập tên tài khoản và mật khẩu để truy cập không gian làm việc
                </CardDescription>
              </div>
            </CardHeader>
            <CardContent className="p-5 sm:p-10">
              <form onSubmit={handleEmailSignIn} className="space-y-5">
                <div className="space-y-2">
                  <Label
                    htmlFor="tenantCode"
                    className="text-foreground/80 font-semibold flex items-center gap-1.5"
                  >
                    Mã công ty
                  </Label>
                  <Input
                    id="tenantCode"
                    value={tenantCode}
                    maxLength={50}
                    onChange={(e) => handleTenantCodeChange(e.target.value)}
                    onBlur={() => handleBlur("tenantCode")}
                    aria-invalid={!!errors.tenantCode}
                    className={`h-11 rounded-xl transition-colors ${
                      errors.tenantCode && touched.tenantCode
                        ? "border-destructive focus-visible:ring-destructive/30"
                        : ""
                    }`}
                  />
                  {errors.tenantCode && touched.tenantCode && (
                    <p className="text-destructive text-xs font-medium mt-1.5 animate-in fade-in slide-in-from-top-1 duration-200">
                      {errors.tenantCode}
                    </p>
                  )}
                </div>
                <div className="space-y-2">
                  <Label
                    htmlFor="email"
                    className="text-foreground/80 font-semibold"
                  >
                    Tên tài khoản
                  </Label>
                  <Input
                    id="email"
                    value={email}
                    maxLength={255}
                    onChange={(e) => handleEmailChange(e.target.value)}
                    onBlur={() => handleBlur("email")}
                    aria-invalid={!!errors.email}
                    aria-describedby={errors.email ? "email-error" : undefined}
                    className={`h-11 rounded-xl transition-colors ${
                      errors.email && touched.email
                        ? "border-destructive focus-visible:ring-destructive/30"
                        : ""
                    }`}
                  />
                  {errors.email && touched.email && (
                    <p
                      id="email-error"
                      className="text-destructive text-xs font-medium mt-1.5 animate-in fade-in slide-in-from-top-1 duration-200"
                    >
                      {errors.email}
                    </p>
                  )}
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
                      onChange={(e) => handlePasswordChange(e.target.value)}
                      onBlur={() => handleBlur("password")}
                      aria-invalid={!!errors.password}
                      aria-describedby={
                        errors.password ? "password-error" : undefined
                      }
                      className={`h-11 rounded-xl pr-24 transition-colors ${
                        errors.password && touched.password
                          ? "border-destructive focus-visible:ring-destructive/30"
                          : ""
                      }`}
                    />
                    <button
                      type="button"
                      onClick={() => setShowPassword(!showPassword)}
                      className="absolute right-18 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors focus:outline-none"
                      aria-label={
                        showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"
                      }
                    >
                      {showPassword ? (
                        <EyeOff className="w-4 h-4" />
                      ) : (
                        <Eye className="w-4 h-4" />
                      )}
                    </button>
                  </div>
                  {errors.password && touched.password && (
                    <p
                      id="password-error"
                      className="text-destructive text-xs font-medium mt-1.5 animate-in fade-in slide-in-from-top-1 duration-200"
                    >
                      {errors.password}
                    </p>
                  )}
                </div>

                {/* <div className="flex items-center space-x-2.5">
                  <Checkbox id="remember-me" />
                  <Label
                    htmlFor="remember-me"
                    className="text-sm font-medium leading-none cursor-pointer text-muted-foreground peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                  >
                    Ghi nhớ đăng nhập
                  </Label>
                </div> */}

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

            <CardFooter className="p-5 pt-0 sm:p-10 sm:pt-0">
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
