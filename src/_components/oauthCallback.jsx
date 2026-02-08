import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { LOGIN } from "../_constants/actions";
import useApiClient from "../_hooks/useApiClient";
import { useAuth } from "../_hooks/useAuth";

const AUTH_BASE_URL = process.env.REACT_APP_AUTH_BASE_URL || "http://localhost:81/";
const APP_BASE_URL = process.env.REACT_APP_BASE_URL|| "http://localhost:81/";
const CLIENT_ID = process.env.REACT_APP_OAUTH_CLIENT_ID || "";
const REDIRECT_URI = process.env.REACT_APP_OAUTH_REDIRECT_URI || `${window.location.origin}/oauth/callback`;
const SCOPE = process.env.REACT_APP_OAUTH_SCOPE || "openid profile email offline_access";
const CODE_VERIFIER_KEY = process.env.REACT_APP_OAUTH_CODE_VERIFIER_KEY || "pkce_code_verifier";

const extractToken = (payload) => {
  if (!payload || typeof payload !== "object") return {};
  const tokenPayload = payload.value || payload.result || payload;
  return {
    accessToken: tokenPayload.accessToken || tokenPayload.access_token,
    refreshToken: tokenPayload.refreshToken || tokenPayload.refresh_token,
  };
};

const extractClaims = (userInfo) => {
  if (!userInfo || typeof userInfo !== "object") return [];
  if (Array.isArray(userInfo.permissions)) return userInfo.permissions;
  if (Array.isArray(userInfo.Permissions)) return userInfo.Permissions;
  return [];
};

function OAuthCallback() {
  const navigate = useNavigate();
  const [, dispatch] = useAuth();
  const authApi = useApiClient({ baseURL: AUTH_BASE_URL, skipAuth: true });
  const appApi = useApiClient({ baseURL: APP_BASE_URL, skipAuth: true });
  const [error, setError] = useState("");
  const hasRun = useRef(false);

  useEffect(() => {
    if (hasRun.current) return;
    hasRun.current = true;

    const handleCallback = async () => {
      const params = new URLSearchParams(window.location.search);
      const code = params.get("code");
      const authError = params.get("error");

      if (authError) {
        setError(authError);
        return;
      }

      if (!code) {
        navigate("/login", { replace: true });
        return;
      }

      const codeVerifier = sessionStorage.getItem(CODE_VERIFIER_KEY);
      if (!codeVerifier) {
        setError("Missing code verifier.");
        return;
      }

      try {
        const tokenResponse = await authApi.post("token", {
          grantType: "authorization_code",
          clientId: CLIENT_ID,
          redirectUri: REDIRECT_URI,
          code,
          codeVerifier,
          scope: SCOPE,
        });

        const tokenPayload = tokenResponse?.data;
        const { accessToken, refreshToken } = extractToken(tokenPayload);

        if (!accessToken) {
          throw new Error("Token response did not include an access token.");
        }

        console.log("Access Token:", accessToken); // For debugging purposes only
        
        const userInfoResponse = await appApi.get("admin/user/permissions", {
          headers: { Authorization: `Bearer ${accessToken}` },
        });

        console.log("userInfoResponse:", userInfoResponse); // For debugging purposes only

        const userInfoResult = userInfoResponse?.data;
        if (userInfoResult?.isSuccess === false) {
          throw new Error(userInfoResult?.error?.error || "Unable to load user permissions.");
        }

        const userInfo = userInfoResult?.value || {};
        var permissions = extractClaims(userInfo)

        dispatch({
          type: LOGIN,
          payload: {
            user: {
              userId: userInfo.userId ?? userInfo.UserId ?? 0,
              tenantId: userInfo.tenantId ?? userInfo.TenantId ?? 0,
              isAuthenticated: true,
              token: accessToken,
              refreshToken: refreshToken || "",
              userName: userInfo.userName || userInfo.UserName || "",
              permissions: permissions,
              landingPage: "/dashboard",
            },
          },
        });

        sessionStorage.removeItem(CODE_VERIFIER_KEY);
        navigate("/dashboard", { replace: true });

      } catch (err) {
        setError(err.message || "Unable to complete sign-in.");
      }
    };

    handleCallback();
  }, [authApi, dispatch, navigate]);

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

export default OAuthCallback;
