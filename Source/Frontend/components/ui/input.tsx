"use client";

import * as React from "react";
import { cn } from "@/lib/utils";
import { X } from "lucide-react";

interface InputProps extends React.ComponentProps<"input"> {
  onClear?: () => void;
}

function Input({
  className,
  type,
  maxLength = 50,
  value,
  defaultValue,
  onChange,
  onClear,
  ...props
}: InputProps) {
  const inputRef = React.useRef<HTMLInputElement>(null);

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
    // HTML maxLength doesn't work on type="number", enforce it manually
    if (type === "number" && maxLength && e.target.value.length > maxLength) {
      e.target.value = e.target.value.slice(0, maxLength);
    }
    setCharCount(e.target.value.length);
    if (onChange) {
      onChange(e);
    }
  };

  const handleClear = (e: React.MouseEvent) => {
    e.preventDefault();
    const input = inputRef.current;
    if (input) {
      const nativeInputValueSetter = Object.getOwnPropertyDescriptor(
        window.HTMLInputElement.prototype,
        "value",
      )?.set;
      nativeInputValueSetter?.call(input, "");
      input.dispatchEvent(new Event("input", { bubbles: true }));
      input.focus();
    }
    setCharCount(0);
    if (onClear) onClear();
  };

  const showClearButton = charCount > 0 && !props.disabled;

  return (
    <div className="relative w-full">
      <input
        ref={inputRef}
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
          maxLength ? "pr-20" : "pr-8",
          className,
        )}
        {...props}
      />

      <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center gap-1">
        {showClearButton && (
          <button
            type="button"
            onClick={handleClear}
            className="p-1 hover:bg-muted rounded-full text-muted-foreground hover:text-foreground transition-colors"
            aria-label="Clear input"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        )}

        {maxLength && (
          <div className="text-[11px] font-medium text-muted-foreground/70 min-w-[24px] text-center">
            {charCount}/{maxLength}
          </div>
        )}
      </div>
    </div>
  );
}

export { Input };
