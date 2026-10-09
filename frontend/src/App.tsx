import { Component } from "react";
import type { ReactNode } from "react";
import { BrowserRouter, Routes, Route, Link } from "react-router-dom";
import { AuthProvider } from "./context/AuthContext";
import { ToastProvider } from "./context/ToastContext";
import { Layout, RoleGuard } from "./components/Layout";
import { Login } from "./pages/Login";
import { Dashboard } from "./pages/Dashboard";
import { Employees } from "./pages/Employees";
import { Requests } from "./pages/Requests";
import { Balances } from "./pages/Balances";
import { Profile } from "./pages/Profile";
import { Settings, Audit } from "./pages/Settings";
class ErrorBoundary extends Component<
  { children: ReactNode },
  { failed: boolean }
> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  render() {
    return this.state.failed ? (
      <div className="access-denied">
        <h1>Something went wrong</h1>
        <p>Please reload your workspace to try again.</p>
        <button className="btn btn-primary" onClick={() => location.reload()}>
          Reload workspace
        </button>
      </div>
    ) : (
      this.props.children
    );
  }
}
export default function App() {
  return (
    <ErrorBoundary>
      <BrowserRouter>
        <AuthProvider>
          <ToastProvider>
            <Routes>
              <Route path="/login" element={<Login />} />
              <Route element={<Layout />}>
                <Route index element={<Dashboard />} />
                <Route
                  path="employees"
                  element={
                    <RoleGuard roles={["Administrator"]}>
                      <Employees />
                    </RoleGuard>
                  }
                />
                <Route
                  path="team"
                  element={
                    <RoleGuard roles={["Manager"]}>
                      <Employees team />
                    </RoleGuard>
                  }
                />
                <Route path="requests" element={<Requests />} />
                <Route
                  path="approvals"
                  element={
                    <RoleGuard roles={["Manager"]}>
                      <Requests pendingOnly />
                    </RoleGuard>
                  }
                />
                <Route
                  path="balances"
                  element={
                    <RoleGuard roles={["Employee"]}>
                      <Balances />
                    </RoleGuard>
                  }
                />
                <Route
                  path="profile"
                  element={
                    <RoleGuard roles={["Employee", "Manager"]}>
                      <Profile />
                    </RoleGuard>
                  }
                />
                <Route
                  path="departments"
                  element={
                    <RoleGuard roles={["Administrator"]}>
                      <Settings />
                    </RoleGuard>
                  }
                />
                <Route
                  path="leave-types"
                  element={
                    <RoleGuard roles={["Administrator"]}>
                      <Settings leaveTypes />
                    </RoleGuard>
                  }
                />
                <Route
                  path="audit"
                  element={
                    <RoleGuard roles={["Administrator"]}>
                      <Audit />
                    </RoleGuard>
                  }
                />
                <Route
                  path="*"
                  element={
                    <div className="access-denied">
                      <h1>Page not found</h1>
                      <Link to="/" className="btn btn-primary">
                        Return to overview
                      </Link>
                    </div>
                  }
                />
              </Route>
            </Routes>
          </ToastProvider>
        </AuthProvider>
      </BrowserRouter>
    </ErrorBoundary>
  );
}
