import {
  ArrowRight,
  CalendarDays,
  CalendarCheck2,
  Clock3,
  UsersRound,
  FolderKanban,
  Wallet,
  XCircle,
  Sparkles,
  ArrowUpRight,
} from "lucide-react";
import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { Badge, DataState, PageTitle } from "../components/UI";
import { formatDate, useResource } from "../lib/hooks";
import type {
  Dashboard as DashboardData,
  LeavePage,
  Balance,
} from "../lib/types";

export function Dashboard() {
  const { user } = useAuth();
  const role = user!.role;
  const dashboard = useResource<DashboardData>(
    `/Dashboard/${role === "Administrator" ? "admin" : role.toLowerCase()}`,
  );
  const requests = useResource<LeavePage>(
    `/LeaveRequests${role === "Employee" ? "/my" : role === "Manager" ? "/pending" : ""}?pageSize=5&sortBy=createdat&sortOrder=desc`,
  );
  const balances = useResource<Balance[]>(
    role === "Employee" ? "/LeaveRequests/my-balance" : null,
  );
  const cards =
    role === "Administrator"
      ? [
          {
            key: "totalEmployees",
            label: "Total employees",
            icon: UsersRound,
            text: "People in your workspace",
            tone: "sage",
          },
          {
            key: "activeEmployees",
            label: "Active employees",
            icon: Sparkles,
            text: "Ready for the work ahead",
            tone: "blue",
          },
          {
            key: "pendingLeaveRequests",
            label: "Pending requests",
            icon: Clock3,
            text: "Waiting for a decision",
            tone: "sand",
          },
          {
            key: "approvedThisMonth",
            label: "Approved this month",
            icon: CalendarCheck2,
            text: "Approved leave starting this month",
            tone: "lavender",
          },
        ]
      : role === "Manager"
        ? [
            {
              key: "teamMembers",
              label: "Team members",
              icon: UsersRound,
              text: "The people you look after",
              tone: "sage",
            },
            {
              key: "pendingRequests",
              label: "Pending requests",
              icon: Clock3,
              text: "Ready for your review",
              tone: "sand",
            },
            {
              key: "approvedThisMonth",
              label: "Approved this month",
              icon: CalendarCheck2,
              text: "Approved leave starting this month",
              tone: "blue",
            },
            {
              key: "employeesCurrentlyAway",
              label: "Currently away",
              icon: CalendarDays,
              text: "Enjoying their time off",
              tone: "lavender",
            },
          ]
        : [
            {
              key: "annualLeaveBalance",
              label: "Annual leave balance",
              icon: Wallet,
              text: "Days available to request",
              tone: "sage",
            },
            {
              key: "pendingRequests",
              label: "Pending requests",
              icon: Clock3,
              text: "Waiting for your manager",
              tone: "sand",
            },
            {
              key: "approvedThisYear",
              label: "Approved this year",
              icon: CalendarCheck2,
              text: "Approved leave starting this year",
              tone: "blue",
            },
            {
              key: "rejectedRequests",
              label: "Rejected requests",
              icon: XCircle,
              text: "Review your request history",
              tone: "lavender",
            },
          ];
  const rows = requests.data?.items || requests.data?.requests || [];
  return (
    <>
      <PageTitle
        eyebrow="A CLEAR VIEW OF YOUR DAY"
        title="Your workspace, at a glance."
        description={
          role === "Administrator"
            ? "A little clarity on your people, teams, and time off."
            : role === "Manager"
              ? "Keep your team connected and their time off on track."
              : "Your time, your balance, and a little room to recharge."
        }
        action={
          <Link
            className="btn btn-primary"
            to={
              role === "Employee"
                ? "/requests?new=1"
                : role === "Manager"
                  ? "/approvals"
                  : "/employees"
            }
          >
            {role === "Employee"
              ? "Request time off"
              : role === "Manager"
                ? "Review requests"
                : "Manage employees"}
            <ArrowUpRight size={17} />
          </Link>
        }
      />
      <DataState
        loading={dashboard.loading}
        error={dashboard.error}
        retry={dashboard.reload}
      >
        <div className="stats-grid">
          {cards.map((card) => (
            <article className="stat-card" key={card.key}>
              <div className="stat-top">
                <span>{card.label}</span>
                <div className={`stat-icon ${card.tone}`}>
                  <card.icon size={20} />
                </div>
              </div>
              <div className="stat-value">
                {dashboard.data?.[card.key] ?? 0}
                {card.key === "annualLeaveBalance" && <small>days</small>}
              </div>
              <p>{card.text}</p>
            </article>
          ))}
        </div>
      </DataState>
      <div className="dashboard-grid">
        <section className="panel">
          <div className="panel-heading">
            <div>
              <h2>
                {role === "Manager"
                  ? "Waiting for your review"
                  : "Recent leave requests"}
              </h2>
              <p>
                {role === "Employee"
                  ? "A quick look at your latest time-off plans."
                  : "The latest requests from your people."}
              </p>
            </div>
            <Link
              className="text-link"
              to={role === "Manager" ? "/approvals" : "/requests"}
            >
              View all
              <ArrowRight size={16} />
            </Link>
          </div>
          <DataState
            loading={requests.loading}
            error={requests.error}
            retry={requests.reload}
            empty={!rows.length}
            emptyTitle="A fresh start"
            emptyDescription={
              role === "Employee"
                ? "Your leave requests will appear here once you submit one."
                : "There are no requests to show right now."
            }
          >
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>{role === "Employee" ? "Leave type" : "Employee"}</th>
                    <th>Dates</th>
                    <th>Days</th>
                    <th>Status</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((row) => (
                    <tr key={row.id}>
                      <td>
                        <strong>
                          {role === "Employee"
                            ? row.leaveTypeName
                            : row.employeeName}
                        </strong>
                        <small>
                          {role === "Employee" ? row.reason : row.leaveTypeName}
                        </small>
                      </td>
                      <td>
                        {formatDate(row.startDate)}
                        <small>to {formatDate(row.endDate)}</small>
                      </td>
                      <td>{row.days}</td>
                      <td>
                        <Badge value={row.status} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </DataState>
        </section>
        <aside className="dashboard-aside">
          <section className="quiet-card">
            <div className="quiet-icon">
              <LeafArtwork />
            </div>
            <div className="eyebrow">A HEALTHIER RHYTHM</div>
            <h2>
              Good work starts
              <br />
              with a little balance.
            </h2>
            <p>
              {role === "Employee"
                ? "Plan your next break. Your workspace keeps the details organized."
                : "Make space for your people to do their best work, and take a well-earned break."}
            </p>
            <Link
              className="text-link"
              to={
                role === "Employee"
                  ? "/balances"
                  : role === "Manager"
                    ? "/team"
                    : "/departments"
              }
            >
              {role === "Employee"
                ? "Explore your balance"
                : "Explore your team"}
              <ArrowUpRight size={16} />
            </Link>
          </section>
          {role === "Employee" ? (
            <section className="panel mini-panel">
              <h3>Your leave balances</h3>
              <DataState
                loading={balances.loading}
                error={balances.error}
                retry={balances.reload}
                empty={!balances.data?.length}
              >
                <div className="balance-summary">
                  {balances.data?.map((balance) => (
                    <div key={balance.id}>
                      <span>{balance.leaveTypeName}</span>
                      <strong>
                        {balance.remainingDays}
                        <small> days</small>
                      </strong>
                    </div>
                  ))}
                </div>
              </DataState>
            </section>
          ) : (
            <section className="panel mini-panel">
              <div className="mini-label">
                <FolderKanban size={18} />
                <span>
                  {role === "Administrator" ? "Departments" : "Team overview"}
                </span>
              </div>
              <strong className="mini-number">
                {role === "Administrator"
                  ? (dashboard.data?.departments ?? 0)
                  : (dashboard.data?.teamMembers ?? 0)}
              </strong>
              <p>
                {role === "Administrator"
                  ? "Places for your people to grow."
                  : "Every person makes a difference."}
              </p>
            </section>
          )}
        </aside>
      </div>
    </>
  );
}
function LeafArtwork() {
  return (
    <svg viewBox="0 0 120 110" aria-hidden="true">
      <path d="M62 92C42 64 38 35 59 12c26 18 31 45 3 80Z" fill="#c5d8b4" />
      <path d="M61 93C44 66 16 72 13 44c32-7 50 9 48 49Z" fill="#9eb99a" />
      <path d="M63 89c7-33 18-43 43-42-2 29-15 41-43 42Z" fill="#789b79" />
      <path
        d="M62 101V42M62 82 35 59M63 86l25-23"
        fill="none"
        stroke="#335c47"
        strokeWidth="3"
        strokeLinecap="round"
      />
    </svg>
  );
}
