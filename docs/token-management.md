# Token Inventory Dashboard - Design + Implementation Notes

This module implements a read-optimized Token Inventory Dashboard for high-scale OAuth 2.1 / OIDC admin usage.

## API contracts

Base path: `/admin`

### List tokens

`POST /admin/token/list`

Request body (SearchData):
```json
{
  "pageNumber": 1,
  "pageSize": 25,
  "sortColumn": "IssuedAt",
  "sortOrder": "desc",
  "searchAll": false,
  "searchCriterias": [
    { "columnName": "Search", "value": "jdoe", "columnType": 1 },
    { "columnName": "TokenType", "value": "Access", "columnType": 1 },
    { "columnName": "ClientId", "value": "portal-api", "columnType": 1 },
    { "columnName": "UserId", "value": "42", "columnType": 0 },
    { "columnName": "Status", "value": "Active", "columnType": 1 }
  ]
}
```

Response:
```json
{
  "isSuccess": true,
  "value": {
    "items": [
      {
        "id": 120931,
        "tokenId": "9fc8d7a4e2f1...",
        "tokenType": "Access",
        "clientId": "portal-api",
        "clientName": "Portal API",
        "userId": 42,
        "userName": "jdoe@tenant.com",
        "subject": "user:42",
        "issuedAt": "2026-01-21T12:30:00Z",
        "expiresAt": "2026-01-21T13:30:00Z",
        "status": 0
      }
    ],
    "totalCount": 1,
    "totalPages": 1,
    "currentPage": 1,
    "hasPrevious": false,
    "hasNext": false
  }
}
```

### Token detail

`GET /admin/tokens/{id}`

Response includes claims, scopes, audience, metadata, and audit fields:
```json
{
  "isSuccess": true,
  "value": {
    "id": 120931,
    "tokenId": "9fc8d7a4e2f1...",
    "tokenType": "Access",
    "clientId": "portal-api",
    "clientName": "Portal API",
    "userId": 42,
    "userName": "jdoe@tenant.com",
    "subject": "user:42",
    "issuedAt": "2026-01-21T12:30:00Z",
    "expiresAt": "2026-01-21T13:30:00Z",
    "status": 0,
    "scopes": "openid profile email",
    "audience": "https://api.example.com",
    "claimsJson": "{ \"sub\": \"user:42\", \"amr\": [\"pwd\"] }",
    "metadataJson": "{ \"issuer\": \"https://idp.example.com\" }",
    "issuedByIp": "203.0.113.10",
    "issuedUserAgent": "Mozilla/5.0 ...",
    "issuedBy": "system",
    "revokedAt": null,
    "revokedBy": null,
    "revokedByIp": null,
    "revokedReason": null
  }
}
```

### Revoke token

`POST /admin/tokens/{id}/revoke`

Body (optional):
```json
{ "reason": "Suspicious activity" }
```

Returns 204 No Content on success.

### Force expire token

`POST /admin/tokens/{id}/expire`

Returns 204 No Content on success.

### Lookups

`GET /admin/token/lookups`

Response includes token types, statuses, client list, and user list for dropdowns.

## EF Core read model

Read model entity: `IDP.Domain.ComplexTypes.TokenSearch`.

Key fields:
- `TokenIdHash` holds a masked or hashed token identifier (never raw tokens).
- `TokenType` string for UI and filters.
- `Status` as `TokenStatus` enum (int).
- `SourceTokenId` + `SourceType` for command routing.

DbSet: `IApplicationDbContext.TokensSearch`.

## SQL read model (reference)

```sql
CREATE TABLE TokenReadModel (
    Id INT NOT NULL PRIMARY KEY,
    TenantId INT NOT NULL,
    SourceTokenId INT NOT NULL,
    SourceType NVARCHAR(64) NOT NULL,
    TokenIdHash NVARCHAR(256) NOT NULL,
    TokenType NVARCHAR(64) NOT NULL,
    ClientId NVARCHAR(200) NOT NULL,
    ClientName NVARCHAR(256) NULL,
    UserId INT NULL,
    UserName NVARCHAR(256) NULL,
    Subject NVARCHAR(256) NULL,
    IssuedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    Status INT NOT NULL,
    Scopes NVARCHAR(1024) NULL,
    Audience NVARCHAR(1024) NULL,
    ClaimsJson NVARCHAR(MAX) NULL,
    MetadataJson NVARCHAR(MAX) NULL,
    IssuedByIp NVARCHAR(128) NULL,
    IssuedUserAgent NVARCHAR(512) NULL,
    IssuedBy NVARCHAR(256) NULL,
    RevokedAt DATETIME2 NULL,
    RevokedBy NVARCHAR(256) NULL,
    RevokedByIp NVARCHAR(128) NULL,
    RevokedReason NVARCHAR(512) NULL,
    CreatedOn DATETIME2 NOT NULL,
    UpdatedOn DATETIME2 NULL
);

CREATE INDEX IX_TokenReadModel_Tenant_Status
    ON TokenReadModel (TenantId, Status);
CREATE INDEX IX_TokenReadModel_Tenant_TokenType
    ON TokenReadModel (TenantId, TokenType);
CREATE INDEX IX_TokenReadModel_Tenant_ClientId
    ON TokenReadModel (TenantId, ClientId);
CREATE INDEX IX_TokenReadModel_Tenant_UserId
    ON TokenReadModel (TenantId, UserId);
CREATE INDEX IX_TokenReadModel_Tenant_IssuedAt
    ON TokenReadModel (TenantId, IssuedAt DESC);
```

## CQRS query handler

`Admin.Core.Tokens.GetTokenUseCase`:
- Filters by `TenantId` + criteria.
- Search over `TokenIdHash`, `ClientId`, `ClientName`, `UserName`, and `Subject`.
- Sort + page using `ApplySort` and `ToPaginatedListAsync`.
- Returns projection DTOs only (no entity loading).

## Admin UI table component

React components:
- `Admin-Portal/src/_components/tokens/tokensList.jsx`
- `Admin-Portal/src/_components/tokens/tokenInspectModal.jsx`

Behavior:
- Debounced search (400ms).
- AND-combined server filters.
- Sortable columns.
- Actions: Inspect, Audit, Revoke, Force Expire.
- Loading skeleton rows and empty state.

## Filtering logic (server-side)

Criteria mapping:
- `Search` -> full-text search over token id, client, user, subject.
- `TokenType` -> exact match.
- `ClientId` -> exact match.
- `UserId` -> exact match.
- `Status` -> enum match.

## Token revoke flow

1. Admin initiates revoke from list.
2. `TokenCommandUseCase.RevokeToken` resolves `TokenSearch` entry.
3. Dispatches to underlying token type (`RefreshToken` or `ReferenceToken`).
4. Writes audit entry (`AuditLog`).
5. Persists changes (soft revoke).

## Security considerations checklist

- Admin-only scopes required for all endpoints.
- Correlation IDs logged per request.
- Token values never returned; only hashes/masked identifiers.
- Tenant isolation enforced at query and command layers.
- Soft revoke and audit logs are immutable.
- IP and user-agent captured for audit.
- Pagination + indexed filters for read scale.
- Claims/metadata returned only for privileged admin scopes.
- GDPR: user filters restricted by tenant and RBAC.
