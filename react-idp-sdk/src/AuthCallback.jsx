import React, { useEffect, useRef, useState } from "react";
import { useAuth } from "./AuthProvider";
import { useNavigate } from "react-router-dom";
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

export function AuthCallback({
  redirectTo,
  logo = null,
  logoAlt = "Application logo",
  fallbackBadge = "ID",
}) {
  const navigate = useNavigate();
  const auth = useAuth();
  const [error, setError] = useState(null);
  const ranRef = useRef(false);

  useEffect(() => {
    const run = async () => {
      if (ranRef.current) return;
      ranRef.current = true;

      const qs = new URLSearchParams(window.location.search);
      const code = qs.get("code");
      const state = qs.get("state");
      const err = qs.get("error");
      const errDesc = qs.get("error_description");

      if (err) {
        const msg = errDesc || err || "Authentication error.";
        setError(msg);
        auth.setError(msg);
        return;
      }

      if (!code || !state) {
        const msg = "Missing authorization code or state.";
        setError(msg);
        auth.setError(msg);
        return;
      }

      try {
        await auth.handleCallback({ code, state });
        const target = redirectTo || auth.landingPage || "/";
        navigate(target, { replace: true });
      } catch (e) {
        const msg = e?.message || "Login callback failed.";
        setError(msg);
        auth.setError(msg);
      }
    };

    run();
  }, [auth, navigate, redirectTo]);

  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section redirect-card">
          <div className="col-12 p-4 text-center">
            {renderLogoContent(logo, logoAlt, fallbackBadge)}
            <h1>Completing sign-in</h1>
            <p className="text-muted">
              {error ? error : "Please wait while we finish authentication."}
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
