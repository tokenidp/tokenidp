import { useEffect } from "react";
import { useAuth } from "tokenidp-react";
import logo from "../_assets/images/TokenIDP.svg";

export function LoginPage() {
  const auth = useAuth();
  const isLoggedOut =
    new URLSearchParams(window.location.search).get("logged_out") === "1";

  useEffect(() => {
    if (isLoggedOut) {
      return;
    }

    const redirectToAuthorize = async () => {
      auth.login();
    };

    redirectToAuthorize();
  }, [auth, isLoggedOut]);

  if (isLoggedOut) {
    return (
      <div className="login-page">
        <section className="login-section-container">
          <div className="login-section redirect-card">
            <div className="p-4 text-center">
              <div className="logo mb-3">Signed out</div>
              <h1>You have been signed out</h1>
              <p className="text-muted">
                Start a new session when you are ready.
              </p>
              <button
                className="btn btn-primary mt-3"
                onClick={() => auth.login()}
              >
                Sign in again
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
            <div className="mb-3">
              <img src={logo} alt="TokenIDP logo" width="250" />
            </div>
            <h1>Redirecting to TokenIDP...</h1>
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
