import React, { useEffect, useState } from "react";
import { useAuth } from "tokenidp-react";

function Header({ onToggleSidebar, onToggleTheme, theme }) {
  const [name, setName] = useState("");
  const user = useAuth();

  useEffect(() => {
    setName(user.userName);
  }, [user]);

  function logout() {
    user.logout();
  }

  return (
    <header className="topbar">
      <button
        className="sidebar-toggle d-lg-none"
        type="button"
        onClick={onToggleSidebar}
        aria-label="Toggle sidebar"
      >
        <i className="fa fa-bars"></i>
      </button>
      <div className="topbar-right">
        <span className="text-secondary">Hi, {name || "Admin"}</span>
        <i className="fa fa-bell text-secondary"></i>
        <button className="logout-btn" type="button" onClick={logout}>
          <i className="fa fa-sign-out-alt me-1"></i>
          Logout
        </button>
      </div>
    </header>
  );
}

export default Header;
