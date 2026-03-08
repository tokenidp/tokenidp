export const fallbackAppTypes = [
  { label: "SPA", value: "0", icon: "fa fa-globe" },
  { label: "Mobile", value: "1", icon: "fa fa-mobile-alt" },
  { label: "Desktop", value: "2", icon: "fa fa-desktop" },
  { label: "WebApp", value: "3", icon: "fa fa-window-maximize" },
  { label: "Backend", value: "4", icon: "fa fa-robot" },
  { label: "Device/IOT (Under Development)", value: "5", icon: "fa fa-microchip" },
];

export const GrantTypeId = Object.freeze({
  AuthorizationCode: 0,
  RefreshToken: 1,
  ClientCredentials: 2,
  DeviceCode: 3,
  Ciba: 4,
  Password: 5,
});

const grantTypeMetadataByKey = Object.freeze({
  authorization_code: {
    id: GrantTypeId.AuthorizationCode,
    icon: "fa fa-key",
  },
  refresh_token: {
    id: GrantTypeId.RefreshToken,
    icon: "fa fa-sync",
  },
  client_credentials: {
    id: GrantTypeId.ClientCredentials,
    icon: "fa fa-server",
  },
  device_code: {
    id: GrantTypeId.DeviceCode,
    icon: "fa fa-mobile-screen",
  },
  ciba: {
    id: GrantTypeId.Ciba,
    icon: "fa fa-link",
  },
  password: {
    id: GrantTypeId.Password,
    icon: "fa fa-user",
  },
});

export const getGrantTypeDisplayLabel = (value) => {
  switch (value) {
    case "authorization_code":
      return "Authorization Code";
    case "refresh_token":
      return "Refresh Token";
    case "client_credentials":
      return "Client Credentials";
    case "device_code":
      return "Device Code";
    case "ciba":
      return "CIBA";
    case "password":
      return "Resource Owner Password Credentials";
    default:
      return String(value);
  }
};

export const fallbackGrantTypes = Object.keys(grantTypeMetadataByKey).map((key) => ({
  id: grantTypeMetadataByKey[key].id,
  key,
  value: getGrantTypeDisplayLabel(key),
  icon: grantTypeMetadataByKey[key].icon,
}));

export const fallbackScopes = [
  { value: "openid", label: "openid", icon: "fa fa-fingerprint" },
  { value: "profile", label: "profile", icon: "fa fa-id-card" },
  { value: "email", label: "email", icon: "fa fa-envelope" },
  { value: "offline_access", label: "offline_access", icon: "fa fa-clock" },
  { value: "api.read", label: "api.read", icon: "fa fa-eye" },
  { value: "api.write", label: "api.write", icon: "fa fa-pen" },
];

export const normalizeLookupOptions = (items) =>
  (items || []).map((option) => ({
    key: option.key ?? option.id ?? option.Key ?? option.Id,
    value: option.value ?? option.name ?? option.Value ?? option.Name,
  }));

export const normalizeGrantTypeOptions = (items) => {
  const normalized = normalizeLookupOptions(items)
    .map((option) => {
      const key = String(option.key ?? "").trim();
      const metadata = grantTypeMetadataByKey[key];
      if (!metadata) {
        return null;
      }

      return {
        id: metadata.id,
        key,
        value: option.value || getGrantTypeDisplayLabel(key),
        icon: metadata.icon,
      };
    })
    .filter(Boolean);

  return normalized.length ? normalized : fallbackGrantTypes;
};

export const normalizeValue = (value, fallback) =>
  value === undefined || value === null ? fallback : String(value);

export const normalizeTimeWindow = (value) => {
  if (value === undefined || value === null) {
    return null;
  }

  const raw = String(value).trim();
  if (!raw) {
    return null;
  }

  if (/^\d+$/.test(raw)) {
    const totalMinutes = Number(raw);
    if (!Number.isFinite(totalMinutes)) {
      return null;
    }
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return `${String(hours).padStart(2, "0")}:${String(minutes).padStart(2, "0")}:00`;
  }

  const dayPattern = /^(\d+)\.(\d{1,2}):([0-5]\d):([0-5]\d)(\.\d{1,7})?$/;
  const hmsPattern = /^(\d{1,2}):([0-5]\d):([0-5]\d)$/;
  if (dayPattern.test(raw)) {
    const match = raw.match(dayPattern);
    if (!match) {
      return null;
    }
    const [, days, hours, minutes, seconds, fractional = ""] = match;
    return `${days}.${hours.padStart(2, "0")}:${minutes}:${seconds}${fractional}`;
  }

  if (hmsPattern.test(raw)) {
    const [hours, minutes, seconds] = raw.split(":");
    return `${hours.padStart(2, "0")}:${minutes}:${seconds}`;
  }

  return null;
};

export const isValidTimeWindow = (value) =>
  !value || Boolean(normalizeTimeWindow(value));

export const createWizardState = (initialValues = {}) => ({
  ...initialValues,
});
