import React, { useMemo, useState } from "react";

const formatList = (value) => {
  if (!value) return "--";
  if (Array.isArray(value)) {
    return value.length ? value.join(", ") : "--";
  }
  const trimmed = String(value).trim();
  return trimmed || "--";
};

const getOptionLabel = (options, value) => {
  const match = options.find((option) => String(option.value) === String(value));
  return match?.label ?? "--";
};

function ReviewStep({
  values,
  appTypeOptions,
  tokenTypeOptions,
  grantTypes,
  scopeOptions,
  grantOptions,
  scopes,
  onEditStep,
  stepIndexById,
}) {
  const [confirmed, setConfirmed] = useState(false);

  const scopeLabelMap = {
    openid: "OpenID",
    profile: "Profile",
    email: "Email",
    offline_access: "Offline Access",
  };

  const grantLabels = grantOptions
    .filter((grant) => grantTypes.includes(grant.id))
    .map((grant) => ({
      label: grant.value,
      raw: grant.key,
    }));

  const scopeLabels = scopeOptions
    .filter((scope) => scopes.includes(scope.value))
    .map((scope) => ({
      label: scopeLabelMap[scope.label] || scope.label,
      raw: scope.value,
    }));

  const redirectWarnings = useMemo(() => {
    const warnings = [];
    const redirects = String(values.redirectUri || "")
      .split(/\r?\n/)
      .map((line) => line.trim())
      .filter(Boolean);
    const hasInsecure = redirects.some((uri) => {
      try {
        const parsed = new URL(uri);
        return parsed.protocol === "http:" && parsed.hostname !== "localhost";
      } catch (error) {
        return false;
      }
    });
    if (hasInsecure) {
      warnings.push("Insecure redirect URI detected. Use HTTPS in production.");
    }
    return warnings;
  }, [values.redirectUri]);

  const offlineAccessWarning = scopes.includes("offline_access")
    ? "offline_access allows long-lived access. Ensure this is required."
    : null;

  const refreshTokenWarning =
    values.refreshTokenExpiration && Number(values.refreshTokenExpiration) > 30
      ? "Long-lived refresh tokens increase risk if compromised."
      : null;

  const statusLabel = values.isActive ? "Active" : "Draft";
  const statusClass = values.isActive ? "status-pill-success" : "status-pill-warning";
  const statusHelp = values.isActive
    ? "This client will become live immediately after saving."
    : "This client will not be usable until activated.";

  const handleEdit = (stepId) => {
    const nextIndex = stepIndexById?.[stepId];
    if (nextIndex !== undefined) {
      onEditStep?.(nextIndex);
    }
  };

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-lg-8 col-xl-7">
        <div className="card">
          <div className="card-body">
            <h6 className="card-title">Review Summary</h6>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Basic Information</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("basicInfo")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Client Name</div>
                  <div>{values.clientName || "--"}</div>
                </div>
                <div>
                  <div className="text-muted small">Client ID</div>
                  <div>{values.clientId || "--"}</div>
                </div>
                <div>
                  <div className="text-muted small">Application Type</div>
                  <div>{getOptionLabel(appTypeOptions, values.appType)}</div>
                </div>
                <div>
                  <div className="text-muted small">Description</div>
                  <div>{values.description || "--"}</div>
                </div>
              </div>
            </div>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Redirect & Logout URLs</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("redirects")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Redirect URIs</div>
                  <div>{formatList(values.redirectUri)}</div>
                </div>
                <div>
                  <div className="text-muted small">Post-Logout Redirect URIs</div>
                  <div>{formatList(values.logoutRedirectUri)}</div>
                </div>
              </div>
              {redirectWarnings.map((warning) => (
                <div className="alert alert-warning mt-2 mb-0" key={warning}>
                  {warning}
                </div>
              ))}
            </div>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Authentication & Grants</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("auth")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Grant Types</div>
                  <div>
                    {grantLabels.length
                      ? grantLabels.map((grant) => (
                          <div key={grant.raw}>
                            {grant.label}
                            <span className="text-muted small ms-2">{grant.raw}</span>
                          </div>
                        ))
                      : "--"}
                  </div>
                </div>
              </div>
            </div>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Token Settings</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("tokens")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Token Type</div>
                  <div>{getOptionLabel(tokenTypeOptions, values.tokenType)}</div>
                </div>
                <div>
                  <div className="text-muted small">Access Token Lifetime</div>
                  <div>
                    {values.accessTokenLifetime
                      ? `${values.accessTokenLifetime} minutes`
                      : "--"}
                  </div>
                </div>
                <div>
                  <div className="text-muted small">Authorization Code Lifetime</div>
                  <div>
                    {values.authorizationCodeLifetime
                      ? `${values.authorizationCodeLifetime} minutes`
                      : "--"}
                  </div>
                </div>
                <div>
                  <div className="text-muted small">Refresh Token Expiration</div>
                  <div>
                    {values.refreshTokenExpiration
                      ? `${values.refreshTokenExpiration} days`
                      : "--"}
                  </div>
                </div>
              </div>
              {refreshTokenWarning && (
                <div className="alert alert-warning mt-2 mb-0">
                  {refreshTokenWarning}
                </div>
              )}
            </div>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Scopes & Audience</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("scopes")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Scopes</div>
                  <div>
                    {scopeLabels.length
                      ? scopeLabels.map((scope) => (
                          <div key={scope.raw}>
                            {scope.label}
                            <span className="text-muted small ms-2">{scope.raw}</span>
                          </div>
                        ))
                      : "--"}
                  </div>
                </div>
                <div>
                  <div className="text-muted small">Audience</div>
                  <div>{values.clientAudience || "--"}</div>
                </div>
              </div>
              {offlineAccessWarning && (
                <div className="alert alert-warning mt-2 mb-0">
                  {offlineAccessWarning}
                </div>
              )}
            </div>

            <div className="review-section">
              <div className="review-header">
                <div className="token-title">Rate Limits & Tracking</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("protection")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <div className="text-muted small">Rate Limits</div>
                  <div>
                    Permit: {values.permitLimit || "--"} / Queue: {values.queueLimit || "--"}
                  </div>
                </div>
                <div>
                  <div className="text-muted small">Time Window</div>
                  <div>{values.timeWindow || "--"}</div>
                </div>
                <div>
                  <div className="text-muted small">Interaction Tracking</div>
                  <div>{values.enableITracking ? "Enabled" : "Disabled"}</div>
                </div>
              </div>
            </div>

            <div className="review-section mb-0">
              <div className="review-header">
                <div className="token-title">Status</div>
                <button
                  type="button"
                  className="btn btn-link p-0"
                  onClick={() => handleEdit("basicInfo")}
                >
                  Edit
                </button>
              </div>
              <div className="review-grid">
                <div>
                  <span className={`status-pill ${statusClass}`}>{statusLabel}</span>
                  <div className="form-text">{statusHelp}</div>
                </div>
              </div>
            </div>

            <div className="form-check mt-4">
              <input
                className="form-check-input"
                type="checkbox"
                id="reviewConfirm"
                checked={confirmed}
                onChange={(event) => setConfirmed(event.target.checked)}
              />
              <label className="form-check-label" htmlFor="reviewConfirm">
                I have reviewed redirect URIs, grants, and scopes for security.
              </label>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ReviewStep;
