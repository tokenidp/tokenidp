import React, { useState } from "react";
import { BrowserRouter as Router, Route, Routes } from "react-router-dom";
import "./_assets/portal.css";
import Roles from "./_components/roles/roles";
import RolesLayout from "./_components/roles/rolesLayout";
import AddEditRole from "./_components/roles/addEditRole";
import AssignPermissions from "./_components/roles/assignPermissions";
import Users from "./_components/users/users";
import UsersLayout from "./_components/users/usersLayout";
import LandingLayout from "./_components/layout/landingLayout";
import DefaultLayout from "./_components/layout/defaultLayout";
import PrivateRoute from "./_components/privateRoute";
import AddEditUser from "./_components/users/addEditUser";
import NoMatch from "./_components/noMatch";
import UnAuthorized from "./_components/unauthorized";
import ApplicationsList from "./_components/applications/applicationsList";
import ApplicationCreate from "./_components/applications/applicationCreate";
import ApplicationsLayout from "./_components/applications/applicationsLayout";
import ApplicationEdit from "./_components/applications/applicationEdit";
import Dashboard from "./_components/dashboard/Dashboard";
import AdminDashboard from "./_components/admin-dashboard/AdminDashboard";
import TenantsLayout from "./_components/tenants/tenantsLayout";
import TenantsList from "./_components/tenants/tenantsList";
import AddEditTenant from "./_components/tenants/addEditTenant";
import TokensList from "./_components/tokens/tokensList";
import TokensLayout from "./_components/tokens/tokensLayout";
import ActivitiesList from "./_components/activities/activitiesList";
import ActivitiesLayout from "./_components/activities/activitiesLayout";
import SettingsList from "./_components/settings/settingsList";
import SettingsLayout from "./_components/settings/settingsLayout";
import AddPermission from "./_components/permissions/addPermission";
import PermissionsList from "./_components/permissions/permissionsList";
import PermissionsLayout from "./_components/permissions/permissionsLayout";
import { AuthCallback } from "@tokentresor/idp-react";
import { LoginPage } from "./_components/LoginPage"; // simple UI, not OAuth logic

function App() {
  const [claimId, setClaimId] = useState(0);
  const [pageName, setPageName] = useState(0);

  const onClick = (id, name) => {
    setClaimId(id);
    setPageName(name);
  };

  return (
    <Router>
      <Routes>
        <Route path="/auth/callback" element={<AuthCallback />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/" element={<LandingLayout />} />
        <Route element={<DefaultLayout onClick={onClick} />}>
          <Route element={<PrivateRoute />}>
            <Route
              path="/dashboard"
              element={
                <PrivateRoute requiredAnyOf={["dashboard.view"]}>
                  <Dashboard claimId={claimId} />
                </PrivateRoute>
              }
            />
            <Route
              path="/roles"
              element={
                <PrivateRoute requiredAnyOf={["roles.view"]}>
                  <RolesLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<Roles />} />
              <Route
                path="new"
                element={
                  <PrivateRoute requiredAnyOf={["roles.add"]}>
                    <AddEditRole mode="add" />
                  </PrivateRoute>
                }
              />
              <Route
                path="edit/:roleId"
                element={
                  <PrivateRoute requiredAnyOf={["roles.edit"]}>
                    <AddEditRole mode="edit" />
                  </PrivateRoute>
                }
              />
              <Route
                path="permissions/:roleId"
                element={
                  <PrivateRoute requiredAnyOf={["roles.edit"]}>
                    <AssignPermissions />
                  </PrivateRoute>
                }
              />
            </Route>
            <Route
              path="/applications"
              element={
                <PrivateRoute requiredAnyOf={["applications.view"]}>
                  <ApplicationsLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<ApplicationsList />} />
              <Route
                path="new"
                element={
                  <PrivateRoute requiredAnyOf={["applications.add"]}>
                    <ApplicationCreate />
                  </PrivateRoute>
                }
              />
              <Route
                path="edit/:id"
                element={
                  <PrivateRoute requiredAnyOf={["applications.edit"]}>
                    <ApplicationEdit />
                  </PrivateRoute>
                }
              />
            </Route>
            <Route
              path="/users"
              element={
                <PrivateRoute requiredAnyOf={["users.view"]}>
                  <UsersLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<Users />} />
              <Route
                path="new"
                element={
                  <PrivateRoute requiredAnyOf={["users.add"]}>
                    <AddEditUser mode="add" />
                  </PrivateRoute>
                }
              />
              <Route
                path="edit/:userId"
                element={
                  <PrivateRoute requiredAnyOf={["users.edit"]}>
                    <AddEditUser mode="edit" />
                  </PrivateRoute>
                }
              />
            </Route>
            <Route
              path="/tenants"
              element={
                <PrivateRoute requiredAnyOf={["tenants.view"]}>
                  <TenantsLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<TenantsList />} />
              <Route
                path="new"
                element={
                  <PrivateRoute requiredAnyOf={["tenants.add"]}>
                    <AddEditTenant mode="add" />
                  </PrivateRoute>
                }
              />
              <Route
                path="edit/:tenantId"
                element={
                  <PrivateRoute requiredAnyOf={["tenants.edit"]}>
                    <AddEditTenant mode="edit" />
                  </PrivateRoute>
                }
              />
            </Route>
            <Route
              path="/tokens"
              element={
                <PrivateRoute requiredAnyOf={["tokens.view"]}>
                  <TokensLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<TokensList />} />
            </Route>
            <Route
              path="/activities"
              element={
                <PrivateRoute requiredAnyOf={["activities.view"]}>
                  <ActivitiesLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<ActivitiesList />} />
            </Route>
            <Route
              path="/settings"
              element={
                <PrivateRoute requiredAnyOf={["settings.view"]}>
                  <SettingsLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<SettingsList />} />
            </Route>
            <Route
              path="/permissions"
              element={
                <PrivateRoute requiredAnyOf={["permissions.view"]}>
                  <PermissionsLayout />
                </PrivateRoute>
              }
            >
              <Route index element={<PermissionsList />} />
              <Route
                path="new"
                element={
                  <PrivateRoute requiredAnyOf={["permissions.add"]}>
                    <AddPermission />
                  </PrivateRoute>
                }
              />
              <Route
                path="edit/:permissionId"
                element={
                  <PrivateRoute requiredAnyOf={["permissions.edit"]}>
                    <AddPermission mode="edit" />
                  </PrivateRoute>
                }
              />
            </Route>
          </Route>
          <Route path="/unauthorized" element={<UnAuthorized />} />
          <Route path="*" element={<NoMatch />} />
        </Route>
      </Routes>
    </Router>
  );
}

export default App;
