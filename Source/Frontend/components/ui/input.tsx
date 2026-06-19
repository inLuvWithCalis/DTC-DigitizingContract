"use client";

import * as React from "react";
import { cn } from "@/lib/utils";

function Input({
  className,
  type,
  maxLength = 50,
  value,
  defaultValue,
  onChange,
  ...props
}: React.ComponentProps<"input">) {
  const [charCount, setCharCount] = React.useState(() => {
    if (value) return String(value).length;
    if (defaultValue) return String(defaultValue).length;
    return 0;
  });

  React.useEffect(() => {
    if (value !== undefined) {
      setCharCount(String(value).length);
    }
  }, [value]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setCharCount(e.target.value.length);
    if (onChange) {
      onChange(e);
    }
  };

  return (
    <div className="relative w-full">
      <input
        type={type}
        data-slot="input"
        maxLength={maxLength}
        value={value}
        defaultValue={defaultValue}
        onChange={handleChange}
        className={cn(
          "file:text-foreground placeholder:text-muted-foreground selection:bg-primary selection:text-primary-foreground dark:bg-input/30 border-input h-9 w-full min-w-0 rounded-md border bg-transparent px-3 py-1 text-base shadow-xs transition-[color,box-shadow] outline-none file:inline-flex file:h-7 file:border-0 file:bg-transparent file:text-sm file:font-medium disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 md:text-sm",
          "focus-visible:border-ring focus-visible:ring-ring/50 focus-visible:ring-[3px]",
          "aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive",
          maxLength && "pr-14",
          className,
        )}
        {...props}
      />

      {maxLength && (
        <div className="pointer-events-none absolute right-3 top-1/2 -translate-y-1/2 text-[11px] font-medium text-muted-foreground/70">
          {charCount}/{maxLength}
        </div>
      )}
    </div>
  );
}

export { Input };
