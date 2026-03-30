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
  const url = config.authority + config.tokenPath;
  return await httpPostJson(url, payload);
}

export async function refreshWithToken(config, payload) {
  const url = config.authority + config.tokenPath;
  return await httpPostJson(url, payload);
}

export async function loadUserPermissions(config, accessToken) {
  const url = config.authority + config.userPermissionsPath;
  return await httpGetJson(url, { Authorization: `Bearer ${accessToken}` });
}

export function buildLogoutUrl(config) {
  if (!config?.authority) return "";

  const url = new URL(config.logoutPath || "/logout", config.authority);
  if (config.clientId) {
    url.searchParams.set("client_id", config.clientId);
  }

  return url.toString();
}
