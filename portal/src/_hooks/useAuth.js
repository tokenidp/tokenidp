import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useAuth as useIdpAuth } from "tokenidp-react";
import { getPortalConfig } from "../config";

const PortalAuthContext = createContext(null);

function extractApiValue(payload) {
  return payload?.value ?? payload?.Value ?? payload ?? {};
}

function extractPermissions(userInfo) {
  const direct =
    userInfo?.permissions ||
    userInfo?.Permissions ||
    userInfo?.claims ||
    userInfo?.Claims;

  if (Array.isArray(direct)) return direct;
  if (Array.isArray(userInfo?.permissionKeys)) return userInfo.permissionKeys;
  if (Array.isArray(userInfo?.PermissionKeys)) return userInfo.PermissionKeys;

  return [];
}

function buildPermissionUrl(tenantKey) {
  const portalConfig = getPortalConfig();
  const url = new URL(
    portalConfig.userPermissionsPath,
    portalConfig.baseUrl || window.location.origin,
  );
  const tenantQueryParameter = portalConfig.tenantQueryParameter;

  if (tenantKey && !url.searchParams.has(tenantQueryParameter)) {
    url.searchParams.set(tenantQueryParameter, tenantKey);
  }

  return url.toString();
}

export function AuthProvider({ children }) {
  const idpAuth = useIdpAuth();
  const [profile, setProfile] = useState({
    userId: 0,
    tenantId: 0,
    tenantKey: "",
    isSystemTenant: false,
    userName: "",
    permissions: [],
  });
  const [permissionsLoading, setPermissionsLoading] = useState(false);
  const [permissionsError, setPermissionsError] = useState("");
  const [loadedAccessToken, setLoadedAccessToken] = useState("");

  const loadPermissions = useCallback(async () => {
    if (!idpAuth?.isAuthenticated || !idpAuth?.accessToken) {
      setProfile({
        userId: 0,
        tenantId: 0,
        tenantKey: "",
        isSystemTenant: false,
        userName: "",
        permissions: [],
      });
      setPermissionsError("");
      setLoadedAccessToken("");
      return null;
    }

    setPermissionsLoading(true);
    setPermissionsError("");

    try {
      const response = await fetch(buildPermissionUrl(idpAuth.tenantKey), {
        method: "GET",
        headers: {
          Authorization: `Bearer ${idpAuth.accessToken}`,
          "Content-Type": "application/json",
        },
      });

      const text = await response.text();
      const payload = text ? JSON.parse(text) : null;

      if (!response.ok || payload?.isSuccess === false || payload?.IsSuccess === false) {
        const message =
          payload?.error?.error ||
          payload?.Error?.Error ||
          payload?.message ||
          payload?.Message ||
          `Unable to load user permissions (${response.status}).`;
        throw new Error(message);
      }

      const userInfo = extractApiValue(payload);
      const nextProfile = {
        userId: userInfo.userId ?? userInfo.UserId ?? 0,
        tenantId: userInfo.tenantId ?? userInfo.TenantId ?? 0,
        tenantKey:
          userInfo.tenantKey ??
          userInfo.TenantKey ??
          idpAuth.tenantKey ??
          "",
        isSystemTenant:
          userInfo.isSystemTenant ?? userInfo.IsSystemTenant ?? false,
        userName: userInfo.userName ?? userInfo.UserName ?? "",
        permissions: extractPermissions(userInfo),
      };

      setProfile(nextProfile);
      setLoadedAccessToken(idpAuth.accessToken);
      return nextProfile;
    } catch (error) {
      const message = error?.message || "Unable to load user permissions.";
      setPermissionsError(message);
      setProfile((current) => ({ ...current, permissions: [] }));
      setLoadedAccessToken(idpAuth.accessToken);
      return null;
    } finally {
      setPermissionsLoading(false);
    }
  }, [idpAuth?.accessToken, idpAuth?.isAuthenticated, idpAuth?.tenantKey]);

  useEffect(() => {
    loadPermissions();
  }, [loadPermissions]);

  const value = useMemo(
    () => {
      const needsPermissionLoad =
        !!idpAuth?.isAuthenticated &&
        !!idpAuth?.accessToken &&
        loadedAccessToken !== idpAuth.accessToken;

      return {
        ...idpAuth,
        ...profile,
        tenantKey: profile.tenantKey || idpAuth?.tenantKey || "",
        permissionsLoading: permissionsLoading || needsPermissionLoad,
        permissionsError,
        reloadPermissions: loadPermissions,
        hasPermission: (permissionKey) =>
          valueHasPermission(profile.permissions, permissionKey),
        hasAnyPermission: (permissionKeys) =>
          Array.isArray(permissionKeys) &&
          permissionKeys.some((permissionKey) =>
            valueHasPermission(profile.permissions, permissionKey),
          ),
        hasAllPermissions: (permissionKeys) =>
          Array.isArray(permissionKeys) &&
          permissionKeys.every((permissionKey) =>
            valueHasPermission(profile.permissions, permissionKey),
          ),
      };
    },
    [
      idpAuth,
      loadPermissions,
      loadedAccessToken,
      permissionsError,
      permissionsLoading,
      profile,
    ],
  );

  return (
    <PortalAuthContext.Provider value={value}>
      {children}
    </PortalAuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(PortalAuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider");
  }

  return context;
}

function valueHasPermission(permissions, permissionKey) {
  return permissions.some(
    (permission) =>
      String(
        permission?.permissionKey ||
          permission?.PermissionKey ||
          permission?.Key ||
          permission,
      ).toLowerCase() === String(permissionKey).toLowerCase(),
  );
}
