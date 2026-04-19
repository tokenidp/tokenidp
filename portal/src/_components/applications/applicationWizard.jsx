import React, { useCallback, useEffect, useMemo, useState } from "react";
import { FormProvider, useForm } from "react-hook-form";
import InfoModal from "../common/infoModal";
import WizardHeader from "./wizard/WizardHeader";
import WizardFooter from "./wizard/WizardFooter";
import { WizardStep, wizardSteps } from "./wizard/wizardSteps";
import {
  fallbackAppTypes,
  fallbackScopes,
  GrantTypeId,
  isValidTimeWindow,
  createWizardState,
  normalizeGrantTypeOptions,
  normalizeLookupOptions,
  normalizeTimeWindow,
  normalizeValue,
} from "./wizard/wizardState";
import BasicInfoStep from "./wizard/steps/BasicInfoStep";
import AuthStep from "./wizard/steps/AuthStep";
import ConfigurationsStep from "./wizard/steps/ConfigurationsStep";
import ScopesStep from "./wizard/steps/ScopesStep";
import ProtectionStep from "./wizard/steps/ProtectionStep";
import ReviewStep from "./wizard/steps/ReviewStep";

const stepFields = {
  [WizardStep.BasicInfo]: ["clientName", "appType", "iconUrl"],
  [WizardStep.Auth]: ["redirectUri"],
  [WizardStep.Scopes]: [],
  [WizardStep.Configurations]: [
    "tokenType",
    "accessTokenLifetime",
    "authorizationCodeLifetime",
    "refreshTokenExpiration",
  ],
  [WizardStep.Protection]: ["permitLimit", "queueLimit", "timeWindow"],
  [WizardStep.Review]: [],
};

const normalizeGrantSelection = (value) => {
  if (!Array.isArray(value)) {
    return [GrantTypeId.AuthorizationCode];
  }

  const normalized = value
    .map((grant) => Number(grant))
    .filter((grant) => Number.isInteger(grant) && grant >= 0);

  return normalized.length ? normalized : [GrantTypeId.AuthorizationCode];
};

const isDeviceIotLabel = (label) => /device[\s_/-]*iot/i.test(String(label || ""));

const formatAppTypeLabel = (label) => {
  if (isDeviceIotLabel(label)) {
    return "Device/IOT";
  }
  return String(label || "");
};

const fallbackRefreshTokenDeliveryModes = [
  { value: "1", label: "Response" },
  { value: "2", label: "Cookie" },
  { value: "3", label: "Both" },
];

