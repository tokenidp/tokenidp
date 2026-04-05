import React, { useMemo } from "react";

const getField = (item, ...keys) =>
  keys.find((key) => item?.[key] !== undefined) !== undefined
    ? item[keys.find((key) => item?.[key] !== undefined)]
    : undefined;

const normalizeJson = (value) => {
  if (!value) {
    return "";
  }
  if (typeof value === "string") {
    try {
      return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
      return value;
    }
  }
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return String(value);
  }
};

const formatLocalDateTime = (value) => {
  if (!value) {
    return "-";
  }
  const raw = String(value);
  const hasTimezone = /[zZ]$|[+-]\d{2}:\d{2}$/.test(raw);
  const iso = hasTimezone ? raw : `${raw}Z`;
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
  const get = (type) => parts.find((part) => part.type === type)?.value || "";
  return `${get("month")} ${get("day")}, ${get("year")} ${get("hour")}:${get("minute")} ${get("dayPeriod")}`.trim();
};

function TokenInspectModal({ open, token, onClose }) {

  const tokenId = useMemo(
    () => getField(token, "tokenId", "TokenId", "tokenHash", "TokenHash"),
    [token]
  );
  const tokenType = useMemo(
    () => getField(token, "tokenType", "TokenType"),
    [token]
  );
  const clientName = useMemo(
    () => getField(token, "clientName", "ClientName"),
    [token]
  );
  const clientId = useMemo(
    () => getField(token, "clientId", "ClientId"),
    [token]
  );
  const userName = useMemo(
    () => getField(token, "userName", "UserName"),
    [token]
  );
  const subject = useMemo(
    () => getField(token, "subject", "Subject"),
    [token]
  );
  const issuedAt = useMemo(
    () => getField(token, "issuedAt", "IssuedAt"),
    [token]
  );
  const expiresAt = useMemo(
    () => getField(token, "expiresAt", "ExpiresAt"),
    [token]
  );
  const rawStatus = useMemo(
    () => getField(token, "status", "Status"),
    [token]
  );
  const status = useMemo(() => {
    const map = {
      0: "Active",
      1: "Expired",
      2: "Revoked",
      3: "Compromised",
      4: "Suspended",
    };
    return typeof rawStatus === "number"
      ? map[rawStatus] || "Unknown"
      : map[Number(rawStatus)] || rawStatus || "Unknown";
  }, [rawStatus]);
  const scopes = useMemo(
    () => getField(token, "scopes", "Scopes"),
    [token]
  );
  const audience = useMemo(
    () => getField(token, "audience", "Audience"),
    [token]
  );
  const claimsJson = useMemo(
    () => normalizeJson(getField(token, "claimsJson", "ClaimsJson", "claims")),
    [token]
  );
  const metadataJson = useMemo(
    () => normalizeJson(getField(token, "metadataJson", "MetadataJson", "metadata")),
    [token]
  );
  const issuedByIp = useMemo(
    () => getField(token, "issuedByIp", "IssuedByIp"),
    [token]
  );
  const issuedUserAgent = useMemo(
    () => getField(token, "issuedUserAgent", "IssuedUserAgent"),
    [token]
  );
  const issuedBy = useMemo(
    () => getField(token, "issuedBy", "IssuedBy"),
    [token]
  );
  const revokedAt = useMemo(
    () => getField(token, "revokedAt", "RevokedAt"),
    [token]
  );
  const revokedBy = useMemo(
    () => getField(token, "revokedBy", "RevokedBy"),
    [token]
  );
  const revokedByIp = useMemo(
    () => getField(token, "revokedByIp", "RevokedByIp"),
    [token]
  );
  const revokedReason = useMemo(
    () => getField(token, "revokedReason", "RevokedReason"),
    [token]
  );

  if (!open) {
    return null;
  }

  return (
    <>
      <div className="modal fade show d-block" tabIndex="-1" role="dialog">
        <div
          className="modal-dialog modal-dialog-centered modal-xl token-inspect-modal"
          role="document"
        >
          <div className="modal-content">
            <div className="modal-header">
              <h5 className="modal-title">Inspect Token</h5>
              <button
                type="button"
                className="btn-close"
                aria-label="Close"
                onClick={onClose}
              ></button>
            </div>
            <div className="modal-body">
              <div className="token-section">
                <h6 className="mb-3">Summary</h6>
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="text-muted small">Token ID</div>
                    <div className="fw-semibold">{tokenId || "tok_****"}</div>
                </div>
                <div className="col-md-3">
                  <div className="text-muted small">Type</div>
                  <div className="fw-semibold">{tokenType || "-"}</div>
                </div>
                <div className="col-md-3">
                  <div className="text-muted small">Status</div>
                  <div className="fw-semibold">{status || "-"}</div>
                </div>
                <div className="col-md-6">
                  <div className="text-muted small">Client</div>
                  <div className="fw-semibold">
                    {clientName ? `${clientName} (${clientId || "-"})` : clientId || "-"}
                  </div>
                </div>
                <div className="col-md-6">
                  <div className="text-muted small">User</div>
                  <div className="fw-semibold">{userName || subject || "-"}</div>
                </div>
                <div className="col-md-6">
                  <div className="text-muted small">Issued At</div>
                  <div className="fw-semibold">{formatLocalDateTime(issuedAt)}</div>
                </div>
                  <div className="col-md-6">
                    <div className="text-muted small">Expires At</div>
                    <div className="fw-semibold">{formatLocalDateTime(expiresAt)}</div>
                  </div>
                </div>
              </div>

              <div className="token-section">
                <h6 className="mb-3">Scopes &amp; Audience</h6>
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="text-muted small">Scopes</div>
                    <div className="fw-semibold">{scopes || "-"}</div>
                  </div>
                  <div className="col-md-6">
                    <div className="text-muted small">Audience</div>
                    <div className="fw-semibold">{audience || "-"}</div>
                  </div>
                </div>
              </div>

              <div className="token-section">
                <h6 className="mb-3">Claims</h6>
                <pre className="bg-light p-3 rounded small mb-0">
                  {claimsJson || "No claims available."}
                </pre>
              </div>

              <div className="token-section">
                <h6 className="mb-3">Metadata</h6>
                <pre className="bg-light p-3 rounded small mb-0">
                  {metadataJson || "No metadata available."}
                </pre>
              </div>

              <div className="token-section">
                <h6 className="mb-3">Audit</h6>
                <div className="row g-3">
                  <div className="col-md-6">
                    <div className="text-muted small">Issued By</div>
                    <div className="fw-semibold">{issuedBy || "-"}</div>
                  </div>
                  <div className="col-md-6">
                    <div className="text-muted small">Issued IP</div>
                    <div className="fw-semibold">{issuedByIp || "-"}</div>
                  </div>
                  <div className="col-md-12">
                    <div className="text-muted small">User Agent</div>
                    <div className="fw-semibold">{issuedUserAgent || "-"}</div>
                  </div>
                  <div className="col-md-4">
                    <div className="text-muted small">Revoked At</div>
                    <div className="fw-semibold">{revokedAt || "-"}</div>
                  </div>
                  <div className="col-md-4">
                    <div className="text-muted small">Revoked By</div>
                    <div className="fw-semibold">{revokedBy || "-"}</div>
                  </div>
                  <div className="col-md-4">
                    <div className="text-muted small">Revoked IP</div>
                    <div className="fw-semibold">{revokedByIp || "-"}</div>
                  </div>
                  <div className="col-md-12">
                    <div className="text-muted small">Revoke Reason</div>
                    <div className="fw-semibold">{revokedReason || "-"}</div>
                  </div>
                </div>
              </div>
            </div>
            <div className="modal-footer">
              <button type="button" className="btn btn-primary" onClick={onClose}>
                Close
              </button>
            </div>
          </div>
        </div>
      </div>
      <div className="modal-backdrop fade show"></div>
    </>
  );
}

export default TokenInspectModal;
