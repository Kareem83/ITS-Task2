import { useState } from "react";
import {
  ArrowRight,
  CalendarCheck2,
  Eye,
  EyeOff,
  Leaf,
  ShieldCheck,
  UsersRound,
} from "lucide-react";
import { Navigate, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { Button, ErrorMessage, Field } from "../components/UI";

export function Login() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [visible, setVisible] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  if (user) return <Navigate to="/" replace />;
  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");
    try {
      await login(email.trim(), password);
      navigate("/", { replace: true });
    } catch (reason) {
      setError((reason as Error).message);
    } finally {
      setBusy(false);
    }
  };
  return (
    <div className="login-page">
      <section className="login-story">
        <div className="brand light">
          <span className="brand-icon">
            <Leaf size={25} />
          </span>
          <span>
            ITS<span className="brand-dot">.</span>
            <small>people & leave</small>
          </span>
        </div>
        <div className="login-story-content">
          <span className="story-tag">MAKE ROOM FOR WHAT MATTERS</span>
          <h1>
            Great teams.
            <br />A little more
            <br />
            <em>balance.</em>
          </h1>
          <p>
            A thoughtful home for your people, their time off, and the work you
            do together.
          </p>
          <div className="story-features">
            <span>
              <UsersRound size={18} />
              Your people, connected
            </span>
            <span>
              <CalendarCheck2 size={18} />
              Time off, made simple
            </span>
            <span>
              <ShieldCheck size={18} />
              The right access for every role
            </span>
          </div>
        </div>
        <div className="story-foot">One workspace. Less busywork.</div>
        <div className="orbit orbit-one" />
        <div className="orbit orbit-two" />
      </section>
      <section className="login-form-panel">
        <div className="login-form-wrap">
          <div className="eyebrow">WELCOME TO YOUR WORKSPACE</div>
          <h2>Good to see you.</h2>
          <p>Sign in to manage your people and time off.</p>
          <form onSubmit={submit}>
            <ErrorMessage message={error} />
            <Field label="Email address">
              <input
                type="email"
                name="email"
                autoComplete="username"
                placeholder="you@company.com"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                required
                maxLength={150}
              />
            </Field>
            <Field label="Password">
              <div className="password-input">
                <input
                  type={visible ? "text" : "password"}
                  name="password"
                  autoComplete="current-password"
                  placeholder="Enter your password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  required
                />
                <button
                  type="button"
                  aria-label={visible ? "Hide password" : "Show password"}
                  onClick={() => setVisible((v) => !v)}
                >
                  {visible ? <EyeOff size={18} /> : <Eye size={18} />}
                </button>
              </div>
            </Field>
            <Button type="submit" busy={busy} className="login-button">
              Sign in
              <ArrowRight size={18} />
            </Button>
          </form>
          {(import.meta.env.DEV ||
            import.meta.env.VITE_SHOW_DEMO_ACCOUNTS === "true") && (
            <div className="demo-accounts">
              <span>Development demo accounts</span>
              <div>
                {[
                  {
                    name: "Administrator",
                    email: "admin@its.com",
                    password: "Admin@123",
                  },
                  {
                    name: "Manager",
                    email: "manager@its.com",
                    password: "Demo@123",
                  },
                  {
                    name: "Employee",
                    email: "employee@its.com",
                    password: "Demo@123",
                  },
                ].map((account) => (
                  <button
                    key={account.name}
                    type="button"
                    disabled={busy}
                    onClick={() => {
                      setEmail(account.email);
                      setPassword(account.password);
                      setError("");
                    }}
                  >
                    {account.name}
                    <ArrowRight size={13} />
                  </button>
                ))}
              </div>
              <small>These accounts require development seed data.</small>
            </div>
          )}
          <div className="login-help">
            Need an account? Contact your administrator.
          </div>
        </div>
        <div className="login-bottom">ITS · Employee & Leave Management</div>
      </section>
    </div>
  );
}
