import type { SessionUser, Tokens, Role } from "./types";

const key = "its.session";
const listeners = new Set<() => void>();
export function parseUser(token: string): SessionUser {
  const segment = token.split(".")[1];
  if (!segment) throw new Error("Invalid session token");
  const base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
  const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
  const data = JSON.parse(new TextDecoder().decode(bytes));
  const roles =
    data.role ??
    data["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];
  const list = Array.isArray(roles) ? roles : [roles];
  const role = (["Administrator", "Manager", "Employee"] as Role[]).find((r) =>
    list.includes(r),
  );
  const email =
    data.email ??
    data["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"];
  const id =
    data.sub ??
    data[
      "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"
    ];
  if (!role || !email || !id || !Number.isFinite(data.exp))
    throw new Error(
      "Your account does not have an available role. Contact your administrator.",
    );
  return { id, email, role, expires: data.exp };
}
function load(): Tokens | null {
  try {
    const value = sessionStorage.getItem(key);
    if (!value) return null;
    const tokens = JSON.parse(value) as Tokens;
    parseUser(tokens.accessToken);
    if (!tokens.refreshToken) return null;
    return tokens;
  } catch {
    sessionStorage.removeItem(key);
    return null;
  }
}
let tokens = load();
export const session = {
  get: () => tokens,
  user: () => (tokens ? parseUser(tokens.accessToken) : null),
  set: (value: Tokens | null) => {
    if (value) parseUser(value.accessToken);
    tokens = value;
    if (value) sessionStorage.setItem(key, JSON.stringify(value));
    else sessionStorage.removeItem(key);
    listeners.forEach((fn) => fn());
  },
  subscribe: (fn: () => void) => {
    listeners.add(fn);
    return () => {
      listeners.delete(fn);
    };
  },
};
