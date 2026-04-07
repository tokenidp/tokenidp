import React from "react";

function ProtectionStep({ register, errors, isValidTimeWindow }) {
  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title d-flex align-items-center gap-2">
            Rate Limits &amp; Tracking
            <span className="badge bg-warning text-dark preview-badge">Coming Soon</span>
          </h6>

          <div className="alert alert-warning mb-3" role="status">
            <strong>This feature is under development</strong>
            <div>
              Rate limits and interaction tracking are not enforced yet. Settings
              saved here will take effect in a future release.
            </div>
          </div>

          <div className="wizard-info-banner" role="status">
            Rate limits help protect your identity platform from abuse, credential
            stuffing, and denial-of-service attacks.
          </div>

          <div className="token-section">
            <div className="token-title mb-2">Rate Limits</div>
            <div className="row g-3">
              <div className="col-12 col-lg-4">
                <label className="form-label fw-semibold">Permit Limit</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-gauge-high"></i>
                  </span>
                  <input
                    className="form-control"
                    type="number"
                    min="0"
                    placeholder="100"
                    {...register("permitLimit")}
                  />
                </div>
                <div className="form-text">
                  Maximum number of requests allowed within the time window.
                </div>
                <div className="form-text text-warning">
                  These settings are currently not active.
                </div>
              </div>

              <div className="col-12 col-lg-4">
                <label className="form-label fw-semibold">Queue Limit</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-list-ol"></i>
                  </span>
                  <input
                    className="form-control"
                    type="number"
                    min="0"
                    placeholder="50"
                    {...register("queueLimit")}
                  />
                </div>
                <div className="form-text">
                  Number of excess requests queued before rejection.
                </div>
                <div className="form-text text-warning">
                  These settings are currently not active.
                </div>
              </div>

              <div className="col-12 col-lg-4">
                <label className="form-label fw-semibold">Time Window</label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-clock"></i>
                  </span>
                  <input
                    className={`form-control${errors?.timeWindow ? " is-invalid" : ""}`}
                    type="text"
                    placeholder="01:00:00"
                    {...register("timeWindow", {
                      validate: (value) =>
                        isValidTimeWindow(value) ||
                        "Invalid format. Use hh:mm:ss (e.g., 01:00:00).",
                    })}
                  />
                </div>
                <div className="form-text">Format: hh:mm:ss.</div>
                <div className="form-text text-warning">
                  These settings are currently not active.
                </div>
                {errors?.timeWindow && (
                  <div className="error-msg">{errors.timeWindow.message}</div>
                )}
              </div>
            </div>
          </div>

          <div className="token-section mb-0">
            <div className="token-title mb-2">Interaction Tracking</div>
            <div className="form-check form-switch">
              <input
                className="form-check-input"
                type="checkbox"
                id="trackingToggle"
                {...register("enableITracking")}
              />
              <label className="form-check-label" htmlFor="trackingToggle">
                <i className="fa fa-wave-square me-2 text-secondary"></i>
                Enable Interaction Tracking
              </label>
            </div>
            <div className="form-text">
              Enable tracking to audit user sign-in prompts and consent flows.
            </div>
            <div className="form-text text-warning">
              These settings are currently not active.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProtectionStep;
