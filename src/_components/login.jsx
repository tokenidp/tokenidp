import React, { useEffect } from "react";

const AUTH_BASE_URL =
  process.env.REACT_APP_AUTH_BASE_URL || "https://localhost:5001/";
const CLIENT_ID = process.env.REACT_APP_OAUTH_CLIENT_ID || "";
const REDIRECT_URI =
  process.env.REACT_APP_OAUTH_REDIRECT_URI ||
  `${window.location.origin}/oauth/callback`;
const SCOPE =
  process.env.REACT_APP_OAUTH_SCOPE ||
  "openid profile email offline_access";
const CODE_VERIFIER_KEY =
  process.env.REACT_APP_OAUTH_CODE_VERIFIER_KEY || "pkce_code_verifier";

function base64UrlEncode(bytes) {
  let binary = "";
  bytes.forEach((byte) => {
    binary += String.fromCharCode(byte);
  });
  return btoa(binary).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/g, "");
}

function generateCodeVerifier() {
  const bytes = new Uint8Array(32);
  window.crypto.getRandomValues(bytes);
  return base64UrlEncode(bytes);
}

async function generateCodeChallenge(verifier) {
  const data = new TextEncoder().encode(verifier);
  const digest = await window.crypto.subtle.digest("SHA-256", data);
  return base64UrlEncode(new Uint8Array(digest));
}

function Login() {
  useEffect(() => {
    const redirectToAuthorize = async () => {
      const verifier = generateCodeVerifier();
      sessionStorage.setItem(CODE_VERIFIER_KEY, verifier);
      const challenge = await generateCodeChallenge(verifier);

      const authUrl =
        `${new URL("authorize", AUTH_BASE_URL).toString()}?` +
        `client_id=${encodeURIComponent(CLIENT_ID)}&` +
        `redirect_uri=${encodeURIComponent(REDIRECT_URI)}&` +
        "response_type=code&" +
        `scope=${encodeURIComponent(SCOPE)}&` +
        `code_challenge=${challenge}&` +
        "code_challenge_method=SHA256";

      window.location.assign(authUrl);
    };

    redirectToAuthorize();
  }, []);

  return (
    <div className="login-page">
      <section className="login-section-container">
        <div className="login-section row overflow-hidden mx-3">
          <div className="col-12 p-4 text-center">
            <h1>Redirecting to sign-in…</h1>
            <p className="text-muted">Please wait while we connect to Identity.</p>
          </div>
        </div>
      </section>
    </div>
  );
}

export default Login;
