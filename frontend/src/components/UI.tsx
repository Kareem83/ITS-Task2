import { useEffect, useRef } from "react";
import type { ReactNode } from "react";
import {
  ArrowDown,
  ArrowUp,
  ArrowUpDown,
  ChevronLeft,
  ChevronRight,
  Inbox,
  LoaderCircle,
  RotateCw,
  Search,
  TriangleAlert,
  X,
} from "lucide-react";

export function PageTitle({
  eyebrow,
  title,
  description,
  action,
}: {
  eyebrow: string;
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="page-heading">
      <div>
        <div className="eyebrow">{eyebrow}</div>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {action}
    </div>
  );
}
export function Button({
  children,
  busy,
  variant = "primary",
  ...props
}: React.ButtonHTMLAttributes<HTMLButtonElement> & {
  busy?: boolean;
  variant?: "primary" | "secondary" | "danger" | "ghost";
}) {
  return (
    <button
      type="button"
      {...props}
      disabled={props.disabled || busy}
      className={`btn btn-${variant} ${props.className || ""}`}
    >
      {busy && <LoaderCircle size={17} className="spin" />}
      {children}
    </button>
  );
}
export function ErrorMessage({ message }: { message: string }) {
  return message ? (
    <div className="error-message" role="alert">
      <TriangleAlert size={18} />
      <span>{message}</span>
    </div>
  ) : null;
}
export function DataState({
  loading,
  error,
  retry,
  empty,
  children,
  emptyTitle = "No results yet",
  emptyDescription = "Try changing your filters or add your first record.",
}: {
  loading: boolean;
  error: string;
  retry: () => void;
  empty?: boolean;
  children: ReactNode;
  emptyTitle?: string;
  emptyDescription?: string;
}) {
  if (loading)
    return (
      <div className="data-state" role="status">
        <LoaderCircle className="spin" size={27} />
        <strong>Loading your workspace…</strong>
        <span>Getting the latest information.</span>
      </div>
    );
  if (error)
    return (
      <div className="data-state">
        <TriangleAlert size={29} />
        <strong>We couldn’t load this information</strong>
        <span role="alert">{error}</span>
        <Button variant="secondary" onClick={retry}>
          <RotateCw size={16} />
          Try again
        </Button>
      </div>
    );
  if (empty)
    return (
      <div className="data-state">
        <div className="empty-icon">
          <Inbox size={29} />
        </div>
        <strong>{emptyTitle}</strong>
        <span>{emptyDescription}</span>
      </div>
    );
  return <>{children}</>;
}
export function SearchInput({
  value,
  onChange,
  placeholder = "Search…",
}: {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}) {
  return (
    <div className="search-input">
      <Search size={18} />
      <input
        aria-label={placeholder}
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
      />
      {value && (
        <button aria-label="Clear search" onClick={() => onChange("")}>
          <X size={14} />
        </button>
      )}
    </div>
  );
}
export function Field({
  label,
  children,
  hint,
}: {
  label: string;
  children: ReactNode;
  hint?: string;
}) {
  return (
    <label className="field">
      <span>{label}</span>
      {children}
      {hint && <small>{hint}</small>}
    </label>
  );
}
export function Modal({
  title,
  description,
  children,
  onClose,
  busy,
}: {
  title: string;
  description?: string;
  children: ReactNode;
  onClose: () => void;
  busy?: boolean;
}) {
  const ref = useRef<HTMLDialogElement>(null);
  useEffect(() => {
    const dialog = ref.current;
    const previous = document.activeElement as HTMLElement;
    dialog?.showModal();
    return () => {
      dialog?.close();
      previous?.focus();
    };
  }, []);
  return (
    <dialog
      ref={ref}
      className="modal"
      aria-label={title}
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onClose();
      }}
    >
      <div className="modal-inner">
        <div className="modal-header">
          <div>
            <h2>{title}</h2>
            {description && <p>{description}</p>}
          </div>
          <button
            className="icon-button"
            disabled={busy}
            aria-label="Close dialog"
            onClick={onClose}
          >
            <X size={21} />
          </button>
        </div>
        {children}
      </div>
    </dialog>
  );
}
export function Badge({ value }: { value: string }) {
  return (
    <span className={`badge badge-${value.toLowerCase().replace(/\s/g, "-")}`}>
      <span />
      {value}
    </span>
  );
}
export function Pagination({
  page,
  totalPages,
  totalCount,
  pageSize,
  onChange,
  loading,
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  pageSize: number;
  onChange: (page: number) => void;
  loading?: boolean;
}) {
  const from = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  return (
    <div className="pagination">
      <span>
        Showing{" "}
        <strong>
          {from}–{Math.min(page * pageSize, totalCount)}
        </strong>{" "}
        of <strong>{totalCount}</strong>
      </span>
      <div>
        <button
          aria-label="Previous page"
          disabled={page <= 1 || loading}
          onClick={() => onChange(page - 1)}
        >
          <ChevronLeft size={17} />
        </button>
        <span>
          Page {page} of {Math.max(1, totalPages)}
        </span>
        <button
          aria-label="Next page"
          disabled={page >= totalPages || loading}
          onClick={() => onChange(page + 1)}
        >
          <ChevronRight size={17} />
        </button>
      </div>
    </div>
  );
}
export function SortHead({
  label,
  field,
  sort,
  order,
  onChange,
}: {
  label: string;
  field: string;
  sort: string;
  order: string;
  onChange: (sort: string, order: string) => void;
}) {
  const Icon =
    sort !== field ? ArrowUpDown : order === "asc" ? ArrowUp : ArrowDown;
  return (
    <button
      className="sort-head"
      onClick={() =>
        onChange(field, sort === field && order === "asc" ? "desc" : "asc")
      }
    >
      {label}
      <Icon size={13} />
    </button>
  );
}
