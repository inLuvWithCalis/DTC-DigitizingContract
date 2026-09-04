"use client";

import Link from "next/link";
import {
  AlertTriangle,
  ArrowUpRight,
  CalendarClock,
  CheckCircle2,
  ChevronRight,
  Clock,
  User,
} from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { ExpiringContract } from "@/services/dashboard-api";
import { cn } from "@/lib/utils";

interface ExpiringContractsProps {
  items: ExpiringContract[];
}

function getUrgencyMeta(expiresAt: string) {
  const expiryDate = new Date(expiresAt);
  const now = new Date();
  const diffTime = expiryDate.getTime() - now.getTime();
  const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));

  if (diffDays < 0) {
    return {
      label: `Quá hạn ${Math.abs(diffDays)} ngày`,
      badgeClass:
        "bg-destructive text-destructive-foreground border-destructive animate-pulse",
      icon: AlertTriangle,
    };
  }
  if (diffDays === 0) {
    return {
      label: "Hết hạn hôm nay",
      badgeClass:
        "bg-rose-500/15 text-rose-600 border-rose-500/30 dark:text-rose-400 font-bold animate-pulse",
      icon: Clock,
    };
  }
  if (diffDays <= 7) {
    return {
      label: `Còn ${diffDays} ngày`,
      badgeClass:
        "bg-rose-500/10 text-rose-600 border-rose-500/20 dark:text-rose-400 font-semibold",
      icon: Clock,
    };
  }
  if (diffDays <= 14) {
    return {
      label: `Còn ${diffDays} ngày`,
      badgeClass:
        "bg-amber-500/10 text-amber-600 border-amber-500/20 dark:text-amber-400 font-medium",
      icon: Clock,
    };
  }
  return {
    label: `Còn ${diffDays} ngày`,
    badgeClass:
      "bg-blue-500/10 text-blue-600 border-blue-500/20 dark:text-blue-400 font-medium",
    icon: Clock,
  };
}

export function ExpiringContracts({ items }: ExpiringContractsProps) {
  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between pb-3">
        <div className="space-y-1">
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-rose-500/10 text-rose-600 dark:bg-rose-500/15 dark:text-rose-400">
              <CalendarClock className="size-4" />
            </div>
            <span>Hợp đồng sắp hết hạn</span>
          </CardTitle>
          <p className="text-xs text-muted-foreground">
            Danh sách hợp đồng cần chú ý gia hạn hoặc thanh lý
          </p>
        </div>

        <div className="flex items-center gap-2">
          {items.length > 0 && (
            <Badge
              variant="outline"
              className="text-xs font-semibold text-rose-600 border-rose-500/30 bg-rose-500/10"
            >
              {items.length} cảnh báo
            </Badge>
          )}
          <Button
            variant="ghost"
            size="sm"
            asChild
            className="h-8 gap-1 text-xs text-muted-foreground hover:text-primary"
          >
            <Link href="/contracts">
              <span>Xem tất cả</span>
              <ArrowUpRight className="size-3.5" />
            </Link>
          </Button>
        </div>
      </CardHeader>

      <CardContent>
        {items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-10 text-center">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-emerald-500/10 text-emerald-600 dark:bg-emerald-500/15 dark:text-emerald-400">
              <CheckCircle2 className="size-6" />
            </div>
            <p className="mt-3 text-sm font-semibold text-foreground">
              Không có hợp đồng nào sắp hết hạn
            </p>
            <p className="mt-1 text-xs text-muted-foreground">
              Tất cả hợp đồng đều an toàn trong 30 ngày tới.
            </p>
          </div>
        ) : (
          <div className="space-y-2.5 max-h-[380px] overflow-y-auto pr-1">
            {items.map((item) => {
              const urgency = getUrgencyMeta(item.expiresAt);
              const UrgencyIcon = urgency.icon;
              const formattedDate = new Date(item.expiresAt).toLocaleDateString(
                "vi-VN",
                {
                  day: "2-digit",
                  month: "2-digit",
                  year: "numeric",
                },
              );

              return (
                <Link
                  key={item.contractId}
                  href={`/contracts/${item.contractId}`}
                  className="group flex items-center justify-between gap-3 rounded-xl border border-border/60 bg-muted/20 p-3.5 transition-all duration-200 hover:border-primary/40 hover:bg-muted/40 hover:shadow-sm"
                >
                  <div className="min-w-0 flex-1 space-y-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="font-mono text-xs font-bold text-primary">
                        {item.contractCode}
                      </span>
                      <Badge
                        variant="outline"
                        className={cn(
                          "flex items-center gap-1 text-[11px] py-0 px-2",
                          urgency.badgeClass,
                        )}
                      >
                        <UrgencyIcon className="size-2.5" />
                        <span>{urgency.label}</span>
                      </Badge>
                    </div>

                    <p className="truncate text-sm font-medium text-foreground transition-colors group-hover:text-primary">
                      {item.contractName}
                    </p>

                    <div className="flex flex-wrap items-center gap-3 text-xs text-muted-foreground">
                      <span>
                        Hết hạn:{" "}
                        <span className="font-medium text-foreground">
                          {formattedDate}
                        </span>
                      </span>
                      {item.responsibleEmployeeName && (
                        <span className="flex items-center gap-1">
                          <User className="size-3 text-muted-foreground" />
                          <span>{item.responsibleEmployeeName}</span>
                        </span>
                      )}
                    </div>
                  </div>

                  <div className="flex size-8 shrink-0 items-center justify-center rounded-lg text-muted-foreground transition-colors group-hover:bg-primary/10 group-hover:text-primary">
                    <ChevronRight className="size-4 transition-transform group-hover:translate-x-0.5" />
                  </div>
                </Link>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
