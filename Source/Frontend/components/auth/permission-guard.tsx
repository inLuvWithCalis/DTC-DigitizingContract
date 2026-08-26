"use client";

import { useState, type ReactNode } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import {
  ArrowLeft,
  Check,
  Copy,
  HelpCircle,
  Home,
  KeyRound,
  Lock,
  ShieldAlert,
  UserCheck,
} from "lucide-react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useAuthStore } from "@/hooks/use-auth-store";
import { usePermission } from "@/hooks/use-permission";
import { RBAC_PERMISSIONS, type RbacPermission } from "@/lib/rbac";
import { cn } from "@/lib/utils";

const PERMISSION_METADATA: Record<string, { title: string; category: string }> =
  {
    [RBAC_PERMISSIONS.employeeDirectoryRead]: {
      title: "Xem danh bạ nhân sự",
      category: "Nhân sự",
    },
    [RBAC_PERMISSIONS.employeeManage]: {
      title: "Quản lý nhân viên & tài khoản",
      category: "Nhân sự",
    },
    [RBAC_PERMISSIONS.departmentManage]: {
      title: "Quản lý cơ cấu phòng ban",
      category: "Tổ chức",
    },
    [RBAC_PERMISSIONS.catalogRead]: {
      title: "Xem danh mục hàng hóa / dịch vụ",
      category: "Danh mục",
    },
    [RBAC_PERMISSIONS.catalogManage]: {
      title: "Quản lý danh mục hàng hóa / dịch vụ",
      category: "Danh mục",
    },
    [RBAC_PERMISSIONS.customerLookup]: {
      title: "Tra cứu khách hàng",
      category: "Khách hàng",
    },
    [RBAC_PERMISSIONS.customerManage]: {
      title: "Quản lý dữ liệu khách hàng",
      category: "Khách hàng",
    },
    [RBAC_PERMISSIONS.quotationManage]: {
      title: "Quản lý báo giá",
      category: "Kinh doanh",
    },
    [RBAC_PERMISSIONS.contractCreate]: {
      title: "Khởi tạo hợp đồng mới",
      category: "Hợp đồng",
    },
    [RBAC_PERMISSIONS.contractReadOwn]: {
      title: "Xem hợp đồng cá nhân",
      category: "Hợp đồng",
    },
    [RBAC_PERMISSIONS.contractReadTenant]: {
      title: "Xem toàn bộ hợp đồng doanh nghiệp",
      category: "Hợp đồng",
    },
    [RBAC_PERMISSIONS.contractManageOwn]: {
      title: "Quản lý hợp đồng cá nhân",
      category: "Hợp đồng",
    },
    [RBAC_PERMISSIONS.contractSupport]: {
      title: "Hỗ trợ xử lý hợp đồng",
      category: "Hợp đồng",
    },
    [RBAC_PERMISSIONS.templateAvailableRead]: {
      title: "Xem mẫu hợp đồng",
      category: "Biểu mẫu",
    },
    [RBAC_PERMISSIONS.templateManage]: {
      title: "Quản lý mẫu hợp đồng & thiết lập",
      category: "Biểu mẫu",
    },
    [RBAC_PERMISSIONS.contractAuditReadOwn]: {
      title: "Xem nhật ký hợp đồng cá nhân",
      category: "Kiểm toán",
    },
    [RBAC_PERMISSIONS.contractAuditReadTenant]: {
      title: "Xem nhật ký kiểm toán hợp đồng",
      category: "Kiểm toán",
    },
    [RBAC_PERMISSIONS.securityAuditReadTenant]: {
      title: "Xem nhật ký bảo mật hệ thống",
      category: "Bảo mật",
    },
    [RBAC_PERMISSIONS.tenantLegalProfileManage]: {
      title: "Quản lý hồ sơ pháp lý doanh nghiệp",
      category: "Tổ chức",
    },
    [RBAC_PERMISSIONS.fileAccessByResource]: {
      title: "Truy cập tệp tin đính kèm",
      category: "Tài liệu",
    },
  };

