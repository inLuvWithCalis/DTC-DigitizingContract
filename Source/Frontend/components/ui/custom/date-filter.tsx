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
import { useMediaQuery } from "@/hooks/use-media-query";

interface DateFilterProps {
  date: Date | undefined;
  onChange: (date: Date | undefined) => void;
  id?: string;
  placeholder?: string;
  className?: string;
  disabled?: boolean;
}

export function DateFilter({
  date,
  onChange,
  id,
  placeholder = "Ngày tạo",
  className = "w-[160px]",
  disabled = false,
}: DateFilterProps) {
  const isMobile = useMediaQuery("(max-width: 767px)");

  return (
    <div className={`flex items-center gap-1 ${isMobile ? "w-full" : ""}`}>
      <Popover>
        <PopoverTrigger asChild>
          <Button
            id={id}
            type="button"
            variant="outline"
            disabled={disabled}
            className={cn(
              "justify-start text-left font-normal h-9 bg-background shadow-sm cursor-pointer",
              isMobile ? "flex-1 w-full" : className,
              !date && "text-muted-foreground",
            )}
          >
            <CalendarDays className="mr-2 h-4 w-4 text-muted-foreground shrink-0" />
            <span className="truncate">
              {date ? format(date, "dd/MM/yyyy") : placeholder}
            </span>
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            mode="single"
            selected={date}
            onSelect={onChange}
            initialFocus
          />
        </PopoverContent>
      </Popover>

      {date && (
        <Button
          type="button"
          disabled={disabled}
          variant="ghost"
          size="sm"
          className="h-9 w-9 p-0 text-muted-foreground hover:bg-destructive/10 hover:text-destructive shrink-0"
          onClick={() => onChange(undefined)}
          title="Xóa lọc ngày"
        >
          <Delete className="h-4 w-4" />
        </Button>
      )}
    </div>
  );
}
