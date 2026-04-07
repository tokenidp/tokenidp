import React, { useMemo } from "react";
import { GrantTypeId } from "../wizardState";

function TokensStep({
  register,
  errors,
  tokenType,
  setTokenType,
  tokenTypeOptions,
  setValue,
  clearErrors,
  grantTypes,
}) {
  const hasAuthCode = useMemo(
    () => grantTypes.includes(GrantTypeId.AuthorizationCode),
    [grantTypes]
  );
  const hasRefreshToken = useMemo(
    () => grantTypes.includes(GrantTypeId.RefreshToken),
    [grantTypes]
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
    [tokenTypeOptions]
  );

  const showReferenceWarning = useMemo(() => {
    const selected = normalizedTokenOptions.find(
      (option) => String(option.value) === String(tokenType)
    );
    return selected && selected.label === "Reference Token";
  }, [normalizedTokenOptions, tokenType]);

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell wizard-step-shell-spaced">
          <h6 className="wizard-step-title">Token Settings</h6>
          <div className="token-section">
            <label className="form-label fw-semibold">Token Type</label>
            <div className="row g-2">
              {normalizedTokenOptions.map((option) => (
                <div className="col-12 col-lg-6" key={option.value}>
                  <div
                    className={`option-card d-flex align-items-start gap-2 ${
                      tokenType === option.value ? "option-card-active" : ""
                    }`}
                  >
                    <input
                      className={`form-check-input mt-1${
                        errors.tokenType ? " is-invalid" : ""
                      }`}
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
            {errors.tokenType && (
              <div className="error-msg">Token type is required.</div>
            )}
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
                    className={`form-control${
                      errors.accessTokenLifetime ? " is-invalid" : ""
                    }`}
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
                <div className="form-text">
                  Typical values: 5-60 minutes.
                </div>
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
                  <div className="error-msg">
                    Authorization code lifetime is required.
                  </div>
                )}
                <div className="form-text">
                  Recommended 10 minutes or less.
                </div>
              </div>
              <div className="col-12 col-lg-4">
                <label className="form-label">Refresh Token</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-sync"></i>
                  </span>
                  <input
                    className={`form-control${
                      errors.refreshTokenExpiration ? " is-invalid" : ""
                    }`}
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
                <div className="form-text">
                  Longer-lived refresh tokens increase risk.
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default TokensStep;