function ApplicationWizard({
  initialValues,
  onSubmit,
  onRegenerateSecret,
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
    normalizeGrantSelection(initialValues.grantTypes)
  );
  const [scopes, setScopes] = useState(
    Array.isArray(initialValues.scopes) ? initialValues.scopes : ["openid", "profile"]
  );
  const [selectedApiResources, setSelectedApiResources] = useState(
    Array.isArray(initialValues.apiResources) ? initialValues.apiResources : []
  );
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
    setGrantTypes(normalizeGrantSelection(initialValues.grantTypes));
    setScopes(
      Array.isArray(initialValues.scopes) ? initialValues.scopes : ["openid", "profile"]
    );
    setSelectedApiResources(
      Array.isArray(initialValues.apiResources) ? initialValues.apiResources : []
    );
  }, [initialValues, reset]);

  const appTypeOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.appTypes);
    const helperText = {
      0: "Browser-based apps (React, Angular). Uses PKCE. No client secret.",
      1: "Native mobile apps. Uses PKCE. No client secret.",
      2: "Installed desktop apps. Uses PKCE. No client secret.",
      3: "Server-rendered apps. Can securely store client secrets.",
      4: "Machine-to-machine services. Uses client credentials.",
      5: "Device and IoT apps use the Device Authorization flow with the device_code grant.",
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

  const refreshTokenDeliveryModeOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.refreshTokenDeliveryModes);
    if (normalized.length) {
      return normalized.map((option) => ({
        value: String(option.key ?? ""),
        label: String(option.value ?? ""),
      }));
    }

    return fallbackRefreshTokenDeliveryModes;
  }, [lookups?.refreshTokenDeliveryModes]);

  const availableGrantTypes = useMemo(
    () => normalizeGrantTypeOptions(lookups?.grantTypes),
    [lookups?.grantTypes]
  );

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

  const apiResourceOptions = useMemo(() => {
    return Array.isArray(lookups?.apiResources) ? lookups.apiResources : [];
  }, [lookups?.apiResources]);

  const scopeOwnerMap = useMemo(() => {
    const map = new Map();
    apiResourceOptions.forEach((apiResource) => {
      const resourceName = String(apiResource?.name ?? apiResource?.Name ?? "");
      const resourceScopes = apiResource?.scopes ?? apiResource?.Scopes ?? [];
      resourceScopes.forEach((scope) => {
        const scopeName = String(scope?.name ?? scope?.Name ?? "");
        if (scopeName) {
          map.set(scopeName, resourceName);
        }
      });
    });
    return map;
  }, [apiResourceOptions]);

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

  const externalAssignableRoleOptions = useMemo(() => {
    const normalized = normalizeLookupOptions(lookups?.roles);
    if (!normalized.length) {
      return [];
    }

    return normalized
      .map((option) => ({
        value: String(option.key ?? ""),
        label: String(option.value ?? ""),
      }))
      .filter((option) => option.value && option.label);
  }, [lookups?.roles]);

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
    if (isBackend) {
      return new Set([
        GrantTypeId.ClientCredentials,
        GrantTypeId.RefreshToken,
        GrantTypeId.Password,
      ]);
    }
    if (isWeb) {
      return new Set([
        GrantTypeId.AuthorizationCode,
        GrantTypeId.RefreshToken,
        GrantTypeId.Ciba,
        GrantTypeId.Password,
      ]);
    }
    if (isDesktop) {
      return new Set([
        GrantTypeId.AuthorizationCode,
        GrantTypeId.RefreshToken,
        GrantTypeId.DeviceCode,
        GrantTypeId.Password,
      ]);
    }
    if (isMobile) {
      return new Set([
        GrantTypeId.AuthorizationCode,
        GrantTypeId.RefreshToken,
        GrantTypeId.Password,
      ]);
    }
    if (isSpa) {
      return new Set([GrantTypeId.AuthorizationCode, GrantTypeId.RefreshToken]);
    }
    if (isDeviceIot) return new Set([GrantTypeId.DeviceCode]);
    return new Set(availableGrantTypes.map((grant) => grant.id));
  }, [availableGrantTypes, isBackend, isWeb, isDesktop, isMobile, isSpa, isDeviceIot]);

  const authGrantOptions = useMemo(() => {
    if (isBackend) {
      return availableGrantTypes.filter((grant) => allowedGrants.has(grant.id));
    }
    if (isDeviceIot) {
      return availableGrantTypes.filter((grant) => allowedGrants.has(grant.id));
    }
    return availableGrantTypes;
  }, [allowedGrants, availableGrantTypes, isBackend, isDeviceIot]);

  useEffect(() => {
    setGrantTypes((prev) => {
      let next = prev.filter((grant) => allowedGrants.has(grant));

      if (
        next.includes(GrantTypeId.RefreshToken) &&
        !next.includes(GrantTypeId.AuthorizationCode) &&
        !next.includes(GrantTypeId.Password)
      ) {
        next = next.filter((grant) => grant !== GrantTypeId.RefreshToken);
      }

      if (isDeviceIot && !next.includes(GrantTypeId.DeviceCode)) {
        next = [GrantTypeId.DeviceCode];
      } else if (isSpa && !next.includes(GrantTypeId.AuthorizationCode)) {
        next = Array.from(new Set([...next, GrantTypeId.AuthorizationCode]));
      } else if (!next.length) {
        next = isBackend
          ? [GrantTypeId.ClientCredentials]
          : [GrantTypeId.AuthorizationCode];
      }

      return next;
    });
  }, [allowedGrants, isBackend, isDeviceIot, isSpa]);

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

  const validateGrantSelection = useCallback(() => {
    const clientSecretValue = getValues("clientSecret");
    if ((isPublicClient || isDeviceIot) && clientSecretValue) {
      setGrantError("Public clients cannot have client secrets.");
      return false;
    }
    if (isPublicClient && grantTypes.includes(GrantTypeId.ClientCredentials)) {
      setGrantError("Public clients cannot use client_credentials.");
      return false;
    }
    if (
      (isSpa || isMobile || isDesktop) &&
      grantTypes.includes(GrantTypeId.ClientCredentials)
    ) {
      setGrantError("SPA, Mobile, and Desktop apps cannot use client_credentials.");
      return false;
    }
    if (grantTypes.includes(GrantTypeId.Password) && (isSpa || isDeviceIot)) {
      setGrantError("Password grant is supported for Mobile, Desktop, Web, and Backend clients.");
      return false;
    }
    if (
      grantTypes.includes(GrantTypeId.Ciba) &&
      (isPublicClient || isBackend || isDeviceIot)
    ) {
      setGrantError("CIBA is supported for Web applications only.");
      return false;
    }
    if (
      grantTypes.includes(GrantTypeId.DeviceCode) &&
      (isSpa || isWeb || isBackend)
    ) {
      setGrantError("Device Code is supported for Mobile, Desktop, and Device/IOT clients.");
      return false;
    }
    if (isDeviceIot && !grantTypes.includes(GrantTypeId.DeviceCode)) {
      setGrantError("Device/IOT clients must use device_code.");
      return false;
    }
    const unsupportedGrant = grantTypes.find((grant) => !allowedGrants.has(grant));
    if (unsupportedGrant !== undefined) {
      setGrantError("Selected grant type is not allowed for this application type.");
      return false;
    }
    if (isSpa && !grantTypes.includes(GrantTypeId.AuthorizationCode)) {
      setGrantError("SPA clients must use authorization_code with PKCE.");
      return false;
    }
    if (
      grantTypes.includes(GrantTypeId.RefreshToken) &&
      !grantTypes.includes(GrantTypeId.AuthorizationCode) &&
      !grantTypes.includes(GrantTypeId.Password)
    ) {
      setGrantError("refresh_token requires authorization_code or password.");
      return false;
    }
    setGrantError("");
    return true;
  }, [
    allowedGrants,
    getValues,
    grantTypes,
    isBackend,
    isDesktop,
    isDeviceIot,
    isMobile,
    isPublicClient,
    isSpa,
    isWeb,
  ]);

  useEffect(() => {
    if (grantError) {
      validateGrantSelection();
    }
  }, [grantError, validateGrantSelection]);

  const toggleGrant = (value) => {
    setGrantTypes((prev) => {
      if (
        value === GrantTypeId.AuthorizationCode &&
        prev.includes(GrantTypeId.AuthorizationCode) &&
        prev.includes(GrantTypeId.RefreshToken) &&
        !prev.includes(GrantTypeId.Password)
      ) {
        return prev.filter(
          (grant) =>
            grant !== GrantTypeId.AuthorizationCode && grant !== GrantTypeId.RefreshToken
        );
      }
      if (
        value === GrantTypeId.Password &&
        prev.includes(GrantTypeId.Password) &&
        prev.includes(GrantTypeId.RefreshToken) &&
        !prev.includes(GrantTypeId.AuthorizationCode)
      ) {
        return prev.filter(
          (grant) => grant !== GrantTypeId.Password && grant !== GrantTypeId.RefreshToken
        );
      }
      return prev.includes(value) ? prev.filter((g) => g !== value) : [...prev, value];
    });
  };

  const toggleScope = (value) => {
    const owner = scopeOwnerMap.get(value);
    if (owner && !selectedApiResources.includes(owner)) {
      const nextApiResources = [...selectedApiResources, owner];
      setSelectedApiResources(nextApiResources);
      setValue("apiResources", nextApiResources, {
        shouldDirty: true,
        shouldValidate: true,
      });
    }

    setScopes((prev) =>
      prev.includes(value) ? prev.filter((s) => s !== value) : [...prev, value]
    );
  };

  useEffect(() => {
    setScopes((prev) =>
      prev.filter((scope) => {
        const owner = scopeOwnerMap.get(scope);
        return !owner || selectedApiResources.includes(owner);
      })
    );
  }, [scopeOwnerMap, selectedApiResources]);

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

  const regenerateSecret = async () => {
    if (!onRegenerateSecret) {
      setInfoContent({
        title: "Save Required",
        message: "Save the application first, then regenerate the client secret from edit mode.",
      });
      setInfoOpen(true);
      return;
    }

    const currentExpiry = getValues("clientSecretExpiry");
    const payload = {
      clientSecretExpiry:
        currentExpiry === "" || currentExpiry === null || currentExpiry === undefined
          ? null
          : Number(currentExpiry),
    };

    const result = await onRegenerateSecret(payload);
    if (!result?.ok) {
      setInfoContent({
        title: "Unable to regenerate",
        message: "Client secret regeneration failed. Review the current client settings and try again.",
      });
      setInfoOpen(true);
      return;
    }

    const rotatedSecret =
      result.result?.clientSecret ?? result.result?.ClientSecret ?? "";
    const rotatedExpiry =
      result.result?.clientSecretExpiry ?? result.result?.ClientSecretExpiry ?? payload.clientSecretExpiry;

    setValue("clientSecret", rotatedSecret, {
      shouldDirty: true,
      shouldValidate: true,
    });

    if (rotatedExpiry === null || rotatedExpiry === undefined) {
      setValue("clientSecretExpiry", "", {
        shouldDirty: true,
        shouldValidate: true,
      });
    } else {
      setValue("clientSecretExpiry", String(rotatedExpiry), {
        shouldDirty: true,
        shouldValidate: true,
      });
    }

    setShowSecret(true);
    setInfoContent({
      title: "Client secret rotated",
      message: "The new client secret is now shown once. Copy it now because only the hash is stored.",
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
      apiResources: selectedApiResources,
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
              errors={errors}
              appType={appType}
              isPublicClient={isPublicClient}
              isDeviceIot={isDeviceIot}
              showSecret={showSecret}
              setShowSecret={setShowSecret}
            onRegenerateSecret={regenerateSecret}
            grantTypes={grantTypes}
              toggleGrant={toggleGrant}
              grantOptions={authGrantOptions}
              allowedGrants={allowedGrants}
              hasInsecureGrant={hasInsecureGrant}
              grantError={grantError}
            />
        );
      case WizardStep.Scopes:
        return (
          <ScopesStep
            scopeOptions={scopeOptions}
            apiResourceOptions={apiResourceOptions}
            scopes={scopes}
            toggleScope={toggleScope}
            selectedApiResources={selectedApiResources}
            setSelectedApiResources={setSelectedApiResources}
            setValue={setValue}
            register={register}
            grantTypes={grantTypes}
          />
        );
      case WizardStep.Configurations:
        return (
          <ConfigurationsStep
              register={register}
              watch={watch}
              setValue={setValue}
              errors={errors}
              tokenType={tokenType}
              setTokenType={setTokenType}
            tokenTypeOptions={tokenTypeOptions}
            refreshTokenDeliveryModeOptions={refreshTokenDeliveryModeOptions}
            clearErrors={clearErrors}
            grantTypes={grantTypes}
            externalProviderOptions={externalProviderOptions}
              externalRoleOptions={externalAssignableRoleOptions}
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
            refreshTokenDeliveryModeOptions={refreshTokenDeliveryModeOptions}
            grantTypes={grantTypes}
            grantOptions={availableGrantTypes}
              scopeOptions={scopeOptions}
              apiResourceOptions={apiResourceOptions}
              externalProviderOptions={externalProviderOptions}
              externalRoleOptions={externalAssignableRoleOptions}
              scopes={scopes}
              selectedApiResources={selectedApiResources}
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
          <div className="row g-4 justify-content-center">
            <div className="col-12 col-xl-10">
              <div className="wizard-info-banner" role="status">
                Next steps configure authentication, redirect URLs, token settings, and
                permissions. Incorrect configuration may affect security.
              </div>
            </div>
          </div>
        )}
        <form onSubmit={handleSubmit(submitForm)}>
          {mode === "add" && (
            <div className="row g-4 justify-content-center">
              <div className="col-12 col-xl-10">
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
  onRegenerateSecret: null,
  onCancel: () => {},
  submitting: false,
  lookups: {},
  mode: "add",
};

export default ApplicationWizard;
