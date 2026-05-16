import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import { GlobalErrorProvider } from "./_hooks/useGlobalError";
import { GlobalSuccessProvider } from "./_hooks/useGlobalSuccess";
import Spinner from "./_components/common/spinner";
import GlobalSuccessToast from "./_components/common/globalSuccessToast";
import { IdpAuthProvider } from "tokenidp-react";
import { AuthProvider } from "./_hooks/useAuth";

const container = document.getElementById("root");
const root = ReactDOM.createRoot(container);
const deploymentEnvironment =
  process.env.REACT_APP_DEPLOYMENT_ENV || process.env.NODE_ENV || "development";
const tenantPropagationMode =
  process.env.REACT_APP_TENANT_PROPAGATION_MODE || "all";

if (process.env.NODE_ENV === "production" && deploymentEnvironment !== "production") {
  console.info(`Running a production build for ${deploymentEnvironment}.`);
}

root.render(
  <React.StrictMode>
    <IdpAuthProvider
      config={{
        authority: process.env.REACT_APP_AUTH_BASE_URL,
        clientId: process.env.REACT_APP_OAUTH_CLIENT_ID,
        tenantPropagationMode,
        redirectUri: process.env.REACT_APP_OAUTH_REDIRECT_URI,
        postLoginRedirectUri: "/dashboard",
        postLogoutRedirectUri:
          process.env.REACT_APP_OAUTH_POST_LOGOUT_REDIRECT_URI || "/login",
        scope: process.env.REACT_APP_OAUTH_SCOPE,
        storage: "localStorage",
      }}
    >
      <GlobalErrorProvider>
        <GlobalSuccessProvider>
          <AuthProvider>
            <App />
            <GlobalSuccessToast />
            <Spinner />
          </AuthProvider>
        </GlobalSuccessProvider>
      </GlobalErrorProvider>
    </IdpAuthProvider>
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals(console.log);
