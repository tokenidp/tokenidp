import React from "react";
import { Outlet } from "react-router-dom";
import { PermissionsProvider } from "../../_hooks/usePermissions";

function PermissionsLayout() {
  return (
    <PermissionsProvider>
      <Outlet />
    </PermissionsProvider>
  );
}

export default PermissionsLayout;
