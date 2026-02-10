import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@tokentresor/idp-react";

function Header({ onToggleSidebar }) {
  const navigate = useNavigate();
  const [name, setName] = useState("");
  const user  = useAuth();

  useEffect(() => {
    setName(user.userName);
  }, [user]);

  function logout() {
    user.logout();
    navigate("/login");
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
      <div className="search-box">
        <i className="fa fa-search"></i>
        <input type="text" className="form-control" placeholder="Quick Search..." />
      </div>
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
