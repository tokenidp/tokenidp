import React from "react";

function WizardFooter({
  canGoBack,
  canGoNext,
  isLastStep,
  submitting,
  onBack,
  onCancel,
  onNext,
  onSubmit,
}) {
  return (
    <div className="d-flex justify-content-between align-items-center gap-2 mt-4">
      <div className="d-flex gap-2">
        {onCancel && (
          <button className="btn btn-outline-secondary" type="button" onClick={onCancel}>
            Cancel
          </button>
        )}
        <button
          className="btn btn-outline-secondary"
          type="button"
          onClick={onBack}
          disabled={!canGoBack}
        >
          Previous
        </button>
      </div>
      <div className="d-flex gap-2">
        {!isLastStep && (
          <button
            className="btn btn-primary-solid"
            type="button"
            onClick={onNext}
            disabled={!canGoNext}
          >
            Next
          </button>
        )}
        {isLastStep && (
          <button
            className="btn btn-primary-solid"
            type="button"
            onClick={onSubmit}
            disabled={submitting}
          >
            Save Client
          </button>
        )}
      </div>
    </div>
  );
}

export default WizardFooter;
