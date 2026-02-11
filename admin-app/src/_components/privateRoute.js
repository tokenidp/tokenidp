import React from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "tokentresor-idp-react";

function normalizePermissions(user) {
  const rawPerms = user?.permissions ?? user?.Permissions ?? [];
  let perms = [];
  if (Array.isArray(rawPerms)) {
    perms = rawPerms;
  } else if (typeof rawPerms === "string") {
    try {
      const parsed = JSON.parse(rawPerms);
      perms = Array.isArray(parsed) ? parsed : [];
    } catch {
      perms = [];
    }
  }

  // Create a normalized list of permission keys (case-insensitive)
  return perms
    .map((p) => p?.permissionKey || p?.PermissionKey || p?.Key)
    .filter(Boolean)
    .map((k) => String(k).trim().toLowerCase());
}

/**
 * Props:
 * - requiredAnyOf: array of permission keys; user must have at least ONE
 * - requiredAllOf: array of permission keys; user must have ALL
 */
function PrivateRoute({ children, requiredAnyOf, requiredAllOf }) {
  const user = useAuth();
  const location = useLocation();
  if (!user?.isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  const userKeys = normalizePermissions(user);

  const anyOk =
    !requiredAnyOf?.length ||
    requiredAnyOf.some((k) => userKeys.includes(String(k).toLowerCase()));

  const allOk =
    !requiredAllOf?.length ||
    requiredAllOf.every((k) => userKeys.includes(String(k).toLowerCase()));

  const permissionEntries = Array.isArray(user?.permissions)
    ? user.permissions
    : Array.isArray(user?.Permissions)
      ? user.Permissions
      : [];

  const hasRoutePermission = permissionEntries.some((perm) => {
    const url = perm?.url || perm?.Url;
    if (!url || url === "null") {
      return false;
    }
    const value = perm?.permissionValue || perm?.PermissionValue;

    if (String(value).toLowerCase() === "false") {
      return false;
    }
    return url.toLowerCase() === location.pathname.toLowerCase();
  });

  const hasAccess = (anyOk && allOk) || hasRoutePermission;

  return hasAccess ? (
    children ? (
      children
    ) : (
      <Outlet />
    )
  ) : (
    <Navigate to="/unauthorized" replace />
  );
}

export default PrivateRoute;
