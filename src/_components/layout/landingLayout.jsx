import { Navigate } from "react-router-dom";
import { useAuth } from "../../_hooks/useAuth";

function LandingLayout() {
  const [user] = useAuth();

  if (!user) {
    return null;
  }

  const destination = user.isAuthenticated
    ? "/dashboard" || "/"
    : "/login";

  return <Navigate to={destination} replace />;
}

export default LandingLayout;
