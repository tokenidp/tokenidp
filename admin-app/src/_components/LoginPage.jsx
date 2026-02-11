import { useEffect } from "react";
import { useAuth } from "@tokentresor/idp-react";

export function LoginPage() {
  const auth = useAuth();

  useEffect(() => {
    const redirectToAuthorize = async () => {
      auth.login();
    };

    redirectToAuthorize();
  }, []);

  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section redirect-card">
          <div className="p-4 text-center">
            <div className="logo mb-3">✒️</div>
            <h1>Redirecting to SmartDevCon IDP…</h1>
            <p className="text-muted">
              Please wait while we securely connect to Identity.
            </p>

            <div className="spinner mt-3"></div>
          </div>
        </div>
      </section>
    </div>
  );
}
