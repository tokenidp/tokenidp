import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import ApplicationWizard from "./applicationWizard";
import Breadcrumbs from "../common/breadcrumbs";
import { useApplications } from "../../_hooks/useApplications";

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
  clientAudience: "",
};

function ApplicationEdit() {
  const { id } = useParams();
  const navigate = useNavigate();
  const {
    state,
    loadLookups,
    getApplicationById,
    updateApplication,
    clearStatus,
  } = useApplications();
  const [initialValues, setInitialValues] = useState(emptyValues);

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    if (!id) return;
    const loadClient = async () => {
      const data = await getApplicationById(id);
      if (!data) return;

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
        clientAudience:
          data.clientAudience ??
          data.ClientAudience ??
          (data.audiences ?? data.Audiences ?? [""])[0],
      });
    };

    loadClient();
  }, [getApplicationById, id]);

  const handleSubmit = async (data) => {
    const payload = {
      id: Number(id),
      clientId: data.clientId.trim(),
      clientName: data.clientName.trim(),
      description: data.description?.trim() || null,
      appType: Number(data.appType),
      accessTokenType: Number(data.tokenType),
      redirectUri: data.redirectUri.trim(),
      logoutRedirectUri: data.logoutRedirectUri?.trim() || null,
      isActive: !!data.isActive,
      clientSecretExpiry: data.clientSecretExpiry || null,
      accessTokenLifetime: Number(data.accessTokenLifetime),
      authorizationCodeLifetime: Number(data.authorizationCodeLifetime),
      refreshTokenExpiration: Number(data.refreshTokenExpiration),
      permitLimit: data.permitLimit === "" ? null : Number(data.permitLimit),
      timeWindow: data.timeWindow || null,
      queueLimit: data.queueLimit === "" ? null : Number(data.queueLimit),
      enableITracking: !!data.enableITracking,
      scopes: data.scopes || [],
      grantTypes: data.grantTypes || [],
      audiences: data.audiences || [],
      clientSecret: data.clientSecret || null,
      clientSecretDescription: null,
    };

    const result = await updateApplication(id, payload);
    if (result.ok) {
      clearStatus();
      navigate("/applications");
    }
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Edit Application</h5>
          <Breadcrumbs
            className="app-breadcrumb mb-2"
            appendLabel={id ? `Editing client: ${id}` : ""}
          />
        </div>
      </div>

      <ApplicationWizard
        initialValues={initialValues}
        onSubmit={handleSubmit}
        onCancel={() => navigate("/applications")}
        submitting={state.loading}
        lookups={state}
        mode="edit"
      />
    </div>
  );
}

export default ApplicationEdit;
