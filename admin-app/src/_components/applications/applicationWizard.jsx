import React, { useEffect, useMemo, useState } from "react";
import { FormProvider, useForm } from "react-hook-form";
import InfoModal from "../common/infoModal";
import WizardHeader from "./wizard/WizardHeader";
import WizardFooter from "./wizard/WizardFooter";
import { WizardStep, wizardSteps } from "./wizard/wizardSteps";
import {
  fallbackAppTypes,
  fallbackGrantTypes,
  fallbackScopes,
  isValidTimeWindow,
  createWizardState,
  normalizeLookupOptions,
  normalizeTimeWindow,
  normalizeValue,
} from "./wizard/wizardState";
import BasicInfoStep from "./wizard/steps/BasicInfoStep";
import AuthStep from "./wizard/steps/AuthStep";
import RedirectsStep from "./wizard/steps/RedirectsStep";
import TokensStep from "./wizard/steps/TokensStep";
import ScopesStep from "./wizard/steps/ScopesStep";
import ProtectionStep from "./wizard/steps/ProtectionStep";
import ReviewStep from "./wizard/steps/ReviewStep";

const stepFields = {
  [WizardStep.BasicInfo]: ["clientName", "appType"],
  [WizardStep.Auth]: [],
  [WizardStep.Redirects]: ["redirectUri"],
  [WizardStep.Tokens]: [
    "tokenType",
    "accessTokenLifetime",
    "authorizationCodeLifetime",
    "refreshTokenExpiration",
  ],
  [WizardStep.Scopes]: [],
  [WizardStep.Protection]: ["timeWindow"],
  [WizardStep.Review]: [],
};

const GrantType = {
  AuthorizationCode: 0,
  RefreshToken: 1,
  ClientCredentials: 2,
  DeviceCode: 3,
  Ciba: 4,
};

const isDeviceIotLabel = (label) => /device[\s_/-]*iot/i.test(String(label || ""));

const formatAppTypeLabel = (label) => {
  if (isDeviceIotLabel(label)) {
    return "Device/IOT (Under Development)";
  }
  return String(label || "");
};

