import {
  Building2,
  CalendarDays,
  Mail,
  ShieldCheck,
  UserRound,
} from "lucide-react";
import { useAuth } from "../context/AuthContext";
import { DataState, PageTitle } from "../components/UI";
import { formatDate, initials, useResource } from "../lib/hooks";
import type { Employee } from "../lib/types";

export function Profile() {
  const { user } = useAuth();
  const profile = useResource<Employee>("/Employees/me");
  return (
    <>
      <PageTitle
        eyebrow="YOUR PLACE IN THE TEAM"
        title="My profile"
        description="Your information, all in one place. Contact your administrator to update it."
      />
      <section className="panel profile-panel">
        <DataState
          loading={profile.loading}
          error={profile.error}
          retry={profile.reload}
        >
          {profile.data && (
            <>
              <div className="profile-cover">
                <div className="profile-avatar">
                  {initials(profile.data.fullName)}
                </div>
                <div>
                  <h2>{profile.data.fullName}</h2>
                  <p>{profile.data.jobTitle}</p>
                </div>
                <span className="role-pill">
                  <ShieldCheck size={15} />
                  {user!.role}
                </span>
              </div>
              <div className="profile-details">
                {[
                  {
                    label: "Full name",
                    value: profile.data.fullName,
                    icon: UserRound,
                  },
                  {
                    label: "Email address",
                    value: profile.data.email,
                    icon: Mail,
                  },
                  {
                    label: "Department",
                    value: profile.data.departmentName,
                    icon: Building2,
                  },
                  {
                    label: "Hire date",
                    value: formatDate(profile.data.hireDate),
                    icon: CalendarDays,
                  },
                  {
                    label: "Employee number",
                    value: `ITS-${String(profile.data.employeeId).padStart(4, "0")}`,
                    icon: ShieldCheck,
                  },
                  {
                    label: "Account status",
                    value: profile.data.isActive ? "Active" : "Inactive",
                    icon: UserRound,
                  },
                ].map((item) => (
                  <div key={item.label}>
                    <item.icon size={20} />
                    <div>
                      <small>{item.label}</small>
                      <strong>{item.value}</strong>
                    </div>
                  </div>
                ))}
              </div>
            </>
          )}
        </DataState>
      </section>
    </>
  );
}
