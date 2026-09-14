"use client";

import { ReactNode } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

export interface SummaryCardItem {
  title: string;
  value: string | number;
  icon: ReactNode;
  iconWrapperClassName?: string;
  valueClassName?: string;
}

interface SummaryCardsProps {
  items: SummaryCardItem[];
  isLoading?: boolean;
  count?: number;
}

export function SummaryCardsSkeleton({ count = 3 }: { count?: number }) {
  return (
    <div className="flex flex-col md:flex-row gap-4 w-full">
      {Array.from({ length: count }).map((_, index) => (
        <Card
          key={index}
          className="bg-card border-border shadow-sm flex-1 min-w-0 py-0"
        >
          <CardContent className="p-4 flex items-center gap-4">
            <Skeleton className="w-12 h-12 rounded-lg shrink-0" />
            <div className="min-w-0 flex-1 space-y-2">
              <Skeleton className="h-4 w-24" />
              <Skeleton className="h-7 w-16" />
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

export function SummaryCards({ items, isLoading, count = 3 }: SummaryCardsProps) {
  if (isLoading) {
    return <SummaryCardsSkeleton count={items?.length || count} />;
  }

  if (!items?.length) return null;

  return (
    <div className="flex flex-col md:flex-row gap-4 w-full">
      {items.map((item, index) => (
        <Card
          key={index}
          className={cn("bg-card border-border shadow-sm flex-1 min-w-0 py-0")}
        >
          <CardContent className="p-4 flex items-center gap-4">
            <div
              className={cn(
                "p-3 rounded-lg flex-shrink-0",
                item.iconWrapperClassName,
              )}
            >
              {item.icon}
            </div>
            <div className="min-w-0 flex-1 break-words">
              <p className="text-sm text-muted-foreground font-medium truncate">
                {item.title}
              </p>
              <h3
                className={cn(
                  "text-2xl font-bold text-foreground truncate",
                  item.valueClassName,
                )}
              >
                {item.value}
              </h3>
            </div>
          </CardContent>
        </Card>
      ))}
    </div>
  );
}
