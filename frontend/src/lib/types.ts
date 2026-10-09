export type Role = "Administrator" | "Manager" | "Employee";
export type Tokens = { accessToken: string; refreshToken: string };
export type SessionUser = {
  id: string;
  email: string;
  role: Role;
  expires: number;
};
export type Employee = {
  employeeId: number;
  fullName: string;
  email: string;
  departmentId: number;
  departmentName: string;
  jobTitle: string;
  hireDate: string;
  managerId: number | null;
  isActive: boolean;
  hasAccount: boolean;
  roles: string[];
};
export type Department = { id: number; name: string };
export type LeaveType = {
  id: number;
  name: string;
  allowedDays: number;
  isActive: boolean;
  isAnnual: boolean;
};
export type Balance = {
  id: number;
  isActive: boolean;
  leaveTypeId: number;
  leaveTypeName: string;
  allocatedDays: number;
  usedDays: number;
  pendingDays: number;
  remainingDays: number;
};
export type LeaveRequest = {
  id: number;
  employeeId: number;
  employeeName: string;
  leaveTypeId: number;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  days: number;
  reason: string;
  status: "Pending" | "Approved" | "Rejected" | "Cancelled";
  rejectReason: string | null;
  createdAt: string;
};
export type AuditLog = {
  id: number;
  userName: string | null;
  userId: string;
  action: string;
  entityName: string;
  entityId: string;
  timestamp: string;
  details: string | null;
};
export type EmployeePage = {
  employees: Employee[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
export type LeavePage = {
  items?: LeaveRequest[];
  requests?: LeaveRequest[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
export type Page<T> = {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};
export type Dashboard = Record<string, number>;
