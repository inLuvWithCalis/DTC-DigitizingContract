"use client";

import { useEffect, useRef, useState } from "react";
import { isProseMirrorCellSelection, type JSONContent } from "@tiptap/core";
import { TableCell, TableHeader, TableKit } from "@tiptap/extension-table";
import TextAlign from "@tiptap/extension-text-align";
import { FontSize, TextStyle } from "@tiptap/extension-text-style";
import { EditorContent, useEditor, useEditorState } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import {
  Bold,
  AlignCenter,
  AlignLeft,
  AlignRight,
  ChevronDown,
  Combine,
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
  type ContractRichTextAlignment,
  type ContractRichTextParagraph,
  type ContractRichTextRun,
  type ContractRichTextVerticalAlignment,
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

const verticalAlignAttribute = {
  default: null,
  parseHTML: (element: HTMLElement) => {
    const value = element.style.verticalAlign;
    return value === "top" || value === "center" || value === "bottom"
      ? value
      : null;
  },
  renderHTML: (attributes: Record<string, unknown>) => {
    const value = attributes.verticalAlign;
    return value === "top" || value === "center" || value === "bottom"
      ? { style: `vertical-align:${value}` }
      : {};
  },
};

const ContractTableCell = TableCell.extend({
  addAttributes() {
    return {
      ...(this.parent?.() ?? {}),
      verticalAlign: verticalAlignAttribute,
    };
  },
});

const ContractTableHeader = TableHeader.extend({
  addAttributes() {
    return {
      ...(this.parent?.() ?? {}),
      verticalAlign: verticalAlignAttribute,
    };
  },
});

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
      return {
        type: "paragraph",
        attrs: { textAlign: block.alignment ?? "left" },
        content: runsToTiptapContent(block.runs),
      };
    }

    return {
      type: "table",
      content: block.rows.map((row) => ({
        type: "tableRow",
        content: row.cells.map((cell) => ({
          type: "tableCell",
          attrs: {
            colspan: cell.colspan ?? 1,
            rowspan: cell.rowspan ?? 1,
            colwidth: cell.colwidth ?? null,
            verticalAlign: cell.verticalAlign ?? null,
          },
          content: cell.paragraphs.map((paragraph) => ({
            type: "paragraph",
            attrs: { textAlign: paragraph.alignment ?? "left" },
            content: runsToTiptapContent(paragraph.runs),
          })),
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

const paragraphAlignment = (
  paragraph: JSONContent,
): ContractRichTextAlignment | undefined => {
  const value = paragraph.attrs?.textAlign;
  return value === "center" || value === "right" ? value : undefined;
};

const tiptapParagraph = (paragraph: JSONContent): ContractRichTextParagraph => {
  const alignment = paragraphAlignment(paragraph);
  return {
    runs: tiptapInlineToRuns(paragraph.content),
    ...(alignment ? { alignment } : {}),
  };
};

const cellVerticalAlignment = (
  value: unknown,
): ContractRichTextVerticalAlignment | undefined =>
  value === "top" || value === "center" || value === "bottom"
    ? value
    : undefined;

const fromTiptapContent = (document: JSONContent) => {
  const blocks: ContractRichTextBlock[] = [];
  document.content?.forEach((node) => {
    if (node.type === "paragraph") {
      blocks.push({ type: "paragraph", ...tiptapParagraph(node) });
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
          .map((cell) => {
            const colspan = Number(cell.attrs?.colspan);
            const rowspan = Number(cell.attrs?.rowspan);
            const colwidth = Array.isArray(cell.attrs?.colwidth)
              ? cell.attrs.colwidth.filter(
                  (width): width is number =>
                    typeof width === "number" && Number.isFinite(width),
                )
              : undefined;
            const verticalAlign = cellVerticalAlignment(
              cell.attrs?.verticalAlign,
            );
            return {
              paragraphs: (cell.content ?? [])
                .filter((paragraph) => paragraph.type === "paragraph")
                .map(tiptapParagraph),
              ...(Number.isInteger(colspan) && colspan > 1 ? { colspan } : {}),
              ...(Number.isInteger(rowspan) && rowspan > 1 ? { rowspan } : {}),
              ...(colwidth && colwidth.length > 0 ? { colwidth } : {}),
              ...(verticalAlign ? { verticalAlign } : {}),
            };
          }),
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
      TextAlign.configure({
        types: ["paragraph"],
        alignments: ["left", "center", "right"],
        defaultAlignment: "left",
      }),
      TableKit.configure({
        table: {
          resizable: true,
          cellMinWidth: 50,
          HTMLAttributes: { class: "contract-rich-text-table" },
        },
        tableCell: false,
        tableHeader: false,
      }),
      ContractTableCell,
      ContractTableHeader,
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

  const tableState = useEditorState({
    editor,
    selector: ({ editor: currentEditor }) => {
      if (!currentEditor) {
        return { inTable: false, selectedCellCount: 0, canMerge: false };
      }
      const selection = currentEditor.state.selection;
      return {
        inTable: currentEditor.isActive("table"),
        selectedCellCount: isProseMirrorCellSelection(selection)
          ? selection.ranges.length
          : 0,
        canMerge: currentEditor.can().chain().mergeCells().run(),
      };
    },
  }) ?? { inTable: false, selectedCellCount: 0, canMerge: false };

  if (!editor) {
    return <div className="h-64 animate-pulse rounded-md border bg-muted/30" />;
  }

  const { inTable, selectedCellCount, canMerge } = tableState;

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
        {([
          ["left", AlignLeft, "Căn trái"],
          ["center", AlignCenter, "Căn giữa"],
          ["right", AlignRight, "Căn phải"],
        ] as const).map(([alignment, Icon, label]) => (
          <Button
            key={alignment}
            type="button"
            variant={editor.isActive({ textAlign: alignment }) ? "secondary" : "ghost"}
            size="icon"
            className="size-8"
            title={label}
            aria-label={label}
            aria-pressed={editor.isActive({ textAlign: alignment })}
            onClick={() => editor.chain().focus().setTextAlign(alignment).run()}
          >
            <Icon className="size-4" />
          </Button>
        ))}
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
          <>
            <Button
              type="button"
              variant={canMerge ? "secondary" : "ghost"}
              size="sm"
              className="h-8 gap-1"
              title="Chọn từ hai ô liền nhau để gộp"
              disabled={!canMerge}
              onClick={() => editor.chain().focus().mergeCells().run()}
            >
              <Combine className="size-4" /> Gộp ô
            </Button>
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
              <DropdownMenuLabel>Hàng và cột</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().addRowBefore().run()}
              >
                Thêm hàng phía trên
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().addRowAfter().run()}
              >
                Thêm hàng phía dưới
              </DropdownMenuItem>
              <DropdownMenuItem
                onSelect={() => editor.chain().focus().addColumnBefore().run()}
              >
                Thêm cột bên trái
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
              <DropdownMenuLabel>Ô</DropdownMenuLabel>
              <DropdownMenuItem
                disabled={!canMerge}
                onSelect={() => editor.chain().focus().mergeCells().run()}
              >
                Gộp các ô đã chọn
              </DropdownMenuItem>
              <DropdownMenuItem
                disabled={!editor.can().chain().focus().splitCell().run()}
                onSelect={() => editor.chain().focus().splitCell().run()}
              >
                Tách ô
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuLabel>Căn dọc nội dung ô</DropdownMenuLabel>
              {(["top", "center", "bottom"] as const).map((alignment) => (
                <DropdownMenuItem
                  key={alignment}
                  onSelect={() =>
                    editor
                      .chain()
                      .focus()
                      .setCellAttribute("verticalAlign", alignment)
                      .run()
                  }
                >
                  {alignment === "top"
                    ? "Căn trên"
                    : alignment === "center"
                      ? "Căn giữa"
                      : "Căn dưới"}
                </DropdownMenuItem>
              ))}
              <DropdownMenuSeparator />
              <DropdownMenuItem
                className="text-destructive focus:text-destructive"
                onSelect={() => editor.chain().focus().deleteTable().run()}
              >
                Xóa bảng
              </DropdownMenuItem>
              </DropdownMenuContent>
            </DropdownMenu>
          </>
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

      {inTable && (
        <div className="shrink-0 border-b bg-primary/5 px-3 py-1.5 text-xs text-muted-foreground">
          {selectedCellCount > 1 ? (
            <span className="font-medium text-primary">
              Đã chọn {selectedCellCount} ô. Bấm “Gộp ô” để gộp vùng đang chọn.
            </span>
          ) : (
            <span>
              Để gộp ô: đặt con trỏ ở ô đầu, giữ Shift rồi bấm ô cuối
              (hoặc kéo chuột qua các ô).
            </span>
          )}
        </div>
      )}

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
