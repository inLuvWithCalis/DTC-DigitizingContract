import { toast } from "@/components/ui/sonner";
import { useEffect, useRef, useState } from "react";

interface ConfirmToastProps {
  title?: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void | Promise<void>;
  onCancel?: () => void;
}

const ConfirmToast = ({
  id,
  title,
  description,
  confirmLabel,
  cancelLabel,
  onConfirm,
  onCancel,
}: ConfirmToastProps & { id: string | number }) => {
  const ref = useRef<HTMLDivElement>(null);

  const [isActionPending, setIsActionPending] = useState(false);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (ref.current && !ref.current.contains(event.target as Node)) {
        toast.dismiss(id);
        if (onCancel) onCancel();
      }
    };

    const timeoutId = setTimeout(() => {
      document.addEventListener("mousedown", handleClickOutside);
    }, 0);

    return () => {
      clearTimeout(timeoutId);
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [id, onCancel]);

  const handleConfirm = async () => {
    if (isActionPending) return;
    setIsActionPending(true);

    setTimeout(async () => {
      await onConfirm();
      toast.dismiss(id);
    }, 10);
  };

  const handleCancel = () => {
    if (isActionPending) return;
    setIsActionPending(true);

    setTimeout(() => {
      if (onCancel) onCancel();
      toast.dismiss(id);
    }, 10);
  };

  return (
    <div
      ref={ref}
      className="pointer-events-auto flex flex-col gap-3 w-full bg-white dark:bg-zinc-950 border border-zinc-200 dark:border-zinc-800 p-4 rounded-lg shadow-lg z-[9999]"
    >
      <div className="flex flex-col gap-1">
        <h3 className="font-semibold text-sm text-zinc-900 dark:text-zinc-50">
          {title || "Chắc chắn thực hiện hành động này?"}
        </h3>
        <p className="text-sm text-zinc-500 dark:text-zinc-400">
          {description || "Hành động này không thể hoàn tác."}
        </p>
      </div>
      <div className="flex gap-2 justify-end mt-1">
        <button
          onClick={handleCancel}
          disabled={isActionPending}
          className="text-xs font-medium px-3 py-2 rounded-md hover:bg-zinc-100 dark:hover:bg-zinc-800 transition-colors text-zinc-600 dark:text-zinc-300 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {cancelLabel || "Hủy"}
        </button>
        <button
          onClick={handleConfirm}
          disabled={isActionPending}
          className="text-xs font-medium px-3 py-2 rounded-md bg-primary text-white hover:bg-indigo-500 dark:bg-zinc-50 dark:text-zinc-900 dark:hover:bg-zinc-200 transition-colors cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
        >
          {confirmLabel || "Xác nhận"}
        </button>
      </div>
    </div>
  );
};

export const showConfirmToast = ({
  title,
  description,
  confirmLabel,
  cancelLabel,
  onConfirm,
  onCancel,
}: ConfirmToastProps) => {
  toast.custom(
    (id) => (
      <ConfirmToast
        id={id}
        title={title}
        description={description}
        confirmLabel={confirmLabel}
        cancelLabel={cancelLabel}
        onConfirm={onConfirm}
        onCancel={onCancel}
      />
    ),
    {
      duration: Infinity,
    },
  );
};
