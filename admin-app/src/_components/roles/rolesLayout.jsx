import React from "react";
import { Outlet } from "react-router-dom";
import { RolesProvider } from "../../_hooks/useRoles";

function RolesLayout() {
  return (
    <RolesProvider>
      <Outlet />
    </RolesProvider>
  );
}

export default RolesLayout;
