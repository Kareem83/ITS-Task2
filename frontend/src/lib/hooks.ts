import { useCallback, useEffect, useState } from "react";
import { api } from "./api";

export function useResource<T>(path: string | null) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [version, setVersion] = useState(0);
  const reload = useCallback(() => setVersion((v) => v + 1), []);
  useEffect(() => {
    const controller = new AbortController();
    if (!path) {
      setData(null);
      setLoading(false);
      setError("");
      return () => controller.abort();
    }
    setLoading(true);
    setError("");
    setData(null);
    api<T>(path, { signal: controller.signal })
      .then((value) => {
        if (!controller.signal.aborted) setData(value);
      })
      .catch((reason) => {
        if (!controller.signal.aborted)
          setError(reason.message || "Unable to load data");
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [path, version]);
  return { data, loading, error, reload };
}
export function useDebounce<T>(value: T, delay = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(timer);
  }, [value, delay]);
  return debounced;
}
export function formatDate(value: string) {
  if (!value) return "—";
  const date = new Date(value.slice(0, 10) + "T12:00:00");
  return Number.isNaN(date.getTime())
    ? "—"
    : date.toLocaleDateString("en-GB", {
        day: "numeric",
        month: "short",
        year: "numeric",
      });
}
export function inputDate(value: string) {
  return value.slice(0, 10);
}
export function initials(name: string) {
  return name
    .split(/[\s@._]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((s) => s[0].toUpperCase())
    .join("");
}
export function workingDays(start: string, end: string) {
  if (!start || !end || end < start) return 0;
  const first = Date.parse(start + "T12:00:00Z"),
    last = Date.parse(end + "T12:00:00Z");
  if (!Number.isFinite(first) || !Number.isFinite(last)) return 0;
  const days = Math.floor((last - first) / 86400000) + 1;
  let total = Math.floor(days / 7) * 5;
  for (let i = 0; i < days % 7; i++) {
    const weekday = new Date(first + i * 86400000).getUTCDay();
    if (weekday !== 5 && weekday !== 6) total++;
  }
  return total;
}
