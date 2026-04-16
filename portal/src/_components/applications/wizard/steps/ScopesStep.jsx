import React, { useEffect, useMemo, useState } from "react";
import { GrantTypeId } from "../wizardState";

function ScopesStep({
  scopeOptions,
  apiResourceOptions,
  scopes,
  toggleScope,
  selectedApiResources,
  setSelectedApiResources,
  setValue,
  grantTypes,
}) {
  const hasClientCredentials = useMemo(
    () => grantTypes.includes(GrantTypeId.ClientCredentials),
    [grantTypes],
  );
  const hasRefreshToken = useMemo(
    () => grantTypes.includes(GrantTypeId.RefreshToken),
    [grantTypes],
  );

  const scopeDescriptions = {
    openid: "Required for OpenID Connect login (ID Token issuance).",
    profile: "Access to basic user profile claims (name, picture, etc.).",
    email: "Access to user email address.",
    offline_access: "Allows issuing refresh tokens for long-lived access.",
  };

  const normalizedApiResources = useMemo(
    () =>
      (Array.isArray(apiResourceOptions) ? apiResourceOptions : []).map((resource) => ({
        id: resource?.id ?? resource?.Id ?? resource?.name ?? resource?.Name,
        name: String(resource?.name ?? resource?.Name ?? ""),
        displayName: String(
          resource?.displayName ??
            resource?.DisplayName ??
            resource?.name ??
            resource?.Name ??
            "",
        ),
        scopes: Array.isArray(resource?.scopes ?? resource?.Scopes)
          ? (resource?.scopes ?? resource?.Scopes).map((scope) => ({
              id: scope?.id ?? scope?.Id ?? scope?.name ?? scope?.Name,
              name: String(scope?.name ?? scope?.Name ?? ""),
              displayName: String(
                scope?.displayName ??
                  scope?.DisplayName ??
                  scope?.name ??
                  scope?.Name ??
                  "",
              ),
            }))
          : [],
      })),
    [apiResourceOptions],
  );

  const apiScopeNameSet = useMemo(
    () =>
      new Set(
        normalizedApiResources.flatMap((resource) =>
          resource.scopes
            .map((scope) => scope.name)
            .filter(Boolean),
        ),
      ),
    [normalizedApiResources],
  );

  const systemScopeOptions = useMemo(
    () =>
      (Array.isArray(scopeOptions) ? scopeOptions : []).filter(
        (scope) => !apiScopeNameSet.has(String(scope?.value ?? "")),
      ),
    [apiScopeNameSet, scopeOptions],
  );

  const selectedResourceSet = useMemo(
    () => new Set(selectedApiResources),
    [selectedApiResources],
  );

  const [activeApiResourceName, setActiveApiResourceName] = useState("");

  useEffect(() => {
    if (normalizedApiResources.length === 0) {
      if (activeApiResourceName) {
        setActiveApiResourceName("");
      }
      return;
    }

    const hasActiveResource = normalizedApiResources.some(
      (resource) => resource.name === activeApiResourceName,
    );
    if (hasActiveResource) {
      return;
    }

    const nextActiveResourceName =
      normalizedApiResources.find((resource) => selectedResourceSet.has(resource.name))
        ?.name ?? normalizedApiResources[0].name;

    if (nextActiveResourceName !== activeApiResourceName) {
      setActiveApiResourceName(nextActiveResourceName);
    }
  }, [activeApiResourceName, normalizedApiResources, selectedResourceSet]);

  const activeApiResource = useMemo(
    () =>
      normalizedApiResources.find((resource) => resource.name === activeApiResourceName) ??
      null,
    [activeApiResourceName, normalizedApiResources],
  );

  const isSystemScopeDisabled = (scope) => {
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

  const handleApiResourceSelection = (values) => {
    setSelectedApiResources(values);
    setValue("apiResources", values, {
      shouldDirty: true,
      shouldValidate: true,
    });
  };

  const handleApiResourceToggle = (resourceName) => {
    const values = selectedResourceSet.has(resourceName)
      ? selectedApiResources.filter((value) => value !== resourceName)
      : [...selectedApiResources, resourceName];

    setActiveApiResourceName(resourceName);
    handleApiResourceSelection(values);
  };

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title">Scopes &amp; Api Resources</h6>
          <div className="wizard-info-banner" role="status">
            Audiences are derived from requested API scopes. This IDP issues a
            single-audience token and rejects token requests that span multiple
            ApiResources.
          </div>

          <div className="token-section">
            <label className="form-label fw-semibold mb-3">System Scopes</label>
            {systemScopeOptions.length === 0 ? (
              <div className="scopes-resource-empty">
                No system scopes are available.
              </div>
            ) : (
              <div className="row g-3">
                <div className="col-12">
                  <div className="row g-2">
                    {systemScopeOptions.map((scope) => {
                      const disabled = isSystemScopeDisabled(scope.value);
                      const reason = getScopeReason(scope.value);

                      return (
                        <div className="col-12 col-md-6" key={scope.value}>
                          <div
                            className={`option-card d-flex align-items-start gap-3 h-100 ${
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
                                {scopeDescriptions[scope.value] || "System scope."}
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
                </div>
              </div>
            )}
          </div>

          <div className="token-section mb-0">
            <div className="d-flex flex-wrap align-items-center justify-content-between gap-2 mb-3">
              <label className="form-label fw-semibold mb-0">Api Resources</label>
              <div className="scopes-resource-count">
                {selectedApiResources.length} selected
              </div>
            </div>
            {normalizedApiResources.length === 0 ? (
              <div className="scopes-resource-empty">
                No API resources are available yet.
              </div>
            ) : (
              <>
                <div className="scopes-api-layout">
                  <div className="scopes-api-sidebar">
                    <div className="scopes-resource-stack" aria-label="Api Resources">
                      {normalizedApiResources.map((resource) => {
                        const assigned = selectedResourceSet.has(resource.name);
                        const isActive = resource.name === activeApiResourceName;
                        const resourceLabel = resource.displayName || resource.name;

                        return (
                          <div
                            key={resource.id}
                            className={`scopes-resource-card scopes-resource-card-interactive ${
                              assigned ? "is-selected" : ""
                            } ${isActive ? "is-focused" : ""}`}
                            role="button"
                            tabIndex={0}
                            onClick={() => setActiveApiResourceName(resource.name)}
                            onKeyDown={(event) => {
                              if (event.key === "Enter" || event.key === " ") {
                                event.preventDefault();
                                setActiveApiResourceName(resource.name);
                              }
                            }}
                            aria-current={isActive ? "true" : undefined}
                            aria-label={`View ${resourceLabel} scopes`}
                          >
                            <div className="scopes-resource-card-top">
                              <div className="scopes-resource-icon" aria-hidden="true">
                                <i className="fa fa-database" />
                              </div>
                              <button
                                type="button"
                                className={`scopes-resource-toggle ${
                                  assigned ? "is-selected" : ""
                                }`}
                                onClick={(event) => {
                                  event.stopPropagation();
                                  handleApiResourceToggle(resource.name);
                                }}
                                aria-pressed={assigned}
                              >
                                {assigned ? "Assigned" : "Assign"}
                              </button>
                            </div>
                            <div className="scopes-resource-title">{resourceLabel}</div>
                            <div className="scopes-resource-key">{resource.name}</div>
                            <div className="scopes-resource-meta">
                              <span>
                                <i className="fa fa-key" /> {resource.scopes.length} scopes
                              </span>
                              <span>
                                <i
                                  className={`fa ${
                                    isActive
                                      ? "fa-arrow-right"
                                      : assigned
                                        ? "fa-check-circle"
                                        : "fa-circle"
                                  }`}
                                />{" "}
                                {isActive
                                  ? "Viewing"
                                  : assigned
                                    ? "Included"
                                    : "Select to view"}
                              </span>
                            </div>
                          </div>
                        );
                      })}
                    </div>
                  </div>

                  <div className="scopes-api-detail">
                    {activeApiResource ? (
                      <>
                        <div className="scopes-api-detail-header">
                          <div>
                            <div className="token-title">
                              {activeApiResource.displayName || activeApiResource.name}
                            </div>
                            <div className="token-helper">{activeApiResource.name}</div>
                          </div>
                        </div>

                        {activeApiResource.scopes.length === 0 ? (
                          <div className="scopes-resource-empty mb-0">
                            No scopes configured for this API resource.
                          </div>
                        ) : (
                          <div className="row g-2">
                            {activeApiResource.scopes.map((scope) => {
                              const checked = scopes.includes(scope.name);

                              return (
                                <div className="col-12 col-md-6" key={scope.id}>
                                <div
                                  className={`option-card d-flex align-items-start gap-3 h-100 ${
                                    checked ? "option-card-active" : ""
                                    }`}
                                >
                                    <input
                                      className="form-check-input mt-1"
                                      type="checkbox"
                                      id={`api-scope-${scope.id}`}
                                      checked={checked}
                                      onChange={() => toggleScope(scope.name)}
                                      aria-label={`${scope.displayName} scope`}
                                    />
                                    <label
                                      className="form-check-label w-100"
                                      htmlFor={`api-scope-${scope.id}`}
                                    >
                                      <div className="token-title">
                                        {scope.displayName}
                                      </div>
                                      <div className="token-helper">{scope.name}</div>
                                    </label>
                                  </div>
                                </div>
                              );
                            })}
                          </div>
                        )}
                      </>
                    ) : (
                      <div className="scopes-resource-empty mb-0">
                        Select an API resource to review its scopes.
                      </div>
                    )}
                  </div>
                </div>

              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

export default ScopesStep;
