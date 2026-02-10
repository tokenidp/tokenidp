import React from "react";
import { Outlet } from "react-router-dom";
import { UsersProvider } from "../../_hooks/useUsers";

function UsersLayout() {
  return (
    <UsersProvider>
      <Outlet />
    </UsersProvider>
  );
}

export default UsersLayout;
