import React, { useState, useEffect } from "react";
import { Link, useLocation, useParams } from "react-router-dom";

const bcMap = {
  "/dashboard": "Dashboard",
  "/users": "Users",
  "/users/adduser": "Create User",
  "/users/edituser": "Edit User",
  "/users/new": "Create User",
  "/users/edit": "Edit User",
  "/roles": "Roles",
  "/roles/new": "Add Role",
  "/roles/edit": "Edit Role",
  "/roles/permissions": "Assign Permissions",
  "/tenants": "Tenants",
  "/tenants/new": "Create Tenant",
  "/tenants/edit": "Edit Tenant",
  "/tokens": "Tokens",
  "/activities": "Activities",
  "/settings": "Settings",
  "/permissions": "Permissions",
  "/permissions/new": "Add Permission",
  "/api-resources": "Api Resources",
  "/api-resources/new": "Add Api Resource",
  "/api-resources/edit": "Edit Api Resource",
  "/users/addrole": "Create Role",
  "/users/editrole": "Edit Role",
};

function Breadcrumbs({ className = "", appendLabel = "" }) {
  const [pathnames, setPathNames] = useState([]);
  const location = useLocation();
  const params = useParams(); // Get all route parameters

  const paths = location.pathname.split("/").filter((x) => x);

  useEffect(() => {
    // Split the pathname and filter out empty segments
    const filteredPaths = paths.filter(
      (item) => !Object.values(params).includes(item)
    );
    setPathNames(filteredPaths);
  }, [location.pathname]); // Run this effect whenever the pathname changes

  return (
    <nav aria-label="breadcrumb">
      <ol className={`breadcrumb ${className}`.trim()}>
        {pathnames.map((value, index) => {
          // Construct the full path to this breadcrumb segment
          const to = `/${pathnames.slice(0, index + 1).join("/")}`;
          const isLast = index === pathnames.length - 1;

          // Check if this segment corresponds to a parameter
          const isParam = Object.values(params).includes(value);

          let bcName =
            bcMap[to] || value.charAt(0).toUpperCase() + value.slice(1);

          bcName = isParam
            ? `${Object.keys(params).find(
                (key) => params[key] === value
              )}: ${bcName}`
            : bcName;

          // Render the breadcrumb
          return isLast ? (
            <li key={to} className="breadcrumb-item active" aria-current="page">
              {bcName}
            </li>
          ) : (
            <li key={to} className="breadcrumb-item">
              <Link to={to}>{bcName}</Link>
            </li>
          );
        })}
        {appendLabel && (
          <li className="breadcrumb-item active" aria-current="page">
            {appendLabel}
          </li>
        )}
      </ol>
    </nav>
  );
}

export default Breadcrumbs;
