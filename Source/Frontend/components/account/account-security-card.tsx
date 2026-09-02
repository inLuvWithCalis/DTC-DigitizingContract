import Link from "next/link";
import {
  ArrowRight,
  CheckCircle2,
  KeyRound,
  Lock,
  ShieldAlert,
  ShieldCheck,
  Sparkles,
} from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";

interface AccountSecurityCardProps {
  mustChangePassword: boolean;
  passwordChangedAt: string | null;
}

export function AccountSecurityCard({
  mustChangePassword,
  passwordChangedAt,
}: AccountSecurityCardProps) {
  const formattedDate = formatDateTime(passwordChangedAt);

  return (
    <Card className="border-border/80 shadow-sm transition-all duration-200 hover:shadow-md gap-0">
      <CardHeader className="pb-4">
        <div className="flex items-center justify-between gap-2">
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div
              className={`flex size-9 items-center justify-center rounded-lg ${
                mustChangePassword
                  ? "bg-amber-500/10 text-amber-600 dark:bg-amber-500/20 dark:text-amber-400"
                  : "bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/20 dark:text-emerald-400"
              }`}
            >
              {mustChangePassword ? (
                <ShieldAlert className="size-5" />
              ) : (
                <ShieldCheck className="size-5" />
              )}
            </div>
            <span>Bảo mật tài khoản</span>
          </CardTitle>
          <Badge
            variant={mustChangePassword ? "destructive" : "secondary"}
            className="px-2.5 py-1 text-xs font-medium"
          >
            {mustChangePassword ? "Cần đổi mật khẩu" : "Đã kích hoạt"}
          </Badge>
        </div>
        <CardDescription className="text-xs text-muted-foreground pt-1">
          Quản lý quyền truy cập và an toàn thông tin tài khoản của bạn
        </CardDescription>
      </CardHeader>

      <CardContent className="space-y-4 pt-0">
        {/* Status Box */}
        <div
          className={`rounded-xl border p-3.5 text-sm transition-colors ${
            mustChangePassword
              ? "border-amber-200 bg-amber-50/70 text-amber-900 dark:border-amber-900/50 dark:bg-amber-950/30 dark:text-amber-200"
              : "border-border/60 bg-muted/40 text-foreground"
          }`}
        >
          <div className="flex items-start gap-2.5">
            <Lock className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
            <div className="space-y-1">
              <div className="font-medium text-xs sm:text-sm">
                {mustChangePassword
                  ? "Mật khẩu tạm thời hoặc đã hết hạn"
                  : "Mật khẩu đang hoạt động bình thường"}
              </div>
              <div className="text-xs text-muted-foreground">
                Lần đổi gần nhất:{" "}
                <span className="font-medium text-foreground">
                  {formattedDate}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Security Recommendations */}
        {/* <div className="space-y-2 rounded-xl bg-card border border-border/60 p-3.5 text-xs text-muted-foreground">
          <div className="flex items-center gap-1.5 font-medium text-foreground text-xs pb-1">
            <Sparkles className="size-3.5 text-primary" />
            <span>Khuyến nghị bảo mật</span>
          </div>
          <ul className="space-y-2">
            <li className="flex items-start gap-2">
              <CheckCircle2 className="mt-0.5 size-3.5 shrink-0 text-emerald-500" />
              <span>
                Sử dụng ít nhất 12 ký tự kết hợp chữ hoa, chữ thường và số.
              </span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="mt-0.5 size-3.5 shrink-0 text-emerald-500" />
              <span>Đổi mật khẩu định kỳ để nâng cao tính bảo mật.</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="mt-0.5 size-3.5 shrink-0 text-emerald-500" />
              <span>
                Mọi phiên đăng nhập cũ sẽ tự động hết hạn khi đổi mật khẩu.
              </span>
            </li>
          </ul>
        </div> */}
      </CardContent>

      <CardFooter className="pt-0">
        <Button
          asChild
          variant={mustChangePassword ? "default" : "outline"}
          className={`w-full group justify-between mt-6 ${
            mustChangePassword
              ? "bg-amber-600 hover:bg-amber-700 text-white dark:bg-amber-600 dark:hover:bg-amber-700"
              : ""
          }`}
        >
          <Link href="/change-password">
            <span className="flex items-center gap-2">
              <KeyRound className="size-4" />
              <span>Đổi mật khẩu</span>
            </span>
            <ArrowRight className="size-4 transition-transform group-hover:translate-x-1" />
          </Link>
        </Button>
      </CardFooter>
    </Card>
  );
}

function formatDateTime(value: string | null) {
  if (!value) return "Chưa có thông tin";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "Chưa có thông tin";

  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
