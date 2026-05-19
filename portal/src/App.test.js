import { render } from "@testing-library/react";
import App from "./App";
import { IdpAuthProvider } from "tokenidp-react";
import { AuthProvider } from "./_hooks/useAuth";
import { GlobalErrorProvider } from "./_hooks/useGlobalError";
import { GlobalSuccessProvider } from "./_hooks/useGlobalSuccess";

test("renders app without crashing", () => {
  window.history.pushState({}, "", "/unauthorized");

  expect(() =>
    render(
      <IdpAuthProvider
        config={{
          authority: "https://idp.example.test",
          clientId: "idp-admin",
          redirectUri: "https://admin.example.test/auth/callback",
          storage: "memory",
        }}
      >
        <GlobalErrorProvider>
          <GlobalSuccessProvider>
            <AuthProvider>
              <App />
            </AuthProvider>
          </GlobalSuccessProvider>
        </GlobalErrorProvider>
      </IdpAuthProvider>
    )
  ).not.toThrow();
});
