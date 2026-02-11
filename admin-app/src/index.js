import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";
import App from "./App";
import reportWebVitals from "./reportWebVitals";
import { GlobalErrorProvider } from "./_hooks/useGlobalError";
import Spinner from "./_components/common/spinner";
import { IdpAuthProvider } from "@tokentresor/idp-react";

const container = document.getElementById("root");
const root = ReactDOM.createRoot(container);

root.render(
  <React.StrictMode>
    <IdpAuthProvider
      config={{
        authority: process.env.REACT_APP_AUTH_BASE_URL,
        clientId: process.env.REACT_APP_OAUTH_CLIENT_ID,
        redirectUri: process.env.REACT_APP_OAUTH_REDIRECT_URI,
        postLoginRedirectUri: "/dashboard",
        scope: process.env.REACT_APP_OAUTH_SCOPE,
        storage: "localStorage",
      }}
    >
      <GlobalErrorProvider>
        <App />
        <Spinner />
      </GlobalErrorProvider>
    </IdpAuthProvider>
  </React.StrictMode>
);

// If you want to start measuring performance in your app, pass a function
// to log results (for example: reportWebVitals(console.log))
// or send to an analytics endpoint. Learn more: https://bit.ly/CRA-vitals
reportWebVitals(console.log);
