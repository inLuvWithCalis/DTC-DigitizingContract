"use client";

import { ReactNode } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { cn } from "@/lib/utils";
import { useMediaQuery } from "@/hooks/use-media-query";

export interface SummaryCardItem {
  title: string;
  value: string | number;
  icon: ReactNode;
  iconWrapperClassName?: string; // Dùng để đổi màu nền/chữ cho icon
  valueClassName?: string; // Dùng để chỉnh size chữ nếu cần
}

interface SummaryCardsProps {
  items: SummaryCardItem[];
}

export function SummaryCards({ items }: SummaryCardsProps) {
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
