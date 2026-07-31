"use client";

import * as React from "react";
import { Row } from "@tanstack/react-table";
import { Checkbox } from "@/components/ui/checkbox";
import { useLongPress } from "@/hooks/use-long-press";

export interface MobileCardWrapperProps<TData> {
  row: Row<TData>;
  isSelectionMode: boolean;
  onLongPress: () => void;
  onTapInSelectionMode: () => void;
  onRowClick?: (data: TData) => void;
  children: React.ReactNode;
}

export function MobileCardWrapper<TData>({
  row,
  isSelectionMode,
  onLongPress,
  onTapInSelectionMode,
  onRowClick,
  children,
}: MobileCardWrapperProps<TData>) {
  const { didLongPress, ...longPressEvents } = useLongPress(onLongPress, {
    delay: 500,
  });
  const isSelected = row.getIsSelected();

  const handleClick = (e: React.MouseEvent) => {
    if (didLongPress.current) {
      e.preventDefault();
      return;
    }
    const selection = window.getSelection()?.toString();
    if (selection && selection.trim().length > 0) {
      return;
    }
    if (isSelectionMode) {
      e.preventDefault();
      onTapInSelectionMode();
    } else if (onRowClick) {
      onRowClick(row.original);
    }
  };

  const ml = row.depth > 0 ? `${row.depth * 1.25}rem` : "0";

  return (
    <div
      style={{ marginLeft: ml }}
      className={`relative transition-all duration-200 ${
        isSelected ? "ring-2 ring-primary/50 rounded-xl" : ""
      }`}
      {...longPressEvents}
      onClick={handleClick}
    >
      {isSelectionMode && (
        <div
          className="absolute top-3 left-3 z-10"
          onClick={(e) => {
            e.stopPropagation();
            onTapInSelectionMode();
          }}
        >
          <Checkbox
            checked={isSelected}
            onCheckedChange={() => onTapInSelectionMode()}
            className="h-5 w-5 rounded-md shadow-sm bg-background border-2"
          />
        </div>
      )}

      <div
        className={`transition-all duration-200 ${
          isSelectionMode ? "pl-9" : ""
        }`}
      >
        {children}
      </div>
    </div>
  );
}
