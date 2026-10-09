import { test, expect } from "@playwright/test";
import type { Page } from "@playwright/test";
async function login(page: Page, email: string, password: string) {
  await page.goto("/login");
  await page.getByLabel("Email address").fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await page.getByRole("button", { name: "Sign in", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Your workspace, at a glance." }),
  ).toBeVisible();
  await expect(page.getByText("Loading your workspace…")).toHaveCount(0);
  await expect(page.getByText("We couldn’t load this information")).toHaveCount(
    0,
  );
}
async function logout(page: Page) {
  await page.getByRole("button", { name: "Sign out" }).click();
  await expect(
    page.getByRole("heading", { name: "Good to see you." }),
  ).toBeVisible();
}
test("full administrator, employee, and manager workflow", async ({ page }) => {
  const stamp = Date.now().toString(),
    employee = `Tester ${stamp}`,
    email = `tester${stamp}@its.com`,
    department = `QA ${stamp}`,
    reason = `Integration ${stamp}`;
  const errors: string[] = [];
  page.on("pageerror", (e) => errors.push(e.message));
  await login(page, "admin@its.com", "Admin@123");
  await page.getByRole("link", { name: "Departments", exact: true }).click();
  await page
    .getByRole("button", { name: "Add department", exact: true })
    .click();
  await page.getByLabel("Name", { exact: true }).fill(department);
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText(department, { exact: true })).toBeVisible();
  await page.getByRole("link", { name: "Employees", exact: true }).click();
  await page.getByRole("button", { name: "Add employee" }).click();
  await page.getByLabel("Full name").fill(employee);
  await page.getByLabel("Email address").fill(email);
  await page
    .getByRole("combobox", { name: "Department", exact: true })
    .selectOption({ label: department });
  await page.getByLabel("Job title").fill("QA Engineer");
  await page.getByLabel("Hire date", { exact: true }).fill("2026-01-01");
  await page.getByLabel("Initial password").fill("Testing@123");
  await page
    .getByRole("button", { name: "Create employee", exact: true })
    .click();
  await page
    .getByRole("textbox", { name: "Search people by name, email, or job…" })
    .fill(employee);
  await expect(page.getByText(email, { exact: true })).toBeVisible();
  await page.getByLabel(`Manage ${employee}`).selectOption("assign");
  await page.getByRole("button", { name: "manager", exact: true }).click();
  await page
    .getByRole("combobox", { name: "Manager", exact: true })
    .selectOption({ label: "Demo Manager · manager@its.com" });
  await page.getByRole("button", { name: "Save manager", exact: true }).click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await page.getByRole("link", { name: "Audit history", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: "Audit history", exact: true }),
  ).toBeVisible();
  await page
    .getByRole("textbox", { name: "Search audit history" })
    .fill(employee);
  await expect(page.getByText("Loading your workspace…")).toHaveCount(0);
  await expect(page.getByText("We couldn’t load this information")).toHaveCount(
    0,
  );
  await logout(page);
  await login(page, email, "Testing@123");
  await page.goto("/employees");
  await expect(
    page.getByRole("heading", { name: "This page is restricted" }),
  ).toBeVisible();
  await page.getByRole("link", { name: "Leave requests", exact: true }).click();
  await page
    .getByRole("button", { name: "Request time off", exact: true })
    .click();
  await page
    .getByRole("combobox", { name: "Leave type", exact: true })
    .selectOption({ label: "Annual Leave · 21 days available" });
  await page.getByLabel("Start date").fill("2027-01-03");
  await page.getByLabel("End date").fill("2027-01-04");
  await page.getByLabel("Reason", { exact: false }).fill(reason);
  await page.getByRole("button", { name: "Submit request" }).click();
  await expect(page.getByText(reason, { exact: true })).toBeVisible();
  await expect(
    page.getByRole("table").getByText("Pending", { exact: true }),
  ).toBeVisible();
  await logout(page);
  await login(page, "manager@its.com", "Demo@123");
  await page.getByRole("link", { name: "Approvals", exact: true }).click();
  await page
    .getByRole("textbox", { name: "Search leave requests…" })
    .fill(reason);
  const row = page.getByRole("row").filter({ hasText: reason });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: /Approve request/ }).click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Approve request", exact: true })
    .click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByText("All caught up")).toBeVisible();
  await logout(page);
  await login(page, email, "Testing@123");
  await page.getByRole("link", { name: "Leave balances", exact: true }).click();
  const annual = page
    .locator(".balance-card")
    .filter({ hasText: "Annual Leave" });
  await expect(annual.locator(".balance-available strong")).toHaveText("19");
  await page.getByRole("link", { name: "Leave requests", exact: true }).click();
  await expect(
    page.getByRole("table").getByText("Approved", { exact: true }),
  ).toBeVisible();
  await page.setViewportSize({ width: 390, height: 844 });
  await page.getByRole("button", { name: "Open navigation" }).click();
  await expect(
    page.getByRole("link", { name: "My profile", exact: true }),
  ).toBeVisible();
  await page.getByRole("link", { name: "My profile", exact: true }).click();
  await expect(
    page.getByRole("heading", { name: employee, exact: true }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});
