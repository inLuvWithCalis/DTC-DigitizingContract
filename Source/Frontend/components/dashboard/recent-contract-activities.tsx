import Link from "next/link";
import { Activity } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { RecentContractActivity } from "@/services/dashboard-api";
import { CONTRACT_AUDIT_ACTION_LABELS } from "@/services/contract-audit-api";

export function RecentContractActivities({ items }: { items: RecentContractActivity[] }) {
  const actionLabels = CONTRACT_AUDIT_ACTION_LABELS as Record<string, string>;
  return (
    <Card className="rounded-2xl">
      <CardHeader><CardTitle className="text-base">Hoạt động hợp đồng gần đây</CardTitle></CardHeader>
      <CardContent className="space-y-1">
        {items.length === 0 ? <p className="py-10 text-center text-sm text-muted-foreground">Chưa có hoạt động trong khoảng đã chọn.</p> : items.map((item) => (
          <Link key={item.auditId} href={`/contracts/${item.contractId}`} className="flex gap-3 rounded-xl p-3 transition-colors hover:bg-muted/60">
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary"><Activity className="size-4" /></span>
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-semibold">{actionLabels[item.action] ?? item.action} · {item.contractCode}</span>
              <span className="mt-1 block text-xs text-muted-foreground">{item.actorDisplayName || "Hệ thống"} · {new Date(item.occurredAt).toLocaleString("vi-VN")}</span>
            </span>
          </Link>
        ))}
      </CardContent>
    </Card>
  );
}
