import React from "react";
import { Outlet } from "react-router-dom";
import { TokensProvider } from "../../_hooks/useTokens";

function TokensLayout() {
  return (
    <TokensProvider>
      <Outlet />
    </TokensProvider>
  );
}

export default TokensLayout;
