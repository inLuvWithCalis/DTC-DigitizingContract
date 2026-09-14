"use client";

import {
  useEffect,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
} from "react";
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
  columnWidthsBpsToPixels,
  normalizeColumnWidthsBps,
  parseContractRichText,
  serializeContractRichText,
  type ContractRichTextBlock,
  type ContractRichTextAlignment,
  type ContractRichTextParagraph,
  type ContractRichTextRun,
  type ContractRichTextRow,
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

const DEFAULT_EDITOR_CONTENT_WIDTH = 800;
const EDITOR_HORIZONTAL_PADDING = 24;
const EDITOR_VIEWPORT_GUTTER = 16;

const measureEditorContentWidth = (surface: HTMLDivElement) => {
  const surfaceRect = surface.getBoundingClientRect();
  const viewportWidth = document.documentElement.clientWidth;
  const visibleSurfaceWidth = Math.max(
    0,
    viewportWidth - Math.max(0, surfaceRect.left) - EDITOR_VIEWPORT_GUTTER,
  );

  return Math.max(
    200,
    Math.floor(
      Math.min(surface.clientWidth, visibleSurfaceWidth) -
        EDITOR_HORIZONTAL_PADDING,
    ),
  );
};

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

const cellStartColumns = (
  rows: ContractRichTextRow[],
  columnCount: number,
) => {
  const activeRowspans = Array<number>(columnCount).fill(0);
  return rows.map((row) => {
    const occupied = activeRowspans.map((remaining) => remaining > 0);
    activeRowspans.forEach((remaining, index) => {
      if (remaining > 0) activeRowspans[index] = remaining - 1;
    });
    let cursor = 0;
    return row.cells.map((cell) => {
      while (cursor < columnCount && occupied[cursor]) cursor += 1;
      const start = cursor;
      const colspan = cell.colspan ?? 1;
      const rowspan = cell.rowspan ?? 1;
      for (let column = start; column < start + colspan; column += 1) {
        occupied[column] = true;
        if (rowspan > 1) activeRowspans[column] = rowspan - 1;
      }
      cursor += colspan;
      return start;
    });
  });
};

