import { useEffect, useState } from "react";
import { Check, Plus, SlidersHorizontal, X } from "lucide-react";
import { useSearchParams } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { api, query } from "../lib/api";
import {
  formatDate,
  useDebounce,
  useResource,
  workingDays,
} from "../lib/hooks";
import {
  Badge,
  Button,
  DataState,
  ErrorMessage,
  Field,
  Modal,
  PageTitle,
  Pagination,
  SearchInput,
  SortHead,
} from "../components/UI";
import { EmployeePicker } from "../components/EmployeePicker";
import type {
  Balance,
  Department,
  LeavePage,
  LeaveRequest,
  LeaveType,
} from "../lib/types";

export function Requests({ pendingOnly = false }: { pendingOnly?: boolean }) {
  const { user } = useAuth();
  const role = user!.role;
  const [params, setParams] = useSearchParams();
  const notify = useToast();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [status, setStatus] = useState("");
  const [employee, setEmployee] = useState("");
  const [department, setDepartment] = useState("");
  const [leaveType, setLeaveType] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [sort, setSort] = useState("createdat");
  const [order, setOrder] = useState("desc");
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const [action, setAction] = useState<{
    type: "approve" | "reject" | "cancel";
    request: LeaveRequest;
  } | null>(null);
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const debounced = useDebounce(search);
  const departments = useResource<Department[]>(
    role === "Administrator"
      ? "/Departments"
      : role === "Manager"
        ? "/Departments/team-options"
        : null,
  );
  const types = useResource<LeaveType[]>(
    role !== "Employee" ? "/LeaveTypes/options" : null,
  );
  const ownBalances = useResource<Balance[]>(
    role === "Employee" ? "/LeaveRequests/my-balance" : null,
  );
  const endpoint =
    role === "Administrator"
      ? "/LeaveRequests"
      : role === "Employee"
        ? "/LeaveRequests/my"
        : pendingOnly
          ? "/LeaveRequests/pending"
          : "/LeaveRequests/team";
  const requests = useResource<LeavePage>(
    endpoint +
      query({
        page,
        pageSize,
        search: debounced,
        status: pendingOnly ? "" : status,
        employeeId: employee,
        departmentId: department,
        leaveTypeId: leaveType,
        fromDate,
        toDate,
        sortBy: sort,
        sortOrder: order,
      }),
  );
  const rows = requests.data?.items || requests.data?.requests || [];
  useEffect(() => {
    if (params.get("new") === "1" && role === "Employee") {
      setCreateOpen(true);
      setParams({}, { replace: true });
    }
  }, [params, role, setParams]);
  const change = (setter: (v: string) => void, value: string) => {
    setter(value);
    setPage(1);
  };
  const sortChange = (field: string, direction: string) => {
    setSort(field);
    setOrder(direction);
    setPage(1);
  };
  const perform = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!action) return;
    setBusy(true);
    setError("");
    try {
      await api(`/LeaveRequests/${action.request.id}/${action.type}`, {
        method: "PUT",
        ...(action.type === "reject"
          ? { body: { reason: reason.trim() } }
          : {}),
      });
      notify(
        action.type === "approve"
          ? "Leave request approved."
          : action.type === "reject"
            ? "Leave request rejected."
            : "Leave request cancelled. Your reserved days have been returned.",
      );
      setAction(null);
      requests.reload();
      ownBalances.reload();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  const openAction = (
    type: "approve" | "reject" | "cancel",
    request: LeaveRequest,
  ) => {
    setAction({ type, request });
    setReason("");
    setError("");
  };
  return (
    <>
      <PageTitle
        eyebrow={
          pendingOnly
            ? "A LITTLE ATTENTION GOES A LONG WAY"
            : "TIME OFF, ALL TOGETHER"
        }
        title={
          pendingOnly
            ? "Pending approvals"
            : role === "Employee"
              ? "My leave requests"
              : role === "Manager"
                ? "Team leave requests"
                : "Leave requests"
        }
        description={
          pendingOnly
            ? "Review your team’s plans and keep everyone on the same page."
            : role === "Employee"
              ? "Plan a break, check its progress, and keep your history close."
              : "A clear view of leave across your people and teams."
        }
        action={
          role === "Employee" ? (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus size={18} />
              Request time off
            </Button>
          ) : (
            <span className="count-pill">
              {requests.data?.totalCount ?? "—"} requests
            </span>
          )
        }
      />
      <section className="panel">
        <div className="toolbar">
          <SearchInput
            value={search}
            onChange={(v) => change(setSearch, v)}
            placeholder="Search leave requests…"
          />
          <div className="toolbar-actions">
            {!pendingOnly && (
              <select
                aria-label="Filter by status"
                value={status}
                onChange={(e) => change(setStatus, e.target.value)}
              >
                <option value="">All statuses</option>
                <option value="1">Pending</option>
                <option value="2">Approved</option>
                <option value="3">Rejected</option>
                <option value="4">Cancelled</option>
              </select>
            )}
            <Button
              variant="secondary"
              onClick={() => setFiltersOpen((v) => !v)}
              aria-expanded={filtersOpen}
            >
              <SlidersHorizontal size={16} />
              Filters
            </Button>
          </div>
        </div>
        {filtersOpen && (
          <div className="filters-grid">
            {role !== "Employee" && (
              <EmployeePicker
                label="Employee"
                value={employee}
                onChange={(v) => change(setEmployee, v)}
                team={role === "Manager"}
              />
            )}
            {role !== "Employee" && (
              <Field label="Department">
                <select
                  value={department}
                  onChange={(e) => change(setDepartment, e.target.value)}
                  disabled={departments.loading}
                >
                  <option value="">All departments</option>
                  {departments.data?.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
                {departments.error && (
                  <small className="field-error">{departments.error}</small>
                )}
              </Field>
            )}
            <Field label="Leave type">
              <select
                value={leaveType}
                onChange={(e) => change(setLeaveType, e.target.value)}
              >
                <option value="">All leave types</option>
                {role === "Employee"
                  ? ownBalances.data?.map((b) => (
                      <option key={b.leaveTypeId} value={b.leaveTypeId}>
                        {b.leaveTypeName}
                      </option>
                    ))
                  : types.data?.map((t) => (
                      <option key={t.id} value={t.id}>
                        {t.name}
                      </option>
                    ))}
              </select>
              {types.error && (
                <small className="field-error">{types.error}</small>
              )}
            </Field>
            <Field label="From date">
              <input
                type="date"
                value={fromDate}
                onChange={(e) => change(setFromDate, e.target.value)}
              />
            </Field>
            <Field label="To date">
              <input
                type="date"
                min={fromDate || undefined}
                value={toDate}
                onChange={(e) => change(setToDate, e.target.value)}
              />
            </Field>
            <Button
              variant="ghost"
              onClick={() => {
                setEmployee("");
                setDepartment("");
                setLeaveType("");
                setFromDate("");
                setToDate("");
                setStatus("");
                setPage(1);
              }}
            >
              Reset filters
            </Button>
          </div>
        )}
        <DataState
          loading={requests.loading}
          error={requests.error}
          retry={requests.reload}
          empty={!rows.length}
          emptyTitle={pendingOnly ? "All caught up" : "No requests found"}
          emptyDescription={
            pendingOnly
              ? "There are no pending requests matching these filters."
              : "Try another search or submit your first request."
          }
        >
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  {role !== "Employee" && (
                    <th>
                      <SortHead
                        label="Employee"
                        field="employee"
                        sort={sort}
                        order={order}
                        onChange={sortChange}
                      />
                    </th>
                  )}
                  <th>
                    <SortHead
                      label="Leave type"
                      field="leavetype"
                      sort={sort}
                      order={order}
                      onChange={sortChange}
                    />
                  </th>
                  <th>
                    <SortHead
                      label="Dates"
                      field="startdate"
                      sort={sort}
                      order={order}
                      onChange={sortChange}
                    />
                  </th>
                  <th>Days</th>
                  <th>
                    <SortHead
                      label="Status"
                      field="status"
                      sort={sort}
                      order={order}
                      onChange={sortChange}
                    />
                  </th>
                  <th>Reason</th>
                  {role !== "Administrator" && (
                    <th className="right">Actions</th>
                  )}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.id}>
                    {role !== "Employee" && (
                      <td>
                        <strong>{row.employeeName}</strong>
                        <small>Employee #{row.employeeId}</small>
                      </td>
                    )}
                    <td>
                      <strong>{row.leaveTypeName}</strong>
                      <small>Request #{row.id}</small>
                    </td>
                    <td className="nowrap">
                      {formatDate(row.startDate)}
                      <small>to {formatDate(row.endDate)}</small>
                    </td>
                    <td>
                      <span className="days-pill">{row.days}d</span>
                    </td>
                    <td>
                      <Badge value={row.status} />
                    </td>
                    <td className="reason-cell">
                      <span>{row.reason}</span>
                      {row.rejectReason && (
                        <small className="rejection-reason">
                          Rejected: {row.rejectReason}
                        </small>
                      )}
                    </td>
                    {role !== "Administrator" && (
                      <td className="right">
                        {row.status === "Pending" ? (
                          role === "Employee" ? (
                            <Button
                              variant="ghost"
                              onClick={() => openAction("cancel", row)}
                            >
                              Cancel
                            </Button>
                          ) : (
                            <div className="row-actions">
                              <button
                                className="approve-button"
                                aria-label={`Approve request ${row.id}`}
                                onClick={() => openAction("approve", row)}
                              >
                                <Check size={16} />
                                Approve
                              </button>
                              <button
                                className="reject-button"
                                aria-label={`Reject request ${row.id}`}
                                onClick={() => openAction("reject", row)}
                              >
                                <X size={16} />
                              </button>
                            </div>
                          )
                        ) : (
                          <span className="muted">—</span>
                        )}
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </DataState>
        <div className="table-bottom">
          <select
            aria-label="Requests per page"
            value={pageSize}
            onChange={(e) => {
              setPageSize(Number(e.target.value));
              setPage(1);
            }}
          >
            <option value="10">10 per page</option>
            <option value="20">20 per page</option>
            <option value="50">50 per page</option>
          </select>
          <Pagination
            page={page}
            pageSize={pageSize}
            totalPages={requests.data?.totalPages || 0}
            totalCount={requests.data?.totalCount || 0}
            onChange={setPage}
            loading={requests.loading}
          />
        </div>
      </section>
      {createOpen && (
        <CreateRequest
          onClose={() => setCreateOpen(false)}
          onSaved={() => {
            setCreateOpen(false);
            setPage(1);
            requests.reload();
            ownBalances.reload();
            notify("Leave request submitted. Your manager can now review it.");
          }}
        />
      )}
      {action && (
        <Modal
          title={
            action.type === "approve"
              ? "Approve this request?"
              : action.type === "reject"
                ? "Reject leave request"
                : "Cancel this request?"
          }
          description={`${action.request.employeeName} · ${action.request.leaveTypeName} · ${action.request.days} working days`}
          onClose={() => setAction(null)}
          busy={busy}
        >
          <form onSubmit={perform}>
            <ErrorMessage message={error} />
            {action.type === "reject" ? (
              <Field label="Rejection reason">
                <textarea
                  rows={4}
                  value={reason}
                  maxLength={500}
                  required
                  onChange={(e) => setReason(e.target.value)}
                  placeholder="Help your employee understand the decision…"
                  disabled={busy}
                />
              </Field>
            ) : (
              <p className="confirmation-copy">
                {action.type === "approve"
                  ? "The reserved days will move to used leave. This decision cannot be reversed from this screen."
                  : "Your reserved days will be returned to your available balance."}
              </p>
            )}
            <div className="modal-actions">
              <Button
                type="button"
                variant="secondary"
                disabled={busy}
                onClick={() => setAction(null)}
              >
                Go back
              </Button>
              <Button
                type="submit"
                variant={
                  action.type === "reject" || action.type === "cancel"
                    ? "danger"
                    : "primary"
                }
                busy={busy}
                disabled={action.type === "reject" && !reason.trim()}
              >
                {action.type === "approve"
                  ? "Approve request"
                  : action.type === "reject"
                    ? "Reject request"
                    : "Cancel request"}
              </Button>
            </div>
          </form>
        </Modal>
      )}
    </>
  );
}
function CreateRequest({
  onClose,
  onSaved,
}: {
  onClose: () => void;
  onSaved: () => void;
}) {
  const balances = useResource<Balance[]>("/LeaveRequests/my-balance");
  const [type, setType] = useState("");
  const [start, setStart] = useState("");
  const [end, setEnd] = useState("");
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const days = workingDays(start, end);
  const balance = balances.data?.find((b) => b.leaveTypeId === Number(type));
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setError("");
    if (!days) {
      setError(
        "Choose dates with at least one working day. Friday and Saturday are excluded.",
      );
      return;
    }
    if (!balance || days > balance.remainingDays) {
      setError("The selected dates exceed your available balance.");
      return;
    }
    setBusy(true);
    try {
      await api("/LeaveRequests", {
        method: "POST",
        body: {
          leaveTypeId: Number(type),
          startDate: start,
          endDate: end,
          reason: reason.trim(),
        },
      });
      onSaved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  const active = balances.data?.filter((b) => b.isActive) || [];
  return (
    <Modal
      title="Make a little time for yourself."
      description="Tell us your plans. Your manager will take it from here."
      onClose={onClose}
      busy={busy}
    >
      <DataState
        loading={balances.loading}
        error={balances.error}
        retry={balances.reload}
        empty={!active.length}
        emptyTitle="No active leave allocation"
        emptyDescription="Contact your administrator to allocate an active leave type."
      >
        <form onSubmit={submit}>
          <ErrorMessage message={error} />
          <fieldset disabled={busy}>
            <Field label="Leave type">
              <select
                value={type}
                onChange={(e) => setType(e.target.value)}
                required
              >
                <option value="">Choose a leave type</option>
                {active.map((b) => (
                  <option key={b.id} value={b.leaveTypeId}>
                    {b.leaveTypeName} · {b.remainingDays} days available
                  </option>
                ))}
              </select>
            </Field>
            <div className="form-grid">
              <Field label="Start date">
                <input
                  type="date"
                  required
                  value={start}
                  onChange={(e) => setStart(e.target.value)}
                />
              </Field>
              <Field label="End date">
                <input
                  type="date"
                  required
                  min={start || undefined}
                  value={end}
                  onChange={(e) => setEnd(e.target.value)}
                />
              </Field>
            </div>
            <Field label="Reason" hint={`${reason.length}/500 characters`}>
              <textarea
                rows={4}
                placeholder="A short note about your time off…"
                required
                maxLength={500}
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
            </Field>
          </fieldset>
          <div className="leave-estimate">
            <span>
              Requested time off
              <strong>
                {days} working {days === 1 ? "day" : "days"}
              </strong>
            </span>
            <span>
              Balance after reservation
              <strong>
                {balance ? balance.remainingDays - days : "—"} days
              </strong>
            </span>
            <small>
              Friday and Saturday are excluded. Your manager’s decision is
              required.
            </small>
          </div>
          <div className="modal-actions">
            <Button
              type="button"
              variant="secondary"
              disabled={busy}
              onClick={onClose}
            >
              Cancel
            </Button>
            <Button
              type="submit"
              busy={busy}
              disabled={!type || !start || !end || !reason.trim()}
            >
              Submit request
            </Button>
          </div>
        </form>
      </DataState>
    </Modal>
  );
}
