import React from "react";
import { Outlet } from "react-router-dom";
import { SettingsProvider } from "../../_hooks/useSettings";

function SettingsLayout() {
  return (
    <SettingsProvider>
      <Outlet />
    </SettingsProvider>
  );
}

export default SettingsLayout;
