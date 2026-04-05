import React from "react";

function BasicInfoStep({
  register,
  errors,
  appType,
  setAppType,
  appTypeOptions,
  setValue,
  clearErrors,
  onCopyClientId,
  clientIdValue,
  isActive,
  setIsActive,
}) {
  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-lg-8 col-xl-7">
        <div className="card form-section-card">
          <div className="card-body">
            <h6 className="card-title">Basic Information</h6>
            <div className="mb-3">
              <label className="form-label fw-semibold">Client Name *</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-id-badge"></i>
                </span>
                <input
                  className={`form-control${errors.clientName ? " is-invalid" : ""}`}
                  type="text"
                  placeholder="Acme Web Portal"
                  {...register("clientName", { required: true })}
                />
              </div>
              <input
                type="hidden"
                value={isActive ? "true" : "false"}
                {...register("isActive")}
              />
              {errors.clientName && (
                <div className="error-msg">Client name is required.</div>
              )}
            </div>
            <div className="mb-3">
              <label className="form-label fw-semibold">Client ID</label>
              <input type="hidden" value={clientIdValue || ""} {...register("clientId")} />
              <div className="token-field">
                <span className="token-icon" aria-hidden="true">
                  <i className="fa fa-fingerprint"></i>
                </span>
                <div
                  className="token-value"
                  role="textbox"
                  aria-readonly="true"
                  aria-label="Client ID"
                >
                  {clientIdValue || "Client ID will be generated after saving."}
                </div>
                <button
                  className="btn btn-outline-secondary btn-sm"
                  type="button"
                  onClick={onCopyClientId}
                  disabled={!clientIdValue}
                >
                  Copy
                </button>
              </div>
              <div className="form-text">Use this identifier in OAuth flows.</div>
            </div>
            <div className="mb-3">
              <label className="form-label fw-semibold">Application Type *</label>
              <div className="row g-3">
                {appTypeOptions.map((option) => (
                  <div className="col-12" key={option.value}>
                    <div
                      className={`option-card d-flex align-items-center gap-3 ${
                        appType === option.value ? "option-card-active" : ""
                      }`}
                    >
                      <input
                        className={`form-check-input mt-0${
                          errors.appType ? " is-invalid" : ""
                        }`}
                        type="radio"
                        name="appType"
                        value={option.value}
                        id={`appType-${option.value}`}
                        checked={appType === option.value}
                        {...register("appType", { required: true })}
                        onChange={() => {
                          setAppType(option.value);
                          setValue("appType", option.value, {
                            shouldValidate: true,
                            shouldDirty: true,
                            shouldTouch: true,
                          });
                          clearErrors("appType");
                        }}
                      />
                      <label
                        className="form-check-label w-100"
                        htmlFor={`appType-${option.value}`}
                      >
                        <div className="d-flex align-items-center gap-2">
                          <i className={`${option.icon} text-secondary`}></i>
                          <span className="fw-semibold">{option.label}</span>
                        </div>
                        {option.helper && (
                          <div className="option-helper text-muted small mt-1">
                            {option.helper}
                          </div>
                        )}
                      </label>
                    </div>
                  </div>
                ))}
              </div>
              <div className="form-text mt-2">
                App type controls grant availability and token constraints.
              </div>
              {errors.appType && (
                <div className="error-msg">App type is required.</div>
              )}
            </div>
            <div className="mb-0">
              <label className="form-label fw-semibold">Description</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-align-left"></i>
                </span>
                <textarea
                  className={`form-control${errors.description ? " is-invalid" : ""}`}
                  rows="3"
                  placeholder="Describe the purpose of this application (e.g., 'Customer portal - Prod')."
                  {...register("description")}
                ></textarea>
              </div>
            </div>
            <div className="form-check form-switch app-switch mt-3 basic-info-active">
              <input
                className="form-check-input app-switch-input"
                type="checkbox"
                checked={isActive}
                onChange={(event) => {
                  setIsActive(event.target.checked);
                  setValue("isActive", event.target.checked);
                }}
              />
              <label className="form-check-label">Active</label>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default BasicInfoStep;
