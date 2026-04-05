import React, { useMemo } from "react";

const parseLines = (value) =>
  String(value || "")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean);

const validateUriLines = (value) => {
  const lines = parseLines(value);
  if (!lines.length) {
    return "Redirect URI is required.";
  }
  for (const line of lines) {
    if (line.includes("*")) {
      return "Wildcards are not allowed in redirect URIs";
    }
    if (line.includes("#")) {
      return "Fragments are not allowed in redirect URIs";
    }
    if (line.startsWith("?")) {
      return "Invalid URI format";
    }
    let parsed;
    try {
      parsed = new URL(line);
    } catch (error) {
      return "Invalid URI format";
    }
    const isLocalhost = parsed.hostname === "localhost";
    if (!isLocalhost && parsed.protocol !== "https:") {
      return "Only HTTPS is allowed (except http://localhost for development)";
    }
  }
  return true;
};

const validateLogoutLines = (value) => {
  const lines = parseLines(value);
  if (!lines.length) {
    return true;
  }
  for (const line of lines) {
    if (line.includes("*")) {
      return "Wildcards are not allowed in redirect URIs";
    }
    if (line.includes("#")) {
      return "Fragments are not allowed in redirect URIs";
    }
    if (line.startsWith("?")) {
      return "Invalid URI format";
    }
    let parsed;
    try {
      parsed = new URL(line);
    } catch (error) {
      return "Invalid URI format";
    }
    const isLocalhost = parsed.hostname === "localhost";
    if (!isLocalhost && parsed.protocol !== "https:") {
      return "Only HTTPS is allowed (except http://localhost for development)";
    }
  }
  return true;
};

function RedirectsStep({ register, errors, appType }) {
  const redirectHint = useMemo(() => {
    if (appType === "4") {
      return "This application type does not use redirect-based flows. Redirect URIs are not required.";
    }
    return "Redirect URIs are required for authorization code flow.";
  }, [appType]);

  return (
    <div className="row g-4 justify-content-center">
      <div className="col-12 col-lg-8 col-xl-7">
        <div className="card form-section-card">
          <div className="card-body">
            <h6 className="card-title">Redirect &amp; Logout URLs</h6>
            <div className="wizard-info-banner" role="status">
              Redirect URIs must exactly match the URLs used by your application.
              Do not use wildcards or broad domains in production.
              Incorrect configuration can lead to token leakage.
            </div>

            <div className="mb-3">
              <label className="form-label fw-semibold">Redirect URIs *</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-link"></i>
                </span>
                <textarea
                  className={`form-control${errors.redirectUri ? " is-invalid" : ""}`}
                  rows="4"
                  placeholder={
                    "https://app.example.com/callback\nhttp://localhost:3000/callback"
                  }
                  {...register("redirectUri", { validate: validateUriLines })}
                ></textarea>
              </div>
              {errors.redirectUri && (
                <div className="error-msg">{errors.redirectUri.message}</div>
              )}
              <div className="form-text">
                Add one URI per line. Required for authorization code flows.
              </div>
              <div className="form-text text-muted">{redirectHint}</div>
            </div>

            <div className="mb-0">
              <label className="form-label fw-semibold">Logout Redirect URIs</label>
              <div className="input-group">
                <span className="input-group-text">
                  <i className="fa fa-sign-out-alt"></i>
                </span>
                <textarea
                  className={`form-control${
                    errors.logoutRedirectUri ? " is-invalid" : ""
                  }`}
                  rows="4"
                  placeholder={"https://app.example.com/logout\nhttp://localhost:3000/logout"}
                  {...register("logoutRedirectUri", { validate: validateLogoutLines })}
                ></textarea>
              </div>
              {errors.logoutRedirectUri && (
                <div className="error-msg">{errors.logoutRedirectUri.message}</div>
              )}
              <div className="form-text">
                Optional. Used after user signs out to redirect back to your application.
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

export default RedirectsStep;
