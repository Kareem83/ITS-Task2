import { useState } from "react";
import { Mail, Pencil, Plus, SlidersHorizontal } from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { useToast } from "../context/ToastContext";
import { api, query } from "../lib/api";
import {
  formatDate,
  initials,
  inputDate,
  useDebounce,
  useResource,
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
import type { Department, Employee, EmployeePage } from "../lib/types";

type Mode = {
  type: "edit" | "assign" | "deactivate" | "account" | "view";
  employee: Employee;
};
export function Employees({ team = false }: { team?: boolean }) {
  const { user } = useAuth();
  const notify = useToast();
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [department, setDepartment] = useState("");
  const [manager, setManager] = useState("");
  const [active, setActive] = useState("");
  const [role, setRole] = useState("");
  const [sort, setSort] = useState("name");
  const [order, setOrder] = useState("asc");
  const [filters, setFilters] = useState(false);
  const [adding, setAdding] = useState(false);
  const [mode, setMode] = useState<Mode | null>(null);
  const departments = useResource<Department[]>(
    team ? "/Departments/team-options" : "/Departments",
  );
  const employees = useResource<EmployeePage>(
    `/Employees${team ? "/my-team" : ""}${query({ page, pageSize, search: useDebounce(search), departmentId: department, managerId: manager, isActive: active, role, sortBy: sort, sortOrder: order })}`,
  );
  const change = (setter: (v: string) => void, value: string) => {
    setter(value);
    setPage(1);
  };
  const saved = (message: string) => {
    setMode(null);
    setAdding(false);
    employees.reload();
    notify(message);
  };
  return (
    <>
      <PageTitle
        eyebrow={team ? "THE PEOPLE BEHIND THE WORK" : "PEOPLE COME FIRST"}
        title={team ? "My team" : "Your people"}
        description={
          team
            ? "Get to know the people assigned to you and keep your team connected."
            : "A home for every employee, their details, and their place in the team."
        }
        action={
          !team && (
            <Button onClick={() => setAdding(true)}>
              <Plus size={18} />
              Add employee
            </Button>
          )
        }
      />
      <section className="panel">
        <div className="toolbar">
          <SearchInput
            value={search}
            onChange={(v) => change(setSearch, v)}
            placeholder="Search people by name, email, or job…"
          />
          <div className="toolbar-actions">
            <select
              aria-label="Filter by employee status"
              value={active}
              onChange={(e) => change(setActive, e.target.value)}
            >
              <option value="">All statuses</option>
              <option value="true">Active</option>
              <option value="false">Inactive</option>
            </select>
            <Button
              variant="secondary"
              aria-expanded={filters}
              onClick={() => setFilters((v) => !v)}
            >
              <SlidersHorizontal size={16} />
              Filters
            </Button>
          </div>
        </div>
        {filters && (
          <div className="filters-grid">
            <Field label="Department">
              <select
                value={department}
                onChange={(e) => change(setDepartment, e.target.value)}
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
            <Field label="Role">
              <select
                value={role}
                onChange={(e) => change(setRole, e.target.value)}
              >
                <option value="">All roles</option>
                <option>Employee</option>
                <option>Manager</option>
                <option>Administrator</option>
              </select>
            </Field>
            {!team && (
              <EmployeePicker
                label="Manager"
                value={manager}
                onChange={(v) => change(setManager, v)}
                managersOnly
              />
            )}
            <Button
              variant="ghost"
              onClick={() => {
                setDepartment("");
                setRole("");
                setManager("");
                setActive("");
                setPage(1);
              }}
            >
              Reset filters
            </Button>
          </div>
        )}
        <DataState
          loading={employees.loading}
          error={employees.error}
          retry={employees.reload}
          empty={!employees.data?.employees.length}
          emptyTitle="No people found"
          emptyDescription={
            team
              ? "No team members match these filters. Your administrator manages team assignments."
              : "Try a different search or add a new employee."
          }
        >
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    <SortHead
                      label="Employee"
                      field="name"
                      sort={sort}
                      order={order}
                      onChange={(s, o) => {
                        setSort(s);
                        setOrder(o);
                        setPage(1);
                      }}
                    />
                  </th>
                  <th>
                    <SortHead
                      label="Department"
                      field="department"
                      sort={sort}
                      order={order}
                      onChange={(s, o) => {
                        setSort(s);
                        setOrder(o);
                        setPage(1);
                      }}
                    />
                  </th>
                  <th>Role</th>
                  <th>
                    <SortHead
                      label="Hire date"
                      field="hiredate"
                      sort={sort}
                      order={order}
                      onChange={(s, o) => {
                        setSort(s);
                        setOrder(o);
                        setPage(1);
                      }}
                    />
                  </th>
                  <th>Status</th>
                  <th className="right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {employees.data?.employees.map((employee) => (
                  <tr key={employee.employeeId}>
                    <td>
                      <div className="person-cell">
                        <div className="avatar">
                          {initials(employee.fullName)}
                        </div>
                        <div>
                          <strong>{employee.fullName}</strong>
                          <small>{employee.email}</small>
                        </div>
                      </div>
                    </td>
                    <td>
                      <strong>{employee.departmentName}</strong>
                      <small>{employee.jobTitle}</small>
                    </td>
                    <td>
                      <span className="role-tag">
                        {employee.roles?.join(", ") || "Not assigned"}
                      </span>
                    </td>
                    <td>{formatDate(employee.hireDate)}</td>
                    <td>
                      <Badge
                        value={employee.isActive ? "Active" : "Inactive"}
                      />
                    </td>
                    <td className="right">
                      {team ? (
                        <Button
                          variant="ghost"
                          onClick={() => setMode({ type: "view", employee })}
                        >
                          View profile
                        </Button>
                      ) : (
                        <div className="row-actions">
                          <button
                            className="icon-button"
                            aria-label={`Edit ${employee.fullName}`}
                            onClick={() => setMode({ type: "edit", employee })}
                          >
                            <Pencil size={16} />
                          </button>
                          <select
                            className="row-menu"
                            aria-label={`Manage ${employee.fullName}`}
                            value=""
                            onChange={(e) => {
                              if (e.target.value)
                                setMode({
                                  type: e.target.value as Mode["type"],
                                  employee,
                                });
                            }}
                          >
                            <option value="">Manage</option>
                            <option value="view">View profile</option>
                            <option value="assign">Assignments & role</option>
                            {!employee.hasAccount && employee.isActive && (
                              <option value="account">Create account</option>
                            )}
                            <option
                              value="deactivate"
                              disabled={!employee.isActive}
                            >
                              Deactivate
                            </option>
                          </select>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </DataState>
        <div className="table-bottom">
          <select
            aria-label="Employees per page"
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
            totalPages={employees.data?.totalPages || 0}
            totalCount={employees.data?.totalCount || 0}
            onChange={setPage}
            loading={employees.loading}
          />
        </div>
      </section>
      {(adding || mode?.type === "edit") && (
        <EmployeeForm
          employee={mode?.employee}
          onClose={() => {
            setAdding(false);
            setMode(null);
          }}
          onSaved={() =>
            saved(
              adding
                ? "Employee created with an account and leave allocations."
                : "Employee details updated.",
            )
          }
        />
      )}
      {mode?.type === "assign" && (
        <Assignments
          employee={mode.employee}
          onClose={() => setMode(null)}
          onSaved={(message) => {
            saved(message);
            if (mode.employee.email === user?.email)
              notify("Your role changed. Please sign in again.");
          }}
        />
      )}
      {mode && ["deactivate", "account"].includes(mode.type) && (
        <AccountAction
          mode={mode}
          onClose={() => setMode(null)}
          onSaved={() =>
            saved(
              mode.type === "deactivate"
                ? "Employee deactivated. Their history has been preserved."
                : "Employee account created.",
            )
          }
        />
      )}
      {mode?.type === "view" && (
        <Modal
          title={mode.employee.fullName}
          description={mode.employee.jobTitle}
          onClose={() => setMode(null)}
        >
          <div className="profile-modal-details">
            <p>
              <Mail size={16} />
              {mode.employee.email}
            </p>
            <p>
              Department<strong>{mode.employee.departmentName}</strong>
            </p>
            <p>
              Hire date<strong>{formatDate(mode.employee.hireDate)}</strong>
            </p>
            <p>
              Role
              <strong>
                {mode.employee.roles?.join(", ") || "Not assigned"}
              </strong>
            </p>
            <p>
              Employee number
              <strong>
                ITS-{String(mode.employee.employeeId).padStart(4, "0")}
              </strong>
            </p>
            <p>
              Status
              <Badge value={mode.employee.isActive ? "Active" : "Inactive"} />
            </p>
          </div>
          <div className="modal-actions">
            <Button onClick={() => setMode(null)} variant="secondary">
              Close
            </Button>
          </div>
        </Modal>
      )}
    </>
  );
}
function EmployeeForm({
  employee,
  onClose,
  onSaved,
}: {
  employee?: Employee;
  onClose: () => void;
  onSaved: () => void;
}) {
  const departments = useResource<Department[]>("/Departments");
  const [name, setName] = useState(employee?.fullName || "");
  const [email, setEmail] = useState(employee?.email || "");
  const [title, setTitle] = useState(employee?.jobTitle || "");
  const [department, setDepartment] = useState(
    String(employee?.departmentId || ""),
  );
  const [date, setDate] = useState(
    employee ? inputDate(employee.hireDate) : "",
  );
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const today = new Date().toLocaleDateString("en-CA");
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await api(`/Employees${employee ? "/" + employee.employeeId : ""}`, {
        method: employee ? "PUT" : "POST",
        body: {
          fullName: name.trim(),
          email: email.trim(),
          jobTitle: title.trim(),
          departmentId: Number(department),
          hireDate: date,
          ...(!employee ? { password } : {}),
        },
      });
      onSaved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  return (
    <Modal
      title={employee ? "Edit employee" : "Welcome someone new."}
      description={
        employee
          ? "Keep their details up to date."
          : "Their account and active leave allocations will be created together."
      }
      onClose={onClose}
      busy={busy}
    >
      <DataState
        loading={departments.loading}
        error={departments.error}
        retry={departments.reload}
        empty={!departments.data?.length}
        emptyTitle="Create a department first"
        emptyDescription="Add a department from the Departments page before adding employees."
      >
        <form onSubmit={submit}>
          <ErrorMessage message={error} />
          <fieldset disabled={busy}>
            <Field label="Full name">
              <input
                value={name}
                required
                maxLength={100}
                onChange={(e) => setName(e.target.value)}
                placeholder="e.g. Sarah Ahmed"
              />
            </Field>
            <Field label="Email address">
              <input
                type="email"
                value={email}
                required
                maxLength={150}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="sarah@company.com"
              />
            </Field>
            <div className="form-grid">
              <Field label="Department">
                <select
                  value={department}
                  required
                  onChange={(e) => setDepartment(e.target.value)}
                >
                  <option value="">Choose department</option>
                  {departments.data?.map((d) => (
                    <option key={d.id} value={d.id}>
                      {d.name}
                    </option>
                  ))}
                </select>
              </Field>
              <Field label="Job title">
                <input
                  value={title}
                  required
                  maxLength={100}
                  onChange={(e) => setTitle(e.target.value)}
                  placeholder="e.g. Software Engineer"
                />
              </Field>
            </div>
            <Field label="Hire date">
              <input
                type="date"
                value={date}
                required
                max={today}
                onChange={(e) => setDate(e.target.value)}
              />
            </Field>
            {!employee && (
              <Field
                label="Initial password"
                hint="Use at least 6 characters with upper/lowercase letters, a digit, and a symbol."
              >
                <input
                  type="password"
                  autoComplete="new-password"
                  value={password}
                  required
                  minLength={6}
                  maxLength={128}
                  onChange={(e) => setPassword(e.target.value)}
                />
              </Field>
            )}
          </fieldset>
          <div className="modal-actions">
            <Button
              type="button"
              variant="secondary"
              disabled={busy}
              onClick={onClose}
            >
              Cancel
            </Button>
            <Button type="submit" busy={busy}>
              {employee ? "Save changes" : "Create employee"}
            </Button>
          </div>
        </form>
      </DataState>
    </Modal>
  );
}
function Assignments({
  employee,
  onClose,
  onSaved,
}: {
  employee: Employee;
  onClose: () => void;
  onSaved: (message: string) => void;
}) {
  const [tab, setTab] = useState<"department" | "manager" | "role">(
    "department",
  );
  const [department, setDepartment] = useState(String(employee.departmentId));
  const [manager, setManager] = useState(String(employee.managerId || ""));
  const [role, setRole] = useState(employee.roles?.[0] || "");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const departments = useResource<Department[]>("/Departments");
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await api(
        `/Employees/${employee.employeeId}/${tab}${query(tab === "department" ? { DepartmentId: department } : tab === "manager" ? { managerId: manager || "0" } : { role })}`,
        { method: "PUT" },
      );
      onSaved(`${employee.fullName}’s ${tab} updated.`);
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  return (
    <Modal
      title="Find their place in the team."
      description={employee.fullName}
      onClose={onClose}
      busy={busy}
    >
      <div className="tabs">
        {(["department", "manager", "role"] as const).map((value) => (
          <button
            key={value}
            type="button"
            disabled={busy}
            className={tab === value ? "active" : ""}
            onClick={() => {
              setTab(value);
              setError("");
            }}
          >
            {value}
          </button>
        ))}
      </div>
      <form onSubmit={submit}>
        <ErrorMessage message={error} />
        <fieldset disabled={busy}>
          {tab === "department" && (
            <Field label="Department">
              <select
                value={department}
                required
                onChange={(e) => setDepartment(e.target.value)}
              >
                <option value="">Select department</option>
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
          {tab === "manager" && (
            <EmployeePicker
              label="Manager"
              value={manager}
              onChange={setManager}
              managersOnly
            />
          )}
          {tab === "role" && (
            <Field
              label="Role"
              hint="Changing the role ends their current session. A manager’s team must be reassigned before changing their role."
            >
              <select
                value={role}
                required
                onChange={(e) => setRole(e.target.value)}
              >
                <option value="">Select role</option>
                <option>Employee</option>
                <option>Manager</option>
                <option>Administrator</option>
              </select>
            </Field>
          )}
        </fieldset>
        <div className="modal-actions">
          <Button
            type="button"
            variant="secondary"
            disabled={busy}
            onClick={onClose}
          >
            Cancel
          </Button>
          <Button type="submit" busy={busy}>
            Save {tab}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
function AccountAction({
  mode,
  onClose,
  onSaved,
}: {
  mode: Mode;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);
  const deactivate = mode.type === "deactivate";
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await api(
        `/Employees/${mode.employee.employeeId}/${deactivate ? "deactivate" : "account"}`,
        {
          method: deactivate ? "PUT" : "POST",
          ...(deactivate ? {} : { body: { password } }),
        },
      );
      onSaved();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  };
  return (
    <Modal
      title={
        deactivate ? "Deactivate this employee?" : "Create employee account"
      }
      description={mode.employee.fullName}
      onClose={onClose}
      busy={busy}
    >
      <form onSubmit={submit}>
        <ErrorMessage message={error} />
        {deactivate ? (
          <p className="confirmation-copy">
            They will no longer be able to sign in or use their current tokens.
            Their employee details, leave history, balances, and audit records
            will remain available.
          </p>
        ) : (
          <Field
            label="Initial password"
            hint="At least 6 characters, including uppercase, lowercase, a number, and a symbol."
          >
            <input
              type="password"
              autoComplete="new-password"
              minLength={6}
              maxLength={128}
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={busy}
            />
          </Field>
        )}
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
            variant={deactivate ? "danger" : "primary"}
            busy={busy}
          >
            {deactivate ? "Deactivate employee" : "Create account"}
          </Button>
        </div>
      </form>
    </Modal>
  );
}