function ApplicationWizard({
  initialValues,
  onSubmit,
  onCancel,
  submitting,
  lookups,
  mode,
}) {
  const methods = useForm({
    defaultValues: createWizardState(initialValues),
  });

  const {
    register,
    handleSubmit,
    setValue,
    clearErrors,
    reset,
    getValues,
    watch,
    trigger,
    formState: { errors },
  } = methods;

  const [stepIndex, setStepIndex] = useState(0);
  const [appType, setAppType] = useState(
    normalizeValue(initialValues.appType, "")
  );
  const [tokenType, setTokenType] = useState(
    normalizeValue(initialValues.accessTokenType ?? initialValues.tokenType, "")
  );
  const [showSecret, setShowSecret] = useState(false);
  const [grantTypes, setGrantTypes] = useState(
    Array.isArray(initialValues.grantTypes) ? initialValues.grantTypes : [0]
  );
  const [scopes, setScopes] = useState(
    Array.isArray(initialValues.scopes) ? initialValues.scopes : ["openid", "profile"]
  );
  const [clientAudience, setClientAudience] = useState(initialValues.clientAudience || "");
  const [isActive, setIsActive] = useState(
    initialValues.isActive === undefined ? true : initialValues.isActive
  );
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({
    title: "",
    message: "",
  });

  useEffect(() => {
    reset(createWizardState(initialValues));
    setAppType(normalizeValue(initialValues.appType, ""));
    setTokenType(
      normalizeValue(initialValues.accessTokenType ?? initialValues.tokenType, "")
    );
    setGrantTypes(
      Array.isArray(initialValues.grantTypes) ? initialValues.grantTypes : [0]
    );
    setScopes(
      Array.isArray(initialValues.scopes) ? initialValues.scopes : ["openid", "profile"]
    );
    setClientAudience(initialValues.clientAudience || "");
  }, [initialValues, reset]);

  const appTypeOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.appTypes);
    const helperText = {
      0: "Browser-based apps (React, Angular). Uses PKCE. No client secret.",
      1: "Native mobile apps. Uses PKCE. No client secret.",
      2: "Installed desktop apps. Uses PKCE. No client secret.",
      3: "Server-rendered apps. Can securely store client secrets.",
      4: "Machine-to-machine services. Uses client credentials.",
      5: "Device/IOT flow is under development. Device Code only.",
    };
    if (normalized.length) {
      return normalized.map((option) => ({
        ...option,
        value: String(option.key ?? ""),
        label: formatAppTypeLabel(option.value),
        helper: isDeviceIotLabel(option.value)
          ? helperText[5]
          : helperText[option.key],
        icon:
          option.value?.toLowerCase() === "spa"
            ? "fa fa-globe"
            : option.value?.toLowerCase() === "mobile"
              ? "fa fa-mobile-alt"
              : option.value?.toLowerCase() === "desktop"
                ? "fa fa-desktop"
                : option.value?.toLowerCase() === "webapp"
                  ? "fa fa-window-maximize"
                  : isDeviceIotLabel(option.value)
                    ? "fa fa-microchip"
                    : "fa fa-robot",
      }));
    }
    return fallbackAppTypes.map((option) => ({
      ...option,
      helper: isDeviceIotLabel(option.label) ? helperText[5] : helperText[option.value],
    }));
  }, [lookups?.appTypes]);


  const tokenTypeOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.tokenTypes);
    if (normalized.length) {
      return normalized.map((option) => ({
        value: String(option.key ?? ""),
        label: option.value,
      }));
    }
    return [
      { value: "0", label: "JWT" },
      { value: "1", label: "Reference" },
    ];
  }, [lookups?.tokenTypes]);

  const scopeOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.scopes);
    if (normalized.length) {
      return normalized.map((option) => ({
        value: String(option.value ?? ""),
        label: option.value,
        icon: "fa fa-badge-check",
      }));
    }
    return fallbackScopes;
  }, [lookups?.scopes]);

  const externalProviderOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.externalProviders);
    if (!normalized.length) {
      return [];
    }

    return normalized
      .map((option) => ({
        value: String(option.key ?? ""),
        label: String(option.value ?? ""),
      }))
      .filter((option) => option.value && option.label);
  }, [lookups?.externalProviders]);

  const selectedAppType = useMemo(
    () => appTypeOptions.find((option) => String(option.value) === String(appType)),
    [appTypeOptions, appType]
  );
  const selectedAppTypeLabel = String(selectedAppType?.label || "");

  const isSpa = appType === "0" || selectedAppTypeLabel.toLowerCase() === "spa";
  const isMobile = appType === "1" || selectedAppTypeLabel.toLowerCase() === "mobile";
  const isDesktop = appType === "2" || selectedAppTypeLabel.toLowerCase() === "desktop";
  const isWeb = appType === "3" || selectedAppTypeLabel.toLowerCase().includes("web");
  const isBackend = appType === "4" || selectedAppTypeLabel.toLowerCase().includes("backend");
  const isDeviceIot = appType === "5" || isDeviceIotLabel(selectedAppTypeLabel);

  const isPublicClient = isSpa || isMobile || isDesktop;

  const allowedGrants = useMemo(() => {
    if (isBackend) return new Set([GrantType.ClientCredentials]);
    if (isWeb) return new Set([GrantType.AuthorizationCode, GrantType.RefreshToken, GrantType.Ciba]);
    if (isDesktop) {
      return new Set([
        GrantType.AuthorizationCode,
        GrantType.RefreshToken,
        GrantType.DeviceCode,
      ]);
    }
    if (isMobile || isSpa) {
      return new Set([GrantType.AuthorizationCode, GrantType.RefreshToken]);
    }
    if (isDeviceIot) return new Set([GrantType.DeviceCode]);
    return new Set([
      GrantType.AuthorizationCode,
      GrantType.RefreshToken,
      GrantType.ClientCredentials,
      GrantType.DeviceCode,
      GrantType.Ciba,
    ]);
  }, [isBackend, isWeb, isDesktop, isMobile, isSpa, isDeviceIot]);

  const authGrantOptions = useMemo(() => {
    if (isBackend) {
      return fallbackGrantTypes.filter((grant) => grant.id === GrantType.ClientCredentials);
    }
    if (isDeviceIot) {
      return fallbackGrantTypes.filter((grant) => grant.id === GrantType.DeviceCode);
    }
    return fallbackGrantTypes;
  }, [isBackend, isDeviceIot]);

  useEffect(() => {
    setGrantTypes((prev) => {
      let next = prev.filter((grant) => allowedGrants.has(grant));
      if (isBackend && !next.includes(GrantType.ClientCredentials)) {
        next = [...next, GrantType.ClientCredentials];
      }
      if (isDeviceIot && !next.includes(GrantType.DeviceCode)) {
        next = [...next, GrantType.DeviceCode];
      }
      if ((isSpa || isMobile || isDesktop) && !next.includes(GrantType.AuthorizationCode)) {
        next = [...next, GrantType.AuthorizationCode];
      }
      return next;
    });
  }, [allowedGrants, isBackend, isDeviceIot, isSpa, isMobile, isDesktop]);

  useEffect(() => {
    if (isPublicClient || isDeviceIot) {
      setValue("clientSecret", "", { shouldDirty: true });
      setValue("clientSecretExpiry", "", { shouldDirty: true });
    }
  }, [isPublicClient, isDeviceIot, setValue]);

  const hasInsecureGrant = useMemo(() => {
    return grantTypes.some((grant) => !allowedGrants.has(grant));
  }, [allowedGrants, grantTypes]);

  const [grantError, setGrantError] = useState("");

  const validateGrantSelection = () => {
    const clientSecretValue = getValues("clientSecret");
    if ((isPublicClient || isDeviceIot) && clientSecretValue) {
      setGrantError("Public clients cannot have client secrets.");
      return false;
    }
    if (isPublicClient && grantTypes.includes(GrantType.ClientCredentials)) {
      setGrantError("Public clients cannot use client_credentials.");
      return false;
    }
    if ((isSpa || isMobile || isDesktop) && grantTypes.includes(GrantType.ClientCredentials)) {
      setGrantError("SPA, Mobile, and Desktop apps cannot use client_credentials.");
      return false;
    }
    if (grantTypes.includes(GrantType.Ciba) && (isPublicClient || isBackend || isDeviceIot)) {
      setGrantError("CIBA is supported for Web applications only.");
      return false;
    }
    if (
      grantTypes.includes(GrantType.DeviceCode) &&
      (isSpa || isWeb || isBackend)
    ) {
      setGrantError("Device Code is supported for Mobile and Desktop clients.");
      return false;
    }
    if (isBackend && !grantTypes.includes(GrantType.ClientCredentials)) {
      setGrantError("Backend apps must support client_credentials.");
      return false;
    }
    if (isDeviceIot && !grantTypes.includes(GrantType.DeviceCode)) {
      setGrantError("Device/IOT clients must use device_code.");
      return false;
    }
    const unsupportedGrant = grantTypes.find((grant) => !allowedGrants.has(grant));
    if (unsupportedGrant !== undefined) {
      setGrantError("Selected grant type is not allowed for this application type.");
      return false;
    }
    if ((isSpa || isMobile || isDesktop) && !grantTypes.includes(GrantType.AuthorizationCode)) {
      setGrantError("Public clients must use authorization_code with PKCE.");
      return false;
    }
    if (
      grantTypes.includes(GrantType.RefreshToken) &&
      !grantTypes.includes(GrantType.AuthorizationCode)
    ) {
      setGrantError("refresh_token requires authorization_code.");
      return false;
    }
    setGrantError("");
    return true;
  };

  useEffect(() => {
    if (grantError) {
      validateGrantSelection();
    }
  }, [
    appType,
    grantTypes,
    isPublicClient,
    isDeviceIot,
    isSpa,
    isMobile,
    isDesktop,
    isWeb,
    isBackend,
    grantError,
    allowedGrants,
  ]);

  const toggleGrant = (value) => {
    setGrantTypes((prev) => {
      if (value === 1 && !prev.includes(0)) {
        return [...prev, 0, 1];
      }
      if (value === 0 && prev.includes(0) && prev.includes(1)) {
        return prev.filter((g) => g !== 0 && g !== 1);
      }
      return prev.includes(value) ? prev.filter((g) => g !== value) : [...prev, value];
    });
  };

  const toggleScope = (value) => {
    setScopes((prev) =>
      prev.includes(value) ? prev.filter((s) => s !== value) : [...prev, value]
    );
  };

  const clientIdValue = watch("clientId");

  const copyClientId = async () => {
    if (!clientIdValue) return;
    try {
      await navigator.clipboard.writeText(clientIdValue);
      setInfoContent({
        title: "Client ID copied",
        message: "Client ID has been copied to clipboard.",
      });
    } catch (error) {
      setInfoContent({
        title: "Unable to copy",
        message: "Copy failed. Please select the Client ID and copy manually.",
      });
    }
    setInfoOpen(true);
  };

  const regenerateSecret = () => {
    setInfoContent({
      title: "Client secret regenerated",
      message: "A new client secret has been generated (placeholder).",
    });
    setInfoOpen(true);
  };

  const submitForm = (data) => {
    if (!validateGrantSelection()) {
      return;
    }
    const normalizedTimeWindow = normalizeTimeWindow(data.timeWindow);
    onSubmit?.({
      ...data,
      grantTypes,
      scopes,
      audiences: data.clientAudience ? [data.clientAudience] : [],
      timeWindow: normalizedTimeWindow,
    });
  };

  const currentStep = wizardSteps[stepIndex];
  const isLastStep = stepIndex === wizardSteps.length - 1;
  const canGoBack = stepIndex > 0;
  const canGoNext = !isLastStep && !submitting;

  const handleNext = async () => {
    if (isLastStep) {
      return;
    }
    if (currentStep.id === WizardStep.Auth && !validateGrantSelection()) {
      return;
    }
    const fields = stepFields[currentStep.id] || [];
    const valid = fields.length ? await trigger(fields) : true;
    if (valid) {
      setStepIndex((prev) => Math.min(prev + 1, wizardSteps.length - 1));
    }
  };

  const handleBack = () => {
    if (canGoBack) {
      setStepIndex((prev) => Math.max(prev - 1, 0));
    }
  };

  const handleStepChange = (nextIndex) => {
    if (nextIndex < 0 || nextIndex > stepIndex) return;
    setStepIndex(nextIndex);
  };

  const renderStep = () => {
    switch (currentStep.id) {
      case WizardStep.BasicInfo:
        return (
          <BasicInfoStep
            register={register}
            errors={errors}
            isActive={isActive}
            setIsActive={setIsActive}
            appType={appType}
            setAppType={setAppType}
            appTypeOptions={appTypeOptions}
            setValue={setValue}
            clearErrors={clearErrors}
            onCopyClientId={copyClientId}
            clientIdValue={clientIdValue}
          />
        );
      case WizardStep.Auth:
        return (
          <AuthStep
            register={register}
            isPublicClient={isPublicClient}
            isDeviceIot={isDeviceIot}
            isWebClient={isWeb}
            showSecret={showSecret}
            setShowSecret={setShowSecret}
            onRegenerateSecret={regenerateSecret}
            grantTypes={grantTypes}
            toggleGrant={toggleGrant}
            grantOptions={authGrantOptions}
            allowedGrants={allowedGrants}
            hasInsecureGrant={hasInsecureGrant}
            grantError={grantError}
            externalProviderOptions={externalProviderOptions}
            watch={watch}
          />
        );
      case WizardStep.Redirects:
        return (
          <RedirectsStep
            register={register}
            errors={errors}
            appType={appType}
          />
        );
      case WizardStep.Tokens:
        return (
            <TokensStep
              register={register}
              errors={errors}
              tokenType={tokenType}
              setTokenType={setTokenType}
              tokenTypeOptions={tokenTypeOptions}
              setValue={setValue}
              clearErrors={clearErrors}
              grantTypes={grantTypes}
            />
        );
      case WizardStep.Scopes:
        return (
          <ScopesStep
            scopeOptions={scopeOptions}
            scopes={scopes}
            toggleScope={toggleScope}
            clientAudience={clientAudience}
            setClientAudience={setClientAudience}
            setValue={setValue}
            register={register}
            grantTypes={grantTypes}
          />
        );
      case WizardStep.Protection:
        return (
          <ProtectionStep
            register={register}
            errors={errors}
            isValidTimeWindow={isValidTimeWindow}
          />
        );
      case WizardStep.Review:
      default:
        return (
          <ReviewStep
            values={getValues()}
            appTypeOptions={appTypeOptions}
            tokenTypeOptions={tokenTypeOptions}
            grantTypes={grantTypes}
            fallbackGrantTypes={fallbackGrantTypes}
            scopeOptions={scopeOptions}
            scopes={scopes}
            onEditStep={handleStepChange}
            stepIndexById={wizardSteps.reduce((acc, step, index) => {
              acc[step.id] = index;
              return acc;
            }, {})}
          />
        );
    }
  };

  return (
    <FormProvider {...methods}>
      <div className="card-surface form-surface">
        <WizardHeader
          stepIndex={stepIndex}
          steps={wizardSteps}
          onStepChange={handleStepChange}
        />
        {currentStep.id === WizardStep.BasicInfo && (
          <div className="wizard-info-banner" role="status">
            Next steps configure authentication, redirect URLs, token settings, and
            permissions. Incorrect configuration may affect security.
          </div>
        )}
        <form onSubmit={handleSubmit(submitForm)}>
          {mode === "add" && (
            <div className="row g-4 justify-content-center">
              <div className="col-12 col-lg-8 col-xl-7">
                <div className="wizard-status wizard-status-aligned mb-1">
                  <span
                    className="status-pill status-pill-warning"
                    title="Draft applications are not active until setup is complete."
                    aria-label="Draft applications are not active until setup is complete."
                  >
                    Status: Draft
                  </span>
                  <div className="wizard-status-help">
                    This application will not be usable until you complete setup and
                    activate it.
                  </div>
                </div>
              </div>
            </div>
          )}
          <div className="pt-2">{renderStep()}</div>
          <WizardFooter
            canGoBack={canGoBack}
            canGoNext={canGoNext}
            isLastStep={isLastStep}
            submitting={submitting}
            onBack={handleBack}
            onCancel={onCancel}
            onNext={handleNext}
            onSubmit={handleSubmit(submitForm)}
          />
        </form>
        <InfoModal
          open={infoOpen}
          title={infoContent.title}
          message={infoContent.message}
          onClose={() => setInfoOpen(false)}
        />
      </div>
    </FormProvider>
  );
}

ApplicationWizard.defaultProps = {
  initialValues: {},
  onSubmit: () => {},
  onCancel: () => {},
  submitting: false,
  lookups: {},
  mode: "add",
};

export default ApplicationWizard;