const toTiptapContent = (
  value?: string | null,
  availableWidth = 800,
): JSONContent => ({
  type: "doc",
  content: parseContractRichText(value).blocks.map((block) => {
    if (block.type === "paragraph") {
      return {
        type: "paragraph",
        attrs: { textAlign: block.alignment ?? "left" },
        content: runsToTiptapContent(block.runs),
      };
    }

    const columnWidths = columnWidthsBpsToPixels(
      block.columnWidthsBps,
      availableWidth,
    );
    const starts = cellStartColumns(block.rows, columnWidths.length);
    return {
      type: "table",
      content: block.rows.map((row, rowIndex) => ({
        type: "tableRow",
        content: row.cells.map((cell, cellIndex) => ({
          type: "tableCell",
          attrs: {
            colspan: cell.colspan ?? 1,
            rowspan: cell.rowspan ?? 1,
            colwidth: columnWidths.slice(
              starts[rowIndex][cellIndex],
              starts[rowIndex][cellIndex] + (cell.colspan ?? 1),
            ),
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

const tablePixelWidths = (table: JSONContent) => {
  const firstRow = (table.content ?? []).find(
    (row) => row.type === "tableRow",
  );
  const widths: number[] = [];
  (firstRow?.content ?? [])
    .filter(
      (cell) => cell.type === "tableCell" || cell.type === "tableHeader",
    )
    .forEach((cell) => {
      const colspan = Math.max(1, Number(cell.attrs?.colspan) || 1);
      const colwidth = Array.isArray(cell.attrs?.colwidth)
        ? cell.attrs.colwidth.filter(
            (width): width is number =>
              typeof width === "number" && Number.isFinite(width) && width > 0,
          )
        : [];
      widths.push(
        ...(colwidth.length === colspan
          ? colwidth
          : Array<number>(colspan).fill(100)),
      );
    });
  return widths;
};

const fromTiptapDocument = (document: JSONContent) => {
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
            const verticalAlign = cellVerticalAlignment(
              cell.attrs?.verticalAlign,
            );
            return {
              paragraphs: (cell.content ?? [])
                .filter((paragraph) => paragraph.type === "paragraph")
                .map(tiptapParagraph),
              ...(Number.isInteger(colspan) && colspan > 1 ? { colspan } : {}),
              ...(Number.isInteger(rowspan) && rowspan > 1 ? { rowspan } : {}),
              ...(verticalAlign ? { verticalAlign } : {}),
            };
          }),
      }));
    if (rows.length > 0) {
      const pixels = tablePixelWidths(node);
      blocks.push({
        type: "table",
        columnWidthsBps: normalizeColumnWidthsBps(pixels),
        rows,
      });
    }
  });
  return { blocks };
};

const fromTiptapContent = (document: JSONContent) =>
  serializeContractRichText(fromTiptapDocument(document));

export function ContractRichTextEditor({
  id,
  value,
  onChange,
  placeholder = "Nhập nội dung điều khoản...",
  ariaLabel = "Nội dung điều khoản",
  className,
}: ContractRichTextEditorProps) {
  const [isFullscreen, setIsFullscreen] = useState(false);
  const [resizeHint, setResizeHint] = useState<{
    left: number;
    top: number;
    label: string;
  }>();
  const fullscreenButtonRef = useRef<HTMLButtonElement>(null);
  const editorSurfaceRef = useRef<HTMLDivElement>(null);
  const editorContentWidthRef = useRef(DEFAULT_EDITOR_CONTENT_WIDTH);
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
          cellMinWidth: 20,
          HTMLAttributes: { class: "contract-rich-text-table" },
        },
        tableCell: false,
        tableHeader: false,
      }),
      ContractTableCell,
      ContractTableHeader,
    ],
    content: toTiptapContent(value, DEFAULT_EDITOR_CONTENT_WIDTH),
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
    editor.commands.setContent(
      toTiptapContent(nextValue, editorContentWidthRef.current),
      {
        emitUpdate: false,
      },
    );
  }, [editor, value]);

  useEffect(() => {
    const surface = editorSurfaceRef.current;
    if (!editor || !surface) return;

    const applyMeasuredWidth = () => {
      const nextWidth = measureEditorContentWidth(surface);
      if (Math.abs(nextWidth - editorContentWidthRef.current) < 2) return;
      editorContentWidthRef.current = nextWidth;
      const current = serializeContractRichText(
        fromTiptapDocument(editor.getJSON()),
      );
      editor.commands.setContent(toTiptapContent(current, nextWidth), {
        emitUpdate: false,
      });
    };

    applyMeasuredWidth();
    const observer = new ResizeObserver(applyMeasuredWidth);
    observer.observe(surface);
    return () => observer.disconnect();
  }, [editor, isFullscreen]);

  const clearResizeHint = () => {
    editorSurfaceRef.current?.classList.remove("contract-table-edge-hover");
    setResizeHint(undefined);
  };

  const showResizeHint = (event: ReactPointerEvent<HTMLDivElement>) => {
    const surface = editorSurfaceRef.current;
    const target = event.target as HTMLElement;
    const cell = target.closest<HTMLTableCellElement>(
      ".contract-rich-text-table td, .contract-rich-text-table th",
    );
    if (!surface || !cell) {
      if (event.buttons === 0) clearResizeHint();
      return;
    }

    const cellRect = cell.getBoundingClientRect();
    const table = cell.closest<HTMLTableElement>("table");
    if (!table || Math.abs(cellRect.right - event.clientX) > 10) {
      if (event.buttons === 0) clearResizeHint();
      return;
    }

    const surfaceRect = surface.getBoundingClientRect();
    const tableWidth = Math.max(1, table.getBoundingClientRect().width);
    surface.classList.add("contract-table-edge-hover");
    setResizeHint({
      left: cellRect.right - surfaceRect.left,
      top: cellRect.top - surfaceRect.top + 4,
      label: `${((cellRect.width / tableWidth) * 100).toFixed(1)}%`,
    });
  };

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
      ref={editorSurfaceRef}
      onPointerMove={showResizeHint}
      onPointerLeave={(event) => {
        if (event.buttons === 0) clearResizeHint();
      }}
      onPointerUp={clearResizeHint}
      className={cn(
        "contract-rich-text-editor-surface relative w-full min-w-0 max-w-full overflow-hidden rounded-md border bg-background",
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

      <div
        className={cn(
          "relative min-w-0 max-w-full overflow-hidden",
          isFullscreen && "min-h-0 flex-1",
        )}
      >
        {resizeHint && (
          <span
            className="pointer-events-none absolute z-30 -translate-x-1/2 rounded bg-primary px-1.5 py-0.5 text-[10px] font-semibold text-primary-foreground shadow"
            style={{ left: resizeHint.left, top: resizeHint.top }}
          >
            {resizeHint.label}
          </span>
        )}
        {editor.isEmpty && (
          <span className="pointer-events-none absolute left-3 top-2.5 z-10 text-sm text-muted-foreground">
            {placeholder}
          </span>
        )}
        <EditorContent
          editor={editor}
          className={cn(
            "min-w-0 max-w-full",
            isFullscreen && "h-full [&_.tiptap]:h-full",
          )}
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
