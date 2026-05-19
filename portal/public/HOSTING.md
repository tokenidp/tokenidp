# TokenIDP Admin Portal Hosting

This folder is a static React application. Host the files with IIS, Nginx,
Azure Static Web Apps, S3/CloudFront, Netlify, or any equivalent static host.

Before deployment, edit `config.json`:

```json
{
  "baseUrl": "https://idp.customer.com",
  "authority": "https://idp.customer.com",
  "clientId": "idp-admin",
  "redirectUri": "https://admin.customer.com/auth/callback",
  "postLogoutRedirectUri": "https://admin.customer.com/login"
}
```

Configure the static host to rewrite SPA routes to `index.html`, including
`/dashboard`, `/applications`, and `/auth/callback`.
