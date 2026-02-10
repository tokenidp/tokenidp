import React from "react";

function WizardHeader({ stepIndex, steps, onStepChange }) {
  return (
    <div className="wizard-header">
      <div className="wizard-steps">
        {steps.map((step, index) => {
          const isCurrent = index === stepIndex;
          const isComplete = index < stepIndex;
          const canJump = index <= stepIndex;
          return (
            <button
              key={step.id}
              type="button"
              className={`wizard-step ${isCurrent ? "is-current" : ""} ${
                isComplete ? "is-complete" : ""
              }`}
              onClick={() => canJump && onStepChange?.(index)}
              disabled={!canJump}
              aria-current={isCurrent ? "step" : undefined}
            >
              <span className="wizard-step-icon" aria-hidden="true">
                <i className={step.icon || "fa fa-circle"} />
              </span>
              <span className="wizard-step-label">{step.label}</span>
            </button>
          );
        })}
      </div>
    </div>
  );
}

export default WizardHeader;
