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
  apiResourceOptions,
  externalProviderOptions,
  externalRoleOptions,
  grantOptions,
  scopes,
  selectedApiResources,
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

  const apiScopeLabelMap = Object.fromEntries(
    (Array.isArray(apiResourceOptions) ? apiResourceOptions : []).flatMap((resource) =>
      (resource?.scopes ?? resource?.Scopes ?? []).map((scope) => [
        String(scope?.name ?? scope?.Name ?? ""),
        String(scope?.displayName ?? scope?.DisplayName ?? scope?.name ?? scope?.Name ?? ""),
      ]),
    ),
  );

  const scopeLabels = scopes.map((scopeName) => {
    const systemScope = scopeOptions.find((scope) => scope.value === scopeName);
    if (systemScope) {
      return {
        label: scopeLabelMap[systemScope.label] || systemScope.label,
        raw: scopeName,
      };
    }

    return {
      label: apiScopeLabelMap[scopeName] || scopeName,
      raw: scopeName,
    };
  });

  const apiResourceLabelMap = Object.fromEntries(
    (Array.isArray(apiResourceOptions) ? apiResourceOptions : []).map((resource) => [
      String(resource?.name ?? resource?.Name ?? ""),
      String(resource?.displayName ?? resource?.DisplayName ?? resource?.name ?? resource?.Name ?? ""),
    ]),
  );

  const externalProviderLabelMap = Object.fromEntries(
    (Array.isArray(externalProviderOptions) ? externalProviderOptions : []).map((provider) => [
      String(provider.value),
      String(provider.label),
    ]),
  );

  const roleLabelMap = Object.fromEntries(
    (Array.isArray(externalRoleOptions) ? externalRoleOptions : []).map((role) => [
      String(role.value),
      String(role.label),
    ]),
  );

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

  const authPolicy = values.authPolicy || {};
  const enabledPolicyLabels = [
    authPolicy.allowLocalLoginOverride ? "Allow Local Login Override" : null,
    authPolicy.allowSelfRegistrationOverride ? "Allow Self Registration Override" : null,
    authPolicy.mfaPolicyOverride ? "Enforce MFA Override" : null,
    authPolicy.showStaySignedIn ? "Show Stay Signed In" : null,
    authPolicy.showCreateAccountLink ? "Show Create Account Link" : null,
    authPolicy.showExternalProviders ? "Show External Providers" : null,
    authPolicy.autoCreateUsers ? "Auto Create Users" : null,
  ].filter(Boolean);

  const handleEdit = (stepId) => {
    const nextIndex = stepIndexById?.[stepId];
    if (nextIndex !== undefined) {
      onEditStep?.(nextIndex);
    }
  };

  const configuredClientSecret = values.clientSecret ? "Configured" : "Not configured";
  const defaultRoleValue = authPolicy.defaultRoleId ? String(authPolicy.defaultRoleId) : "";
  const selectedProviderValues = Array.isArray(values.externalProviders)
    ? values.externalProviders
    : [];

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title">Review Summary</h6>

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
              <div className="token-title">Authentication</div>
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
              <div>
                <div className="text-muted small">Client Secret</div>
                <div>{configuredClientSecret}</div>
              </div>
              <div>
                <div className="text-muted small">Client Secret Expiry</div>
                <div>{values.clientSecretExpiry || "--"}</div>
              </div>
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
              <div className="token-title">Scopes & Api Resources</div>
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
                <div className="text-muted small">Assigned Api Resources</div>
                <div>
                  {selectedApiResources?.length
                    ? selectedApiResources.map((resourceName) => (
                        <div key={resourceName}>
                          {apiResourceLabelMap[resourceName] || resourceName}
                          <span className="text-muted small ms-2">{resourceName}</span>
                        </div>
                      ))
                    : "--"}
                </div>
              </div>
            </div>
            {offlineAccessWarning && (
              <div className="alert alert-warning mt-2 mb-0">{offlineAccessWarning}</div>
            )}
            <div className="alert alert-info mt-2 mb-0">
              Access token audience is derived from the requested API scopes at runtime.
            </div>
          </div>

          <div className="review-section">
            <div className="review-header">
              <div className="token-title">Configurations</div>
              <button
                type="button"
                className="btn btn-link p-0"
                onClick={() => handleEdit("configurations")}
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
                  {values.accessTokenLifetime ? `${values.accessTokenLifetime} minutes` : "--"}
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
              <div>
                <div className="text-muted small">Authentication Policy</div>
                <div>
                  {enabledPolicyLabels.length
                    ? enabledPolicyLabels.map((label) => <div key={label}>{label}</div>)
                    : "--"}
                </div>
              </div>
              <div>
                <div className="text-muted small">Default Role For New Users</div>
                <div>
                  {defaultRoleValue ? roleLabelMap[defaultRoleValue] || defaultRoleValue : "--"}
                </div>
              </div>
              <div>
                <div className="text-muted small">External Providers</div>
                <div>
                  {selectedProviderValues.length
                    ? selectedProviderValues.map((providerValue) => (
                        <div key={providerValue}>
                          {externalProviderLabelMap[String(providerValue)] || providerValue}
                        </div>
                      ))
                    : "--"}
                </div>
              </div>
            </div>
            {refreshTokenWarning && (
              <div className="alert alert-warning mt-2 mb-0">{refreshTokenWarning}</div>
            )}
          </div>

          <div className="review-section">
            <div className="review-header">
              <div className="token-title">Rate Limit & Tracking</div>
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
              I have reviewed authentication, scopes, and configuration settings for
              security.
            </label>
          </div>
        </div>
      </div>
    </div>
  );
}

ReviewStep.defaultProps = {
  externalProviderOptions: [],
  externalRoleOptions: [],
};

export default ReviewStep;
