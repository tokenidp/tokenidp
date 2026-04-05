import React from "react";
import { Outlet } from "react-router-dom";
import { ApplicationsProvider } from "../../_hooks/useApplications";

function ApplicationsLayout() {
  return (
    <ApplicationsProvider>
      <Outlet />
    </ApplicationsProvider>
  );
}

export default ApplicationsLayout;
