"use client";

import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useMediaQuery } from "@/hooks/use-media-query";

interface FilterOption {
  label: string;
  value: string;
}

interface SelectFilterProps {
  value: string;
  onChange: (value: string) => void;
  options: FilterOption[];
  placeholder?: string;
  className?: string;
}

export function SelectFilter({
  value,
  onChange,
  options,
  placeholder = "Chọn giá trị",
  className = "w-[160px]",
}: SelectFilterProps) {
  const isMobile = useMediaQuery("(max-width: 767px)");
  return (
    <Select value={value} onValueChange={onChange}>
      <SelectTrigger
        className={`bg-background border-border shadow-sm h-9 cursor-pointer ${className} ${isMobile ? "w-full" : ""}`}
      >
        <SelectValue placeholder={placeholder} />
      </SelectTrigger>
      <SelectContent>
        {options.map((option) => (
          <SelectItem key={option.value} value={option.value}>
            {option.label}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
}
