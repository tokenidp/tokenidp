import React, { useEffect, useMemo, useState } from "react";
import { useAuth } from "tokenidp-react";
import Breadcrumbs from "../common/breadcrumbs";
import ConfirmModal from "../common/confirmModal";
import InfoModal from "../common/infoModal";
import useCibaRequests from "../../_hooks/useCibaRequests";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

const getField = (item, ...keys) => {
  const key = keys.find((candidate) => item?.[candidate] !== undefined);
  return key ? item[key] : undefined;
};

const normalizePermissions = (source) => {
  if (!Array.isArray(source)) {
    return [];
  }

  return source
    .map(
      (item) =>
        item?.permissionKey ||
        item?.PermissionKey ||
        item?.key ||
        item?.Key ||
        "",
    )
    .filter(Boolean)
    .map((value) => String(value).toLowerCase());
};

const formatLocalDateTime = (value) => {
  if (!value) {
    return "-";
  }

  const raw = String(value);
  const normalized = raw.replace(/\.\d+(?=z$|Z$)/, "");
  const hasTimezone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(normalized);
  const iso = hasTimezone ? normalized : `${normalized}Z`;
  const parsed = new Date(iso);
  if (Number.isNaN(parsed.getTime())) {
    return raw;
  }

  const parts = new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: true,
  }).formatToParts(parsed);
  const read = (type) =>
    parts.find((part) => part.type === type)?.value || "";

  return `${read("month")} ${read("day")}, ${read("year")} ${read("hour")}:${read("minute")} ${read("dayPeriod")}`.trim();
};

