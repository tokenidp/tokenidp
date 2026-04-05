# Authorization Code Flow with PKCE - .NET 8 Implementation

## Overview
This repository implements an **OAuth 2.0 Authorization Code Flow with PKCE** in **.NET 8**, supporting both **public clients (SPA, mobile apps)** and **confidential clients (backend services)**. The authentication server issues **JWT tokens** signed with **HMAC (MHAC)** and stores authorization codes securely in **PostgreSQL**.

## Flow Diagram
```mermaid
graph TD;
    A[Client (Web App / Mobile App)] -->|Authenticate| B[/authorize Endpoint/];
    B -->|Return Authorization Code| A;
    A -->|Exchange Code + Code Verifier| C[/token Endpoint/];
    C -->|Validate & Return Access Token| A;
    A -->|Use Access Token| D[Backend API];
    A -->|Refresh Token Request| E[/refresh_token Endpoint/];
    E -->|Return New Access Token| A;
```

## Endpoints Implemented

### 1. `/authorize` (Authorization Endpoint)
- Used by public clients (SPA/Mobile) and confidential clients (backend apps) to request an **authorization code**.
- Supports **PKCE** (Proof Key for Code Exchange) to prevent CSRF and code interception attacks.
- Parameters:
  - `client_id`
  - `response_type=code`
  - `redirect_uri`
  - `scope`
  - `code_challenge`
  - `code_challenge_method`
- Returns: **Authorization Code**

### 2. `/token` (Token Exchange Endpoint)
- Used to exchange an **authorization code** for an **access token**.
- Verifies **PKCE challenge** by comparing `code_challenge` and `code_verifier`.
- Parameters:
  - `client_id`
  - `grant_type=authorization_code`
  - `code`
  - `redirect_uri`
  - `code_verifier`
- Returns: **JWT Access Token + Refresh Token**

### 3. `/userinfo` (Fetch User Information)
- Returns user profile data based on a valid **access token**.
- Requires an `Authorization: Bearer <access_token>` header.
- Returns: **User Info (e.g., Name, Email, Roles, etc.)**

### 4. `/refresh_token` (Token Refresh Endpoint)
- Allows clients to obtain a new **access token** using a valid **refresh token**.
- Parameters:
  - `client_id`
  - `grant_type=refresh_token`
  - `refresh_token`
- Returns: **New JWT Access Token**

### 5. `/login` (Mobile App Login)
- Allows mobile apps to authenticate users and receive an **authorization code**.
- Implementation aligns with the `/authorize` endpoint for consistency.
- Returns: **Authorization Code**

## Security Measures
- **PKCE for Public Clients** (SPA & Mobile Apps) to enhance security.
- **JWT Tokens Signed with HMAC (MHAC)** for integrity and authenticity.
- **Authorization Codes Stored in PostgreSQL** for secure validation.
- **Refresh Tokens Implemented** to extend user sessions securely.

## Supported Grant Types
- `authorization_code`
- `refresh_token`

## Tech Stack
- **Backend:** .NET 8 (Custom OAuth2.0 Implementation)
- **Database:** PostgreSQL (for storing authorization codes & user details)
- **Authentication:** JWT (Signed with HMAC)
- **Frontend Clients:** React (SPA), Mobile Apps (iOS/Android)
- **Microservices Architecture:** Backend REST APIs

## How to Use
### Step 1: Request Authorization Code
```http
GET /authorize?client_id=myclient&response_type=code&redirect_uri=https://myapp.com/callback&scope=openid&code_challenge=<code_challenge>&code_challenge_method=S256
```
### Step 2: Exchange Code for Token
```http
POST /token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code&client_id=myclient&code=<auth_code>&redirect_uri=https://myapp.com/callback&code_verifier=<code_verifier>
```
### Step 3: Access Protected API
```http
GET /userinfo
Authorization: Bearer <access_token>
```
### Step 4: Refresh Token
```http
POST /refresh_token
Content-Type: application/x-www-form-urlencoded

grant_type=refresh_token&client_id=myclient&refresh_token=<refresh_token>
```

## Future Enhancements
- Implement **OpenID Connect (OIDC)** for additional claims.
- Add **Roles & Permissions** for fine-grained access control.
- Enable **Multi-Factor Authentication (MFA)** for enhanced security.

## Contribution & Feedback
Feel free to open issues and contribute to improvements! 🚀

