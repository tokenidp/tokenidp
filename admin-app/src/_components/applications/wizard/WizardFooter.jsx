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
          <button className="btn btn-soft" type="button" onClick={onCancel}>
            <i className="fa fa-times me-1" aria-hidden="true"></i>
            Cancel
          </button>
        )}
        <button
          className="btn btn-outline-secondary"
          type="button"
          onClick={onBack}
          disabled={!canGoBack}
        >
          <i class="fa-solid pe-2 fa-arrow-left"></i>
          Previous
        </button>
      </div>
      <div className="d-flex gap-2">
        {!isLastStep && (
          <button
            className="btn btn-primary"
            type="button"
            onClick={onNext}
            disabled={!canGoNext}
          >
            Next
            <i class="fa-solid ps-2 fa-arrow-right"></i>
          </button>
        )}
        {isLastStep && (
          <button
            className="btn btn-primary"
            type="button"
            onClick={onSubmit}
            disabled={submitting}
          >
            <i className="fa fa-save pe-2" aria-hidden="true"></i>
            Save Client
          </button>
        )}
      </div>
    </div>
  );
}

export default WizardFooter;