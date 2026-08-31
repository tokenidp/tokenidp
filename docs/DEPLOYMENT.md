# Deployment configuration

TokenIDP keeps only safe defaults and placeholders in source control. A safe
default is a non-sensitive value suitable for a local clone, such as a localhost
URL, a logging level, or an OAuth scope name. A placeholder documents a required
setting without providing a usable credential, such as an empty encryption key.

No real password, connection credential, API token, client secret, private key,
or certificate private key belongs in the repository.

## Local development

`src/TokenIDP.Service/appsettings.json` contains a local SQL Server connection
using Windows integrated authentication. Store local secret values with .NET
User Secrets:

```powershell
cd src/TokenIDP.Service

$secretKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
dotnet user-secrets set "Security:KeyBase64" $secretKey
```

Bootstrap is disabled by default. To opt in locally:

```powershell
dotnet user-secrets set "Bootstrap:Enable" "true"
dotnet user-secrets set "Bootstrap:AdminTempPassword" "<choose-a-local-temporary-password>"
```

Development generates an ephemeral RSA signing key in memory when no signing
material is configured. Tokens issued before a process restart will no longer
validate after that restart. Shared Development, Staging, and Production systems
must use stable signing material supplied outside source control.

## GitHub environment

Create protected GitHub environments named `staging` and `production`. Configure
required reviewers for production, restrict deployment branches, and store the
following values in **Settings > Environments > environment > Environment
secrets**.

### Environment secrets

| Name | Purpose |
| --- | --- |
| `AZURE_CLIENT_ID` | Microsoft Entra application or managed-identity client ID used by OIDC |
| `AZURE_TENANT_ID` | Microsoft Entra tenant ID used by OIDC |
| `AZURE_SUBSCRIPTION_ID` | Azure subscription selected by the deployment |
| `IDENTITY_DB_CONNECTION_STRING` | Runtime identity database connection string |
| `SECURITY_KEY_BASE64` | Random 32-byte AES key encoded as Base64 |
| `TOKEN_SIGNING_KEY` | Base64-encoded RSA private key for non-Production shared environments |
| `BOOTSTRAP_ADMIN_TEMP_PASSWORD` | Required only when bootstrap is enabled |

Production uses an installed signing certificate instead of
`TOKEN_SIGNING_KEY`. The certificate private key stays in Azure/App Service or a
secret store; it is never placed in GitHub.

### Environment variables

Variables are not secret and must be treated as public configuration.

| Name | Example or purpose |
| --- | --- |
| `AZURE_WEBAPP_NAME` | Azure App Service resource name |
| `AZURE_WEBAPP_RESOURCE_GROUP` | Resource group containing the App Service |
| `ASPNETCORE_ENVIRONMENT` | `Staging` or `Production` |
| `TOKEN_ISSUER` | Public HTTPS issuer URL |
| `TOKEN_CERTIFICATE_THUMBPRINT` | Production certificate selector |
| `TOKEN_CERTIFICATE_SUBJECT_NAME` | Alternative production certificate selector |
| `TOKEN_CERTIFICATE_STORE_NAME` | Usually `My` |
| `TOKEN_CERTIFICATE_STORE_LOCATION` | Usually `CurrentUser` or `LocalMachine` |
| `BOOTSTRAP_ENABLE` | `false` by default |
| `BOOTSTRAP_REDIRECT_URI` | Public admin portal callback URL |
| `BOOTSTRAP_LOGOUT_REDIRECT_URI` | Public admin portal logout callback URL |
| `SECURITY_KEY_ID` | Identifier attached to encrypted values |

Portal deployment uses these additional public environment variables:

- `REACT_APP_DEPLOYMENT_ENV`
- `REACT_APP_BASE_URL`
- `REACT_APP_AUTH_BASE_URL`
- `REACT_APP_TENANT_PROPAGATION_MODE`
- `REACT_APP_OAUTH_CLIENT_ID`
- `REACT_APP_OAUTH_REDIRECT_URI`
- `REACT_APP_OAUTH_POST_LOGOUT_REDIRECT_URI`
- `REACT_APP_OAUTH_SCOPE`

The portal workflow also requires the environment secret
`AZURE_STATIC_WEB_APPS_API_TOKEN`. Package publishing uses `NPM_TOKEN` and
`NUGET_API_KEY`; store those as secrets, never variables.

### GitHub access controls

Public visitors cannot open the repository's Actions secrets and variables
settings. GitHub does not reveal a saved secret value in the UI, including to an
administrator; an administrator can replace or delete it. Variables are plain
configuration, are not masked in logs, and must never hold sensitive data.

Secrets are available to an authorized workflow job, so access control must also
protect the workflow itself:

1. Add secrets to a protected GitHub **Environment**, not directly to source.
2. Require an administrator or trusted maintainer to approve production
   deployments.
3. Restrict environment deployment branches to the protected default branch and
   approved release tags.
4. Protect the default branch with pull-request reviews and status checks.
5. Add a `CODEOWNERS` rule for `.github/workflows/**` after choosing the GitHub
   owner or security team that must approve workflow changes.
6. Under **Settings > Actions > General**, require approval for workflows from
   forked pull requests and keep the default workflow token permissions
   read-only unless a job explicitly needs more.

A collaborator who can change and run an approved workflow could otherwise make
that workflow transmit a secret. Environment approval and protected workflow
files are therefore the meaningful security boundary—not merely hiding the
repository settings page.

The workflow validates required values and writes runtime configuration to Azure
App Service during deployment. Secret values are never added to the published
`appsettings.json`.

## Portal configuration

All portal configuration is visible in the browser. Copy the example files for
local use, or let `.github/workflows/portal-deploy.yml` generate
`public/config.json` from GitHub environment variables.

Never place a secret in a `REACT_APP_*` value. The OAuth client ID, issuer URL,
redirect URLs, scopes, tenant propagation mode, and PKCE verifier storage-key
name are public values. The SDK uses its internal `idp_pkce_verifier` key; the
PKCE verifier itself is generated dynamically and is not a deployment setting.

## Before making the repository public

Rotate every database password, bootstrap password, encryption key, signing key,
and deployment token that has ever been committed. Removing current values does
not invalidate credentials or remove them from Git history.
