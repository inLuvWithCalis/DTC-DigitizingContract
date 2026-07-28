"use client";

import { useMemo, useState } from "react";
import {
  Activity,
  Check,
  CheckCircle2,
  Clock3,
  Eye,
  KeyRound,
  LockKeyhole,
  Mail,
  MapPin,
  Monitor,
  Search,
  ShieldCheck,
  ShieldEllipsis,
  UnlockKeyhole,
  UserCheck,
  UserRound,
  Users,
  UserX,
  XCircle,
} from "lucide-react";
import { Header } from "@/components/ui/custom/header";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetDescription,
  SheetFooter,
  SheetHeader,
  SheetTitle,
} from "@/components/ui/sheet";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import {
  MOCK_SYSTEM_ADMINS,
  ROLE_PERMISSIONS,
  SystemAdminAccount,
  SystemAdminRole,
  SystemAdminStatus,
} from "@/services/system-admin-accounts-mock";
import { cn } from "@/lib/utils";

type StatusFilter = "All" | SystemAdminStatus;
type RoleFilter = "All" | SystemAdminRole;

const roles: SystemAdminRole[] = [
  "Super Admin",
  "Operations Admin",
  "Security Auditor",
  "Support Admin",
];

const statusConfig: Record<
  SystemAdminStatus,
  { label: string; className: string; dotClassName: string }
> = {
  Active: {
    label: "Đang hoạt động",
    className:
      "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-900 dark:bg-emerald-950/50 dark:text-emerald-300",
    dotClassName: "bg-emerald-500",
  },
  Locked: {
    label: "Đã khóa",
    className:
      "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-900 dark:bg-rose-950/50 dark:text-rose-300",
    dotClassName: "bg-rose-500",
  },
  Pending: {
    label: "Chờ kích hoạt",
    className:
      "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-900 dark:bg-amber-950/50 dark:text-amber-300",
    dotClassName: "bg-amber-500",
  },
};

const roleConfig: Record<
  SystemAdminRole,
  { className: string; shortLabel: string }
> = {
  "Super Admin": {
    className:
      "border-violet-200 bg-violet-50 text-violet-700 dark:border-violet-900 dark:bg-violet-950/50 dark:text-violet-300",
    shortLabel: "Toàn quyền",
  },
  "Operations Admin": {
    className:
      "border-blue-200 bg-blue-50 text-blue-700 dark:border-blue-900 dark:bg-blue-950/50 dark:text-blue-300",
    shortLabel: "Vận hành",
  },
  "Security Auditor": {
    className:
      "border-cyan-200 bg-cyan-50 text-cyan-700 dark:border-cyan-900 dark:bg-cyan-950/50 dark:text-cyan-300",
    shortLabel: "Kiểm toán",
  },
  "Support Admin": {
    className:
      "border-slate-200 bg-slate-50 text-slate-700 dark:border-slate-800 dark:bg-slate-900/50 dark:text-slate-300",
    shortLabel: "Hỗ trợ",
  },
};

function getInitials(fullName: string) {
  const words = fullName.trim().split(/\s+/);
  if (words.length === 1) return words[0].slice(0, 2).toUpperCase();
  return `${words[0][0]}${words[words.length - 1][0]}`.toUpperCase();
}

function AccountStatusBadge({ status }: { status: SystemAdminStatus }) {
  const config = statusConfig[status];
  return (
    <Badge
      variant="outline"
      className={cn("gap-1.5 whitespace-nowrap font-medium", config.className)}
    >
      <span className={cn("size-1.5 rounded-full", config.dotClassName)} />
      {config.label}
    </Badge>
  );
}

function RoleBadge({ role }: { role: SystemAdminRole }) {
  const config = roleConfig[role];
  return (
    <Badge
      variant="outline"
      className={cn("whitespace-nowrap font-medium", config.className)}
    >
      {role}
    </Badge>
  );
}

function MetricCard({
  title,
  value,
  description,
  icon: Icon,
  tone,
}: {
  title: string;
  value: string;
  description: string;
  icon: typeof Users;
  tone: "blue" | "emerald" | "rose" | "amber";
}) {
  const tones = {
    blue: "bg-blue-50 text-blue-600 dark:bg-blue-950/40 dark:text-blue-300",
    emerald:
      "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300",
    rose: "bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-300",
    amber:
      "bg-amber-50 text-amber-600 dark:bg-amber-950/40 dark:text-amber-300",
  };

  return (
    <Card className="gap-0 py-0 shadow-sm">
      <CardContent className="flex items-start justify-between p-5">
        <div>
          <p className="text-sm font-medium text-muted-foreground">{title}</p>
          <p className="mt-2 text-2xl font-bold tracking-tight">{value}</p>
          <p className="mt-1 text-xs text-muted-foreground">{description}</p>
        </div>
        <div
          className={cn(
            "flex size-10 items-center justify-center rounded-xl",
            tones[tone],
          )}
        >
          <Icon className="size-5" />
        </div>
      </CardContent>
    </Card>
  );
}

