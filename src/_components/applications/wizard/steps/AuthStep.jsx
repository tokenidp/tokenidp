import React from "react";

function AuthStep({
  register,
  isPublicClient,
  showSecret,
  setShowSecret,
  onRegenerateSecret,
  grantTypes,
  toggleGrant,
  fallbackGrantTypes,
  allowedGrants,
  hasInsecureGrant,
  grantError,
  appType,
}) {
  const grantMeta = {
    0: { label: "Authorization Code", sublabel: "authorization_code" },
    1: { label: "Refresh Token", sublabel: "refresh_token" },
    2: { label: "Client Credentials", sublabel: "client_credentials" },
  };

  const isBackendApp = appType === "4";
  const hasAuthCode = grantTypes.includes(0);
  const secretLocked = isPublicClient;

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
              <label className="form-label fw-semibold">OAuth Grants</label>
              <div className="row g-2">
                {fallbackGrantTypes.map((grant) => {
                  const meta = grantMeta[grant.value];
                  const isClientCredentials = grant.value === 2;
                  const isRefreshToken = grant.value === 1;
                  const disabledByAppType =
                    isClientCredentials ? !isBackendApp : !allowedGrants.has(grant.value);
                  const disabledByDependency = isRefreshToken && !hasAuthCode;
                  const disabled = disabledByAppType || disabledByDependency;
                  const reason = isClientCredentials
                    ? "Available only for Backend (machine-to-machine) applications."
                    : isRefreshToken && !hasAuthCode
                      ? "Enable Authorization Code to use refresh_token."
                      : null;
                  return (
                    <div className="col-12" key={grant.value}>
                      <div
                        className={`option-card auth-grant-card d-flex align-items-start gap-2 ${
                          grantTypes.includes(grant.value) ? "option-card-active" : ""
                        } ${disabled ? "is-locked" : ""}`}
                      >
                        <input
                          className="form-check-input mt-1"
                          type="checkbox"
                          id={`grant-${grant.value}`}
                          checked={grantTypes.includes(grant.value)}
                          onChange={() => toggleGrant(grant.value)}
                          disabled={disabled}
                          aria-label={`${meta.label} grant`}
                        />
                        <label
                          className="form-check-label w-100"
                          htmlFor={`grant-${grant.value}`}
                        >
                          <div className="grant-title">{meta.label}</div>
                          <div className="grant-sublabel">{meta.sublabel}</div>
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
              {isPublicClient && (
                <div className="form-text text-muted">
                  Public clients must use PKCE with Authorization Code flow.
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
          </div>
        </div>
      </div>
    </div>
  );
}

export default AuthStep;
