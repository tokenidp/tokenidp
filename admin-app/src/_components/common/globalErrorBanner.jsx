import React from "react";
import { useGlobalError } from "../../_hooks/useGlobalError";

function GlobalErrorBanner() {
  const { error, clearError } = useGlobalError();

  if (!error) {
    return null;
  }

  const messages = Array.isArray(error?.messages)
    ? error.messages.filter(Boolean)
    : [];
  const hasMultipleMessages = messages.length > 1;

  return (
    <div className="alert alert-danger alert-dismissible mb-0" role="alert">
      <div className="d-flex justify-content-between align-items-start">
        <div>
          {error.title && <strong className="me-2">{error.title}</strong>}
          {hasMultipleMessages ? (
            <ul className="mb-0 ps-3 mt-1">
              {messages.map((message) => (
                <li key={message}>{message}</li>
              ))}
            </ul>
          ) : (
            <span>{messages[0] || error.message}</span>
          )}
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
