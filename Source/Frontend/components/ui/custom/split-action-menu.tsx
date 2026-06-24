"use client";

import React, { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ChevronDown, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";

export interface ActionMenuItem {
  label: string;
  icon?: ReactNode;
  onClick: (e: React.MouseEvent) => void;
  isDestructive?: boolean;
  disabled?: boolean;
}

interface SplitActionMenuProps {
  primaryLabel: string;
  primaryIcon?: ReactNode;
  onPrimaryClick: (e: React.MouseEvent) => void;
  isLoading?: boolean;
  disabled?: boolean;
  menuItems: ActionMenuItem[];
}

export function SplitActionMenu({
  primaryLabel,
  primaryIcon,
  onPrimaryClick,
  isLoading = false,
  disabled = false,
  menuItems,
}: SplitActionMenuProps) {
  return (
    <div className="flex items-center justify-end">
      <div className="inline-flex rounded-md shadow-sm">
        <Button
          size="sm"
          disabled={disabled || isLoading}
          onClick={(e) => {
            e.stopPropagation();
            onPrimaryClick(e);
          }}
          className="h-8 rounded-r-none border-r border-primary-foreground/20 focus:z-10"
        >
          {isLoading ? (
            <Loader2 className="w-4 h-4 mr-2 animate-spin" />
          ) : (
            primaryIcon && <span className="mr-2">{primaryIcon}</span>
          )}
          {primaryLabel}
        </Button>

        {menuItems.length > 0 && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                size="sm"
                disabled={disabled || isLoading}
                className="h-8 w-8 px-0 rounded-l-none focus:z-10"
                onClick={(e) => e.stopPropagation()} // Ngăn click nhầm vào Row
              >
                <ChevronDown className="w-4 h-4" />
              </Button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end" className="w-48">
              {menuItems.map((item, index) => (
                <DropdownMenuItem
                  key={index}
                  disabled={item.disabled}
                  className={cn(
                    "cursor-pointer py-2",
                    item.isDestructive
                      ? "text-destructive focus:bg-destructive/10 focus:text-destructive"
                      : "focus:bg-secondary",
                  )}
                  onClick={(e) => {
                    e.stopPropagation();
                    item.onClick(e);
                  }}
                >
                  {item.icon && <span className="mr-2">{item.icon}</span>}
                  <span className="font-medium">{item.label}</span>
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </div>
    </div>
  );
}