function AdministratorDetailSheet({
  account,
  open,
  onOpenChange,
  onRequestStatusChange,
  onRoleChange,
}: {
  account?: SystemAdminAccount;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onRequestStatusChange: (account: SystemAdminAccount) => void;
  onRoleChange: (accountId: number, role: SystemAdminRole) => void;
}) {
  if (!account) return null;

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent className="w-full overflow-hidden p-0 sm:max-w-2xl">
        <SheetHeader className="border-b px-6 py-5">
          <div className="flex items-start gap-3 pr-8">
            <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-primary/10 font-bold text-primary">
              {getInitials(account.fullName)}
            </div>
            <div className="min-w-0">
              <div className="flex flex-wrap items-center gap-2">
                <SheetTitle className="text-lg">
                  {account.fullName}
                </SheetTitle>
                <AccountStatusBadge status={account.status} />
              </div>
              <SheetDescription className="mt-1">
                @{account.username} · ID #{account.systemAdminId}
              </SheetDescription>
            </div>
          </div>
        </SheetHeader>

        <ScrollArea className="min-h-0 flex-1">
          <div className="space-y-6 p-6">
            <div className="grid grid-cols-3 gap-3">
              <div className="rounded-xl border bg-muted/20 p-4">
                <Activity className="size-4 text-muted-foreground" />
                <p className="mt-3 text-xl font-semibold">
                  {account.loginCount}
                </p>
                <p className="text-xs text-muted-foreground">Lần đăng nhập</p>
              </div>
              <div className="rounded-xl border bg-muted/20 p-4">
                <KeyRound className="size-4 text-muted-foreground" />
                <p className="mt-3 text-xl font-semibold">
                  {account.permissions.length}
                </p>
                <p className="text-xs text-muted-foreground">Quyền được cấp</p>
              </div>
              <div className="rounded-xl border bg-muted/20 p-4">
                <ShieldCheck className="size-4 text-muted-foreground" />
                <p className="mt-3 truncate text-sm font-semibold">
                  {roleConfig[account.role].shortLabel}
                </p>
                <p className="text-xs text-muted-foreground">Cấp quyền</p>
              </div>
            </div>

            <section className="space-y-3">
              <div>
                <h3 className="font-semibold">Vai trò và quyền hạn</h3>
                <p className="mt-1 text-sm text-muted-foreground">
                  Thay đổi vai trò sẽ cập nhật bộ quyền tương ứng trong mock UI.
                </p>
              </div>
              <Select
                value={account.role}
                onValueChange={(value) =>
                  onRoleChange(account.systemAdminId, value as SystemAdminRole)
                }
                disabled={account.systemAdminId === 1}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {roles.map((role) => (
                    <SelectItem key={role} value={role}>
                      {role}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {account.systemAdminId === 1 && (
                <p className="text-xs text-muted-foreground">
                  Không thể thay đổi vai trò của tài khoản quản trị gốc.
                </p>
              )}
              <div className="rounded-xl border">
                {account.permissions.map((permission, index) => (
                  <div
                    key={permission}
                    className={cn(
                      "flex items-center gap-3 px-4 py-3",
                      index > 0 && "border-t",
                    )}
                  >
                    <div className="flex size-6 shrink-0 items-center justify-center rounded-full bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300">
                      <Check className="size-3.5" />
                    </div>
                    <span className="text-sm">{permission}</span>
                  </div>
                ))}
              </div>
            </section>

            <section className="space-y-3">
              <h3 className="font-semibold">Thông tin tài khoản</h3>
              <div className="rounded-xl border">
                {[
                  { icon: Mail, label: "Email", value: account.email },
                  {
                    icon: Clock3,
                    label: "Đăng nhập gần nhất",
                    value: account.lastLoginAt,
                  },
                  {
                    icon: MapPin,
                    label: "IP gần nhất",
                    value: account.lastLoginIp,
                  },
                  {
                    icon: UserRound,
                    label: "Người tạo",
                    value: account.createdBy,
                  },
                  {
                    icon: Clock3,
                    label: "Ngày tạo",
                    value: account.createdAt,
                  },
                ].map(({ icon: Icon, label, value }, index) => (
                  <div
                    key={label}
                    className={cn(
                      "flex items-center gap-3 px-4 py-3",
                      index > 0 && "border-t",
                    )}
                  >
                    <Icon className="size-4 shrink-0 text-muted-foreground" />
                    <span className="w-36 shrink-0 text-sm text-muted-foreground">
                      {label}
                    </span>
                    <span className="min-w-0 truncate text-sm font-medium">
                      {value}
                    </span>
                  </div>
                ))}
              </div>
            </section>

            <section className="space-y-3">
              <div className="flex items-center justify-between">
                <h3 className="font-semibold">Lịch sử đăng nhập</h3>
                <span className="text-xs text-muted-foreground">
                  {account.loginHistory.length} phiên gần nhất
                </span>
              </div>

              {account.loginHistory.length ? (
                <div className="rounded-xl border">
                  {account.loginHistory.map((login, index) => (
                    <div
                      key={login.id}
                      className={cn(
                        "flex items-start gap-3 p-4",
                        index > 0 && "border-t",
                      )}
                    >
                      <div
                        className={cn(
                          "flex size-8 shrink-0 items-center justify-center rounded-lg",
                          login.result === "Success"
                            ? "bg-emerald-50 text-emerald-600 dark:bg-emerald-950/40 dark:text-emerald-300"
                            : "bg-rose-50 text-rose-600 dark:bg-rose-950/40 dark:text-rose-300",
                        )}
                      >
                        {login.result === "Success" ? (
                          <CheckCircle2 className="size-4" />
                        ) : (
                          <XCircle className="size-4" />
                        )}
                      </div>
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center justify-between gap-3">
                          <p className="text-sm font-medium">
                            {login.loggedAt}
                          </p>
                          <span
                            className={cn(
                              "text-xs font-semibold",
                              login.result === "Success"
                                ? "text-emerald-600"
                                : "text-rose-600",
                            )}
                          >
                            {login.result === "Success"
                              ? "Thành công"
                              : "Thất bại"}
                          </span>
                        </div>
                        <p className="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
                          <Monitor className="size-3" />
                          {login.device}
                        </p>
                        <p className="mt-1 flex items-center gap-1.5 text-xs text-muted-foreground">
                          <MapPin className="size-3" />
                          {login.ipAddress} · {login.location}
                        </p>
                      </div>
                    </div>
                  ))}
                </div>
              ) : (
                <div className="rounded-xl border border-dashed p-8 text-center">
                  <Clock3 className="mx-auto size-6 text-muted-foreground" />
                  <p className="mt-3 text-sm font-medium">
                    Chưa có lịch sử đăng nhập
                  </p>
                  <p className="mt-1 text-xs text-muted-foreground">
                    Tài khoản chưa được kích hoạt hoặc chưa đăng nhập lần đầu.
                  </p>
                </div>
              )}
            </section>

            <Alert>
              <ShieldEllipsis className="size-4" />
              <AlertTitle>Dữ liệu mock</AlertTitle>
              <AlertDescription>
                Vai trò, trạng thái và lịch sử đăng nhập sẽ được thay bằng API
                quản trị System Admin khi backend hỗ trợ.
              </AlertDescription>
            </Alert>
          </div>
        </ScrollArea>

        <SheetFooter className="border-t bg-background px-6 py-4">
          {account.systemAdminId !== 1 && (
            <Button
              variant={account.status === "Active" ? "destructive" : "default"}
              onClick={() => onRequestStatusChange(account)}
            >
              {account.status === "Active" ? (
                <LockKeyhole />
              ) : (
                <UnlockKeyhole />
              )}
              {account.status === "Active"
                ? "Khóa tài khoản"
                : "Kích hoạt tài khoản"}
            </Button>
          )}
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Đóng
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}

export default function AdministratorsPage() {
  const [accounts, setAccounts] =
    useState<SystemAdminAccount[]>(MOCK_SYSTEM_ADMINS);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<StatusFilter>("All");
  const [roleFilter, setRoleFilter] = useState<RoleFilter>("All");
  const [selectedAccountId, setSelectedAccountId] = useState<number>();
  const [detailOpen, setDetailOpen] = useState(false);
  const [actionTarget, setActionTarget] =
    useState<SystemAdminAccount | null>(null);
  const [actionMessage, setActionMessage] = useState("");

  const filteredAccounts = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return accounts.filter((account) => {
      const matchesSearch =
        !query ||
        account.fullName.toLowerCase().includes(query) ||
        account.username.toLowerCase().includes(query) ||
        account.email.toLowerCase().includes(query);
      const matchesStatus =
        statusFilter === "All" || account.status === statusFilter;
      const matchesRole =
        roleFilter === "All" || account.role === roleFilter;
      return matchesSearch && matchesStatus && matchesRole;
    });
  }, [accounts, roleFilter, searchQuery, statusFilter]);

  const selectedAccount = accounts.find(
    (account) => account.systemAdminId === selectedAccountId,
  );
  const activeCount = accounts.filter(
    (account) => account.status === "Active",
  ).length;
  const lockedCount = accounts.filter(
    (account) => account.status === "Locked",
  ).length;
  const failedLogins = accounts.reduce(
    (total, account) =>
      total +
      account.loginHistory.filter((login) => login.result === "Failed").length,
    0,
  );

  const openDetail = (account: SystemAdminAccount) => {
    setSelectedAccountId(account.systemAdminId);
    setDetailOpen(true);
  };

  const handleStatusChange = () => {
    if (!actionTarget) return;
    const nextStatus: SystemAdminStatus =
      actionTarget.status === "Active" ? "Locked" : "Active";
    setAccounts((current) =>
      current.map((account) =>
        account.systemAdminId === actionTarget.systemAdminId
          ? { ...account, status: nextStatus }
          : account,
      ),
    );
    setActionMessage(
      `${actionTarget.fullName} đã ${
        nextStatus === "Locked" ? "bị khóa" : "được kích hoạt"
      }.`,
    );
    setActionTarget(null);
  };

  const handleRoleChange = (accountId: number, role: SystemAdminRole) => {
    setAccounts((current) =>
      current.map((account) =>
        account.systemAdminId === accountId
          ? { ...account, role, permissions: ROLE_PERMISSIONS[role] }
          : account,
      ),
    );
    const target = accounts.find(
      (account) => account.systemAdminId === accountId,
    );
    setActionMessage(
      `Vai trò của ${target?.fullName || "quản trị viên"} đã đổi thành ${role}.`,
    );
  };

  return (
    <>
      <Header title="Quản trị viên hệ thống" />

      <div className="grow overflow-y-auto bg-muted/20">
        <div className="mx-auto w-full max-w-[1500px] space-y-6 px-4 py-6 sm:px-6 lg:px-10 lg:py-8">
          <div>
            <div className="mb-2 flex items-center gap-2 text-sm font-medium text-primary">
              <ShieldCheck className="size-4" />
              Access Management
            </div>
            <h1 className="text-2xl font-bold tracking-tight sm:text-3xl">
              Tài khoản System Admin
            </h1>
            <p className="mt-2 max-w-2xl text-sm text-muted-foreground">
              Quản lý người có quyền truy cập khu vực quản trị hệ thống, vai
              trò, trạng thái và lịch sử đăng nhập.
            </p>
          </div>

          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            <MetricCard
              title="Tổng quản trị viên"
              value={accounts.length.toString()}
              description="Tất cả tài khoản được cấp"
              icon={Users}
              tone="blue"
            />
            <MetricCard
              title="Đang hoạt động"
              value={activeCount.toString()}
              description="Có thể đăng nhập hệ thống"
              icon={UserCheck}
              tone="emerald"
            />
            <MetricCard
              title="Tài khoản bị khóa"
              value={lockedCount.toString()}
              description="Không thể đăng nhập"
              icon={UserX}
              tone="rose"
            />
            <MetricCard
              title="Đăng nhập thất bại"
              value={failedLogins.toString()}
              description="Trong lịch sử gần nhất"
              icon={XCircle}
              tone="amber"
            />
          </div>

          {actionMessage && (
            <Alert className="border-emerald-200 bg-emerald-50 text-emerald-950 dark:border-emerald-900 dark:bg-emerald-950/40 dark:text-emerald-100">
              <CheckCircle2 className="size-4" />
              <AlertTitle>Đã cập nhật</AlertTitle>
              <AlertDescription>{actionMessage}</AlertDescription>
            </Alert>
          )}

          <Card className="gap-0 overflow-hidden py-0 shadow-sm">
            <div className="flex flex-col gap-3 border-b p-4 xl:flex-row xl:items-center xl:justify-between">
              <div>
                <h2 className="font-semibold">Danh sách quản trị viên</h2>
                <p className="mt-1 text-xs text-muted-foreground">
                  Hiển thị {filteredAccounts.length} trên {accounts.length} tài
                  khoản
                </p>
              </div>
              <div className="flex flex-col gap-2 sm:flex-row">
                <div className="relative sm:w-72">
                  <Search className="absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
                  <Input
                    value={searchQuery}
                    onChange={(event) => setSearchQuery(event.target.value)}
                    placeholder="Tìm tên, username hoặc email..."
                    className="pl-9"
                  />
                </div>
                <Select
                  value={roleFilter}
                  onValueChange={(value) =>
                    setRoleFilter(value as RoleFilter)
                  }
                >
                  <SelectTrigger className="w-full sm:w-48">
                    <SelectValue placeholder="Vai trò" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="All">Tất cả vai trò</SelectItem>
                    {roles.map((role) => (
                      <SelectItem key={role} value={role}>
                        {role}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
                <Select
                  value={statusFilter}
                  onValueChange={(value) =>
                    setStatusFilter(value as StatusFilter)
                  }
                >
                  <SelectTrigger className="w-full sm:w-44">
                    <SelectValue placeholder="Trạng thái" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="All">Tất cả trạng thái</SelectItem>
                    <SelectItem value="Active">Đang hoạt động</SelectItem>
                    <SelectItem value="Locked">Đã khóa</SelectItem>
                    <SelectItem value="Pending">Chờ kích hoạt</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </div>

            <div className="overflow-x-auto">
              <Table className="min-w-[1020px]">
                <TableHeader>
                  <TableRow className="bg-muted/40 hover:bg-muted/40">
                    <TableHead className="w-[300px]">
                      Quản trị viên
                    </TableHead>
                    <TableHead>Vai trò</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead>Quyền hạn</TableHead>
                    <TableHead>Đăng nhập gần nhất</TableHead>
                    <TableHead>Số phiên</TableHead>
                    <TableHead className="text-right">Thao tác</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredAccounts.map((account) => (
                    <TableRow
                      key={account.systemAdminId}
                      className="cursor-pointer"
                      onClick={() => openDetail(account)}
                    >
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <div className="flex size-10 shrink-0 items-center justify-center rounded-xl border bg-primary/5 text-xs font-bold text-primary">
                            {getInitials(account.fullName)}
                          </div>
                          <div className="min-w-0">
                            <p className="truncate font-medium">
                              {account.fullName}
                            </p>
                            <p className="mt-0.5 truncate text-xs text-muted-foreground">
                              @{account.username} · {account.email}
                            </p>
                          </div>
                        </div>
                      </TableCell>
                      <TableCell>
                        <RoleBadge role={account.role} />
                      </TableCell>
                      <TableCell>
                        <AccountStatusBadge status={account.status} />
                      </TableCell>
                      <TableCell>
                        <p className="font-medium">
                          {account.permissions.length} quyền
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {roleConfig[account.role].shortLabel}
                        </p>
                      </TableCell>
                      <TableCell>
                        <p className="text-sm">{account.lastLoginAt}</p>
                        <p className="text-xs text-muted-foreground">
                          {account.lastLoginIp}
                        </p>
                      </TableCell>
                      <TableCell>
                        <span className="font-medium">
                          {account.loginCount.toLocaleString("vi-VN")}
                        </span>
                      </TableCell>
                      <TableCell className="text-right">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={(event) => {
                            event.stopPropagation();
                            openDetail(account);
                          }}
                        >
                          <Eye />
                          Chi tiết
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}

                  {filteredAccounts.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={7} className="h-56 text-center">
                        <Search className="mx-auto size-6 text-muted-foreground" />
                        <p className="mt-3 font-medium">
                          Không tìm thấy quản trị viên
                        </p>
                        <p className="mt-1 text-sm text-muted-foreground">
                          Thử thay đổi từ khóa hoặc bộ lọc.
                        </p>
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          </Card>
        </div>
      </div>

      <AdministratorDetailSheet
        account={selectedAccount}
        open={detailOpen}
        onOpenChange={setDetailOpen}
        onRequestStatusChange={setActionTarget}
        onRoleChange={handleRoleChange}
      />

      <AlertDialog
        open={Boolean(actionTarget)}
        onOpenChange={(open) => !open && setActionTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {actionTarget?.status === "Active"
                ? "Khóa tài khoản quản trị?"
                : "Kích hoạt tài khoản quản trị?"}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {actionTarget?.status === "Active"
                ? `${actionTarget?.fullName} sẽ bị đăng xuất và không thể truy cập khu vực System Admin.`
                : `${actionTarget?.fullName} sẽ có thể đăng nhập và sử dụng các quyền của vai trò ${actionTarget?.role}.`}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Hủy</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleStatusChange}
              className={cn(
                actionTarget?.status === "Active" &&
                  "bg-destructive text-white hover:bg-destructive/90",
              )}
            >
              {actionTarget?.status === "Active" ? (
                <LockKeyhole />
              ) : (
                <UnlockKeyhole />
              )}
              {actionTarget?.status === "Active"
                ? "Xác nhận khóa"
                : "Xác nhận kích hoạt"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </>
  );
}
