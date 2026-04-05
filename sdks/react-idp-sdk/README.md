\# SmartDevCon IDP – React SDK



Official React SDK for integrating SmartDevCon Identity Provider (OAuth 2.1 + OpenID Connect) into React applications.



This SDK handles:

\- OAuth 2.1 Authorization Code + PKCE flow

\- Token management (access + refresh)

\- Login / Logout helpers

\- Auth state management

\- Secure token exchange with SmartDevCon IDP



---



\## ✨ Features



\- ✅ OAuth 2.1 + OpenID Connect compliant

\- 🔐 PKCE enforced for public clients

\- 🔁 Automatic token refresh

\- 🧠 React hooks for auth state

\- ⚡ Works with React + Vite / Next.js / CRA

\- 🧩 Minimal configuration



---



\## 📦 Installation



```bash

npm install @smartdevcon/idp-react

\# or

yarn add @smartdevcon/idp-react







SDK

npx tsup

npm pack



Admin App

npm install "D:\\Solutions\\VSCode\\React-App\\idp-sdk\\tokenidp-react-0.1.0.tgz"



Wrap App with Provider



import { IdpAuthProvider, LoginPage } from "tokenidp-react";



<IdpAuthProvider

&nbsp;     config={{

&nbsp;       authority: process.env.REACT\_APP\_AUTH\_BASE\_URL,

&nbsp;       clientId: process.env.REACT\_APP\_OAUTH\_CLIENT\_ID,

&nbsp;       redirectUri: process.env.REACT\_APP\_OAUTH\_REDIRECT\_URI,

&nbsp;       postLoginRedirectUri: "/dashboard",

&nbsp;       postLogoutRedirectUri: "/login",

&nbsp;       scope: process.env.REACT\_APP\_OAUTH\_SCOPE,

&nbsp;       storage: "localStorage",

&nbsp;     }}

&nbsp;   >

&nbsp;	<App />

</IdpAuthProvider>



Optional built-in login route



<Route

&nbsp; path="/login"

&nbsp; element={

&nbsp;&nbsp; <LoginPage

&nbsp;&nbsp;&nbsp; title="Redirecting to TokenIDP..."

&nbsp;&nbsp;&nbsp; subtitle="Please wait while we securely connect to Identity."

&nbsp;&nbsp;&nbsp; signedOutTitle="You have been signed out"

&nbsp;&nbsp; />

&nbsp; }

/>



var auth  = useAuth();



Add Login Button



import { useAuth } from "@smartdevcon/idp-react";



export function Login() {

&nbsp; const { login, isAuthenticated, user } = useAuth();



&nbsp; if (isAuthenticated) {

&nbsp;   return <div>Welcome {user?.profile?.name}</div>;

&nbsp; }



&nbsp; return <button onClick={login}>Login</button>;

}



Add Callback Route



<Route path="/callback" element={<AuthCallback />} />



