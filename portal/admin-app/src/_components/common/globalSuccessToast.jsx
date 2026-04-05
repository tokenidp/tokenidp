import React from "react";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

function GlobalSuccessToast() {
  const { success, clearSuccess } = useGlobalSuccess();

  if (!success) {
    return null;
  }

  return (
    <div className="global-success-toast">
      <div className="alert alert-success alert-dismissible mb-0 shadow-sm" role="status">
        {success.title && <div><strong>{success.title}</strong></div>}
        <div>{success.message}</div>
        <button
          type="button"
          className="btn-close"
          aria-label="Close"
          onClick={clearSuccess}
        ></button>
      </div>
    </div>
  );
}

export default GlobalSuccessToast;
