export const WizardStep = {
  BasicInfo: "basicInfo",
  Auth: "auth",
  Scopes: "scopes",
  Configurations: "configurations",
  Protection: "protection",
  Review: "review",
};

export const wizardSteps = [
  { id: WizardStep.BasicInfo, label: "Basic Info", icon: "fa fa-user" },
  { id: WizardStep.Auth, label: "Authentication", icon: "fa fa-key" },
  { id: WizardStep.Scopes, label: "Scopes & Permissions", icon: "fa fa-list" },
  { id: WizardStep.Configurations, label: "Configurations", icon: "fa fa-sliders-h" },
  { id: WizardStep.Protection, label: "Rate Limit & Tracking", icon: "fa fa-shield-alt" },
  { id: WizardStep.Review, label: "Review & Confirm", icon: "fa fa-check" },
];
