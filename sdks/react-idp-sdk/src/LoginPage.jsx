import React, { useEffect } from "react";
import { useAuth } from "./AuthProvider";
import "./styles/idp-default.css";

function renderLogoContent(logo, logoAlt, fallback) {
  if (!logo) {
    return <div className="logo mb-3">{fallback}</div>;
  }

  if (typeof logo === "string") {
    return (
      <div className="mb-3">
        <img src={logo} alt={logoAlt} width="250" />
      </div>
    );
  }

  return <div className="mb-3">{logo}</div>;
}

export function LoginPage({
  logo = null,
  logoAlt = "Application logo",
  title = "Redirecting to sign-in...",
  subtitle = "Please wait while we securely connect to Identity.",
  signedOutBadge = "Signed out",
  signedOutTitle = "You have been signed out",
  signedOutSubtitle = "Start a new session when you are ready.",
  signInAgainLabel = "Sign in again",
  fallbackBadge = "ID",
  loginOptions,
}) {
  const auth = useAuth();
  const isLoggedOut =
    new URLSearchParams(window.location.search).get("logged_out") === "1";

  useEffect(() => {
    if (isLoggedOut) {
      return;
    }

    auth.login(loginOptions);
  }, [auth, isLoggedOut, loginOptions]);

  if (isLoggedOut) {
    return (
      <div className="login-page">
        <section className="login-section-container">
          <div className="login-section redirect-card">
            <div className="p-4 text-center">
              {renderLogoContent(logo, logoAlt, fallbackBadge)}
              <h1>{signedOutTitle}</h1>
              <p className="text-muted">{signedOutSubtitle}</p>
              <button
                className="btn btn-primary mt-3"
                onClick={() => auth.login(loginOptions)}
              >
                {signInAgainLabel}
              </button>
            </div>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section redirect-card">
          <div className="p-4 text-center">
            {renderLogoContent(logo, logoAlt, fallbackBadge)}
            <h1>{title}</h1>
            <p className="text-muted">{subtitle}</p>
            <div className="spinner mt-3"></div>
          </div>
        </div>
      </section>
    </div>
  );
}
