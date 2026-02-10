import React from "react";
import { Outlet } from "react-router-dom";
import { ActivitiesProvider } from "../../_hooks/useActivities";

function ActivitiesLayout() {
  return (
    <ActivitiesProvider>
      <Outlet />
    </ActivitiesProvider>
  );
}

export default ActivitiesLayout;
