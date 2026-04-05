import { render } from "@testing-library/react";
import App from "./App";
import { AuthProvider } from "./_hooks/useAuth";

test("renders app without crashing", () => {
  expect(() =>
    render(
      <AuthProvider>
        <App />
      </AuthProvider>
    )
  ).not.toThrow();
});
