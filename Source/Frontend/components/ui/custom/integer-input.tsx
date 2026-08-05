"use client";

import * as React from "react";

import { Input } from "@/components/ui/input";

const MAX_INTEGER_DIGITS = 14;
const MAX_INTEGER_VALUE = Number("9".repeat(MAX_INTEGER_DIGITS));

type IntegerInputProps = Omit<
  React.ComponentProps<typeof Input>,
  | "type"
  | "inputMode"
  | "value"
  | "defaultValue"
  | "onChange"
  | "min"
  | "max"
  | "step"
  | "maxLength"
> & {
  value: number;
  onValueChange: (value: number) => void;
  min?: number;
  max?: number;
};

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value));
}

function sanitizeInteger(rawValue: string) {
  const digits = rawValue.replace(/\D/g, "");
  if (digits.length > MAX_INTEGER_DIGITS) return null;
  return digits.replace(/^0+(?=\d)/, "");
}

function getValueAfterInsert(input: HTMLInputElement, insertedValue: string) {
  const selectionStart = input.selectionStart ?? input.value.length;
  const selectionEnd = input.selectionEnd ?? selectionStart;
  return `${input.value.slice(0, selectionStart)}${insertedValue}${input.value.slice(selectionEnd)}`;
}

function isValidIntegerInput(value: string) {
  return new RegExp(`^\\d{0,${MAX_INTEGER_DIGITS}}$`).test(value);
}

function IntegerInput({
  value,
  onValueChange,
  min = 0,
  max = MAX_INTEGER_VALUE,
  onFocus,
  onBlur,
  onKeyDown,
  onBeforeInput,
  onPaste,
  ...props
}: IntegerInputProps) {
  const upperBound = Math.min(MAX_INTEGER_VALUE, max);
  const lowerBound = Math.min(upperBound, Math.max(0, Math.ceil(min)));
  const [displayValue, setDisplayValue] = React.useState(String(value));
  const isFocusedRef = React.useRef(false);

  React.useEffect(() => {
    if (!isFocusedRef.current) {
      setDisplayValue(String(value));
    }
  }, [value]);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const sanitizedValue = sanitizeInteger(event.target.value);
    if (sanitizedValue === null || sanitizedValue === displayValue) return;

    let nextDisplayValue = sanitizedValue;

    if (nextDisplayValue === "") {
      setDisplayValue("");
      onValueChange(0);
      return;
    }

    let nextValue = Number(nextDisplayValue);
    if (nextValue > upperBound) {
      nextValue = upperBound;
      nextDisplayValue = String(nextValue);
    }

    setDisplayValue(nextDisplayValue);
    onValueChange(nextValue);
  };

  const handleKeyDown = (event: React.KeyboardEvent<HTMLInputElement>) => {
    onKeyDown?.(event);
    if (event.defaultPrevented || !/^\d$/.test(event.key)) return;

    const input = event.currentTarget;
    const hasSelection = input.selectionStart !== input.selectionEnd;
    if (!hasSelection && displayValue.length >= MAX_INTEGER_DIGITS) {
      event.preventDefault();
    }
  };

  const handleBeforeInput = (event: React.InputEvent<HTMLInputElement>) => {
    onBeforeInput?.(event);
    if (event.defaultPrevented) return;

    const insertedValue = event.data;
    if (insertedValue === null) return;

    const nextValue = getValueAfterInsert(event.currentTarget, insertedValue);
    if (!isValidIntegerInput(nextValue)) {
      event.preventDefault();
    }
  };

  const handlePaste = (event: React.ClipboardEvent<HTMLInputElement>) => {
    onPaste?.(event);
    if (event.defaultPrevented) return;

    const pastedValue = event.clipboardData.getData("text");
    const nextValue = getValueAfterInsert(event.currentTarget, pastedValue);
    if (!isValidIntegerInput(nextValue)) {
      event.preventDefault();
    }
  };

  const handleFocus = (event: React.FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = true;
    onFocus?.(event);
  };

  const handleBlur = (event: React.FocusEvent<HTMLInputElement>) => {
    isFocusedRef.current = false;
    const parsedValue = Number(displayValue);
    const normalizedValue = clamp(
      Number.isFinite(parsedValue) ? Math.trunc(parsedValue) : lowerBound,
      lowerBound,
      upperBound,
    );

    setDisplayValue(String(normalizedValue));
    onValueChange(normalizedValue);
    onBlur?.(event);
  };

  return (
    <Input
      {...props}
      type="text"
      inputMode="numeric"
      maxLength={MAX_INTEGER_DIGITS}
      value={displayValue}
      onChange={handleChange}
      onKeyDown={handleKeyDown}
      onBeforeInput={handleBeforeInput}
      onPaste={handlePaste}
      onFocus={handleFocus}
      onBlur={handleBlur}
    />
  );
}

export { IntegerInput, MAX_INTEGER_DIGITS, MAX_INTEGER_VALUE };
