import React from "react";

function ProtectionStep({ register, errors, isValidTimeWindow }) {
  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-xl-10">
        <div className="wizard-step-shell">
          <h6 className="wizard-step-title d-flex align-items-center gap-2">
            Rate Limits &amp; Tracking
          </h6>

          <div className="wizard-info-banner" role="status">
            Rate limits help protect your identity platform from abuse, credential
            stuffing, and denial-of-service attacks. Limits are enforced per client
            and fall back to IP throttling when `client_id` is unavailable.
          </div>

          <div className="token-section">
            <div className="token-title mb-2">Rate Limits</div>
            <div className="row g-3">
              <div className="col-12 col-lg-4">
                <label
                  className="form-label fw-semibold"
                  title="Maximum requests allowed during the configured time window."
                >
                  Permit Limit
                </label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-gauge-high"></i>
                  </span>
                  <input
                    className={`form-control${errors?.permitLimit ? " is-invalid" : ""}`}
                    type="number"
                    min="1"
                    placeholder="20"
                    {...register("permitLimit", {
                      validate: (value) =>
                        value === "" ||
                        value === null ||
                        value === undefined ||
                        Number(value) > 0 ||
                        "Permit limit must be greater than zero.",
                    })}
                  />
                </div>
                <div className="form-text">
                  Maximum number of requests allowed within the time window.
                </div>
                <div className="form-text">Leave blank to use the server default.</div>
                {errors?.permitLimit && (
                  <div className="error-msg">{errors.permitLimit.message}</div>
                )}
              </div>

              <div className="col-12 col-lg-4">
                <label
                  className="form-label fw-semibold"
                  title="Requests allowed to wait in queue after the permit limit is reached."
                >
                  Queue Limit
                </label>
                <div className="input-group">
                  <span className="input-group-text">
                    <i className="fa fa-list-ol"></i>
                  </span>
                  <input
                    className={`form-control${errors?.queueLimit ? " is-invalid" : ""}`}
                    type="number"
                    min="0"
                    placeholder="0"
                    {...register("queueLimit", {
                      validate: (value) =>
                        value === "" ||
                        value === null ||
                        value === undefined ||
                        Number(value) >= 0 ||
                        "Queue limit cannot be negative.",
                    })}
                  />
                </div>
                <div className="form-text">
                  Number of excess requests queued before rejection.
                </div>
                <div className="form-text">Set to `0` to disable request queuing.</div>
                {errors?.queueLimit && (
                  <div className="error-msg">{errors.queueLimit.message}</div>
                )}
              </div>

              <div className="col-12 col-lg-4">
                <label
                  className="form-label fw-semibold"
                  title="Duration for each fixed window. Use hh:mm:ss or d.hh:mm:ss."
                >
                  Time Window
                </label>
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
                <div className="form-text">Format: `hh:mm:ss`.</div>
                <div className="form-text">Leave blank to use the server default of `00:01:00`.</div>
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
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProtectionStep;
