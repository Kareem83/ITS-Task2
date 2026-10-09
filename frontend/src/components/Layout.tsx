import { useState } from "react";
import { NavLink, Navigate, Outlet, useLocation } from "react-router-dom";
import {
  ArrowUpRight,
  CalendarDays,
  ClipboardList,
  FolderKanban,
  History,
  LayoutDashboard,
  Leaf,
  LogOut,
  Menu,
  PanelLeftClose,
  ShieldCheck,
  UserRound,
  UsersRound,
  Wallet,
  X,
} from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { initials, useResource } from "../lib/hooks";
import type { Employee, Role } from "../lib/types";

const links = [
  {
    path: "/",
    label: "Overview",
    icon: LayoutDashboard,
    roles: ["Administrator", "Manager", "Employee"],
  },
  {
    path: "/employees",
    label: "Employees",
    icon: UsersRound,
    roles: ["Administrator"],
  },
  { path: "/team", label: "My team", icon: UsersRound, roles: ["Manager"] },
  {
    path: "/requests",
    label: "Leave requests",
    icon: CalendarDays,
    roles: ["Administrator", "Employee"],
  },
  {
    path: "/requests",
    label: "Team requests",
    icon: CalendarDays,
    roles: ["Manager"],
  },
  {
    path: "/approvals",
    label: "Approvals",
    icon: ClipboardList,
    roles: ["Manager"],
  },
  {
    path: "/balances",
    label: "Leave balances",
    icon: Wallet,
    roles: ["Employee"],
  },
  {
    path: "/departments",
    label: "Departments",
    icon: FolderKanban,
    roles: ["Administrator"],
  },
  {
    path: "/leave-types",
    label: "Leave types",
    icon: Leaf,
    roles: ["Administrator"],
  },
  {
    path: "/audit",
    label: "Audit history",
    icon: History,
    roles: ["Administrator"],
  },
  {
    path: "/profile",
    label: "My profile",
    icon: UserRound,
    roles: ["Employee", "Manager"],
  },
];
export function RoleGuard({
  roles,
  children,
}: {
  roles: Role[];
  children: React.ReactNode;
}) {
  const { user } = useAuth();
  if (!user) return <Navigate to="/login" replace />;
  if (!roles.includes(user.role))
    return (
      <div className="access-denied">
        <ShieldCheck size={40} />
        <h1>This page is restricted</h1>
        <p>Your current role cannot access this page.</p>
        <NavLink to="/" className="btn btn-primary">
          Return to overview
        </NavLink>
      </div>
    );
  return <>{children}</>;
}
export function Layout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [open, setOpen] = useState(false);
  const profile = useResource<Employee>(
    user && user.role !== "Administrator" ? "/Employees/me" : null,
  );
  if (!user)
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  const name =
    profile.data?.fullName ||
    (user.role === "Administrator"
      ? "Administrator"
      : user.email.split("@")[0]);
  return (
    <div className="app-shell">
      {open && (
        <button
          className="sidebar-scrim"
          aria-label="Close navigation"
          onClick={() => setOpen(false)}
        />
      )}
      <aside className={`sidebar ${open ? "is-open" : ""}`}>
        <NavLink to="/" className="brand" onClick={() => setOpen(false)}>
          <span className="brand-icon">
            <Leaf size={22} />
          </span>
          <span>
            ITS<span className="brand-dot">.</span>
            <small>people & leave</small>
          </span>
        </NavLink>
        <button
          className="mobile-close icon-button"
          aria-label="Close navigation"
          onClick={() => setOpen(false)}
        >
          <X size={20} />
        </button>
        <div className="workspace-label">YOUR WORKSPACE</div>
        <nav aria-label="Main navigation">
          {links
            .filter((link) => link.roles.includes(user.role))
            .map((link) => (
              <NavLink
                key={link.path}
                to={link.path}
                end={link.path === "/"}
                onClick={() => setOpen(false)}
                className={({ isActive }) =>
                  isActive ? "nav-link active" : "nav-link"
                }
              >
                <link.icon size={19} />
                <span>{link.label}</span>
                {link.path === "/approvals" && <span className="nav-dot" />}
              </NavLink>
            ))}
        </nav>
        <div className="sidebar-note">
          <div>
            <ShieldCheck size={19} />
            <strong>A little more organized.</strong>
          </div>
          <p>People, time off, and everything in between.</p>
          <span>
            {user.role} workspace <ArrowUpRight size={13} />
          </span>
        </div>
        <div className="sidebar-user">
          <div className="avatar">{initials(name)}</div>
          <div>
            <strong>{name}</strong>
            <small>{user.role}</small>
          </div>
          <button
            aria-label="Sign out"
            className="icon-button"
            onClick={logout}
          >
            <LogOut size={18} />
          </button>
        </div>
      </aside>
      <div className="main-panel">
        <header className="topbar">
          <div>
            <button
              className="icon-button menu-toggle"
              aria-label="Open navigation"
              onClick={() => setOpen(true)}
            >
              <Menu size={21} />
            </button>
            <PanelLeftClose className="desktop-icon" size={18} />
            <span>Workspace</span>
            <span className="breadcrumb-divider">/</span>
            <strong>
              {links.find(
                (link) =>
                  link.path === location.pathname &&
                  link.roles.includes(user.role),
              )?.label || "Overview"}
            </strong>
          </div>
          <div>
            <span className="today">
              {new Date().toLocaleDateString("en-GB", {
                day: "numeric",
                month: "short",
                year: "numeric",
              })}
            </span>
            <span className="role-pill">
              <ShieldCheck size={14} />
              {user.role}
            </span>
            <div className="avatar small">{initials(name)}</div>
          </div>
        </header>
        <main className="main-content">
          <Outlet />
        </main>
        <footer className="footer">
          <span>ITS · People & Leave</span>
          <span>Built around your people.</span>
        </footer>
      </div>
    </div>
  );
}
