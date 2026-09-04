"use client";

import { Banknote, Coins, CreditCard, DollarSign, WalletCards } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import type { DashboardCurrencyAmount } from "@/services/dashboard-api";

interface CurrencyAmountCardsProps {
  items: DashboardCurrencyAmount[];
}

function getCurrencyBadge(currency: string) {
  const code = currency.toUpperCase();
  switch (code) {
    case "VND":
      return {
        symbol: "₫",
        badgeClass: "bg-emerald-500/10 text-emerald-600 border-emerald-500/20 dark:bg-emerald-500/15 dark:text-emerald-400",
        icon: Banknote,
      };
    case "USD":
      return {
        symbol: "$",
        badgeClass: "bg-blue-500/10 text-blue-600 border-blue-500/20 dark:bg-blue-500/15 dark:text-blue-400",
        icon: DollarSign,
      };
    case "EUR":
      return {
        symbol: "€",
        badgeClass: "bg-indigo-500/10 text-indigo-600 border-indigo-500/20 dark:bg-indigo-500/15 dark:text-indigo-400",
        icon: CreditCard,
      };
    default:
      return {
        symbol: code,
        badgeClass: "bg-slate-500/10 text-slate-600 border-slate-500/20 dark:bg-slate-500/15 dark:text-slate-400",
        icon: Coins,
      };
  }
}

export function CurrencyAmountCards({ items }: CurrencyAmountCardsProps) {
  return (
    <Card className="rounded-2xl border bg-card shadow-sm">
      <CardHeader className="flex flex-row items-center justify-between pb-3">
        <div className="space-y-1">
          <CardTitle className="flex items-center gap-2.5 text-base font-semibold">
            <div className="flex size-8 items-center justify-center rounded-lg bg-primary/10 text-primary">
              <WalletCards className="size-4" />
            </div>
            <span>Giá trị hợp đồng theo tiền tệ</span>
          </CardTitle>
          <p className="text-xs text-muted-foreground">
            Tổng giá trị các hợp đồng phát sinh hoặc có hiệu lực theo từng loại tiền tệ
          </p>
        </div>
        {items.length > 0 && (
          <Badge variant="outline" className="text-xs font-normal">
            {items.length} loại tiền
          </Badge>
        )}
      </CardHeader>
      <CardContent>
        {items.length === 0 ? (
          <div className="flex flex-col items-center justify-center py-8 text-center">
            <div className="flex size-12 items-center justify-center rounded-2xl bg-muted/60 text-muted-foreground">
              <Coins className="size-6 stroke-[1.5]" />
            </div>
            <p className="mt-3 text-sm font-medium text-muted-foreground">
              Chưa có giá trị hợp đồng trong khoảng thời gian đã chọn.
            </p>
          </div>
        ) : (
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            {items.map((item) => {
              const { symbol, badgeClass, icon: CurrencyIcon } = getCurrencyBadge(item.currency);
              const formattedAmount = new Intl.NumberFormat("vi-VN", {
                maximumFractionDigits: 2,
              }).format(item.amount);

              return (
                <div
                  key={item.currency}
                  className="group relative overflow-hidden rounded-xl border bg-gradient-to-br from-card to-muted/20 p-4 transition-all duration-200 hover:-translate-y-0.5 hover:border-primary/40 hover:shadow-md"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-xs font-medium text-muted-foreground">
                      Tiền tệ
                    </span>
                    <Badge
                      variant="outline"
                      className={`flex items-center gap-1 font-semibold ${badgeClass}`}
                    >
                      <CurrencyIcon className="size-3" />
                      <span>{item.currency}</span>
                    </Badge>
                  </div>
                  <div className="mt-3">
                    <div className="flex items-baseline gap-1.5">
                      <span className="text-2xl font-bold tracking-tight text-foreground">
                        {formattedAmount}
                      </span>
                      <span className="text-sm font-semibold text-muted-foreground">
                        {symbol}
                      </span>
                    </div>
                  </div>
                  {/* Subtle accent bar */}
                  <div className="mt-3 h-1 w-full overflow-hidden rounded-full bg-muted/50">
                    <div className="h-full w-2/3 rounded-full bg-primary/40 transition-all duration-500 group-hover:w-full group-hover:bg-primary" />
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