export interface PermissionGuardProps {
  /**
   * Quyền RBAC cần kiểm tra
   */
  permission: RbacPermission;
  /**
   * Nội dung được render khi người dùng có quyền
   */
  children: ReactNode;
  /**
   * Tùy chọn giao diện hiển thị: 'full-page' (mặc định cho trang), 'card' (dạng thẻ), 'inline' (gọn)
   */
  variant?: "full-page" | "card" | "inline";
  /**
   * Tiêu đề tùy chỉnh cho màn hình từ chối quyền
   */
  title?: string;
  /**
   * Mô tả tùy chỉnh
   */
  description?: string;
  /**
   * Component fallback tùy chỉnh nếu không muốn dùng UI mặc định
   */
  fallback?: ReactNode;
  /**
   * Cho phép ẩn/hiện nút Quay lại
   */
  showBackButton?: boolean;
  /**
   * Cho phép ẩn/hiện nút Trang chủ
   */
  showHomeButton?: boolean;
  /**
   * Class name bổ sung cho container
   */
  className?: string;
}

export function PermissionGuard({
  permission,
  children,
  variant = "full-page",
  title,
  description,
  fallback,
  showBackButton = true,
  showHomeButton = true,
  className,
}: PermissionGuardProps) {
  const { can } = usePermission();
  const user = useAuthStore((state) => state.user);
  const router = useRouter();
  const [copied, setCopied] = useState(false);

  if (can(permission)) return children;

  if (fallback) return fallback;

  const permMeta = PERMISSION_METADATA[permission];
  const permissionTitle =
    permMeta?.title || "Thao tác trên phân hệ được bảo vệ";
  const permissionCategory = permMeta?.category;

  const handleCopyCode = async () => {
    try {
      await navigator.clipboard.writeText(permission);
      setCopied(true);
      toast.success("Đã sao chép mã quyền vào clipboard");
      setTimeout(() => setCopied(false), 2000);
    } catch {
      toast.error("Không thể sao chép mã quyền");
    }
  };

  // Dạng Inline: Gọn gàng, thích hợp đặt bên trong bảng, form, dialog hoặc component nhỏ
  if (variant === "inline") {
    return (
      <div
        className={cn(
          "flex items-center justify-between gap-3 rounded-xl border border-amber-500/30 bg-amber-500/10 p-3.5 text-sm text-amber-950 dark:text-amber-200",
          className,
        )}
      >
        <div className="flex items-center gap-3 min-w-0">
          <ShieldAlert className="size-5 shrink-0 text-amber-600 dark:text-amber-400" />
          <div className="min-w-0">
            <p className="font-semibold leading-tight">
              {title || "Không có quyền thực hiện thao tác"}
            </p>
            <p className="text-xs text-muted-foreground line-clamp-1 mt-0.5">
              {description ||
                `Yêu cầu quyền: ${permissionTitle} (${permission})`}
            </p>
          </div>
        </div>

        <button
          type="button"
          onClick={handleCopyCode}
          className="inline-flex shrink-0 items-center gap-1 rounded-md border border-amber-500/30 bg-background/80 px-2 py-1 text-xs font-medium text-foreground shadow-xs transition-colors hover:bg-background"
          title="Sao chép mã quyền"
        >
          {copied ? (
            <>
              <Check className="size-3.5 text-emerald-600 dark:text-emerald-400" />
              <span>Đã chép</span>
            </>
          ) : (
            <>
              <Copy className="size-3.5 text-muted-foreground" />
              <span>{permission}</span>
            </>
          )}
        </button>
      </div>
    );
  }

  // Dạng Card / Full-page: Thiết kế hiện đại, sang trọng, có chiều sâu thị giác
  return (
    <div
      className={cn(
        "relative flex min-w-0 grow flex-col items-center justify-center overflow-y-auto px-4 py-8 sm:px-6 lg:px-8",
        variant === "full-page" && "min-h-[75vh]",
        className,
      )}
    >
      {/* Hiệu ứng ánh sáng nền ambient blur */}
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-0 -z-10 flex items-center justify-center overflow-hidden"
      >
        <div className="h-96 w-96 rounded-full bg-amber-500/10 blur-3xl dark:bg-amber-500/10" />
        <div className="h-80 w-80 translate-x-1/3 -translate-y-1/4 rounded-full bg-primary/10 blur-3xl dark:bg-primary/10" />
      </div>

      <div className="w-full max-w-xl">
        {/* Khung Card chính */}
        <div className="relative overflow-hidden rounded-2xl border border-border/80 bg-card/95 p-6 shadow-2xl backdrop-blur-xl sm:p-8">
          {/* Dải màu gradient viền trên tạo điểm nhấn */}
          <div className="absolute inset-x-0 top-0 h-1 bg-gradient-to-r from-amber-500 via-orange-500 to-rose-500" />

          {/* Header & Biểu tượng */}
          <div className="flex flex-col items-center text-center">
            {/* Hộp biểu tượng có viền tỏa sáng và huy hiệu khóa */}
            <div className="relative mb-5 flex items-center justify-center">
              <div className="absolute size-20 rounded-full bg-amber-500/15 blur-md dark:bg-amber-500/20" />
              <div className="relative flex size-16 items-center justify-center rounded-2xl border border-amber-500/30 bg-gradient-to-b from-amber-500/20 to-amber-500/5 text-amber-600 shadow-inner dark:text-amber-400">
                <ShieldAlert className="size-8" />
                <div className="absolute -bottom-1 -right-1 flex size-6 items-center justify-center rounded-full border-2 border-card bg-amber-600 text-white shadow-sm dark:bg-amber-500 dark:text-slate-950">
                  <Lock className="size-3" />
                </div>
              </div>
            </div>

            {/* Huy hiệu mã lỗi 403 */}
            <Badge
              variant="outline"
              className="mb-3 border-amber-500/30 bg-amber-500/10 px-3 py-1 text-xs font-semibold text-amber-700 dark:text-amber-300"
            >
              403 • Quyền truy cập bị hạn chế
            </Badge>

            {/* Tiêu đề */}
            <h1 className="text-xl font-bold tracking-tight text-foreground sm:text-2xl">
              {title || "Truy cập bị từ chối"}
            </h1>

            {/* Mô tả giải thích */}
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground sm:text-base">
              {description ||
                "Tài khoản của bạn chưa được cấp quyền để xem hoặc thao tác trên phân hệ này."}
            </p>
          </div>

          {/* Khung chi tiết thông tin quyền & tài khoản */}
          <div className="mt-6 space-y-3 rounded-xl border border-border/70 bg-muted/40 p-4 text-xs sm:text-sm">
            <div className="flex items-center justify-between border-b border-border/50 pb-2.5">
              <span className="flex items-center gap-1.5 font-medium text-muted-foreground">
                <KeyRound className="size-3.5" />
                Quyền yêu cầu:
              </span>
              <div className="flex items-center gap-1.5">
                {permissionCategory && (
                  <Badge
                    variant="secondary"
                    className="h-5 px-1.5 text-[11px] font-normal"
                  >
                    {permissionCategory}
                  </Badge>
                )}
                <span className="font-semibold text-foreground">
                  {permissionTitle}
                </span>
              </div>
            </div>

            {user && (
              <div className="flex items-center justify-between border-t border-border/50 pt-2.5">
                <span className="flex items-center gap-1.5 text-muted-foreground">
                  <UserCheck className="size-3.5 text-muted-foreground" />
                  Đang đăng nhập:
                </span>
                <span className="font-medium text-foreground">
                  {user.fullName || user.account || "Tài khoản hiện tại"}{" "}
                  <span className="text-muted-foreground">
                    ({user.roleName || user.employeeType || "Thành viên"})
                  </span>
                </span>
              </div>
            )}
          </div>

          {/* Các nút hành động */}
          <div className="mt-6 flex flex-col-reverse gap-2.5 sm:flex-row sm:items-center sm:justify-end">
            {showBackButton && (
              <Button
                type="button"
                variant="outline"
                onClick={() => router.back()}
                className="w-full sm:w-auto"
              >
                <ArrowLeft className="size-4" />
                Quay lại
              </Button>
            )}

            {showHomeButton && (
              <Button asChild className="w-full sm:w-auto">
                <Link href="/dashboard">
                  <Home className="size-4" />
                  Về trang tổng quan
                </Link>
              </Button>
            )}
          </div>

          {/* Khung hướng dẫn hỗ trợ */}
          <div className="mt-6 flex items-start gap-2.5 rounded-lg border border-border/40 bg-secondary/60 p-3 text-xs text-muted-foreground">
            <HelpCircle className="mt-0.5 size-4 shrink-0 text-muted-foreground" />
            <p className="leading-relaxed">
              Nếu bạn cần truy cập chức năng này để phục vụ công việc, vui lòng
              sao chép mã quyền ở trên và gửi cho <strong>Quản trị viên</strong>{" "}
              hoặc người quản lý tổ chức để được cấp quyền.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}
