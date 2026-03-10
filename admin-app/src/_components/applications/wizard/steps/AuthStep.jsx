import React from "react";
import { GrantTypeId } from "../wizardState";

function AuthStep({
  register,
  watch,
  setValue,
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
  externalProviderOptions,
  externalRoleOptions = [],
}) {
  const hasAuthorizationGrant =
    grantTypes.includes(GrantTypeId.AuthorizationCode) ||
    grantTypes.includes(GrantTypeId.Password);
  const secretLocked = isPublicClient || isDeviceIot;
  const showExternalProviders = watch("authPolicy.showExternalProviders");

  const autoCreateUsersValue = watch("authPolicy.autoCreateUsers");
  const isAutoCreateUsersEnabled =
    autoCreateUsersValue === undefined ? true : !!autoCreateUsersValue;

  const defaultRoleValue = watch("authPolicy.defaultRoleId");
  const parsedDefaultRoleId =
    defaultRoleValue === "" || defaultRoleValue === null || defaultRoleValue === undefined
      ? null
      : Number(defaultRoleValue);
  const hasDefaultRole =
    parsedDefaultRoleId !== null && Number.isFinite(parsedDefaultRoleId) && parsedDefaultRoleId > 0;
  const showRoleSelectionWarning = isAutoCreateUsersEnabled && !hasDefaultRole;

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-lg-8 col-xl-7">
        <div className="card">
          <div className="card-body">
            <h6 className="card-title">Authentication &amp; Grants</h6>
            <div className="wizard-info-banner" role="status">
              Grant and authentication settings directly impact application security.
              Incorrect configuration may expose sensitive resources.
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
                    <div className="col-12" key={grant.id}>
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
                        <label
                          className="form-check-label w-100"
                          htmlFor={`grant-${grant.id}`}
                        >
                          <div className="grant-title">{grant.value}</div>
                          <div className="grant-sublabel">{grant.key}</div>
                          {disabled && reason && (
                            <div className="grant-reason">{reason}</div>
                          )}
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

            <div className={`auth-field ${secretLocked ? "is-locked" : ""}`}>
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
                  This application type (SPA/Mobile/Desktop) cannot securely store secrets.
                  Client secrets and expiry are disabled for public clients.
                  <div className="auth-hint">
                    To enable client secrets, select WebApp or Backend application type.
                  </div>
                </div>
              )}
            </div>

            <div className={`auth-field ${secretLocked ? "is-locked" : ""}`}>
              <label className="form-label fw-semibold">Client Secret Expiry</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className={`fa ${secretLocked ? "fa-lock" : "fa-calendar"}`}></i>
                </span>
                <input
                  className="form-control"
                  type="date"
                  disabled={secretLocked}
                  {...register("clientSecretExpiry")}
                  aria-label="Client secret expiry"
                />
              </div>
            </div>

            <div className="auth-divider"></div>

            <div className="auth-field">
              <label className="form-label fw-semibold">Authentication Policy</label>
              <div className="row g-2">
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="allow-local-login-override"
                      {...register("authPolicy.allowLocalLoginOverride")}
                    />
                    <label className="form-check-label" htmlFor="allow-local-login-override">
                      Allow Local Login Override
                    </label>
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="allow-self-registration-override"
                      {...register("authPolicy.allowSelfRegistrationOverride")}
                    />
                    <label className="form-check-label" htmlFor="allow-self-registration-override">
                      Allow Self Registration Override
                    </label>
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="mfa-policy-override"
                      {...register("authPolicy.mfaPolicyOverride")}
                    />
                    <label className="form-check-label" htmlFor="mfa-policy-override">
                      Enforce MFA Override
                    </label>
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="show-stay-signed-in"
                      {...register("authPolicy.showStaySignedIn")}
                    />
                    <label className="form-check-label" htmlFor="show-stay-signed-in">
                      Show Stay Signed In
                    </label>
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="show-create-account-link"
                      {...register("authPolicy.showCreateAccountLink")}
                    />
                    <label className="form-check-label" htmlFor="show-create-account-link">
                      Show Create Account Link
                    </label>
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="show-external-providers"
                      {...register("authPolicy.showExternalProviders")}
                    />
                    <label className="form-check-label" htmlFor="show-external-providers">
                      Show External Providers
                    </label>
                  </div>
                </div>
                <div className="col-12">
                  <div className="auth-divider my-2"></div>
                </div>
                <div className="col-12 col-md-6">
                  <div className="form-check">
                    <input
                      className="form-check-input"
                      type="checkbox"
                      id="client-auto-create-users"
                      checked={isAutoCreateUsersEnabled}
                      onChange={(event) => {
                        setValue("authPolicy.autoCreateUsers", event.target.checked, {
                          shouldDirty: true,
                          shouldValidate: true,
                        });

                        if (!event.target.checked) {
                          setValue("authPolicy.defaultRoleId", "", {
                            shouldDirty: true,
                            shouldValidate: true,
                          });
                        }
                      }}
                    />
                    <label className="form-check-label" htmlFor="client-auto-create-users">
                      Auto Create Users
                    </label>
                  </div>
                  <div className="form-text text-muted">
                    Applies to new-user provisioning flows such as external login and
                    self-registration.
                  </div>
                </div>
                <div className="col-12 col-md-6">
                  <label className="form-label" htmlFor="client-default-role">
                    Default Role For New Users
                  </label>
                  <select
                    className="form-select"
                    id="client-default-role"
                    value={hasDefaultRole ? String(parsedDefaultRoleId) : ""}
                    disabled={!isAutoCreateUsersEnabled || !externalRoleOptions.length}
                    onChange={(event) =>
                      setValue("authPolicy.defaultRoleId", event.target.value || "", {
                        shouldDirty: true,
                        shouldValidate: true,
                      })
                    }
                  >
                    <option value="">Select default role</option>
                    {externalRoleOptions.map((role) => (
                      <option key={role.value} value={role.value}>
                        {role.label}
                      </option>
                    ))}
                  </select>
                  {!externalRoleOptions.length && (
                    <div className="form-text text-muted">
                      No new-user-assignable roles are available.
                    </div>
                  )}
                  {showRoleSelectionWarning && (
                    <div className="form-text text-danger">
                      Default role is required when auto-create users is enabled.
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="auth-field">
              <label className="form-label fw-semibold">External Providers</label>
              <div className="row g-3">
                {externalProviderOptions.map((option) => (
                  <div className="col-12 col-sm-6" key={option.value}>
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        value={option.value}
                        id={`client-provider-${option.value}`}
                        disabled={!showExternalProviders}
                        {...register("externalProviders")}
                      />
                      <label
                        className="form-check-label"
                        htmlFor={`client-provider-${option.value}`}
                      >
                        {option.label}
                      </label>
                    </div>
                  </div>
                ))}
              </div>
              {!externalProviderOptions.length && (
                <div className="form-text text-muted">
                  No tenant external providers configured.
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default AuthStep;
