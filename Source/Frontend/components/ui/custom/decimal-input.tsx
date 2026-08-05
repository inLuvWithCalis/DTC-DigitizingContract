"use client";

import * as React from "react";

import { Input } from "@/components/ui/input";

const DECIMAL_INTEGER_DIGITS = 14;
const DECIMAL_SCALE = 4;
const MAX_DECIMAL_VALUE = Number(
  `${"9".repeat(DECIMAL_INTEGER_DIGITS)}.${"9".repeat(DECIMAL_SCALE)}`,
);
const MAX_DECIMAL_INPUT_LENGTH =
  DECIMAL_INTEGER_DIGITS + DECIMAL_SCALE + 1;

type DecimalInputProps = Omit<
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

function sanitizeDecimal(rawValue: string) {
  const normalizedValue = rawValue.replace(/,/g, ".").replace(/[^\d.]/g, "");
  const [rawIntegerPart = "", ...fractionParts] = normalizedValue.split(".");
  const hasDecimalSeparator = normalizedValue.includes(".");
  const rawFractionPart = fractionParts.join("");

  if (
    rawIntegerPart.length > DECIMAL_INTEGER_DIGITS ||
    rawFractionPart.length > DECIMAL_SCALE
  ) {
    return null;
  }

  const integerDigits = rawIntegerPart
    .replace(/^0+(?=\d)/, "");
  const integerPart = integerDigits || (hasDecimalSeparator ? "0" : "");

  return hasDecimalSeparator
    ? `${integerPart}.${rawFractionPart}`
    : integerPart;
}

function getValueAfterInsert(input: HTMLInputElement, insertedValue: string) {
  const selectionStart = input.selectionStart ?? input.value.length;
  const selectionEnd = input.selectionEnd ?? selectionStart;
  return `${input.value.slice(0, selectionStart)}${insertedValue}${input.value.slice(selectionEnd)}`;
}

function isValidDecimalInput(value: string) {
  const normalizedValue = value.replace(",", ".");
  return new RegExp(
    `^\\d{0,${DECIMAL_INTEGER_DIGITS}}(?:\\.\\d{0,${DECIMAL_SCALE}})?$`,
  ).test(normalizedValue);
}

function DecimalInput({
  value,
  onValueChange,
  min = 0,
  max = MAX_DECIMAL_VALUE,
  onFocus,
  onBlur,
  onKeyDown,
  onBeforeInput,
  onPaste,
  ...props
}: DecimalInputProps) {
  const upperBound = Math.min(MAX_DECIMAL_VALUE, max);
  const lowerBound = Math.min(upperBound, Math.max(0, min));
  const [displayValue, setDisplayValue] = React.useState(String(value));
  const isFocusedRef = React.useRef(false);

  React.useEffect(() => {
    if (!isFocusedRef.current) {
      setDisplayValue(String(value));
    }
  }, [value]);

  const handleChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const sanitizedValue = sanitizeDecimal(event.target.value);
    if (sanitizedValue === null || sanitizedValue === displayValue) return;

    let nextDisplayValue = sanitizedValue;

    if (nextDisplayValue === "") {
      setDisplayValue("");
      onValueChange(0);
      return;
    }

    let nextValue = Number(nextDisplayValue);
    if (!Number.isFinite(nextValue)) {
      nextValue = 0;
    }

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
    const selectionStart = input.selectionStart ?? 0;
    const selectionEnd = input.selectionEnd ?? selectionStart;
    if (selectionStart !== selectionEnd) return;

    const decimalSeparatorIndex = displayValue.indexOf(".");
    const isFractionPosition =
      decimalSeparatorIndex >= 0 && selectionStart > decimalSeparatorIndex;
    const [integerPart = "", fractionPart = ""] = displayValue.split(".");
    const limitReached = isFractionPosition
      ? fractionPart.length >= DECIMAL_SCALE
      : integerPart.length >= DECIMAL_INTEGER_DIGITS;

    if (limitReached) {
      event.preventDefault();
    }
  };

  const handleBeforeInput = (event: React.InputEvent<HTMLInputElement>) => {
    onBeforeInput?.(event);
    if (event.defaultPrevented) return;

    const insertedValue = event.data;
    if (insertedValue === null) return;

    const nextValue = getValueAfterInsert(event.currentTarget, insertedValue);
    if (!isValidDecimalInput(nextValue)) {
      event.preventDefault();
    }
  };

  const handlePaste = (event: React.ClipboardEvent<HTMLInputElement>) => {
    onPaste?.(event);
    if (event.defaultPrevented) return;

    const pastedValue = event.clipboardData.getData("text");
    const nextValue = getValueAfterInsert(event.currentTarget, pastedValue);
    if (!isValidDecimalInput(nextValue)) {
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
      Number.isFinite(parsedValue) ? parsedValue : lowerBound,
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
      inputMode="decimal"
      maxLength={MAX_DECIMAL_INPUT_LENGTH}
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

export {
  DecimalInput,
  DECIMAL_INTEGER_DIGITS,
  DECIMAL_SCALE,
  MAX_DECIMAL_VALUE,
};
