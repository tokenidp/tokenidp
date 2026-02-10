import React, { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import ApplicationWizard from "./applicationWizard";
import { useApplications } from "../../_hooks/useApplications";
import Breadcrumbs from "../common/breadcrumbs";

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
  permitLimit: "",
  timeWindow: "",
  queueLimit: "",
  enableITracking: false,
  grantTypes: [0],
  scopes: ["openid", "profile"],
  clientAudience: "",
};

function ApplicationCreate() {
  const navigate = useNavigate();
  const { state, createApplication, clearStatus, loadLookups } = useApplications();

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  const handleSubmit = async (data) => {
    const payload = {
      id: 0,
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

    const result = await createApplication(payload);
    if (result.ok) {
      clearStatus();
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

      <div className="card-surface">
        <ApplicationWizard
          initialValues={defaultValues}
          onSubmit={handleSubmit}
          onCancel={() => navigate("/applications")}
          submitting={state.loading}
          lookups={state}
          mode="add"
        />
      </div>
    </div>
  );
}

export default ApplicationCreate;
