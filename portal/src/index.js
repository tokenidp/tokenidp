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
import { getPortalConfig, loadPortalConfig } from "./config";

const container = document.getElementById("root");
const root = ReactDOM.createRoot(container);

loadPortalConfig().then(() => {
  const config = getPortalConfig();

  if (process.env.NODE_ENV === "production" && config.deploymentEnvironment !== "production") {
    console.info(`Running a production build for ${config.deploymentEnvironment}.`);
  }

  root.render(
    <React.StrictMode>
      <IdpAuthProvider
        config={{
          authority: config.authority,
          clientId: config.clientId,
          tenantPropagationMode: config.tenantPropagationMode,
          redirectUri: config.redirectUri,
          postLoginRedirectUri: config.postLoginRedirectUri,
          postLogoutRedirectUri: config.postLogoutRedirectUri,
          scope: config.scope,
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
});

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals(console.log);
