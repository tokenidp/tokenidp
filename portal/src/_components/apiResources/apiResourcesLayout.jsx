import React from "react";
import { Outlet } from "react-router-dom";
import { ApiResourcesProvider } from "../../_hooks/useApiResources";

function ApiResourcesLayout() {
  return (
    <ApiResourcesProvider>
      <Outlet />
    </ApiResourcesProvider>
  );
}

export default ApiResourcesLayout;
