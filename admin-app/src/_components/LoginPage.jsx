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
        <div className="login-section row overflow-hidden mx-3">
          <div className="col-12 p-4 text-center">
            <h1>Redirecting to SmartDevCon IDP…</h1>
            <p className="text-muted">
              Please wait while we connect to Identity.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
