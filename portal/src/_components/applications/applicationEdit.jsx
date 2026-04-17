import React, { useEffect, useState } from "react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import ApplicationWizard from "./applicationWizard";
import Breadcrumbs from "../common/breadcrumbs";
import { useApplications } from "../../_hooks/useApplications";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const emptyValues = {
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
  permitLimit: "",
  timeWindow: "",
  queueLimit: "",
  enableITracking: false,
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

function ApplicationEdit() {
  const { clientKey } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const {
    state,
    loadLookups,
    getApplicationById,
    resolveApplicationIdByClientId,
    updateApplication,
    regenerateClientSecret,
    clearStatus,
  } = useApplications();
  const [initialValues, setInitialValues] = useState(emptyValues);
  const { setSuccess } = useGlobalSuccess();
  const [applicationId, setApplicationId] = useState(
    location?.state?.id ?? null
  );
  const decodedClientKey = decodeURIComponent(clientKey || "");

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    const resolveAndLoad = async () => {
      let resolvedId = applicationId;
      if (!resolvedId && decodedClientKey) {
        resolvedId = await resolveApplicationIdByClientId(decodedClientKey);
        if (resolvedId) {
          setApplicationId(resolvedId);
        }
      }

      if (!resolvedId) return;

      const data = await getApplicationById(resolvedId);
      if (!data) return;

      const externalProviders = (
        data.externalProviders ??
        data.ExternalProviders ??
        data.externalProviderIds ??
        data.ExternalProviderIds ??
        []
      )
        .map((value) => Number(value))
        .filter((value) => Number.isFinite(value) && value > 0)
        .map((value) => String(value));

      setInitialValues({
        clientName: data.clientName ?? data.ClientName ?? "",
        clientId: data.clientId ?? data.ClientId ?? "",
        description: data.description ?? data.Description ?? "",
        appType: String(data.appType ?? data.AppType ?? ""),
        tokenType: String(
          data.tokenType ?? data.TokenType ?? data.accessTokenType ?? data.AccessTokenType ?? ""
        ),
        redirectUri: data.redirectUri ?? data.RedirectUri ?? "",
        logoutRedirectUri: data.logoutRedirectUri ?? data.LogoutRedirectUri ?? "",
        isActive: data.isActive ?? data.IsActive ?? true,
        clientSecretExpiry: data.clientSecretExpiry ?? data.ClientSecretExpiry ?? "",
        accessTokenLifetime: data.accessTokenLifetime ?? data.AccessTokenLifetime ?? 60,
        authorizationCodeLifetime:
          data.authorizationCodeLifetime ?? data.AuthorizationCodeLifetime ?? 5,
        refreshTokenExpiration:
          data.refreshTokenExpiration ?? data.RefreshTokenExpiration ?? 30,
        permitLimit: data.permitLimit ?? data.PermitLimit ?? "",
        timeWindow: data.timeWindow ?? data.TimeWindow ?? "",
        queueLimit: data.queueLimit ?? data.QueueLimit ?? "",
        enableITracking: data.enableITracking ?? data.EnableITracking ?? false,
        grantTypes: data.grantTypes ?? data.GrantTypes ?? [0],
        scopes: data.scopes ?? data.Scopes ?? ["openid"],
        apiResources:
          data.apiResources ??
          data.ApiResources ??
          data.audiences ??
          data.Audiences ??
          [],
        authPolicy: {
          allowLocalLoginOverride:
            data.authPolicy?.allowLocalLoginOverride ??
            data.authPolicy?.AllowLocalLoginOverride ??
            data.AuthPolicy?.allowLocalLoginOverride ??
            data.AuthPolicy?.AllowLocalLoginOverride ??
            false,
          allowSelfRegistrationOverride:
            data.authPolicy?.allowSelfRegistrationOverride ??
            data.authPolicy?.AllowSelfRegistrationOverride ??
            data.AuthPolicy?.allowSelfRegistrationOverride ??
            data.AuthPolicy?.AllowSelfRegistrationOverride ??
            false,
          mfaPolicyOverride:
            data.authPolicy?.mfaPolicyOverride ??
            data.authPolicy?.MfaPolicyOverride ??
            data.AuthPolicy?.mfaPolicyOverride ??
            data.AuthPolicy?.MfaPolicyOverride ??
            false,
          showExternalProviders:
            data.authPolicy?.showExternalProviders ??
            data.authPolicy?.ShowExternalProviders ??
            data.AuthPolicy?.showExternalProviders ??
            data.AuthPolicy?.ShowExternalProviders ??
            true,
          showStaySignedIn:
            data.authPolicy?.showStaySignedIn ??
            data.authPolicy?.ShowStaySignedIn ??
            data.AuthPolicy?.showStaySignedIn ??
            data.AuthPolicy?.ShowStaySignedIn ??
            false,
          showCreateAccountLink:
            data.authPolicy?.showCreateAccountLink ??
            data.authPolicy?.ShowCreateAccountLink ??
            data.AuthPolicy?.showCreateAccountLink ??
            data.AuthPolicy?.ShowCreateAccountLink ??
            false,
          autoCreateUsers:
            data.authPolicy?.autoCreateUsers ??
            data.authPolicy?.AutoCreateUsers ??
            data.AuthPolicy?.autoCreateUsers ??
            data.AuthPolicy?.AutoCreateUsers ??
            data.autoCreateUsers ??
            data.AutoCreateUsers ??
            true,
          defaultRoleId:
            data.authPolicy?.defaultRoleId ??
            data.authPolicy?.DefaultRoleId ??
            data.AuthPolicy?.defaultRoleId ??
            data.AuthPolicy?.DefaultRoleId ??
            data.defaultRoleId ??
            data.DefaultRoleId ??
            "",
        },
        externalProviders,
      });
    };

    resolveAndLoad();
  }, [
    applicationId,
    decodedClientKey,
    getApplicationById,
    resolveApplicationIdByClientId,
  ]);

  const handleSubmit = async (data) => {
    if (!applicationId) {
      return;
    }

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
      id: Number(applicationId),
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
      permitLimit: data.permitLimit === "" ? null : Number(data.permitLimit),
      timeWindow: data.timeWindow || null,
      queueLimit: data.queueLimit === "" ? null : Number(data.queueLimit),
      enableITracking: !!data.enableITracking,
      scopes: data.scopes || [],
      apiResources: data.apiResources || [],
      grantTypes: data.grantTypes || [],
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

    const result = await updateApplication(applicationId, payload);
    if (result.ok) {
      clearStatus();
      setSuccess({
        title: "Application updated",
        message: "Application updated successfully.",
      });
      navigate("/applications");
    }
  };

  const handleRegenerateSecret = async (payload) => {
    if (!applicationId) {
      return { ok: false };
    }

    return regenerateClientSecret(Number(applicationId), payload);
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Edit Application</h5>
          <Breadcrumbs
            className="app-breadcrumb mb-2"
            appendLabel={decodedClientKey ? `Editing client: ${decodedClientKey}` : ""}
          />
        </div>
      </div>

      <ApplicationWizard
        initialValues={initialValues}
        onSubmit={handleSubmit}
        onRegenerateSecret={handleRegenerateSecret}
        onCancel={() => navigate("/applications")}
        submitting={state.loading}
        lookups={state}
        mode="edit"
      />
    </div>
  );
}

export default ApplicationEdit;
