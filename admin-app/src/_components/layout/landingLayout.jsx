import { Navigate } from "react-router-dom";
import { useAuth } from "tokentresor-idp-react";

function LandingLayout() {
  const auth = useAuth();

  if (!auth) {
    return null;
  }

  const destination = auth.isAuthenticated ? "/dashboard" || "/" : "/login";

  console.log("LandingLayout: redirecting to ", destination);

  return <Navigate to={destination} replace />;
}

export default LandingLayout;
