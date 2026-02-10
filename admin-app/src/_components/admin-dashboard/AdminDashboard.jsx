import React from "react";
import AdminDashboardHeader from "./AdminDashboardHeader";
import TenantKpiRow from "./TenantKpiRow";
import UsersRolesCard from "./UsersRolesCard";
import AccessUsageCard from "./AccessUsageCard";
import AppsOverviewCard from "./AppsOverviewCard";
import ActivityAuditCard from "./ActivityAuditCard";
import ComplianceCard from "./ComplianceCard";

const tenantDashboardData = {
  kpis: [
    { label: "Total users", value: "2,480" },
    { label: "Active users", value: "2,210" },
    { label: "Inactive users", value: "270" },
    { label: "Users by role", value: "Admin 12% | Manager 28% | Staff 60%" },
  ],
  recentUsers: [
    { name: "Ava Patel", role: "Manager", added: "2h ago" },
    { name: "Noah Kim", role: "Staff", added: "5h ago" },
    { name: "Sophia Reed", role: "Admin", added: "Yesterday" },
    { name: "Liam Chen", role: "Staff", added: "2 days ago" },
  ],
  accessStats: [
    {
      label: "Top used permissions",
      value: "orders.read, users.view, reports.export",
    },
    { label: "Denied actions", value: "39 (last 7 days)" },
    { label: "Role usage distribution", value: "Balanced across 6 roles" },
    { label: "Unused roles", value: "2 roles unused in 30 days" },
  ],
  permissionTrends: [
    { permission: "orders.read", usage: "1,420" },
    { permission: "users.view", usage: "980" },
    { permission: "reports.export", usage: "740" },
    { permission: "billing.write", usage: "210" },
  ],
  appStats: [
    { label: "Total applications", value: "18" },
    { label: "Active apps", value: "15" },
    { label: "Inactive apps", value: "3" },
    { label: "Tokens per app (avg)", value: "4,200 / day" },
  ],
  expiringApps: [
    { app: "Retail Web", expires: "6 days" },
    { app: "Partner Portal", expires: "12 days" },
    { app: "Finance API", expires: "18 days" },
  ],
  auditEvents: [
    { event: "User created", actor: "J. Khan", time: "10 mins ago" },
    { event: "Role updated", actor: "M. Davis", time: "1 hour ago" },
    { event: "Permission removed", actor: "L. Nguyen", time: "Today" },
    { event: "User disabled", actor: "A. Patel", time: "Yesterday" },
  ],
  complianceStats: [
    { label: "MFA adoption rate", value: "84%" },
    { label: "Privileged users", value: "14" },
    { label: "Pending approvals", value: "6" },
    { label: "Access review reminders", value: "3 due" },
  ],
};

function AdminDashboard() {
  return (
    <>
      <AdminDashboardHeader />
      <TenantKpiRow kpis={tenantDashboardData.kpis} />

      <div className="row g-3">
        <div className="col-12 col-lg-6">
          <UsersRolesCard recentUsers={tenantDashboardData.recentUsers} />
        </div>
        <div className="col-12 col-lg-6">
          <AccessUsageCard
            accessStats={tenantDashboardData.accessStats}
            permissionTrends={tenantDashboardData.permissionTrends}
          />
        </div>
      </div>

      <div className="row g-3 mt-1">
        <div className="col-12 col-lg-6">
          <AppsOverviewCard
            appStats={tenantDashboardData.appStats}
            expiringApps={tenantDashboardData.expiringApps}
          />
        </div>
        <div className="col-12 col-lg-6">
          <ActivityAuditCard auditEvents={tenantDashboardData.auditEvents} />
        </div>
      </div>

      <div className="row g-3 mt-1">
        <div className="col-12">
          <ComplianceCard complianceStats={tenantDashboardData.complianceStats} />
        </div>
      </div>
    </>
  );
}

export default AdminDashboard;
