import React, { useMemo } from "react";
import { GrantTypeId } from "../wizardState";

const parseLines = (value) =>
  String(value || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

const validateUriLines = (value, isRequired) => {
  const lines = parseLines(value);
  if (!lines.length) {
    return isRequired ? "Redirect URI is required." : true;
  }
  for (const line of lines) {
    if (line.includes("*")) {
      return "Wildcards are not allowed in redirect URIs";
    }
    if (line.includes("#")) {
      return "Fragments are not allowed in redirect URIs";
    }
    if (line.startsWith("?")) {
      return "Invalid URI format";
    }
    let parsed;
    try {
      parsed = new URL(line);
    } catch (error) {
      return "Invalid URI format";
    }
    const isLocalhost = parsed.hostname === "localhost";
    if (!isLocalhost && parsed.protocol !== "https:") {
      return "Only HTTPS is allowed (except http://localhost for development)";
    }
  }
  return true;
};

const validateLogoutLines = (value) => {
  const lines = parseLines(value);
  if (!lines.length) {
    return true;
  }
  for (const line of lines) {
    if (line.includes("*")) {
      return "Wildcards are not allowed in redirect URIs";
    }
    if (line.includes("#")) {
      return "Fragments are not allowed in redirect URIs";
    }
    if (line.startsWith("?")) {
      return "Invalid URI format";
    }
    let parsed;
    try {
      parsed = new URL(line);
    } catch (error) {
      return "Invalid URI format";
    }
    const isLocalhost = parsed.hostname === "localhost";
    if (!isLocalhost && parsed.protocol !== "https:") {
      return "Only HTTPS is allowed (except http://localhost for development)";
    }
  }
  return true;
};

function AuthStep({
  register,
  errors,
  appType,
  isPublicClient,
  showSecret,
  setShowSecret,
  onRegenerateSecret,
  grantTypes,
  toggleGrant,
  grantOptions,
  allowedGrants,
  hasInsecureGrant,
  grantError,
  isDeviceIot,
  isWebClient,
}) {
  const hasAuthorizationGrant =
    grantTypes.includes(GrantTypeId.AuthorizationCode) ||
    grantTypes.includes(GrantTypeId.Password);
  const secretLocked = isPublicClient || isDeviceIot;
  const usesClientCredentials = grantTypes.includes(GrantTypeId.ClientCredentials);

  const requiresRedirectUri = useMemo(
    () => grantTypes.includes(GrantTypeId.AuthorizationCode),
    [grantTypes],
  );
  const redirectFieldsDisabled = usesClientCredentials && !requiresRedirectUri;

  const redirectHint = useMemo(() => {
    if (redirectFieldsDisabled) {
      return "Client credentials is a machine-to-machine flow. Redirect and logout URIs are not used.";
    }
    if (!requiresRedirectUri || appType === "4") {
      return "This application type does not use redirect-based flows. Redirect URIs are not required.";
    }
    return "Redirect URIs are required for authorization code flow.";
  }, [appType, redirectFieldsDisabled, requiresRedirectUri]);

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title">Authentication</h6>
          <div className="wizard-info-banner" role="status">
            Grant selection, client secret handling, and redirect URI configuration
            directly affect client security and sign-in behavior.
          </div>

          <div className="auth-field">
            <label className="form-label fw-semibold">OAuth Grants</label>
            <div className="row g-2">
              {grantOptions.map((grant) => {
                const isRefreshToken = grant.id === GrantTypeId.RefreshToken;
                const disabledByAppType = !allowedGrants.has(grant.id);
                const disabledByDependency = isRefreshToken && !hasAuthorizationGrant;
                const disabled = disabledByAppType || disabledByDependency;
                const reason = disabledByDependency
                  ? "Enable Authorization Code or Password to use refresh_token."
                  : disabledByAppType
                    ? grant.id === GrantTypeId.ClientCredentials
                      ? "Available only for Backend (machine-to-machine) applications."
                      : grant.id === GrantTypeId.DeviceCode
                        ? "Available for Mobile, Desktop, and Device/IOT applications."
                        : grant.id === GrantTypeId.Ciba
                          ? "Available only for Web applications."
                          : grant.id === GrantTypeId.Password
                            ? "Available for Mobile, Desktop, Web, and Backend applications."
                            : "Not supported for this application type."
                    : null;
                return (
                  <div className="col-12 col-lg-6" key={grant.id}>
                    <div
                      className={`option-card auth-grant-card d-flex align-items-start gap-2 ${
                        grantTypes.includes(grant.id) ? "option-card-active" : ""
                      } ${disabled ? "is-locked" : ""}`}
                    >
                      <input
                        className="form-check-input mt-1"
                        type="checkbox"
                        id={`grant-${grant.id}`}
                        checked={grantTypes.includes(grant.id)}
                        onChange={() => toggleGrant(grant.id)}
                        disabled={disabled}
                        aria-label={`${grant.value} grant`}
                      />
                      <label className="form-check-label w-100" htmlFor={`grant-${grant.id}`}>
                        <div className="grant-title">{grant.value}</div>
                        <div className="grant-sublabel">{grant.key}</div>
                        {disabled && reason && <div className="grant-reason">{reason}</div>}
                      </label>
                    </div>
                  </div>
                );
              })}
            </div>
            <div className="form-text">
              Grant types are prefiltered based on app type selection.
            </div>
            {isWebClient && (
              <div className="form-text text-muted">
                CIBA is currently under development and is not yet available for use.
              </div>
            )}
            {isDeviceIot && (
              <div className="form-text text-muted">
                Device and IoT clients sign in with the Device Authorization flow, where users
                complete verification on a separate activation screen.
              </div>
            )}
            {isPublicClient && !isDeviceIot && (
              <div className="form-text text-muted">
                SPA clients must use PKCE with Authorization Code flow. Mobile and Desktop
                clients may also use Password flow when explicitly allowed.
              </div>
            )}
            {hasInsecureGrant && (
              <div className="alert alert-warning mt-3 mb-0">
                The selected grant type is not recommended for this app type.
              </div>
            )}
            {grantError && (
              <div className="alert alert-danger mt-3 mb-0" role="alert">
                {grantError}
              </div>
            )}
          </div>

          <div className="auth-divider"></div>

          <div className="row g-3">
            <div className="col-12 col-lg-6">
              <div className={`auth-field auth-field-inline ${secretLocked ? "is-locked" : ""}`}>
                <label className="form-label fw-semibold">Client Secret</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className={`fa ${secretLocked ? "fa-lock" : "fa-key"}`}></i>
                  </span>
                  <input
                    className="form-control"
                    type={showSecret ? "text" : "password"}
                    {...register("clientSecret")}
                    readOnly
                    disabled={secretLocked}
                    aria-label="Client secret"
                  />
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    onClick={() => setShowSecret((prev) => !prev)}
                    disabled={secretLocked}
                    aria-label={showSecret ? "Hide client secret" : "Show client secret"}
                  >
                    <i className={`fa ${showSecret ? "fa-eye-slash" : "fa-eye"}`}></i>
                  </button>
                  <button
                    className="btn btn-outline-secondary"
                    type="button"
                    onClick={onRegenerateSecret}
                    disabled={secretLocked}
                    aria-label="Regenerate client secret"
                  >
                    Regenerate
                  </button>
                </div>
                {secretLocked && (
                  <div className="auth-helper text-muted">
                    <i className="fa fa-lock me-1" aria-hidden="true"></i>
                    This application type cannot securely store secrets.
                    <div className="auth-hint">
                      To enable client secrets, select WebApp or Backend.
                    </div>
                  </div>
                )}
              </div>
            </div>
            <div className="col-12 col-lg-6">
              <div className={`auth-field auth-field-inline ${secretLocked ? "is-locked" : ""}`}>
                <label className="form-label fw-semibold">Client Secret Expiry</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className={`fa ${secretLocked ? "fa-lock" : "fa-calendar"}`}></i>
                  </span>
                  <input
                    className="form-control"
                    type="number"
                    min="1"
                    disabled={secretLocked}
                    {...register("clientSecretExpiry")}
                    aria-label="Client secret expiry"
                  />
                  <span className="input-group-text">Days</span>
                </div>
                <div className="form-text text-muted">
                  Number of days until the client secret expires.
                </div>
              </div>
            </div>
          </div>

          <div className="auth-divider"></div>

          <div className="row g-3">
            <div className="col-12 col-lg-6">
              <label className="form-label fw-semibold">
                Redirect URIs {requiresRedirectUri ? "*" : ""}
              </label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-link"></i>
                </span>
                <textarea
                  className={`form-control${errors.redirectUri ? " is-invalid" : ""}`}
                  rows="4"
                  disabled={redirectFieldsDisabled}
                  placeholder={
                    "https://app.example.com/callback\nhttp://localhost:3000/callback"
                  }
                  {...register("redirectUri", {
                    validate: (value) =>
                      redirectFieldsDisabled
                        ? true
                        : validateUriLines(value, requiresRedirectUri),
                  })}
                ></textarea>
              </div>
              {errors.redirectUri && <div className="error-msg">{errors.redirectUri.message}</div>}
              <div className="form-text">
                Add one URI per line.{" "}
                {requiresRedirectUri
                  ? "Required for authorization code flows."
                  : redirectFieldsDisabled
                    ? "Disabled for client_credentials-only authentication."
                    : "Optional when authorization code flow is not enabled."}
              </div>
              <div className="form-text text-muted">{redirectHint}</div>
            </div>

            <div className="col-12 col-lg-6">
              <label className="form-label fw-semibold">Logout Redirect URIs</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-sign-out-alt"></i>
                </span>
                <textarea
                  className={`form-control${errors.logoutRedirectUri ? " is-invalid" : ""}`}
                  rows="4"
                  disabled={redirectFieldsDisabled}
                  placeholder={"https://app.example.com/logout\nhttp://localhost:3000/logout"}
                  {...register("logoutRedirectUri", {
                    validate: (value) => (redirectFieldsDisabled ? true : validateLogoutLines(value)),
                  })}
                ></textarea>
              </div>
              {errors.logoutRedirectUri && (
                <div className="error-msg">{errors.logoutRedirectUri.message}</div>
              )}
              <div className="form-text">
                {redirectFieldsDisabled
                  ? "Disabled for client_credentials-only authentication."
                  : "Optional. Used after user signs out to redirect back to your application."}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AuthStep;
