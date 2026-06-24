"use client";

import { Button } from "@/components/ui/button";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Calendar } from "@/components/ui/calendar";
import { CalendarDays, Delete } from "lucide-react";
import { format } from "date-fns";
import { cn } from "@/lib/utils";

export interface DateRange {
  from: Date | undefined;
  to: Date | undefined;
}

interface DateRangeFilterProps {
  dateRange: DateRange;
  onChange: (range: DateRange) => void;
}

export function DateRangeFilter({ dateRange, onChange }: DateRangeFilterProps) {
  return (
    <div className="flex items-center gap-2">
      <Popover>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            className={cn(
              "justify-start text-left font-normal h-9 bg-background shadow-sm",
              !dateRange.from && "text-muted-foreground",
            )}
          >
            <CalendarDays className="mr-2 h-4 w-4" />
            {dateRange.from ? (
              format(dateRange.from, "dd/MM/yyyy")
            ) : (
              <span>Từ ngày</span>
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            mode="single"
            selected={dateRange.from}
            onSelect={(date) => onChange({ ...dateRange, from: date })}
            initialFocus
          />
        </PopoverContent>
      </Popover>

      <span className="text-muted-foreground">-</span>

      <Popover>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            className={cn(
              "justify-start text-left font-normal h-9 bg-background shadow-sm",
              !dateRange.to && "text-muted-foreground",
            )}
          >
            <CalendarDays className="mr-2 h-4 w-4" />
            {dateRange.to ? (
              format(dateRange.to, "dd/MM/yyyy")
            ) : (
              <span>Đến ngày</span>
            )}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            mode="single"
            selected={dateRange.to}
            onSelect={(date) => onChange({ ...dateRange, to: date })}
            initialFocus
            disabled={(date) =>
              dateRange.from ? date < dateRange.from : false
            }
          />
        </PopoverContent>
      </Popover>

      {(dateRange.from || dateRange.to) && (
        <Button
          variant="ghost"
          size="sm"
          className="h-8 text-xs text-muted-foreground px-2 hover:bg-destructive/10 hover:text-destructive"
          onClick={() => onChange({ from: undefined, to: undefined })}
        >
          <Delete className="h-4 w-4" />
        </Button>
      )}
    </div>
  );
}
