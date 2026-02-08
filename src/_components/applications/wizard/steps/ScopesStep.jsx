import React, { useMemo } from "react";

function ScopesStep({
  scopeOptions,
  scopes,
  toggleScope,
  clientAudience,
  setClientAudience,
  setValue,
  register,
  grantTypes,
}) {
  const hasClientCredentials = useMemo(() => grantTypes.includes(2), [grantTypes]);
  const hasRefreshToken = useMemo(() => grantTypes.includes(1), [grantTypes]);

  const scopeDescriptions = {
    openid: "Required for OpenID Connect login (ID Token issuance).",
    profile: "Access to basic user profile claims (name, picture, etc.).",
    email: "Access to user email address.",
    offline_access: "Allows issuing refresh tokens for long-lived access.",
  };

  const isScopeDisabled = (scope) => {
    if (hasClientCredentials) {
      return ["openid", "profile", "email", "offline_access"].includes(scope);
    }
    if (scope === "offline_access" && !hasRefreshToken) {
      return true;
    }
    return false;
  };

  const getScopeReason = (scope) => {
    if (hasClientCredentials) {
      return "User identity scopes are not applicable to machine-to-machine (client_credentials) flows.";
    }
    if (scope === "offline_access" && !hasRefreshToken) {
      return "offline_access is only applicable when refresh tokens are enabled.";
    }
    return null;
  };

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-lg-8 col-xl-7">
        <div className="card">
          <div className="card-body">
            <h6 className="card-title">Scopes &amp; Permissions</h6>
            <div className="wizard-info-banner" role="status">
              Grant only the minimum scopes required. Over-privileged clients increase
              security risk.
            </div>

            <div className="token-section">
              <label className="form-label fw-semibold">Client Scopes</label>
              <div className="row g-3">
                {scopeOptions.map((scope) => {
                  const disabled = isScopeDisabled(scope.value);
                  const reason = getScopeReason(scope.value);
                  return (
                    <div className="col-12" key={scope.value}>
                      <div
                        className={`option-card d-flex align-items-start gap-3 ${
                          scopes.includes(scope.value) ? "option-card-active" : ""
                        } ${disabled ? "is-locked" : ""}`}
                      >
                        <input
                          className="form-check-input mt-1"
                          type="checkbox"
                          id={`scope-${scope.value}`}
                          checked={scopes.includes(scope.value)}
                          onChange={() => toggleScope(scope.value)}
                          disabled={disabled}
                          aria-label={`${scope.label} scope`}
                        />
                        <label
                          className="form-check-label w-100"
                          htmlFor={`scope-${scope.value}`}
                        >
                          <div className="token-title">{scope.label}</div>
                          <div className="token-helper">
                            {scopeDescriptions[scope.value] || "Custom API scope."}
                          </div>
                          {disabled && reason && (
                            <div className="grant-reason">{reason}</div>
                          )}
                        </label>
                      </div>
                    </div>
                  );
                })}
              </div>
              <div className="form-text mt-2">
                Select scopes that the client can request.
              </div>
            </div>

            <div className="auth-divider"></div>

            <div className="token-section mb-0">
              <div className="token-title mb-2">Client Audience</div>
              <label className="form-label fw-semibold">Audience</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-bullseye"></i>
                </span>
                <input
                  className="form-control"
                  type="text"
                  placeholder="https://api.company.com"
                  value={clientAudience}
                  {...register("clientAudience")}
                  onChange={(event) => {
                    setClientAudience(event.target.value);
                    setValue("clientAudience", event.target.value);
                  }}
                  aria-label="Audience"
                />
              </div>
              <div className="form-text">
                Audience identifies the target API (aud claim in access tokens). It
                must exactly match the API resource identifier configured in your
                system.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ScopesStep;
