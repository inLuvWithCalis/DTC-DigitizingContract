import Link from "next/link";
import { CalendarClock } from "lucide-react";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import type { ExpiringContract } from "@/services/dashboard-api";

export function ExpiringContracts({ items }: { items: ExpiringContract[] }) {
  return (
    <Card className="rounded-2xl">
      <CardHeader><CardTitle className="text-base">Hợp đồng sắp hết hạn</CardTitle></CardHeader>
      <CardContent className="space-y-1">
        {items.length === 0 ? <p className="py-10 text-center text-sm text-muted-foreground">Không có hợp đồng sắp hết hạn.</p> : items.map((item) => (
          <Link key={item.contractId} href={`/contracts/${item.contractId}`} className="flex gap-3 rounded-xl p-3 transition-colors hover:bg-muted/60">
            <span className="mt-0.5 flex size-9 shrink-0 items-center justify-center rounded-full bg-rose-500/10 text-rose-600"><CalendarClock className="size-4" /></span>
            <span className="min-w-0 flex-1">
              <span className="block truncate text-sm font-semibold">{item.contractCode} · {item.contractName}</span>
              <span className="mt-1 block text-xs text-muted-foreground">Hết hạn {new Date(item.expiresAt).toLocaleDateString("vi-VN")}{item.responsibleEmployeeName ? ` · ${item.responsibleEmployeeName}` : ""}</span>
            </span>
          </Link>
        ))}
      </CardContent>
    </Card>
  );
}
