import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { workingDays, formatDate } from "../src/lib/hooks";
import { parseUser, session } from "../src/lib/session";
import { api, query, responseMessage } from "../src/lib/api";
import { AuthProvider } from "../src/context/AuthContext";
import { RoleGuard } from "../src/components/Layout";
const token = (role = "Employee") =>
  `header.${btoa(JSON.stringify({ sub: "user-1", email: "employee@its.com", role, exp: 2000000000 }))}.signature`;
beforeEach(() => {
  session.set(null);
  vi.unstubAllGlobals();
});
describe("working days", () => {
  it.each([
    ["2026-10-09", "2026-10-10", 0],
    ["2026-10-11", "2026-10-15", 5],
    ["2026-10-11", "2026-10-24", 10],
    ["2026-10-15", "2026-10-11", 0],
    ["", "", 0],
  ])("%s to %s gives %i", (a, b, n) => expect(workingDays(a, b)).toBe(n));
  it("formats dates without UTC shifts", () =>
    expect(formatDate("2026-10-11T00:00:00Z")).toBe("11 Oct 2026"));
});
describe("session and permissions", () => {
  it("reads Identity URI claims", () =>
    expect(
      parseUser(
        `a.${btoa(JSON.stringify({ "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "u", "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress": "u@its.com", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": ["Employee", "Manager"], exp: 2000000000 }))}.b`,
      ).role,
    ).toBe("Manager"));
  it("rejects invalid tokens", () =>
    expect(() => parseUser("invalid")).toThrow());
  it("blocks restricted pages", () => {
    session.set({ accessToken: token(), refreshToken: "r" });
    render(
      <MemoryRouter>
        <AuthProvider>
          <RoleGuard roles={["Administrator"]}>
            <p>Private content</p>
          </RoleGuard>
        </AuthProvider>
      </MemoryRouter>,
    );
    expect(screen.getByText("This page is restricted")).toBeInTheDocument();
    expect(screen.queryByText("Private content")).not.toBeInTheDocument();
  });
});
describe("API client", () => {
  it("preserves false and zero in queries", () =>
    expect(query({ active: false, page: 0, empty: "" })).toBe(
      "?active=false&page=0",
    ));
  it("shows validation errors", async () =>
    expect(
      await responseMessage(
        new Response(JSON.stringify({ errors: { Name: ["Required"] } }), {
          status: 400,
        }),
      ),
    ).toBe("Required"));
  it("refreshes concurrent requests once", async () => {
    session.set({ accessToken: token(), refreshToken: "old" });
    let renewed = false,
      refreshes = 0;
    vi.stubGlobal(
      "fetch",
      vi.fn(async (url: string) => {
        if (url.endsWith("/Auth/refresh-token")) {
          refreshes++;
          await new Promise((r) => setTimeout(r, 10));
          renewed = true;
          return new Response(
            JSON.stringify({ accessToken: token(), refreshToken: "new" }),
          );
        }
        return renewed
          ? new Response(JSON.stringify({ ok: true }))
          : new Response("", { status: 401 });
      }),
    );
    expect(await Promise.all([api("/one"), api("/two")])).toEqual([
      { ok: true },
      { ok: true },
    ]);
    expect(refreshes).toBe(1);
    expect(session.get()?.refreshToken).toBe("new");
  });
  it("clears rejected sessions", async () => {
    session.set({ accessToken: token(), refreshToken: "bad" });
    vi.stubGlobal(
      "fetch",
      vi.fn(async () => new Response("", { status: 401 })),
    );
    await expect(api("/one")).rejects.toMatchObject({ status: 401 });
    expect(session.get()).toBeNull();
  });
  it("never refreshes forbidden mutations", async () => {
    session.set({ accessToken: token(), refreshToken: "r" });
    const fetcher = vi.fn(async () => new Response("", { status: 403 }));
    vi.stubGlobal("fetch", fetcher);
    await expect(
      api("/one", { method: "POST", body: {} }),
    ).rejects.toMatchObject({ status: 403 });
    expect(fetcher).toHaveBeenCalledTimes(1);
  });
  it("cannot restore a session after logout during refresh", async () => {
    session.set({ accessToken: token(), refreshToken: "r" });
    vi.stubGlobal(
      "fetch",
      vi.fn(async (url: string) => {
        if (url.endsWith("refresh-token")) {
          session.set(null);
          return new Response(
            JSON.stringify({ accessToken: token(), refreshToken: "new" }),
          );
        }
        return new Response("", { status: 401 });
      }),
    );
    await expect(api("/one")).rejects.toMatchObject({ status: 401 });
    expect(session.get()).toBeNull();
  });
});
