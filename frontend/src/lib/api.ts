import { session } from "./session";
import type { Tokens } from "./types";

const base = (import.meta.env.VITE_API_URL || "/api").replace(/\/$/, "");
export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}
export async function responseMessage(response: Response): Promise<string> {
  const text = await response.text();
  try {
    const data = JSON.parse(text);
    if (typeof data === "string") return data;
    if (data.errors) return Object.values(data.errors).flat().join(" ");
    return data.message || data.title || "The request could not be completed.";
  } catch {
    return text && !text.includes("<")
      ? text
      : response.status === 403
        ? "You do not have permission to perform this action."
        : response.status === 401
          ? "Your session has ended. Please sign in again."
          : "The server could not complete your request.";
  }
}
let refreshing: Promise<boolean> | null = null;
async function refreshSession(): Promise<boolean> {
  const original = session.get();
  if (!original) return false;
  try {
    const response = await fetch(`${base}/Auth/refresh-token`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: original.refreshToken }),
    });
    if (!response.ok) {
      if (session.get() === original && response.status === 401)
        session.set(null);
      return false;
    }
    const result = (await response.json()) as Tokens;
    if (session.get() !== original) return false;
    session.set(result);
    return true;
  } catch {
    return false;
  }
}
export async function api<T = void>(
  path: string,
  options: {
    method?: string;
    body?: unknown;
    signal?: AbortSignal;
    anonymous?: boolean;
  } = {},
): Promise<T> {
  const send = () =>
    fetch(`${base}${path}`, {
      method: options.method || "GET",
      signal: options.signal,
      headers: {
        ...(options.body !== undefined
          ? { "Content-Type": "application/json" }
          : {}),
        ...(!options.anonymous && session.get()
          ? { Authorization: `Bearer ${session.get()!.accessToken}` }
          : {}),
      },
      body:
        options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  let response: Response;
  try {
    response = await send();
    if (response.status === 401 && !options.anonymous && session.get()) {
      refreshing ??= refreshSession().finally(() => {
        refreshing = null;
      });
      if (await refreshing) response = await send();
    }
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError")
      throw error;
    throw new ApiError(
      0,
      "Unable to connect. Check your connection and try again.",
    );
  }
  if (!response.ok)
    throw new ApiError(response.status, await responseMessage(response));
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  try {
    return JSON.parse(text) as T;
  } catch {
    return text as T;
  }
}
export function query(
  values: Record<string, string | number | boolean | undefined | null>,
): string {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "")
      params.set(key, String(value));
  });
  const result = params.toString();
  return result ? `?${result}` : "";
}
