import React, { useEffect, useRef, useState } from "react";
import { useAuth } from "./AuthProvider";
import { useNavigate } from "react-router-dom";
import "./styles/idp-default.css";

export function AuthCallback({ redirectTo }) {
  const navigate = useNavigate();
  const auth = useAuth();
  const [error, setError] = useState(null);
  const ranRef = useRef(false);

  useEffect(() => {
    const run = async () => {
      if (ranRef.current) return; // hard stop for duplicates
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
  }, [auth]);

  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section redirect-card">
          <div className="col-12 p-4 text-center">
            <div className="logo mb-3">✒️</div>
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
