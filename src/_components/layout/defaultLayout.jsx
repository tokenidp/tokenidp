import React, { useState } from "react";
import { Outlet } from "react-router-dom";
import Header from "./header";
import NavBar from "./navBar";
import GlobalErrorBanner from "../common/globalErrorBanner";

function DefaultLayout(props) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const handleToggleSidebar = () => {
    setSidebarOpen((open) => !open);
  };

  const handleNavigate = () => {
    setSidebarOpen(false);
  };

  return (
    <div className="main-wrap">
      <NavBar
        onClick={props.onClick}
        isOpen={sidebarOpen}
        onNavigate={handleNavigate}
      />
      <div className="right-panel">
        <Header onToggleSidebar={handleToggleSidebar} />
        <GlobalErrorBanner />
        <div className="content-area">
          <Outlet />
        </div>
      </div>
    </div>
  );
}

export default DefaultLayout;