test("leave type maintenance, cancellation and rejection preserve balances", async ({
  page,
}) => {
  const stamp = Date.now().toString(),
    policy = `Wellness ${stamp}`,
    cancelled = `Cancel ${stamp}`,
    rejected = `Reject ${stamp}`;
  await login(page, "admin@its.com", "Admin@123");
  await page.getByRole("link", { name: "Leave types", exact: true }).click();
  await page.getByRole("button", { name: "Add leave type" }).click();
  await page.getByLabel("Name", { exact: true }).fill(policy);
  await page.getByLabel("Allowed days").fill("3");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText(policy, { exact: true })).toBeVisible();
  await page
    .getByRole("button", { name: `Edit ${policy}`, exact: true })
    .click();
  await page.getByLabel("Allowed days").fill("4");
  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByRole("row").filter({ hasText: policy })).toContainText(
    "4 days",
  );
  await logout(page);
  await login(page, "employee@its.com", "Demo@123");
  await page.getByRole("link", { name: "Leave requests", exact: true }).click();
  async function request(start: string, reason: string) {
    await page
      .getByRole("button", { name: "Request time off", exact: true })
      .click();
    await page
      .getByRole("combobox", { name: "Leave type", exact: true })
      .selectOption({ label: `${policy} · 4 days available` });
    await page.getByLabel("Start date").fill(start);
    await page.getByLabel("End date").fill(start);
    await page.getByLabel("Reason", { exact: false }).fill(reason);
    await page.getByRole("button", { name: "Submit request" }).click();
    await expect(page.getByText(reason, { exact: true })).toBeVisible();
  }
  await request("2027-02-07", cancelled);
  await page
    .getByRole("row")
    .filter({ hasText: cancelled })
    .getByRole("button", { name: "Cancel", exact: true })
    .click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Cancel request", exact: true })
    .click();
  await expect(
    page.getByRole("row").filter({ hasText: cancelled }),
  ).toContainText("Cancelled");
  await request("2027-02-08", rejected);
  await logout(page);
  await login(page, "manager@its.com", "Demo@123");
  await page.getByRole("link", { name: "Approvals", exact: true }).click();
  await page
    .getByRole("textbox", { name: "Search leave requests…" })
    .fill(rejected);
  await page
    .getByRole("row")
    .filter({ hasText: rejected })
    .getByRole("button", { name: /Reject request/ })
    .click();
  await page.getByLabel("Rejection reason").fill("Please choose another day");
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Reject request", exact: true })
    .click();
  await expect(page.getByRole("dialog")).toHaveCount(0);
  await expect(page.getByText("All caught up")).toBeVisible();
  await logout(page);
  await login(page, "employee@its.com", "Demo@123");
  await page.getByRole("link", { name: "Leave balances", exact: true }).click();
  await expect(
    page
      .locator(".balance-card")
      .filter({ hasText: policy })
      .locator(".balance-available strong"),
  ).toHaveText("4");
  await logout(page);
  await login(page, "admin@its.com", "Admin@123");
  await page.getByRole("link", { name: "Leave types", exact: true }).click();
  await page
    .getByRole("button", { name: `Deactivate ${policy}`, exact: true })
    .click();
  await page.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(page.getByRole("row").filter({ hasText: policy })).toContainText(
    "Inactive",
  );
});
