async function httpPostJson(url, body, extraHeaders = {}) {
  const res = await fetch(url, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...extraHeaders },
    body: JSON.stringify(body),
  });

  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }

  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }

  return data;
}

async function httpGetJson(url, extraHeaders = {}) {
  const res = await fetch(url, {
    method: "GET",
    headers: { ...extraHeaders },
  });

  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }

  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }

  return data;
}

function getErrorMessage(data, status) {
  if (!data) return `HTTP ${status}`;

  const wrappedError =
    data.error && typeof data.error === "object"
      ? data.error
      : data.value?.error && typeof data.value.error === "object"
        ? data.value.error
        : null;

  return (
    data.error_description ||
    wrappedError?.error ||
    wrappedError?.Error ||
    wrappedError?.message ||
    wrappedError?.Message ||
    (typeof data.error === "string" ? data.error : "") ||
    data.message ||
    `HTTP ${status}`
  );
}

function withTenant(url, config, target = "api") {
  const tenantKey = target === "auth"
    ? getAuthTenantKey(config)
    : getApiTenantKey(config);
  if (!tenantKey) {
    return url;
  }

  const tenantUrl = new URL(url, typeof window !== "undefined" ? window.location.origin : undefined);
  tenantUrl.searchParams.set(config.tenantQueryParameter || "tenant", tenantKey);
  return tenantUrl.toString();
}

export function extractToken(tokenPayload) {
  if (!tokenPayload)
    return { accessToken: "", refreshToken: "", expiresIn: 0, idToken: "" };

  const accessToken =
    tokenPayload.value.accessToken || tokenPayload.value.access_token || "";
  const refreshToken =
    tokenPayload.value.refreshToken || tokenPayload.value.refresh_token || "";
  const expiresIn =
    Number(
      tokenPayload.value.expiresIn || tokenPayload.value.expires_in || 0,
    ) || 0;
  const idToken = tokenPayload.idToken || tokenPayload.id_token || "";

  return { accessToken, refreshToken, expiresIn, idToken };
}

export function extractPermissions(userInfo) {
  const direct =
    userInfo?.permissions ||
    userInfo?.Permissions ||
    userInfo?.claims ||
    userInfo?.Claims;

  if (Array.isArray(direct)) return direct;

  if (Array.isArray(userInfo?.permissionKeys)) return userInfo.permissionKeys;

  return [];
}

export async function exchangeAuthorizationCode(config, payload) {
  const url = withTenant(config.authority + config.tokenPath, config, "auth");
  return await httpPostJson(url, payload);
}

export async function refreshWithToken(config, payload) {
  const url = withTenant(config.authority + config.tokenPath, config, "auth");
  return await httpPostJson(url, payload);
}

export async function revokeToken(config, { accessToken, token, reasonRevoked }) {
  if (!config?.authority || !accessToken || !token) {
    return null;
  }

  const url = withTenant(
    new URL(config.revokePath || "/revoke", config.authority).toString(),
    config,
    "auth",
  );

  const res = await fetch(url, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${accessToken}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({
      token,
      reasonRevoked: reasonRevoked || "logout",
    }),
  });

  const text = await res.text();
  let data = null;
  try {
    data = text ? JSON.parse(text) : null;
  } catch {
    data = text;
  }

  if (!res.ok) {
    const msg = getErrorMessage(data, res.status);
    const err = new Error(msg);
    err.status = res.status;
    err.data = data;
    throw err;
  }

  return data;
}

export async function loadUserPermissions(config, accessToken) {
  const url = withTenant(config.authority + config.userPermissionsPath, config, "api");
  return await httpGetJson(url, { Authorization: `Bearer ${accessToken}` });
}

export function buildLogoutUrl(config) {
  if (!config?.authority) return "";

  const url = new URL(
    withTenant(
      new URL(config.logoutPath || "/logout", config.authority).toString(),
      config,
      "auth",
    ),
  );
  if (config.clientId) {
    url.searchParams.set("client_id", config.clientId);
  }

  const postLogoutRedirectUri = resolvePostLogoutRedirectUri(config);
  if (postLogoutRedirectUri) {
    url.searchParams.set("post_logout_redirect_uri", postLogoutRedirectUri);
  }

  return url.toString();
}

function resolvePostLogoutRedirectUri(config) {
  const candidate = config?.postLogoutRedirectUri;
  if (!candidate) {
    return "";
  }

  if (typeof window !== "undefined" && window.location?.origin) {
    return new URL(candidate, window.location.origin).toString();
  }

  if (config?.redirectUri) {
    return new URL(candidate, config.redirectUri).toString();
  }

  return String(candidate);
}
