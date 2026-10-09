import { useState } from "react";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { api, query } from "../lib/api";
import { useDebounce, useResource, formatDate } from "../lib/hooks";
import type { Department, LeaveType, AuditLog, Page } from "../lib/types";
import {
  Button,
  DataState,
  ErrorMessage,
  Field,
  Modal,
  PageTitle,
  SearchInput,
  Badge,
  Pagination,
} from "../components/UI";
import { useToast } from "../context/ToastContext";

export function Settings({ leaveTypes = false }: { leaveTypes?: boolean }) {
  const path = leaveTypes ? "/LeaveTypes" : "/Departments";
  const resource = useResource<(Department & Partial<LeaveType>)[]>(path);
  const [search, setSearch] = useState("");
  const [edit, setEdit] = useState<
    (Department & Partial<LeaveType>) | null | undefined
  >();
  const [remove, setRemove] = useState<
    (Department & Partial<LeaveType>) | null
  >(null);
  const [name, setName] = useState(""),
    [days, setDays] = useState(21),
    [annual, setAnnual] = useState(false);
  const [busy, setBusy] = useState(false),
    [error, setError] = useState("");
  const notify = useToast();
  const rows =
    resource.data?.filter((x) =>
      x.name.toLowerCase().includes(search.toLowerCase()),
    ) || [];
  function open(item: typeof edit) {
    setEdit(item);
    setName(item?.name || "");
    setDays(item?.allowedDays ?? 21);
    setAnnual(item?.isAnnual ?? false);
    setError("");
  }
  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError("");
    try {
      await api(path + (edit ? `/${edit.id}` : ""), {
        method: edit ? "PUT" : "POST",
        body: leaveTypes
          ? { name: name.trim(), allowedDays: days, isAnnual: annual }
          : { name: name.trim() },
      });
      setEdit(undefined);
      resource.reload();
      notify("Changes saved successfully.");
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  async function confirm() {
    if (!remove) return;
    setBusy(true);
    setError("");
    try {
      await api(`${path}/${remove.id}${leaveTypes ? "/deactivate" : ""}`, {
        method: leaveTypes ? "PUT" : "DELETE",
      });
      setRemove(null);
      resource.reload();
      notify(
        leaveTypes
          ? "Leave type deactivated. Existing history is preserved."
          : "Department removed.",
      );
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }
  return (
    <>
      <PageTitle
        eyebrow="WORKSPACE SETTINGS"
        title={leaveTypes ? "Leave types" : "Departments"}
        description={
          leaveTypes
            ? "Set your leave policies and annual allowances."
            : "Give every team a place in your organization."
        }
        action={
          <Button onClick={() => open(null)}>
            <Plus size={17} />
            Add {leaveTypes ? "leave type" : "department"}
          </Button>
        }
      />
      <section className="panel">
        <div className="toolbar">
          <SearchInput
            value={search}
            onChange={setSearch}
            placeholder="Search by name"
          />
        </div>
        <DataState
          loading={resource.loading}
          error={resource.error}
          retry={resource.reload}
          empty={!rows.length}
        >
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  {leaveTypes && (
                    <>
                      <th>Allowance</th>
                      <th>Policy</th>
                      <th>Status</th>
                    </>
                  )}
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    <td>
                      <strong>{row.name}</strong>
                      <small>#{row.id}</small>
                    </td>
                    {leaveTypes && (
                      <>
                        <td>{row.allowedDays} days</td>
                        <td>
                          {row.isAnnual ? "Annual leave" : "Standard leave"}
                        </td>
                        <td>
                          <Badge value={row.isActive ? "Active" : "Inactive"} />
                        </td>
                      </>
                    )}
                    <td>
                      <div className="row-actions">
                        <button
                          className="icon-button"
                          aria-label={`Edit ${row.name}`}
                          onClick={() => open(row)}
                        >
                          <Pencil size={17} />
                        </button>
                        {(!leaveTypes || row.isActive) && (
                          <button
                            className="icon-button"
                            aria-label={`${leaveTypes ? "Deactivate" : "Delete"} ${row.name}`}
                            onClick={() => {
                              setRemove(row);
                              setError("");
                            }}
                          >
                            <Trash2 size={17} />
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </DataState>
      </section>
      {edit !== undefined && (
        <Modal
          title={`${edit ? "Edit" : "Add"} ${leaveTypes ? "leave type" : "department"}`}
          onClose={() => setEdit(undefined)}
          busy={busy}
        >
          <form onSubmit={save}>
            <ErrorMessage message={error} />
            <Field label="Name">
              <input
                required
                maxLength={100}
                value={name}
                onChange={(e) => setName(e.target.value)}
                autoFocus
              />
            </Field>
            {leaveTypes && (
              <>
                <Field label="Allowed days">
                  <input
                    type="number"
                    required
                    min={0}
                    max={365}
                    value={days}
                    onChange={(e) => setDays(Number(e.target.value))}
                  />
                </Field>
                <label className="checkbox-field">
                  <input
                    type="checkbox"
                    checked={annual}
                    onChange={(e) => setAnnual(e.target.checked)}
                  />
                  Use as the annual leave policy
                </label>
                <p className="form-note">
                  Only one annual policy is allowed. Existing allocations are
                  managed by the server.
                </p>
              </>
            )}
            <div className="modal-actions">
              <Button
                type="button"
                variant="secondary"
                disabled={busy}
                onClick={() => setEdit(undefined)}
              >
                Cancel
              </Button>
              <Button type="submit" busy={busy}>
                Save changes
              </Button>
            </div>
          </form>
        </Modal>
      )}
      {remove && (
        <Modal
          title={leaveTypes ? "Deactivate leave type" : "Delete department"}
          onClose={() => setRemove(null)}
          busy={busy}
        >
          <p>
            {leaveTypes
              ? `Deactivate ${remove.name}? Existing requests and balances will be retained.`
              : `Delete ${remove.name}? Departments with assigned employees cannot be deleted.`}
          </p>
          <ErrorMessage message={error} />
          <div className="modal-actions">
            <Button
              variant="secondary"
              disabled={busy}
              onClick={() => setRemove(null)}
            >
              Cancel
            </Button>
            <Button variant="danger" busy={busy} onClick={confirm}>
              Confirm
            </Button>
          </div>
        </Modal>
      )}
    </>
  );
}
export function Audit() {
  const [search, setSearch] = useState(""),
    [page, setPage] = useState(1),
    [action, setAction] = useState(""),
    [from, setFrom] = useState(""),
    [to, setTo] = useState("");
  const debounced = useDebounce(search);
  const logs = useResource<Page<AuditLog>>(
    "/AuditLogs/paged" +
      query({
        search: debounced,
        page,
        pageSize: 20,
        action,
        fromDate: from,
        toDate: to,
        sortBy: "timestamp",
        sortOrder: "desc",
      }),
  );
  return (
    <>
      <PageTitle
        eyebrow="ACCOUNTABILITY"
        title="Audit history"
        description="A clear record of the changes made across your workspace."
      />
      <section className="panel">
        <div className="toolbar">
          <SearchInput
            value={search}
            onChange={(v) => {
              setSearch(v);
              setPage(1);
            }}
            placeholder="Search audit history"
          />
          <input
            aria-label="Filter action"
            placeholder="Exact action"
            value={action}
            onChange={(e) => {
              setAction(e.target.value);
              setPage(1);
            }}
          />
          <input
            aria-label="From date"
            type="date"
            value={from}
            onChange={(e) => {
              setFrom(e.target.value);
              setPage(1);
            }}
          />
          <input
            aria-label="To date"
            type="date"
            min={from}
            value={to}
            onChange={(e) => {
              setTo(e.target.value);
              setPage(1);
            }}
          />
        </div>
        <DataState
          loading={logs.loading}
          error={logs.error}
          retry={logs.reload}
          empty={!logs.data?.items.length}
        >
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Action</th>
                  <th>Performed by</th>
                  <th>Record</th>
                  <th>Details</th>
                  <th>Date</th>
                </tr>
              </thead>
              <tbody>
                {logs.data?.items.map((log) => (
                  <tr key={log.id}>
                    <td>
                      <strong>{log.action}</strong>
                    </td>
                    <td>{log.userName || "System"}</td>
                    <td>
                      {log.entityName}
                      <small>#{log.entityId}</small>
                    </td>
                    <td className="audit-details">{log.details || "—"}</td>
                    <td>
                      {formatDate(log.timestamp)}
                      <small>
                        {new Date(log.timestamp).toLocaleTimeString()}
                      </small>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </DataState>
        {logs.data && (
          <Pagination
            page={page}
            pageSize={20}
            totalCount={logs.data.totalCount}
            totalPages={logs.data.totalPages}
            onChange={setPage}
          />
        )}
      </section>
    </>
  );
}
