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
    <div className="row g-4 justify-content-center mt-1">
      <div className="col-12 col-xl-10">
        <div className="d-flex flex-column flex-sm-row justify-content-between align-items-stretch align-items-sm-center gap-2">
          <div className="d-flex flex-wrap gap-2">
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
              <i className="fa-solid pe-2 fa-arrow-left" aria-hidden="true"></i>
              Previous
            </button>
          </div>
          <div className="d-flex justify-content-sm-end">
            {!isLastStep && (
              <button
                className="btn btn-primary"
                type="button"
                onClick={onNext}
                disabled={!canGoNext}
              >
                Next
                <i className="fa-solid ps-2 fa-arrow-right" aria-hidden="true"></i>
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
      </div>
    </div>
  );
}

export default WizardFooter;
