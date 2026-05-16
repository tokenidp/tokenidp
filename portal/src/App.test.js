import { render } from "@testing-library/react";
import App from "./App";
import { IdpAuthProvider } from "tokenidp-react";
import { AuthProvider } from "./_hooks/useAuth";

test("renders app without crashing", () => {
  expect(() =>
    render(
      <IdpAuthProvider config={{ storage: "memory" }}>
        <AuthProvider>
          <App />
        </AuthProvider>
      </IdpAuthProvider>
    )
  ).not.toThrow();
});
