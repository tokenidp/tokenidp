export const fallbackAppTypes = [
  { label: "SPA", value: "0", icon: "fa fa-globe" },
  { label: "Mobile", value: "1", icon: "fa fa-mobile-alt" },
  { label: "Desktop", value: "2", icon: "fa fa-desktop" },
  { label: "WebApp", value: "3", icon: "fa fa-window-maximize" },
  { label: "Backend", value: "4", icon: "fa fa-robot" },
];

export const fallbackGrantTypes = [
  { value: 0, label: "authorization_code", icon: "fa fa-key" },
  { value: 2, label: "client_credentials", icon: "fa fa-server" },
  { value: 1, label: "refresh_token", icon: "fa fa-sync" },
];

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
