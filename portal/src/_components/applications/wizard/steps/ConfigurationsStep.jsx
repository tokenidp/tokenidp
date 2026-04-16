import React, { useMemo } from "react";
import ProviderIcon from "../../../common/providerIcon";
import { GrantTypeId } from "../wizardState";

function ConfigurationsStep({
  register,
  watch,
  setValue,
  errors,
  tokenType,
  setTokenType,
  tokenTypeOptions,
  clearErrors,
  grantTypes,
  externalProviderOptions,
  externalRoleOptions = [],
}) {
  const hasAuthCode = useMemo(
    () => grantTypes.includes(GrantTypeId.AuthorizationCode),
    [grantTypes],
  );
  const hasRefreshToken = useMemo(
    () => grantTypes.includes(GrantTypeId.RefreshToken),
    [grantTypes],
  );

  const normalizedTokenOptions = useMemo(
    () =>
      tokenTypeOptions.map((option) => {
        if (String(option.label).toLowerCase() === "jwt") {
          return {
            ...option,
            label: "JWT (JSON Web Token)",
            helper:
              "Self-contained tokens. Best for APIs and microservices. No introspection required.",
          };
        }
        return {
          ...option,
          label: "Reference Token",
          helper:
            "Opaque tokens. Requires introspection on each API call. Recommended for high-security or revocable tokens.",
        };
      }),
    [tokenTypeOptions],
  );

  const showReferenceWarning = useMemo(() => {
    const selected = normalizedTokenOptions.find(
      (option) => String(option.value) === String(tokenType),
    );
    return selected && selected.label === "Reference Token";
  }, [normalizedTokenOptions, tokenType]);

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
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title">Configurations</h6>
          <div className="wizard-info-banner" role="status">
            Token behavior, sign-in experience, and external identity provider settings
            should align with your client's security and onboarding requirements.
          </div>

          <div className="token-section">
            <label className="form-label fw-semibold">Token Settings</label>
            <div className="row g-2">
              {normalizedTokenOptions.map((option) => (
                <div className="col-12 col-lg-6" key={option.value}>
                  <div
                    className={`option-card d-flex align-items-start gap-2 ${
                      tokenType === option.value ? "option-card-active" : ""
                    }`}
                  >
                    <input
                      className={`form-check-input mt-1${errors.tokenType ? " is-invalid" : ""}`}
                      type="radio"
                      name="tokenType"
                      id={`tokenType-${option.value}`}
                      value={option.value}
                      checked={tokenType === option.value}
                      {...register("tokenType", { required: true })}
                      onChange={(event) => {
                        setTokenType(event.target.value);
                        setValue("tokenType", event.target.value, {
                          shouldValidate: true,
                          shouldDirty: true,
                          shouldTouch: true,
                        });
                        clearErrors("tokenType");
                      }}
                    />
                    <label
                      className="form-check-label w-100"
                      htmlFor={`tokenType-${option.value}`}
                    >
                      <div className="token-title">{option.label}</div>
                      <div className="token-helper">{option.helper}</div>
                    </label>
                  </div>
                </div>
              ))}
            </div>
            {showReferenceWarning && (
              <div className="alert alert-warning mt-3 mb-0" role="alert">
                Reference tokens require your APIs to call the introspection endpoint on
                each request and may impact performance.
              </div>
            )}
            {errors.tokenType && <div className="error-msg">Token type is required.</div>}
          </div>

          <div className="token-section token-section-compact">
            <label className="form-label fw-semibold">Lifetime Settings</label>
            <div className="row g-3">
              <div className="col-12 col-lg-4">
                <label className="form-label">Access Token</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-clock"></i>
                  </span>
                  <input
                    className={`form-control${errors.accessTokenLifetime ? " is-invalid" : ""}`}
                    type="number"
                    min="1"
                    {...register("accessTokenLifetime", {
                      required: true,
                      valueAsNumber: true,
                    })}
                  />
                  <span className="input-group-text">Minutes</span>
                </div>
                {errors.accessTokenLifetime && (
                  <div className="error-msg">Access token lifetime is required.</div>
                )}
                <div className="form-text">Typical values: 5-60 minutes.</div>
              </div>
              <div className="col-12 col-lg-4">
                <label className="form-label">Authorization Code</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-hourglass-half"></i>
                  </span>
                  <input
                    className={`form-control${
                      errors.authorizationCodeLifetime ? " is-invalid" : ""
                    }`}
                    type="number"
                    min="1"
                    disabled={!hasAuthCode}
                    {...register("authorizationCodeLifetime", {
                      required: hasAuthCode,
                      valueAsNumber: true,
                    })}
                  />
                  <span className="input-group-text">Minutes</span>
                </div>
                {!hasAuthCode && (
                  <div className="form-text text-muted">
                    Applies only when authorization_code is enabled.
                  </div>
                )}
                {hasAuthCode && errors.authorizationCodeLifetime && (
                  <div className="error-msg">Authorization code lifetime is required.</div>
                )}
                <div className="form-text">Recommended 10 minutes or less.</div>
              </div>
              <div className="col-12 col-lg-4">
                <label className="form-label">Refresh Token</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-sync"></i>
                  </span>
                  <input
                    className={`form-control${errors.refreshTokenExpiration ? " is-invalid" : ""}`}
                    type="number"
                    min="1"
                    disabled={!hasRefreshToken}
                    {...register("refreshTokenExpiration", {
                      required: hasRefreshToken,
                      valueAsNumber: true,
                      max: {
                        value: 30,
                        message: "Refresh token expiration cannot exceed 30 days.",
                      },
                    })}
                  />
                  <span className="input-group-text">Days</span>
                </div>
                {!hasRefreshToken && (
                  <div className="form-text text-muted">
                    Applies only when refresh tokens are enabled.
                  </div>
                )}
                {hasRefreshToken && errors.refreshTokenExpiration && (
                  <div className="error-msg">
                    {errors.refreshTokenExpiration.message ||
                      "Refresh token expiration is required."}
                  </div>
                )}
                <div className="form-text">Longer-lived refresh tokens increase risk.</div>
              </div>
            </div>
          </div>

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

          <div className="auth-field mb-0">
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
                      className="form-check-label provider-option-label d-inline-flex align-items-center gap-2"
                      htmlFor={`client-provider-${option.value}`}
                    >
                      <ProviderIcon label={option.label} />
                      <span>{option.label}</span>
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
  );
}

export default ConfigurationsStep;