function CibaRequestsList() {
  const user = useAuth();
  const { setSuccess } = useGlobalSuccess();
  const { state, loadPending, approveRequest, denyRequest } = useCibaRequests();
  const [pendingAction, setPendingAction] = useState(null);
  const [denyReason, setDenyReason] = useState("");
  const [infoState, setInfoState] = useState({
    open: false,
    title: "",
    message: "",
  });

  const permissionKeys = useMemo(
    () => normalizePermissions(user?.permissions || user?.Permissions),
    [user?.Permissions, user?.permissions],
  );
  const canApprove = permissionKeys.includes("ciba.approve");
  const canDeny = permissionKeys.includes("ciba.deny");

  useEffect(() => {
    loadPending().catch(() => {});
  }, [loadPending]);

  const closeActionModal = () => {
    setPendingAction(null);
    setDenyReason("");
  };

  const handleApprove = async () => {
    if (!pendingAction?.id) {
      return;
    }

    try {
      await approveRequest(pendingAction.id);
      setSuccess({
        title: "CIBA request approved",
        message: "The pending backchannel authentication request has been approved.",
      });
      closeActionModal();
      await loadPending();
    } catch (error) {
      setInfoState({
        open: true,
        title: "Approval failed",
        message:
          error?.message ||
          "The request could not be approved. Refresh the queue and try again.",
      });
    }
  };

  const handleDeny = async () => {
    if (!pendingAction?.id) {
      return;
    }

    try {
      await denyRequest(pendingAction.id, denyReason);
      setSuccess({
        title: "CIBA request denied",
        message: "The pending backchannel authentication request has been denied.",
      });
      closeActionModal();
      await loadPending();
    } catch (error) {
      setInfoState({
        open: true,
        title: "Deny failed",
        message:
          error?.message ||
          "The request could not be denied. Refresh the queue and try again.",
      });
    }
  };

  const items = Array.isArray(state.items) ? state.items : [];

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">CIBA Requests</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
        <div className="page-header-actions">
          <button
            type="button"
            className="btn btn-outline-secondary"
            onClick={() => loadPending()}
            disabled={state.loading}
          >
            <i className="fa fa-rotate-right me-2"></i>
            Refresh
          </button>
        </div>
      </div>

      <div className="card-surface applications-card">
        <div className="alert alert-info mb-4">
          This queue shows pending CIBA approval requests for the currently signed-in user only.
        </div>

        {state.loading && !state.hasLoaded ? (
          <div className="text-center py-5">Loading pending requests...</div>
        ) : items.length === 0 ? (
          <div className="ciba-empty-state">
            <div className="fw-semibold mb-2">No pending CIBA requests</div>
            <div className="text-muted">
              New backchannel authentication requests will appear here when they are created for your account.
            </div>
          </div>
        ) : (
          <div className="ciba-queue">
            {items.map((item) => {
              const id = getField(item, "id", "Id");
              const clientId = getField(item, "clientId", "ClientId") || "-";
              const clientName =
                getField(item, "clientName", "ClientName") || clientId;
              const requestedScopes =
                getField(item, "requestedScopes", "RequestedScopes") || "-";
              const bindingMessage =
                getField(item, "bindingMessage", "BindingMessage") || "-";
              const createdAt =
                getField(item, "createdAtUtc", "CreatedAtUtc") || null;
              const expiresAt =
                getField(item, "expiresAtUtc", "ExpiresAtUtc") || null;
              const isActing = state.actingId === id;

              return (
                <div key={id} className="ciba-card">
                  <div className="ciba-card-main">
                    <div className="ciba-card-title-row">
                      <div>
                        <div className="ciba-card-title">{clientName}</div>
                        <div className="ciba-card-subtitle">
                          Client ID: <code>{clientId}</code>
                        </div>
                      </div>
                      <span className="status-pill status-pill-warning">
                        Pending
                      </span>
                    </div>

                    <div className="ciba-card-meta">
                      <div>
                        <div className="ciba-meta-label">Scopes</div>
                        <div className="ciba-meta-value">{requestedScopes}</div>
                      </div>
                      <div>
                        <div className="ciba-meta-label">Binding Message</div>
                        <div className="ciba-meta-value">{bindingMessage}</div>
                      </div>
                      <div>
                        <div className="ciba-meta-label">Created</div>
                        <div className="ciba-meta-value">
                          {formatLocalDateTime(createdAt)}
                        </div>
                      </div>
                      <div>
                        <div className="ciba-meta-label">Expires</div>
                        <div className="ciba-meta-value">
                          {formatLocalDateTime(expiresAt)}
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="ciba-card-actions">
                    <button
                      type="button"
                      className="btn btn-success"
                      disabled={!canApprove || isActing}
                      onClick={() =>
                        setPendingAction({
                          type: "approve",
                          id,
                          clientName,
                        })
                      }
                    >
                      {isActing && state.actingAction === "approve" ? "Approving..." : "Approve"}
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-danger"
                      disabled={!canDeny || isActing}
                      onClick={() =>
                        setPendingAction({
                          type: "deny",
                          id,
                          clientName,
                        })
                      }
                    >
                      {isActing && state.actingAction === "deny" ? "Denying..." : "Deny"}
                    </button>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <ConfirmModal
        open={pendingAction?.type === "approve"}
        title="Approve CIBA Request"
        message={`Approve the pending request for ${pendingAction?.clientName || "this client"}?`}
        confirmLabel="Approve"
        onConfirm={handleApprove}
        onClose={closeActionModal}
      />

      {pendingAction?.type === "deny" && (
        <>
          <div className="modal fade show d-block" tabIndex="-1" role="dialog">
            <div className="modal-dialog modal-dialog-centered" role="document">
              <div className="modal-content">
                <div className="modal-header">
                  <h5 className="modal-title">Deny CIBA Request</h5>
                  <button
                    type="button"
                    className="btn-close"
                    aria-label="Close"
                    onClick={closeActionModal}
                  ></button>
                </div>
                <div className="modal-body">
                  <p className="mb-3">
                    Deny the pending request for {pendingAction.clientName || "this client"}.
                  </p>
                  <label className="form-label" htmlFor="ciba-deny-reason">
                    Reason (optional)
                  </label>
                  <textarea
                    id="ciba-deny-reason"
                    className="form-control"
                    rows="3"
                    value={denyReason}
                    onChange={(event) => setDenyReason(event.target.value)}
                    placeholder="Optional note for the denial"
                  />
                </div>
                <div className="modal-footer">
                  <button
                    type="button"
                    className="btn btn-light"
                    onClick={closeActionModal}
                  >
                    Cancel
                  </button>
                  <button
                    type="button"
                    className="btn btn-danger"
                    onClick={handleDeny}
                  >
                    Deny
                  </button>
                </div>
              </div>
            </div>
          </div>
          <div className="modal-backdrop fade show"></div>
        </>
      )}

      <InfoModal
        open={infoState.open}
        title={infoState.title}
        message={infoState.message}
        onClose={() =>
          setInfoState({
            open: false,
            title: "",
            message: "",
          })
        }
      />
    </div>
  );
}

export default CibaRequestsList;
