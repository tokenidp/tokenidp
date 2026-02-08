import React from "react";
import { useGlobalError } from "../../_hooks/useGlobalError";

function GlobalErrorBanner() {
  const { error, clearError } = useGlobalError();

  if (!error) {
    return null;
  }

  return (
    <div className="alert alert-danger alert-dismissible mb-0" role="alert">
      <div className="d-flex justify-content-between align-items-start">
        <div>
          {error.title && <strong className="me-2">{error.title}</strong>}
          <span>{error.message}</span>
        </div>
        <button
          type="button"
          className="btn-close"
          aria-label="Close"
          onClick={clearError}
        ></button>
      </div>
    </div>
  );
}

export default GlobalErrorBanner;
