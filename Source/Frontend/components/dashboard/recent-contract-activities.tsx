"use client";

import Link from "next/link";
import {
  Activity,
  ArrowUpRight,
  CheckCircle2,
  ChevronRight,
  CreditCard,
  FileCheck,
  FileEdit,
  FilePlus,
  History,
  MessageSquare,
  PenTool,
  RotateCcw,
  ShieldCheck,
  User,
  XCircle,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import type { RecentContractActivity } from "@/services/dashboard-api";
import { CONTRACT_AUDIT_ACTION_LABELS } from "@/services/contract-audit-api";
import { cn } from "@/lib/utils";

interface RecentContractActivitiesProps {
  items: RecentContractActivity[];
}

function getActionMeta(action: string) {
  const act = action.toLowerCase();
  if (act.includes("signed") || act.includes("signature")) {
    return {
      icon: PenTool,
      className:
        "bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400 border-emerald-500/20",
    };
  }
  if (act.includes("approved") || act.includes("completed")) {
    return {
      icon: CheckCircle2,
      className:
        "bg-blue-500/10 text-blue-600 dark:bg-blue-500/15 dark:text-blue-400 border-blue-500/20",
    };
  }
  if (
    act.includes("rejected") ||
    act.includes("voided") ||
    act.includes("failed")
  ) {
    return {
      icon: XCircle,
      className:
        "bg-rose-500/10 text-rose-600 dark:bg-rose-500/15 dark:text-rose-400 border-rose-500/20",
    };
  }
  if (
    act.includes("returned") ||
    act.includes("withdrawn") ||
    act.includes("reopened")
  ) {
    return {
      icon: RotateCcw,
      className:
        "bg-amber-500/10 text-amber-600 dark:bg-amber-500/15 dark:text-amber-400 border-amber-500/20",
    };
  }
  if (act.includes("created")) {
    return {
      icon: FilePlus,
      className:
        "bg-indigo-500/10 text-indigo-600 dark:bg-indigo-500/15 dark:text-indigo-400 border-indigo-500/20",
    };
  }
  if (
    act.includes("updated") ||
    act.includes("assigned") ||
    act.includes("transferred")
  ) {
    return {
      icon: FileEdit,
      className:
        "bg-violet-500/10 text-violet-600 dark:bg-violet-500/15 dark:text-violet-400 border-violet-500/20",
    };
  }
  if (act.includes("payment")) {
    return {
      icon: CreditCard,
      className:
        "bg-teal-500/10 text-teal-600 dark:bg-teal-500/15 dark:text-teal-400 border-teal-500/20",
    };
  }
  if (
    act.includes("negotiation") ||
    act.includes("comment") ||
    act.includes("feedback")
  ) {
    return {
      icon: MessageSquare,
      className:
        "bg-purple-500/10 text-purple-600 dark:bg-purple-500/15 dark:text-purple-400 border-purple-500/20",
    };
  }
  if (act.includes("otp") || act.includes("session") || act.includes("link")) {
    return {
      icon: ShieldCheck,
      className:
        "bg-cyan-500/10 text-cyan-600 dark:bg-cyan-500/15 dark:text-cyan-400 border-cyan-500/20",
    };
  }
  return {
    icon: Activity,
    className:
      "bg-slate-500/10 text-slate-600 dark:bg-slate-500/15 dark:text-slate-400 border-slate-500/20",
  };
}

function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffSec = Math.floor(diffMs / 1000);
  const diffMin = Math.floor(diffSec / 60);
  const diffHours = Math.floor(diffMin / 60);
  const diffDays = Math.floor(diffHours / 24);

  if (diffSec < 60) return "Vừa xong";
  if (diffMin < 60) return `${diffMin} phút trước`;
  if (diffHours < 24) return `${diffHours} giờ trước`;
  if (diffDays === 1)
    return `Hôm qua lúc ${date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}`;
  if (diffDays < 7) return `${diffDays} ngày trước`;
  return date.toLocaleDateString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function RecentContractActivities({
  items,
}: RecentContractActivitiesProps) {
  const actionLabels = CONTRACT_AUDIT_ACTION_LABELS as Record<string, string>;

  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between pb-3">
        <div className="space-y-1">
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <History className="size-4" />
            </div>
            <span>Hoạt động gần đây</span>
          </CardTitle>
          <p className="text-xs text-muted-foreground">
            Dòng thời gian các thao tác và biến động trạng thái mới nhất
          </p>
        </div>

        <Button
          variant="ghost"
          size="sm"
          asChild
          className="h-8 gap-1 text-xs text-muted-foreground hover:text-primary"
        >
          <Link href="/contract-audits">
            <span>Xem nhật ký</span>
            <ArrowUpRight className="size-3.5" />
          </Link>
        </Button>
      </CardHeader>

      <CardContent>
        {items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-10 text-center">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60 text-muted-foreground">
              <Activity className="size-6 stroke-[1.5]" />
            </div>
            <p className="mt-3 text-sm font-semibold text-foreground">
              Chưa có hoạt động nào
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Các sự kiện thao tác hợp đồng sẽ được ghi nhận tại đây.
            </p>
          </div>
        ) : (
          <div className="space-y-3 max-h-100 overflow-y-auto pr-1">
            {items.map((item) => {
              const meta = getActionMeta(item.action);
              const ActionIcon = meta.icon;
              const actionName = actionLabels[item.action] ?? item.action;
              const relativeTime = formatRelativeTime(item.occurredAt);

              return (
                <Link
                  key={item.auditId}
                  href={`/contracts/${item.contractId}`}
                  className="group relative flex items-start gap-3.5 rounded-xl border border-border/50 bg-muted/20 p-3 pl-3.5 transition-all duration-200 hover:border-primary/40 hover:bg-muted/40 hover:shadow-sm"
                >
                  <span
                    className={cn(
                      "relative z-10 flex size-8 shrink-0 items-center justify-center rounded-full border shadow-2xs transition-transform group-hover:scale-110",
                      meta.className,
                    )}
                  >
                    <ActionIcon className="size-3.5" />
                  </span>

                  <div className="min-w-0 flex-1 space-y-1">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                      <span className="font-semibold text-xs text-foreground group-hover:text-primary transition-colors">
                        {actionName}
                      </span>
                      <span className="text-[11px] font-medium text-muted-foreground shrink-0">
                        {relativeTime}
                      </span>
                    </div>

                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      <span className="font-mono text-xs font-bold text-primary">
                        {item.contractCode}
                      </span>
                      <span className="text-muted-foreground">•</span>
                      <span className="flex items-center gap-1 text-muted-foreground truncate">
                        <User className="size-3" />
                        <span>{item.actorDisplayName || "Hệ thống"}</span>
                      </span>
                    </div>
                  </div>

                  <ChevronRight className="size-4 text-muted-foreground shrink-0 self-center transition-transform group-hover:translate-x-0.5 group-hover:text-primary" />
                </Link>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
