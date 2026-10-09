import { query } from "../lib/api";
import { useDebounce, useResource } from "../lib/hooks";
import { useState } from "react";
import type { EmployeePage } from "../lib/types";

export function EmployeePicker({
  label,
  value,
  onChange,
  managersOnly = false,
  team = false,
  allowNone = true,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  managersOnly?: boolean;
  team?: boolean;
  allowNone?: boolean;
}) {
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const debounced = useDebounce(search);
  const options = useResource<EmployeePage>(
    `/Employees${team ? "/my-team" : ""}${query({ search: debounced, page, pageSize: 20, sortBy: "name", role: managersOnly ? "Manager" : undefined, isActive: managersOnly ? true : undefined })}`,
  );
  const rows = options.data?.employees || [];
  return (
    <div className="employee-picker">
      <div className="field">
        <span>{label}</span>
        <input
          aria-label={`Search ${label.toLowerCase()}`}
          placeholder="Find by name or email…"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
            setPage(1);
          }}
        />
        <select
          aria-label={label}
          value={value}
          disabled={options.loading}
          onChange={(e) => onChange(e.target.value)}
          required={!allowNone}
        >
          <option value="">
            {options.loading
              ? "Loading…"
              : allowNone
                ? "None / all"
                : "Select an employee"}
          </option>
          {value && !rows.some((e) => String(e.employeeId) === value) && (
            <option value={value}>Selected employee #{value}</option>
          )}
          {rows.map((e) => (
            <option key={e.employeeId} value={e.employeeId}>
              {e.fullName} · {e.email}
            </option>
          ))}
        </select>
      </div>
      {options.error && (
        <small className="field-error" role="alert">
          {options.error}
          <button type="button" onClick={options.reload}>
            Retry
          </button>
        </small>
      )}
      {(options.data?.totalPages || 0) > 1 && (
        <div className="lookup-pagination">
          <button
            type="button"
            disabled={page <= 1 || options.loading}
            onClick={() => setPage((p) => p - 1)}
          >
            Previous
          </button>
          <span>
            {page} / {options.data?.totalPages}
          </span>
          <button
            type="button"
            disabled={
              page >= (options.data?.totalPages || 0) || options.loading
            }
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </button>
        </div>
      )}
    </div>
  );
}
