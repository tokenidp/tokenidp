import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import ApplicationWizard from "./applicationWizard";
import { useApplications } from "../../_hooks/useApplications";
import Breadcrumbs from "../common/breadcrumbs";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";
import { GrantTypeId } from "./wizard/wizardState";

const defaultValues = {
  clientName: "",
  clientId: "",
  description: "",
  appType: "",
  tokenType: "",
  redirectUri: "",
  logoutRedirectUri: "",
  isActive: true,
  clientSecretExpiry: "",
  accessTokenLifetime: 60,
  authorizationCodeLifetime: 5,
  refreshTokenExpiration: 30,
  refreshTokenDeliveryMode: 1,
  permitLimit: "",
  timeWindow: "",
  queueLimit: "",
  enableITracking: false,
  cibaEnabled: false,
  backchannelTokenDeliveryMode: 0,
  cibaDefaultExpirySeconds: 300,
  cibaMinIntervalSeconds: 5,
  requireCibaUserCode: false,
  allowCibaLoginHint: true,
  allowCibaLoginHintToken: false,
  allowCibaIdTokenHint: false,
  grantTypes: [0],
  scopes: ["openid", "profile"],
  apiResources: [],
  authPolicy: {
    allowLocalLoginOverride: false,
    allowSelfRegistrationOverride: false,
    mfaPolicyOverride: false,
    showExternalProviders: true,
    showStaySignedIn: false,
    showCreateAccountLink: false,
    autoCreateUsers: true,
    defaultRoleId: "",
  },
  externalProviders: [],
};

function ApplicationCreate() {
  const navigate = useNavigate();
  const { state, createApplication, clearStatus, loadLookups } = useApplications();
  const { setSuccess } = useGlobalSuccess();

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  const handleSubmit = async (data) => {
    const selectedGrantTypes = Array.isArray(data.grantTypes)
      ? data.grantTypes.map((value) => Number(value)).filter((value) => Number.isInteger(value))
      : [];
    const cibaEnabled = !!data.cibaEnabled || selectedGrantTypes.includes(GrantTypeId.Ciba);
    const selectedProviderIds = Array.isArray(data.externalProviders)
      ? data.externalProviders
          .map((value) => Number(value))
          .filter((value) => Number.isFinite(value) && value > 0)
      : [];

    const autoCreateUsers =
      data.authPolicy?.autoCreateUsers === undefined ? true : !!data.authPolicy?.autoCreateUsers;
    const defaultRoleRaw = data.authPolicy?.defaultRoleId;
    const parsedDefaultRoleId =
      defaultRoleRaw === "" || defaultRoleRaw === null || defaultRoleRaw === undefined
        ? null
        : Number(defaultRoleRaw);
    const defaultRoleId =
      parsedDefaultRoleId !== null && Number.isFinite(parsedDefaultRoleId) && parsedDefaultRoleId > 0
        ? parsedDefaultRoleId
        : null;
    const clientSecretExpiry =
      data.clientSecretExpiry === "" || data.clientSecretExpiry === null || data.clientSecretExpiry === undefined
        ? null
        : Number(data.clientSecretExpiry);

    const payload = {
      id: 0,
      clientId: data.clientId.trim(),
      clientName: data.clientName.trim(),
      description: data.description?.trim() || null,
      appType: Number(data.appType),
      accessTokenType: Number(data.tokenType),
      redirectUri: String(data.redirectUri ?? "").trim(),
      logoutRedirectUri: String(data.logoutRedirectUri ?? "").trim() || null,
      isActive: !!data.isActive,
      clientSecretExpiry:
        clientSecretExpiry !== null && Number.isFinite(clientSecretExpiry)
          ? clientSecretExpiry
          : null,
      accessTokenLifetime: Number(data.accessTokenLifetime),
      authorizationCodeLifetime: Number(data.authorizationCodeLifetime),
      refreshTokenExpiration: Number(data.refreshTokenExpiration),
      refreshTokenDeliveryMode: Number(data.refreshTokenDeliveryMode ?? 1),
      permitLimit: data.permitLimit === "" ? null : Number(data.permitLimit),
      timeWindow: data.timeWindow || null,
      queueLimit: data.queueLimit === "" ? null : Number(data.queueLimit),
      enableITracking: !!data.enableITracking,
      cibaEnabled,
      backchannelTokenDeliveryMode: Number(data.backchannelTokenDeliveryMode ?? 0),
      cibaDefaultExpirySeconds: Number(data.cibaDefaultExpirySeconds ?? 300),
      cibaMinIntervalSeconds: Number(data.cibaMinIntervalSeconds ?? 5),
      requireCibaUserCode: !!data.requireCibaUserCode,
      allowCibaLoginHint:
        data.allowCibaLoginHint === undefined ? true : !!data.allowCibaLoginHint,
      allowCibaLoginHintToken: !!data.allowCibaLoginHintToken,
      allowCibaIdTokenHint: !!data.allowCibaIdTokenHint,
      scopes: data.scopes || [],
      apiResources: data.apiResources || [],
      grantTypes: selectedGrantTypes,
      clientSecret: data.clientSecret || null,
      clientSecretDescription: null,
      authPolicy: {
        allowLocalLoginOverride: !!data.authPolicy?.allowLocalLoginOverride,
        allowSelfRegistrationOverride: !!data.authPolicy?.allowSelfRegistrationOverride,
        mfaPolicyOverride: !!data.authPolicy?.mfaPolicyOverride,
        showExternalProviders: !!data.authPolicy?.showExternalProviders,
        showStaySignedIn: !!data.authPolicy?.showStaySignedIn,
        showCreateAccountLink: !!data.authPolicy?.showCreateAccountLink,
        autoCreateUsers: autoCreateUsers,
        defaultRoleId:
          !!data.authPolicy?.showExternalProviders && autoCreateUsers ? defaultRoleId : null,
      },
      externalProviders: !!data.authPolicy?.showExternalProviders
        ? selectedProviderIds
        : [],
    };

    const result = await createApplication(payload);
    if (result.ok) {
      clearStatus();
      setSuccess({
        title: "Application saved",
        message: "Application created successfully.",
      });
      navigate("/applications");
    }
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Register New OAuth Client</h5>
          <Breadcrumbs className="app-breadcrumb mb-2" />
        </div>
      </div>

      <ApplicationWizard
        initialValues={defaultValues}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/applications")}
        submitting={state.loading}
        lookups={state}
        mode="add"
      />
    </div>
  );
}

export default ApplicationCreate;
