export const WizardStep = {
  BasicInfo: "basicInfo",
  Auth: "auth",
  Redirects: "redirects",
  Tokens: "tokens",
  Scopes: "scopes",
  Protection: "protection",
  Review: "review",
};

export const wizardSteps = [
  { id: WizardStep.BasicInfo, label: "Basic Info", icon: "fa fa-user" },
  { id: WizardStep.Auth, label: "Authentication", icon: "fa fa-key" },
  {
    id: WizardStep.Redirects,
    label: "Redirect & Logout URLs",
    icon: "fa fa-link",
  },
  { id: WizardStep.Tokens, label: "Token Settings", icon: "fa fa-coins" },
  { id: WizardStep.Scopes, label: "Scopes & Permissions", icon: "fa fa-list" },
  { id: WizardStep.Protection, label: "Rate Limits & Tracking", icon: "fa fa-shield-alt" },
  { id: WizardStep.Review, label: "Review & Confirm", icon: "fa fa-check" },
];
