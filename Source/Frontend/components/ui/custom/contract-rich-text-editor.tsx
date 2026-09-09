"use client";

import { useEffect, useRef, useState } from "react";
import type { JSONContent } from "@tiptap/core";
import { TableKit } from "@tiptap/extension-table";
import { FontSize, TextStyle } from "@tiptap/extension-text-style";
import { EditorContent, useEditor } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import {
  Bold,
  ChevronDown,
  Italic,
  Maximize2,
  Minimize2,
  Redo2,
  Table2,
  Underline,
  Undo2,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import {
  CONTRACT_RICH_TEXT_FONT_SIZES,
  parseContractRichText,
  serializeContractRichText,
  type ContractRichTextBlock,
  type ContractRichTextRun,
} from "@/lib/contract-rich-text";
import { cn } from "@/lib/utils";

interface ContractRichTextEditorProps {
  id?: string;
  value?: string | null;
  onChange: (value: string) => void;
  placeholder?: string;
  ariaLabel?: string;
  className?: string;
}

const runsToTiptapContent = (runs: ContractRichTextRun[]): JSONContent[] => {
  const content: JSONContent[] = [];
  runs.forEach((run) => {
    const marks: JSONContent["marks"] = [];
    if (run.bold) marks.push({ type: "bold" });
    if (run.italic) marks.push({ type: "italic" });
    if (run.underline) marks.push({ type: "underline" });
    if (run.fontSize) {
      marks.push({ type: "textStyle", attrs: { fontSize: `${run.fontSize}pt` } });
    }

    run.text.split("\n").forEach((part, index) => {
      if (index > 0) content.push({ type: "hardBreak" });
      if (part) content.push({ type: "text", text: part, marks });
    });
  });
  return content;
};

const toTiptapContent = (value?: string | null): JSONContent => ({
  type: "doc",
  content: parseContractRichText(value).blocks.map((block) => {
    if (block.type === "paragraph") {
      return { type: "paragraph", content: runsToTiptapContent(block.runs) };
    }

    return {
      type: "table",
      content: block.rows.map((row) => ({
        type: "tableRow",
        content: row.cells.map((cell) => ({
          type: "tableCell",
          content: [
            { type: "paragraph", content: runsToTiptapContent(cell.runs) },
          ],
        })),
      })),
    };
  }),
});

const appendRun = (
  runs: ContractRichTextRun[],
  text: string,
  bold?: boolean,
  italic?: boolean,
  underline?: boolean,
  fontSize?: number,
) => {
  if (!text) return;
  const run: ContractRichTextRun = { text };
  if (bold) run.bold = true;
  if (italic) run.italic = true;
  if (underline) run.underline = true;
  if (fontSize) run.fontSize = fontSize;
  const previous = runs.at(-1);
  if (
    previous &&
    previous.bold === run.bold &&
    previous.italic === run.italic &&
    previous.underline === run.underline &&
    previous.fontSize === run.fontSize
  ) {
    previous.text += run.text;
  } else {
    runs.push(run);
  }
};

const tiptapInlineToRuns = (nodes?: JSONContent[]) => {
  const runs: ContractRichTextRun[] = [];
  nodes?.forEach((node) => {
    if (node.type === "hardBreak") {
      appendRun(runs, "\n");
      return;
    }
    if (node.type !== "text" || !node.text) return;
    const bold = node.marks?.some((mark) => mark.type === "bold");
    const italic = node.marks?.some((mark) => mark.type === "italic");
    const underline = node.marks?.some((mark) => mark.type === "underline");
    const fontSizeValue = node.marks?.find(
      (mark) => mark.type === "textStyle",
    )?.attrs?.fontSize;
    const fontSize =
      typeof fontSizeValue === "string"
        ? Number(fontSizeValue.replace("pt", ""))
        : undefined;
    appendRun(runs, node.text, bold, italic, underline, fontSize);
  });
  return runs;
};

const cellToRuns = (cell: JSONContent) => {
  const runs: ContractRichTextRun[] = [];
  cell.content?.forEach((paragraph, index) => {
    if (index > 0) appendRun(runs, "\n");
    tiptapInlineToRuns(paragraph.content).forEach((run) =>
      appendRun(
        runs,
        run.text,
        run.bold,
        run.italic,
        run.underline,
        run.fontSize,
      ),
    );
  });
  return runs;
};

const fromTiptapContent = (document: JSONContent) => {
  const blocks: ContractRichTextBlock[] = [];
  document.content?.forEach((node) => {
    if (node.type === "paragraph") {
      blocks.push({ type: "paragraph", runs: tiptapInlineToRuns(node.content) });
      return;
    }
    if (node.type !== "table") return;
    const rows = (node.content ?? [])
      .filter((row) => row.type === "tableRow")
      .map((row) => ({
        cells: (row.content ?? [])
          .filter(
            (cell) =>
              cell.type === "tableCell" || cell.type === "tableHeader",
          )
          .map((cell) => ({ runs: cellToRuns(cell) })),
      }));
    if (rows.length > 0) blocks.push({ type: "table", rows });
  });
  return serializeContractRichText({ blocks });
};

export function ContractRichTextEditor({
  id,
  value,
  onChange,
  placeholder = "Nhập nội dung điều khoản...",
  ariaLabel = "Nội dung điều khoản",
  className,
}: ContractRichTextEditorProps) {
  const [isFullscreen, setIsFullscreen] = useState(false);
  const fullscreenButtonRef = useRef<HTMLButtonElement>(null);
  const lastEmittedValue = useRef(value ?? "");
  const editor = useEditor({
    immediatelyRender: false,
    extensions: [
      StarterKit.configure({
        heading: false,
        blockquote: false,
        bulletList: false,
        code: false,
        orderedList: false,
        listItem: false,
        codeBlock: false,
        link: false,
        horizontalRule: false,
        strike: false,
      }),
      TextStyle,
      FontSize,
      TableKit.configure({
        table: {
          resizable: false,
          HTMLAttributes: { class: "contract-rich-text-table" },
        },
      }),
    ],
    content: toTiptapContent(value),
    editorProps: {
      attributes: {
        id: id ?? "",
        role: "textbox",
        "aria-label": ariaLabel,
        "aria-multiline": "true",
        class:
          "h-56 overflow-y-auto px-3 py-2 text-sm leading-6 outline-none " +
          "[&_p]:min-h-[1.5rem] [&_table]:my-2 [&_table]:w-full " +
          "[&_table]:border-collapse [&_td]:min-w-24 [&_td]:border " +
          "[&_td]:border-border [&_td]:px-2 [&_td]:py-1.5 [&_td]:align-top",
      },
    },
    onUpdate: ({ editor: currentEditor }) => {
      const nextValue = fromTiptapContent(currentEditor.getJSON());
      lastEmittedValue.current = nextValue;
      onChange(nextValue);
    },
  });

  useEffect(() => {
    const nextValue = value ?? "";
    if (!editor || lastEmittedValue.current === nextValue) return;
    lastEmittedValue.current = nextValue;
    editor.commands.setContent(toTiptapContent(nextValue), {
      emitUpdate: false,
    });
  }, [editor, value]);

  if (!editor) {
    return <div className="h-64 animate-pulse rounded-md border bg-muted/30" />;
  }

  const inTable = editor.isActive("table");

  const editorSurface = (
    <div
      className={cn(
        "overflow-hidden rounded-md border bg-background",
        className,
        isFullscreen && "flex min-h-0 flex-1 flex-col rounded-none border-0",
      )}
    >
      <div className="flex shrink-0 flex-wrap items-center gap-1 border-b bg-muted/40 p-1.5">
        <Button
          type="button"
          variant={editor.isActive("bold") ? "secondary" : "ghost"}
          size="icon"
          className="size-8"
          title="In đậm"
          aria-label="In đậm"
          onClick={() => editor.chain().focus().toggleBold().run()}
        >
          <Bold className="size-4" />
        </Button>
        <Button
          type="button"
          variant={editor.isActive("italic") ? "secondary" : "ghost"}
          size="icon"
          className="size-8"
          title="In nghiêng"
          aria-label="In nghiêng"
          onClick={() => editor.chain().focus().toggleItalic().run()}
        >
          <Italic className="size-4" />
        </Button>
        <Button
          type="button"
          variant={editor.isActive("underline") ? "secondary" : "ghost"}
          size="icon"
          className="size-8"
          title="Gạch chân"
          aria-label="Gạch chân"
          onClick={() => editor.chain().focus().toggleUnderline().run()}
        >
          <Underline className="size-4" />
        </Button>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button type="button" variant="ghost" size="sm" className="h-8 gap-1">
              Cỡ chữ <ChevronDown className="size-3.5" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            <DropdownMenuLabel>Cỡ chữ</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {CONTRACT_RICH_TEXT_FONT_SIZES.map((fontSize) => (
              <DropdownMenuItem
                key={fontSize}
                onSelect={() =>
                  editor.chain().focus().setFontSize(`${fontSize}pt`).run()
                }
              >
                {fontSize} pt
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button type="button" variant="ghost" size="sm" className="h-8 gap-1">
              <Table2 className="size-4" /> Thêm bảng
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="start">
            <DropdownMenuLabel>Kích thước bảng</DropdownMenuLabel>
            <DropdownMenuSeparator />
            {[2, 3, 4].map((size) => (
              <DropdownMenuItem
                key={size}
                onSelect={() =>
                  editor
                    .chain()
                    .focus()
                    .insertTable({ rows: size, cols: size, withHeaderRow: false })
                    .run()
                }
              >
                Bảng {size} × {size}
              </DropdownMenuItem>
            ))}
          </DropdownMenuContent>
        </DropdownMenu>
        {inTable && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                className="h-8 gap-1"
              >
                Sửa bảng <ChevronDown className="size-3.5" />
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="start">
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().addRowAfter().run()}
              >
                Thêm hàng phía dưới
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().addColumnAfter().run()}
              >
                Thêm cột bên phải
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().deleteRow().run()}
              >
                Xóa hàng hiện tại
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().deleteColumn().run()}
              >
                Xóa cột hiện tại
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive focus:text-destructive"
                onSelect={() => editor.chain().focus().deleteTable().run()}
              >
                Xóa bảng
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        )}
        <div className="ml-auto flex items-center gap-1">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-8"
            title="Hoàn tác"
            aria-label="Hoàn tác"
            disabled={!editor.can().chain().focus().undo().run()}
            onClick={() => editor.chain().focus().undo().run()}
          >
            <Undo2 className="size-4" />
          </Button>
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="size-8"
            title="Làm lại"
            aria-label="Làm lại"
            disabled={!editor.can().chain().focus().redo().run()}
            onClick={() => editor.chain().focus().redo().run()}
          >
            <Redo2 className="size-4" />
          </Button>
          <Button
            ref={fullscreenButtonRef}
            type="button"
            variant="ghost"
            size="icon"
            className="size-8"
            title={isFullscreen ? "Thoát toàn màn hình (Esc)" : "Toàn màn hình"}
            aria-label={isFullscreen ? "Thoát toàn màn hình" : "Toàn màn hình"}
            aria-pressed={isFullscreen}
            onClick={() => setIsFullscreen((current) => !current)}
          >
            {isFullscreen ? <Minimize2 className="size-4" /> : <Maximize2 className="size-4" />}
          </Button>
        </div>
      </div>

      <div className={cn("relative", isFullscreen && "min-h-0 flex-1")}>
        {editor.isEmpty && (
          <span className="pointer-events-none absolute left-3 top-2.5 z-10 text-sm text-muted-foreground">
            {placeholder}
          </span>
        )}
        <EditorContent
          editor={editor}
          className={isFullscreen ? "h-full [&_.tiptap]:h-full" : undefined}
        />
      </div>
    </div>
  );

  return (
    <Dialog open={isFullscreen} onOpenChange={setIsFullscreen}>
      {!isFullscreen && editorSurface}
      {isFullscreen && (
        <DialogContent
          className="flex h-dvh w-screen max-w-none flex-col gap-0 rounded-none border-0 p-0 sm:max-w-none"
          showCloseButton={false}
          aria-describedby={undefined}
          onOpenAutoFocus={(event) => {
            event.preventDefault();
            editor.commands.focus();
          }}
          onCloseAutoFocus={(event) => {
            event.preventDefault();
            fullscreenButtonRef.current?.focus();
          }}
        >
          <DialogTitle className="sr-only">{ariaLabel}</DialogTitle>
          {editorSurface}
        </DialogContent>
      )}
    </Dialog>
  );
}
