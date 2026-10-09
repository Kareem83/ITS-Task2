import { createContext, useContext, useSyncExternalStore } from "react";
import type { ReactNode } from "react";
import { api } from "../lib/api";
import { session } from "../lib/session";
import type { SessionUser, Tokens } from "../lib/types";

type Auth = {
  user: SessionUser | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
};
const AuthContext = createContext<Auth | null>(null);
export function AuthProvider({ children }: { children: ReactNode }) {
  const tokens = useSyncExternalStore(session.subscribe, session.get);
  const user = tokens ? session.user() : null;
  const login = async (email: string, password: string) => {
    session.set(
      await api<Tokens>("/Auth/login", {
        method: "POST",
        body: { email, password },
        anonymous: true,
      }),
    );
  };
  return (
    <AuthContext.Provider
      value={{ user, login, logout: () => session.set(null) }}
    >
      {children}
    </AuthContext.Provider>
  );
}
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("AuthProvider is required");
  return context;
}
