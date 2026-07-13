"use client";

import * as React from "react";
import * as SelectPrimitive from "@radix-ui/react-select";
import {
  CheckIcon,
  ChevronDownIcon,
  ChevronUpIcon,
  Search,
} from "lucide-react";

import { cn } from "@/lib/utils";

const SelectValueContext = React.createContext<string | undefined>(undefined);

const SelectSearchContext = React.createContext<{
  search: string;
  setSearch: (value: string) => void;
} | null>(null);

const getTextContent = (children: React.ReactNode): string => {
  if (typeof children === "string" || typeof children === "number")
    return String(children);
  if (Array.isArray(children)) return children.map(getTextContent).join("");

  if (React.isValidElement(children)) {
    return getTextContent(
      (children.props as { children?: React.ReactNode }).children,
    );
  }
  return "";
};

function Select({
  value,
  defaultValue,
  onValueChange,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Root>) {
  const [internalValue, setInternalValue] = React.useState(
    value || defaultValue,
  );

  React.useEffect(() => {
    if (value !== undefined) {
      setInternalValue(value);
    }
  }, [value]);

  const handleValueChange = (val: string) => {
    setInternalValue(val);
    onValueChange?.(val);
  };

  return (
    <SelectValueContext.Provider value={internalValue}>
      <SelectPrimitive.Root
        data-slot="select"
        value={value}
        defaultValue={defaultValue}
        onValueChange={handleValueChange}
        {...props}
      />
    </SelectValueContext.Provider>
  );
}

function SelectGroup({
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Group>) {
  return <SelectPrimitive.Group data-slot="select-group" {...props} />;
}

function SelectValue({
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Value>) {
  return <SelectPrimitive.Value data-slot="select-value" {...props} />;
}

function SelectTrigger({
  className,
  size = "default",
  children,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Trigger> & {
  size?: "sm" | "default";
}) {
  return (
    <SelectPrimitive.Trigger
      data-slot="select-trigger"
      data-size={size}
      className={cn(
        "border-input data-[placeholder]:text-muted-foreground [&_svg:not([class*='text-'])]:text-muted-foreground focus-visible:border-ring focus-visible:ring-ring/50 aria-invalid:ring-destructive/20 dark:aria-invalid:ring-destructive/40 aria-invalid:border-destructive dark:bg-input/30 dark:hover:bg-input/50 flex w-fit items-center justify-between gap-2 rounded-md border bg-transparent px-3 py-2 text-sm whitespace-nowrap shadow-xs transition-[color,box-shadow] outline-none focus-visible:ring-[3px] disabled:cursor-not-allowed disabled:opacity-50 data-[size=default]:h-9 data-[size=sm]:h-8 *:data-[slot=select-value]:line-clamp-1 *:data-[slot=select-value]:flex *:data-[slot=select-value]:items-center *:data-[slot=select-value]:gap-2 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4",
        className,
      )}
      {...props}
    >
      {children}
      <SelectPrimitive.Icon asChild>
        <ChevronDownIcon className="size-4 opacity-50" />
      </SelectPrimitive.Icon>
    </SelectPrimitive.Trigger>
  );
}

function SelectContent({
  className,
  children,
  position = "popper",
  showSearch = true,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Content> & {
  showSearch?: boolean;
  searchPlaceholder?: string;
}) {
  const [search, setSearch] = React.useState("");

  return (
    <SelectSearchContext.Provider value={{ search, setSearch }}>
      <SelectPrimitive.Portal>
        <SelectPrimitive.Content
          data-slot="select-content"
          className={cn(
            "bg-popover text-popover-foreground data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 relative z-50 max-h-(--radix-select-content-available-height) min-w-[8rem] origin-(--radix-select-content-transform-origin) overflow-x-hidden overflow-y-auto rounded-md border shadow-md",
            position === "popper" &&
              "data-[side=bottom]:translate-y-1 data-[side=left]:-translate-x-1 data-[side=right]:translate-x-1 data-[side=top]:-translate-y-1",
            className,
          )}
          position={position}
          {...props}
        >
          <SelectScrollUpButton />

          {showSearch && (
            <div className="flex items-center border-b px-3 py-2 sticky top-0 bg-popover z-10">
              <Search className="mr-2 size-4 shrink-0 opacity-50" />
              <input
                className="flex w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
                placeholder={"Tìm kiếm..."}
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                onKeyDown={(e) => {
                  if (
                    e.key !== "ArrowUp" &&
                    e.key !== "ArrowDown" &&
                    e.key !== "Enter"
                  ) {
                    e.stopPropagation();
                  }
                }}
              />
            </div>
          )}

          <SelectPrimitive.Viewport
            data-slot="select-viewport"
            className={cn(
              "p-1",
              position === "popper" &&
                "h-[var(--radix-select-trigger-height)] w-full min-w-[var(--radix-select-trigger-width)] scroll-my-1",
            )}
          >
            {children}
          </SelectPrimitive.Viewport>
          <SelectScrollDownButton />
        </SelectPrimitive.Content>
      </SelectPrimitive.Portal>
    </SelectSearchContext.Provider>
  );
}

function SelectLabel({
  className,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Label>) {
  return (
    <SelectPrimitive.Label
      data-slot="select-label"
      className={cn("text-muted-foreground px-2 py-1.5 text-xs", className)}
      {...props}
    />
  );
}

function SelectItem({
  className,
  children,
  value,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Item>) {
  const searchCtx = React.useContext(SelectSearchContext);
  const selectedValue = React.useContext(SelectValueContext);

  if (searchCtx && searchCtx.search) {
    const isSelected = selectedValue === value;
    const itemText = getTextContent(children).toLowerCase();
    if (!isSelected && !itemText.includes(searchCtx.search.toLowerCase())) {
      return null;
    }
  }

  return (
    <SelectPrimitive.Item
      data-slot="select-item"
      value={value}
      className={cn(
        "group focus:bg-accent focus:text-accent-foreground [&_svg:not([class*='text-'])]:text-muted-foreground relative flex w-full cursor-default items-center gap-2 rounded-sm py-1.5 pr-8 pl-2 text-sm outline-hidden select-none data-[disabled]:pointer-events-none data-[disabled]:opacity-50 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4 *:[span]:last:flex *:[span]:last:items-center *:[span]:last:gap-2",
        className,
      )}
      {...props}
    >
      <span className="absolute right-2 flex size-3.5 items-center justify-center">
        <SelectPrimitive.ItemIndicator>
          <CheckIcon className="size-4" />
        </SelectPrimitive.ItemIndicator>
      </span>
      <SelectPrimitive.ItemText>{children}</SelectPrimitive.ItemText>
    </SelectPrimitive.Item>
  );
}

function SelectSeparator({
  className,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.Separator>) {
  return (
    <SelectPrimitive.Separator
      data-slot="select-separator"
      className={cn("bg-border pointer-events-none -mx-1 my-1 h-px", className)}
      {...props}
    />
  );
}

function SelectScrollUpButton({
  className,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.ScrollUpButton>) {
  return (
    <SelectPrimitive.ScrollUpButton
      data-slot="select-scroll-up-button"
      className={cn(
        "flex cursor-default items-center justify-center py-1",
        className,
      )}
      {...props}
    >
      <ChevronUpIcon className="size-4" />
    </SelectPrimitive.ScrollUpButton>
  );
}

function SelectScrollDownButton({
  className,
  ...props
}: React.ComponentProps<typeof SelectPrimitive.ScrollDownButton>) {
  return (
    <SelectPrimitive.ScrollDownButton
      data-slot="select-scroll-down-button"
      className={cn(
        "flex cursor-default items-center justify-center py-1",
        className,
      )}
      {...props}
    >
      <ChevronDownIcon className="size-4" />
    </SelectPrimitive.ScrollDownButton>
  );
}

export {
  Select,
  SelectContent,
  SelectGroup,
  SelectItem,
  SelectLabel,
  SelectScrollDownButton,
  SelectScrollUpButton,
  SelectSeparator,
  SelectTrigger,
  SelectValue,
};
