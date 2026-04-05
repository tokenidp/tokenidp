import { usePromiseTracker } from "react-promise-tracker";
import { ThreeDots } from "react-loader-spinner";
import { useAuth } from "tokenidp-react";

function Spinner() {
  const auth = useAuth();
  const { promiseInProgress } = usePromiseTracker();
  return (
    promiseInProgress && (
      <div
        className={auth.isAuthenticated ? "test" : "test2"}
        style={{
          width: "100%",
          height: "100",
          display: "flex",
          justifyContent: "center",
          alignItems: "center",
          position: "fixed",
          top: "50%",
          left: "50%",
          transform: "translate(-50%, -50%)",
          background: "rgb(255 255 255 / 27%)",
          height: "100%",
        }}
      >
        <ThreeDots color="#00a9ff" height="100" width="100" />
      </div>
    )
  );
}

export default Spinner;
