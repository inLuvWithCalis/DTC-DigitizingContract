"use client";

import * as React from "react";

import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

const CODE_VALUE_PATTERN = /^[A-Z0-9]+(?:[-_][A-Z0-9]+)*$/;

type CodeInputProps = Omit<
  React.ComponentProps<typeof Input>,
  "type" | "value" | "defaultValue" | "onChange"
> & {
  value: string;
  onValueChange: (value: string) => void;
};

function normalizeCodeValue(value: string, finalize = false) {
  const normalized = value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[đĐ]/g, "D")
    .toUpperCase()
    .replace(/[^A-Z0-9_-]+/g, "-")
    .replace(/[-_]{2,}/g, "-")
    .replace(/^[-_]+/, "");

  return finalize ? normalized.replace(/[-_]+$/, "") : normalized;
}

function isValidCodeValue(value: string) {
  return CODE_VALUE_PATTERN.test(value);
}

function CodeInput({
  value,
  onValueChange,
  onBlur,
  className,
  maxLength = 50,
  ...props
}: CodeInputProps) {
  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    onValueChange(normalizeCodeValue(event.target.value));
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    const finalizedValue = normalizeCodeValue(event.currentTarget.value, true);
    if (finalizedValue !== value) {
      onValueChange(finalizedValue);
    }
    onBlur?.(event);
  };

  return (
    <Input
      {...props}
      type="text"
      value={value}
      onChange={handleChange}
      onBlur={handleBlur}
      maxLength={maxLength}
      autoCapitalize="characters"
      autoComplete="off"
      spellCheck={false}
      className={cn("font-mono uppercase", className)}
    />
  );
}

export {
  CodeInput,
  CODE_VALUE_PATTERN,
  isValidCodeValue,
  normalizeCodeValue,
};
export type { CodeInputProps };
