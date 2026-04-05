import React from "react";
import { Outlet } from "react-router-dom";
import { TenantsProvider } from "../../_hooks/useTenants";

function TenantsLayout() {
  return (
    <TenantsProvider>
      <Outlet />
    </TenantsProvider>
  );
}

export default TenantsLayout;
