import { useEffect, useState } from "react";
import { useAuth } from "./AuthProvider";
import { useNavigate } from "react-router-dom";

export function AuthCallback({ redirectTo }) {
  const auth = useAuth();
  const navigate = useNavigate();
  const [error, setError] = useState("");

  useEffect(() => {
    const run = async () => {
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

      if (!code) {
        const msg = "Missing authorization code.";
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
  }, []);

  if (error) return <div style={{ padding: 16 }}>Login failed: {error}</div>;
  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section row overflow-hidden mx-3">
          <div className="col-12 p-4 text-center">
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
