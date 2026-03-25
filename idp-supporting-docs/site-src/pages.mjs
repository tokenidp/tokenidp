import { createDocsPages } from "./docs.mjs";

const ICONS = {
  selfHosted: `
            <svg viewBox="0 0 64 64" role="presentation">
              <rect x="18" y="14" width="28" height="36" rx="4"></rect>
              <path d="M24 22h16"></path>
              <path d="M24 30h16"></path>
              <path d="M24 38h10"></path>
              <path d="M32 50v-10"></path>
              <path d="M25 44l7-7 7 7"></path>
            </svg>`,
  centralized: `
            <svg viewBox="0 0 64 64" role="presentation">
              <circle cx="32" cy="18" r="6"></circle>
              <circle cx="18" cy="34" r="5"></circle>
              <circle cx="46" cy="34" r="5"></circle>
              <circle cx="18" cy="50" r="5"></circle>
              <circle cx="46" cy="50" r="5"></circle>
              <path d="M28 22l-6 8"></path>
              <path d="M36 22l6 8"></path>
              <path d="M18 39v6"></path>
              <path d="M46 39v6"></path>
              <path d="M23 34h18"></path>
              <path d="M22 47h20"></path>
            </svg>`,
  security: `
            <svg viewBox="0 0 64 64" role="presentation">
              <path d="M32 12l16 7v12c0 11-6.7 17.8-16 21-9.3-3.2-16-10-16-21V19l16-7z"></path>
              <path d="M25 32l5 5 9-10"></path>
              <path d="M32 18v6"></path>
            </svg>`,
  api: `
            <svg viewBox="0 0 64 64" role="presentation">
              <rect x="14" y="18" width="18" height="28" rx="4"></rect>
              <rect x="32" y="24" width="18" height="16" rx="4"></rect>
              <path d="M23 32h18"></path>
              <path d="M41 20l7 7-7 7"></path>
            </svg>`,
  growth: `
            <svg viewBox="0 0 64 64" role="presentation">
              <path d="M18 44l10-24 8 16 6-10 4 18"></path>
              <path d="M16 48h32"></path>
              <path d="M22 18h8"></path>
              <path d="M20 22h4"></path>
            </svg>`,
  audit: `
            <svg viewBox="0 0 64 64" role="presentation">
              <path d="M16 18h32"></path>
              <path d="M16 30h20"></path>
              <path d="M16 42h14"></path>
              <circle cx="44" cy="40" r="10"></circle>
              <path d="M44 35v5l4 3"></path>
            </svg>`,
  multiTenant: `
            <svg viewBox="0 0 64 64" role="presentation">
              <rect x="16" y="18" width="18" height="28" rx="4"></rect>
              <rect x="30" y="14" width="18" height="36" rx="4"></rect>
              <path d="M25 24h0"></path>
              <path d="M39 22h0"></path>
              <path d="M25 40h0"></path>
              <path d="M39 42h0"></path>
            </svg>`,
  rbac: `
            <svg viewBox="0 0 64 64" role="presentation">
              <circle cx="32" cy="18" r="6"></circle>
              <path d="M20 50v-6c0-5.5 5.4-10 12-10s12 4.5 12 10v6"></path>
              <path d="M46 24h10"></path>
              <path d="M51 19v10"></path>
            </svg>`,
};

const ROUTES = {
  home: "index.html",
  docs: "docs/index.html",
  docsAdminPortal: "docs/admin-portal/index.html",
  docsKeyManagement: "docs/key-management/index.html",
  docsAuthCodePkce: "docs/oauth/authorization-code-pkce/index.html",
  docsClientCredentials: "docs/oauth/client-credentials/index.html",
  docsRopc: "docs/oauth/resource-owner-password-credentials/index.html",
  docsDeviceFlow: "docs/oauth/device-flow/index.html",
  docsCiba: "docs/oauth/ciba/index.html",
  docsRbac: "docs/roles-and-permissions/index.html",
  docsTenants: "docs/multi-tenant-setup/index.html",
  docsMfa: "docs/mfa-policies/index.html",
  docsAudit: "docs/audit-logs/index.html",
  docsApi: "docs/api-integration/index.html",
  blogs: "blogs/index.html",
  contact: "contact/index.html",
  usecaseSaas: "usecases/b2b-saas/index.html",
  usecaseCompliance: "usecases/compliance/index.html",
  usecaseApi: "usecases/api-platform-builders/index.html",
  usecaseEnterprise: "usecases/enterprise-architecture/index.html",
  blogMultiTenant: "blogs/multitenant-identity/index.html",
  blogOauth2: "blogs/oauth2-authorization-code-flow/index.html",
  blogRbac: "blogs/rbac-vs-abac/index.html",
  blogMfa: "blogs/implementing-mfa/index.html",
  blogTokens: "blogs/secure-token-handling/index.html",
};

function renderFeatureCard(card) {
  return `
        <div class="card feature-card">
          <div class="feature-icon" aria-hidden="true">
${ICONS[card.icon]}
          </div>
          <h3>${card.title}</h3>
          <p>
            ${card.description}
          </p>
        </div>`;
}

function renderPlaceholderGraphic(label, modifierClass = "") {
  return `
        <div class="placeholder-figure${modifierClass ? ` ${modifierClass}` : ""}" aria-hidden="true">
          <svg viewBox="0 0 640 360" role="presentation">
            <rect x="1" y="1" width="638" height="358" rx="24"></rect>
            <path d="M92 116h456"></path>
            <path d="M92 180h320"></path>
            <path d="M92 244h388"></path>
            <text x="320" y="302" text-anchor="middle">${label}</text>
          </svg>
        </div>`;
}

function renderAdminCarouselSlide(slide, index) {
  return `
          <article class="admin-slide${index === 0 ? " is-active" : ""}"${index === 0 ? "" : " hidden"}>
            ${renderPlaceholderGraphic(`PLACEHOLDER: ${slide.title.toUpperCase()}`, "placeholder-figure-compact")}
            <div class="admin-slide-copy">
              <h3>${slide.title}</h3>
              <p>${slide.description}</p>
            </div>
          </article>`;
}

function landingPageScript() {
  return `document.querySelectorAll("[data-carousel]").forEach((carousel) => {
  const slides = Array.from(carousel.querySelectorAll(".admin-slide"));
  const dots = Array.from(carousel.querySelectorAll(".carousel-dot"));
  const prev = carousel.querySelector("[data-carousel-prev]");
  const next = carousel.querySelector("[data-carousel-next]");
  let index = 0;

  if (!slides.length) {
    return;
  }

  const show = (nextIndex) => {
    index = (nextIndex + slides.length) % slides.length;

    slides.forEach((slide, slideIndex) => {
      const active = slideIndex === index;
      slide.classList.toggle("is-active", active);
      slide.hidden = !active;
    });

    dots.forEach((dot, dotIndex) => {
      const active = dotIndex === index;
      dot.classList.toggle("is-active", active);
      dot.setAttribute("aria-current", active ? "true" : "false");
    });
  };

  prev?.addEventListener("click", () => show(index - 1));
  next?.addEventListener("click", () => show(index + 1));
  dots.forEach((dot, dotIndex) => {
    dot.addEventListener("click", () => show(dotIndex));
  });

  show(0);
});`;
}

function renderCtaPageHeader(title, subtitle, helpers, options = {}) {
  const contactHref = helpers.relativePagePath(ROUTES.contact);
  const getStartedHref = helpers.relativePagePath("docs/getting-started/index.html");
  const showScheduleDemo = options.showScheduleDemo ?? true;

  return `
    <header class="main-header">
      <div class="hero-content">
        <h1>${title}</h1>
        ${subtitle ? `<p>${subtitle}</p>` : ""}
        <div class="hero-actions">
          <a class="btn" href="${getStartedHref}">Get Started</a>
          ${showScheduleDemo ? `<a class="btn" href="${contactHref}">Schedule a Demo</a>` : ""}
        </div>
      </div>
    </header>`;
}

function renderUseCasePage(useCase, helpers) {
  return `
    ${renderCtaPageHeader(useCase.heroTitle, useCase.heroSubtitle, helpers)}

    <section class="container">
      <h2>${useCase.pageTitle}</h2>
      <p class="section-intro">
        ${useCase.intro}
      </p>

      <div class="grid feature-grid usecase-grid">
${useCase.features.map(renderFeatureCard).join("\n")}
      </div>
    </section>

    <section class="origin-section">
      <div class="origin-card">
        <span class="origin-kicker">${useCase.support.kicker}</span>
        <h2>${useCase.support.title}</h2>
        <p>
          ${useCase.support.description}
        </p>
      </div>
    </section>`;
}

function renderRelatedCards(items, helpers) {
  return items
    .map(
      (item) => `
        <div class="card">
          <h3>${item.title}</h3>
          <p>${item.description}</p>
          <a href="${helpers.relativePagePath(item.href)}">Read article -&gt;</a>
        </div>`,
    )
    .join("\n");
}

function renderBlogArticle(article, helpers) {
  const body = article.sections
    .map((section) => {
      if (section.type === "intro") {
        return `
      <p>
        ${section.body}
      </p>`;
      }

      return `
      <h2>${section.title}</h2>
      ${section.paragraphs
        .map(
          (paragraph) => `
      <p>
        ${paragraph}
      </p>`,
        )
        .join("")}`;
    })
    .join("\n");

  return `
    <section class="container blog-hero">
      <span class="blog-category">${article.category}</span>
      <h1>${article.heroTitle}</h1>
      <p class="blog-date">${article.dateLabel}</p>
    </section>

    <section class="container blog-content">
${body}
    </section>

    <section class="container blog-related">
      <h2>Related Articles</h2>
      <div class="grid">
${renderRelatedCards(article.related, helpers)}
      </div>
    </section>`;
}

function renderBlogsIndex(helpers) {
  const image = (file) => helpers.relativePath(`assets/images/blog/${file}`);
  const cards = [
    {
      image: image("multitenant-identity.svg"),
      alt: "Abstract tenant and identity architecture illustration",
      category: "Identity Architecture",
      title: "Designing Multi-Tenant Identity for SaaS",
      description:
        "Learn how to structure tenants, clients, roles, and permissions when building a scalable identity platform for B2B SaaS.",
      href: ROUTES.blogMultiTenant,
    },
    {
      image: image("oauth2-flow.svg"),
      alt: "OAuth2 authorization flow diagram illustration",
      category: "OAuth2",
      title: "OAuth2 Authorization Code Flow Explained",
      description:
        "A practical walkthrough of the most common OAuth flow for web apps, including PKCE, redirects, tokens, and backend validation.",
      href: ROUTES.blogOauth2,
    },
    {
      image: image("rbac-vs-abac.svg"),
      alt: "Authorization model comparison illustration",
      category: "Authorization",
      title: "RBAC vs ABAC in Enterprise Applications",
      description:
        "Compare role-based and attribute-based access models to decide where each approach fits inside internal admin tools and customer-facing apps.",
      href: ROUTES.blogRbac,
    },
    {
      image: image("mfa.svg"),
      alt: "Multi-factor authentication security illustration",
      category: "Authentication",
      title: "Implementing MFA in Identity Platforms",
      description:
        "Explore how to introduce step-up authentication, tenant policy control, and recovery flows without degrading the sign-in experience.",
      href: ROUTES.blogMfa,
    },
    {
      image: image("token-security.svg"),
      alt: "Secure token handling and API validation illustration",
      category: "API Security",
      title: "Secure Token Handling in APIs",
      description:
        "Review the operational details that matter when validating access tokens, rotating secrets, and reducing leakage across service boundaries.",
      href: ROUTES.blogTokens,
    },
  ];

  return `
    ${renderCtaPageHeader(
      "Blogs",
      "Insights on OAuth2, identity architecture, security hardening, and SaaS engineering patterns teams can apply in production.",
      helpers,
    )}

    <section class="container">
      <div class="grid blog-grid">
${cards
  .map(
    (card) => `
        <article class="card blog-card">
          <img
            class="blog-card-image"
            src="${card.image}"
            alt="${card.alt}"
          />
          <span class="blog-category">${card.category}</span>
          <h3>${card.title}</h3>
          <p>
            ${card.description}
           </p>
          <div class="blog-meta">
            <span>March 2026</span>
            <a href="${helpers.relativePagePath(card.href)}">Read Article -&gt;</a>
          </div>
        </article>`,
  )
  .join("\n")}
      </div>
    </section>`;
}

const DOC_PAGES = [
  {
    key: "getting-started",
    route: ROUTES.docs,
    navLabel: "Getting Started",
    title: "Getting Started | Docs | Enterprise Identity Platform",
    content: `
          <h2>Getting Started</h2>
          <p>
            Start by creating your tenant, registering an application, and setting
            callback URLs for your environment.
          </p>
          <h3>Checklist</h3>
          <ul>
            <li>Create tenant and admin user.</li>
            <li>Register app with <code>client_id</code> and redirect URL.</li>
            <li>Enable required scopes for your APIs.</li>
            <li>Test login in a non-production environment first.</li>
          </ul>
        `,
  },
  {
    key: "admin-portal",
    route: ROUTES.docsAdminPortal,
    navLabel: "Admin Portal",
    title: "Admin Portal | Docs | Enterprise Identity Platform",
    content: `
          <h2>Admin Portal</h2>
          <p>
            The Admin Portal is the operational surface for managing tenants,
            applications, users, roles, authentication policies, and platform
            security settings.
          </p>
          <h3>Common Tasks</h3>
          <ul>
            <li>Create and manage tenants.</li>
            <li>Register applications and configure redirect URIs.</li>
            <li>Assign roles and permissions to admins and operators.</li>
            <li>Review sign-in events, audit trails, and policy changes.</li>
          </ul>
          <h3>Operational Areas</h3>
          <ul>
            <li><strong>Tenant management:</strong> Configure tenant metadata, branding, and access boundaries.</li>
            <li><strong>Application management:</strong> Define clients, scopes, secrets, and callback settings.</li>
            <li><strong>Security policies:</strong> Enforce MFA, session controls, and login requirements.</li>
            <li><strong>Audit and support:</strong> Inspect activity logs and respond to operational issues.</li>
          </ul>
          <h3>Recommended Access Model</h3>
          <p>
            Limit high-privilege access to a small group of administrators, use
            role-based delegation for support and operations teams, and require
            MFA for all privileged accounts.
          </p>
        `,
  },
  {
    key: "key-management",
    route: ROUTES.docsKeyManagement,
    navLabel: "Key Management",
    title: "Key Management | Docs | Enterprise Identity Platform",
    content: `
          <h2>Key Management</h2>
          <p>
            Key management controls how signing keys, encryption keys, client
            secrets, and certificates are created, rotated, protected, and
            retired across the platform.
          </p>
          <h3>Primary Responsibilities</h3>
          <ul>
            <li>Rotate signing keys without breaking token validation.</li>
            <li>Protect private keys and sensitive secrets from application exposure.</li>
            <li>Track key status, usage, and retirement windows.</li>
            <li>Publish current verification material for dependent services.</li>
          </ul>
          <h3>Operational Guidance</h3>
          <ul>
            <li><strong>Generation:</strong> Create strong keys in trusted infrastructure and restrict export where possible.</li>
            <li><strong>Storage:</strong> Use secure stores such as HSM-backed or vault-managed key storage.</li>
            <li><strong>Rotation:</strong> Introduce new keys before retiring old ones so validators have overlap time.</li>
            <li><strong>Revocation:</strong> Be able to disable compromised keys quickly and force dependent systems to refresh metadata.</li>
          </ul>
          <h3>Recommended Practices</h3>
          <p>
            Maintain documented rotation schedules, audit every privileged key
            operation, and separate duties so no single operator can create,
            approve, and deploy sensitive key material alone.
          </p>
        `,
  },
  {
    key: "auth-code-pkce",
    route: ROUTES.docsAuthCodePkce,
    navLabel: "Authorization Code Flow with PKCE",
    group: "OAuth Flows",
    title: "Authorization Code Flow with PKCE | Docs | Enterprise Identity Platform",
    content: (helpers) => {
      const oauthFlowDiagram = helpers.relativePath("Auth Code Flow.png");

      return `
          <h2>Authorization Code Flow with PKCE</h2>
          <p>
            This is the recommended OAuth flow for browser-based apps, native
            mobile apps, and other public clients that cannot safely store a
            long-lived client secret.
          </p>
          <figure class="docs-figure">
            <img
              src="${oauthFlowDiagram}"
              alt="Authorization Code Flow with PKCE diagram"
            />
            <figcaption>
              The diagram shows the browser redirect, user authentication,
              authorization code return, and the token exchange protected by
              PKCE.
            </figcaption>
          </figure>
          <h3>What the Diagram Shows</h3>
          <ul>
            <li>The client starts the flow by redirecting the user to the authorization server.</li>
            <li>A <code>code_challenge</code> is sent up front and tied to a one-time <code>code_verifier</code> kept by the client.</li>
            <li>After sign-in and consent, the authorization server returns a short-lived authorization code.</li>
            <li>The client exchanges that code for tokens by sending the original <code>code_verifier</code>.</li>
            <li>The authorization server validates the verifier before issuing the access token and optional refresh token.</li>
          </ul>
          <h3>How the Flow Works</h3>
          <ol>
            <li>The application creates a random <code>code_verifier</code> and derives a hashed <code>code_challenge</code>.</li>
            <li>The browser is redirected to the authorization endpoint with client details, redirect URI, scopes, state, and the PKCE challenge.</li>
            <li>The user authenticates with the identity provider and approves the requested access.</li>
            <li>The authorization server redirects the browser back to the application with an authorization code.</li>
            <li>The application sends that code to the token endpoint together with the original <code>code_verifier</code>.</li>
            <li>The authorization server compares the verifier to the earlier challenge and, if they match, returns tokens.</li>
            <li>The client uses the access token for API calls and rotates refresh tokens if long-lived sessions are allowed.</li>
          </ol>
          <h3>Why PKCE Matters</h3>
          <p>
            PKCE prevents stolen authorization codes from being redeemed by an
            attacker. Even if the code is intercepted, the token exchange fails
            without the matching <code>code_verifier</code>.
          </p>
          <h3>Best Fit</h3>
          <ul>
            <li>Single-page applications.</li>
            <li>iOS and Android apps.</li>
            <li>Desktop applications.</li>
            <li>Modern web apps that want a redirect-based sign-in flow.</li>
          </ul>
        `;
    },
  },
  {
    key: "client-credentials",
    route: ROUTES.docsClientCredentials,
    navLabel: "Client Credentials",
    group: "OAuth Flows",
    title: "Client Credentials | Docs | Enterprise Identity Platform",
    content: `
          <h2>Client Credentials</h2>
          <p>
            Use this flow for machine-to-machine communication where no end user
            is involved and the calling service can securely hold its own client
            secret or private key.
          </p>
          <h3>How the Flow Works</h3>
          <ol>
            <li>The calling service authenticates directly with the token endpoint.</li>
            <li>It sends its client credentials and requested scopes.</li>
            <li>The authorization server validates the client identity.</li>
            <li>An access token is issued for API-to-API communication.</li>
          </ol>
          <h3>When To Use It</h3>
          <ul>
            <li>Backend jobs and workers.</li>
            <li>Internal service calls.</li>
            <li>Trusted integrations between server-side systems.</li>
          </ul>
          <h3>Implementation Notes</h3>
          <ul>
            <li>Prefer private key JWT or certificate-based auth where supported.</li>
            <li>Issue narrow scopes and short-lived access tokens.</li>
            <li>Do not use this flow for browser or mobile apps.</li>
          </ul>
        `,
  },
  {
    key: "ropc",
    route: ROUTES.docsRopc,
    navLabel: "Resource Owner Password Credentials",
    group: "OAuth Flows",
    title: "Resource Owner Password Credentials | Docs | Enterprise Identity Platform",
    content: `
          <h2>Resource Owner Password Credentials</h2>
          <p>
            This is a legacy flow where the client collects the user's username
            and password directly and exchanges them for tokens. It is
            discouraged for new systems and should only appear in controlled
            migration scenarios.
          </p>
          <h3>Why It Is Risky</h3>
          <ul>
            <li>The client handles raw user credentials directly.</li>
            <li>It bypasses redirect-based login, consent, and many modern security controls.</li>
            <li>It does not fit MFA, passwordless, or external identity federation well.</li>
          </ul>
          <h3>Use Only If</h3>
          <ul>
            <li>You are supporting a temporary migration from a legacy system.</li>
            <li>You control both the client and the identity system end to end.</li>
            <li>You have a clear deprecation plan to remove it.</li>
          </ul>
        `,
  },
  {
    key: "device-flow",
    route: ROUTES.docsDeviceFlow,
    navLabel: "Device Flow",
    group: "OAuth Flows",
    title: "Device Flow | Docs | Enterprise Identity Platform",
    content: (helpers) => {
      const deviceFlowDiagram = helpers.relativePath("Device Flow.png");

      return `
          <h2>Device Flow</h2>
          <p>
            Device Flow is designed for TVs, terminals, printers, kiosks, and
            other devices with limited input capability or no convenient browser.
          </p>
          <figure class="docs-figure">
            <img
              src="${deviceFlowDiagram}"
              alt="Device Flow diagram showing the user code and secondary-device verification flow"
            />
            <figcaption>
              The diagram shows the device code request, the user verification step on a second device,
              and the token polling sequence back to the authorization server.
            </figcaption>
          </figure>
          <h3>How the Flow Works</h3>
          <ol>
            <li>The device requests a device code and user code from the authorization server.</li>
            <li>The device displays the user code and verification URL to the user.</li>
            <li>The user completes authentication on a separate trusted device, such as a phone or laptop.</li>
            <li>The original device polls the token endpoint until the authorization completes.</li>
            <li>Once approved, the authorization server returns tokens to the device.</li>
          </ol>
          <h3>Best Fit</h3>
          <ul>
            <li>Smart TVs and streaming devices.</li>
            <li>CLI tools and headless terminals.</li>
            <li>Shared or constrained hardware with limited keyboards.</li>
          </ul>
        `;
    },
  },
  {
    key: "ciba",
    route: ROUTES.docsCiba,
    navLabel: "Ciba",
    group: "OAuth Flows",
    title: "CIBA | Docs | Enterprise Identity Platform",
    content: `
          <h2>CIBA</h2>
          <p>
            Client-Initiated Backchannel Authentication is a decoupled flow where
            authentication happens on a separate device or channel instead of in
            the same browser session that started the request.
          </p>
          <h3>How the Flow Works</h3>
          <ol>
            <li>The client initiates an authentication request over a backchannel.</li>
            <li>The authorization server triggers an approval request to the user's trusted device or banking app.</li>
            <li>The user authenticates and approves the request out of band.</li>
            <li>The client polls or receives a callback when the request completes.</li>
            <li>The authorization server issues tokens after successful approval.</li>
          </ol>
          <h3>Best Fit</h3>
          <ul>
            <li>Banking and high-assurance approval journeys.</li>
            <li>Cross-device login experiences.</li>
            <li>Flows where the initiating channel should not handle the user password.</li>
          </ul>
        `,
  },
  {
    key: "rbac",
    route: ROUTES.docsRbac,
    navLabel: "Roles and Permissions",
    title: "Roles and Permissions | Docs | Enterprise Identity Platform",
    content: `
          <h2>Roles and Permissions</h2>
          <p>
            Define roles based on job functions and map permissions to each role.
            Keep permissions granular to reduce risk.
          </p>
          <h3>Example Role Strategy</h3>
          <ul>
            <li>Admin: full tenant-level management permissions.</li>
            <li>Operator: run operational tasks with limited settings access.</li>
            <li>Viewer: read-only reports and dashboards.</li>
          </ul>
        `,
  },
  {
    key: "tenants",
    route: ROUTES.docsTenants,
    navLabel: "Multi-Tenant Setup",
    title: "Multi-Tenant Setup | Docs | Enterprise Identity Platform",
    content: `
          <h2>Multi-Tenant Setup</h2>
          <p>
            Isolate users, roles, and policies by tenant. Include tenant identifier
            in tokens and enforce checks in backend APIs.
          </p>
          <h3>Implementation Notes</h3>
          <ul>
            <li>Store tenant metadata separately from global platform settings.</li>
            <li>Validate tenant context on every privileged request.</li>
            <li>Apply tenant-specific MFA and session policies.</li>
          </ul>
        `,
  },
  {
    key: "mfa",
    route: ROUTES.docsMfa,
    navLabel: "MFA Policies",
    title: "MFA Policies | Docs | Enterprise Identity Platform",
    content: `
          <h2>MFA Policies</h2>
          <p>
            Strengthen authentication with policy-based MFA. Enforce higher security
            controls for administrators and sensitive actions.
          </p>
          <h3>Common Enforcement Rules</h3>
          <ul>
            <li>Always require MFA for admin and support roles.</li>
            <li>Challenge users again for high-risk operations.</li>
            <li>Allow tenant-level exemptions only with approval.</li>
          </ul>
        `,
  },
  {
    key: "audit",
    route: ROUTES.docsAudit,
    navLabel: "Audit Logs",
    title: "Audit Logs | Docs | Enterprise Identity Platform",
    content: `
          <h2>Audit Logs</h2>
          <p>
            Keep immutable logs for sign-ins, permission changes, token revocations,
            and policy updates.
          </p>
          <h3>What To Capture</h3>
          <ul>
            <li>Actor identity and tenant context.</li>
            <li>Action timestamp and source IP.</li>
            <li>Before/after values for security-critical changes.</li>
          </ul>
        `,
  },
  {
    key: "api",
    route: ROUTES.docsApi,
    navLabel: "API Integration",
    title: "API Integration | Docs | Enterprise Identity Platform",
    content: `
          <h2>API Integration</h2>
          <p>
            Protect APIs by validating access tokens and enforcing role + tenant
            claims before executing business logic.
          </p>
          <h3>Backend Validation Flow</h3>
          <ul>
            <li>Validate token signature and expiration.</li>
            <li>Check audience and issuer claims.</li>
            <li>Authorize against roles and tenant boundaries.</li>
          </ul>
        `,
  },
];

function renderDocsSidebar(activeKey, helpers) {
  const gettingStartedPage = DOC_PAGES.find((page) => page.key === "getting-started");
  const adminPortalPage = DOC_PAGES.find((page) => page.key === "admin-portal");
  const keyManagementPage = DOC_PAGES.find((page) => page.key === "key-management");
  const otherTopLevelPages = DOC_PAGES.filter(
    (page) =>
      !page.group &&
      page.key !== "getting-started" &&
      page.key !== "admin-portal" &&
      page.key !== "key-management",
  );
  const oauthPages = DOC_PAGES.filter((page) => page.group === "OAuth Flows");
  const hasActiveOauthPage = oauthPages.some((page) => page.key === activeKey);

  return `
      <aside class="docs-sidebar">
        <h2>Topics</h2>
        <a
          class="topic-link${gettingStartedPage.key === activeKey ? " active" : ""}"
          href="${helpers.relativePagePath(gettingStartedPage.route)}"
        >
          ${gettingStartedPage.navLabel}
        </a>
        <a
          class="topic-link${adminPortalPage.key === activeKey ? " active" : ""}"
          href="${helpers.relativePagePath(adminPortalPage.route)}"
        >
          ${adminPortalPage.navLabel}
        </a>
        <a
          class="topic-link${keyManagementPage.key === activeKey ? " active" : ""}"
          href="${helpers.relativePagePath(keyManagementPage.route)}"
        >
          ${keyManagementPage.navLabel}
        </a>
        <div
          class="topic-group${hasActiveOauthPage ? " has-active-topic is-open" : ""}"
          data-topic-group="oauth"
        >
          <button
            class="topic-group-toggle"
            type="button"
            aria-expanded="${hasActiveOauthPage ? "true" : "false"}"
          >
            OAuth Flows
          </button>
          <div class="topic-submenu"${hasActiveOauthPage ? "" : ' hidden'}>
${oauthPages
  .map(
    (page) => `            <a
              class="topic-link topic-sublink${page.key === activeKey ? " active" : ""}"
              href="${helpers.relativePagePath(page.route)}"
            >
              ${page.navLabel}
            </a>`,
  )
  .join("\n")}
          </div>
        </div>
${otherTopLevelPages
  .map(
    (page) => `        <a
          class="topic-link${page.key === activeKey ? " active" : ""}"
          href="${helpers.relativePagePath(page.route)}"
        >
          ${page.navLabel}
        </a>`,
  )
  .join("\n")}
      </aside>`;
}

function docsScript() {
  return `const docsTopicGroups = document.querySelectorAll("[data-topic-group]");

docsTopicGroups.forEach((group) => {
  const toggle = group.querySelector(".topic-group-toggle");
  const submenu = group.querySelector(".topic-submenu");

  if (!toggle || !submenu) {
    return;
  }

  toggle.addEventListener("click", () => {
    const isOpen = toggle.getAttribute("aria-expanded") === "true";
    const nextState = !isOpen;

    toggle.setAttribute("aria-expanded", String(nextState));
    group.classList.toggle("is-open", nextState);
    submenu.hidden = !nextState;
  });
});`;
}

function renderDocsPage(topic, helpers) {
  const content =
    typeof topic.content === "function" ? topic.content(helpers) : topic.content;

  return `
    <section class="docs-wrap">
${renderDocsSidebar(topic.key, helpers)}
      <article class="docs-content">
${content}
      </article>
    </section>`;
}

function renderContactSection() {
  return `
    <section class="container contact-section">
      <div class="contact-layout">
        <div class="contact-copy">
          <h2>Contact with us</h2>
          <p>
            It is easy to get in touch with us. Use the contact form to discuss
            your identity platform needs, deployment plans, or product
            questions.
          </p>

          <div class="contact-meta">
            <h3>Head Office</h3>
            <p>121 King St, Melbourne VIC 3000, Australia</p>
            <p><strong>Phone:</strong> +61 2 8376 6284</p>
            <p>
              <strong>Email:</strong>
              <a href="mailto:hello@yourdomain.com">hello@yourdomain.com</a>
            </p>
          </div>
        </div>

        <div class="contact-form-wrap">
          <p class="contact-form-lead">
            Have questions about integrating TokenIDP into your architecture?
            Our team can help with deployment, integration, and best practices.
          </p>
          <h3>Reach us quickly</h3>
          <form class="contact-form">
            <input
              class="contact-field"
              type="text"
              name="name"
              placeholder="Enter name"
              aria-label="Enter name"
            />
            <input
              class="contact-field"
              type="email"
              name="email"
              placeholder="Enter email"
              aria-label="Enter email"
            />
            <input
              class="contact-field"
              type="tel"
              name="phone"
              placeholder="Your Phone"
              aria-label="Your phone"
            />
            <input
              class="contact-field"
              type="text"
              name="company"
              placeholder="Your Company"
              aria-label="Your company"
            />
            <textarea
              class="contact-field contact-message"
              name="message"
              placeholder="Message"
              aria-label="Message"
            ></textarea>
            <button class="contact-submit" type="submit">Send Message</button>
          </form>
        </div>
      </div>
    </section>`;
}

function renderContactPage(helpers) {
  return `
    ${renderCtaPageHeader(
      "Contact Us",
      "Reach out about product fit, deployment questions, or a short demo.",
      helpers,
      { showScheduleDemo: false },
    )}
${renderContactSection()}`;
}

function renderLandingPage(helpers) {
  const contactHref = helpers.relativePagePath(ROUTES.contact);
  const docsHref = helpers.relativePagePath(ROUTES.docs);
  const getStartedHref = helpers.relativePagePath("docs/getting-started/index.html");
  const integrationWorkflowDiagram = helpers.relativePath("Integration Workflow.png");
  const deviceFlowDiagram = helpers.relativePath("Device Flow.png");
  const adminSlides = [
    {
      title: "Dashboard",
      description:
        "Monitor tenant activity, token posture, and operational signals from one overview screen.",
    },
    {
      title: "Applications",
      description:
        "Register clients, grant scopes, and manage redirect URIs and secrets in one place.",
    },
    {
      title: "Tenant Management",
      description:
        "Configure tenant-specific boundaries, branding, and policy settings without custom admin tooling.",
    },
    {
      title: "User Management",
      description:
        "Manage users, status, roles, and lifecycle operations from a central identity directory.",
    },
    {
      title: "Roles & Permissions",
      description:
        "Model access with controlled role definitions and permission boundaries for operators and applications.",
    },
    {
      title: "MFA Policies",
      description:
        "Apply stronger authentication requirements for administrators and higher-risk access scenarios.",
    },
    {
      title: "External Providers",
      description:
        "Connect third-party identity providers while keeping TokenIDP as the central control plane.",
    },
  ];

  return `
    <header class="main-header">
      <div class="hero-content">
        <h1>One Identity for Every<br>App and Service</h1>
        <p>
          OAuth2, OpenID Connect, RBAC,<br>
          and User Management, built from real-world experience,<br>
          simple to configure &amp; deploy, and easy to operate.
        </p>
        <div class="hero-actions">
          <a class="btn" href="${getStartedHref}">Get Started</a>
          <a class="btn" href="${contactHref}">Schedule a Demo</a>
        </div>
      </div>
    </header>

    <section class="origin-section">
      <div class="origin-card">
        <span class="origin-kicker">Our Purpose</span>
        <h2>Why This Platform Exists</h2>
        <p>
          After building OAuth2 and identity flows across multiple systems, it
          became clear that teams repeatedly re-implement the same patterns:
          client management, token flows, RBAC, tenant separation, and
          operational visibility. This project turns those repeated patterns
          into a reusable, modern identity platform focused on clarity,
          security, and real-world usability.
        </p>
      </div>
    </section>

    <section id="usecase-saas" class="container">
      <h2>Core Features</h2>
      <div class="grid feature-grid">
${[
  {
    icon: "selfHosted",
    title: "Self-Hosted & Data Control",
    description:
      "Keep identity data inside your own infrastructure and databases without depending on external providers.",
  },
  {
    icon: "centralized",
    title: "Centralized Identity",
    description:
      "Manage users, roles, tenant policies, and identity operations from one control place.",
  },
  {
    icon: "security",
    title: "Security Standards",
    description:
      "Built-in OAuth 2.1, OpenID Connect, RBAC, MFA, and GDPR by design help meet modern security expectations.",
  },
  {
    icon: "api",
    title: "API Authorization",
    description:
      "Protect APIs with scopes, token validation, and consistent access control across services.",
  },
  {
    icon: "growth",
    title: "Faster Development",
    description:
      "Avoid building authentication from scratch and reduce delivery time for new applications.",
  },
  {
    icon: "audit",
    title: "Admin Portal",
    description:
      "UI to manage tenants, applications, users, and security settings, with a dashboard for monitoring authentication activity.",
  },
]
  .map(renderFeatureCard)
  .join("\n")}
      </div>
    </section>

    <section class="build-vs-buy-shell">
      <svg class="build-vs-buy-wave" viewBox="0 0 1440 140" preserveAspectRatio="none" aria-hidden="true">
        <path d="M0 40C180 120 360 10 540 48C720 86 900 150 1080 104C1260 58 1350 12 1440 34V0H0Z"></path>
      </svg>
      <div class="container build-vs-buy">
        <h2>Build vs Buy Identity Provider</h2>

        <p class="section-intro">
          Building authentication and access control systems from scratch
          becomes complex as applications scale. TokenIDP provides a
          ready-to-use identity platform so teams can focus on building
          product features instead of security infrastructure.
        </p>

        <div class="comparison-wrap">
          <div class="comparison-table">
            <div class="comparison-column card">
              <h3>Build Authentication Yourself</h3>
              <div class="comparison-row comparison-row-compact">
                <p>Design login + OAuth flows</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p>Build user, rbac &amp; tenant management</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p>Implement token management</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p>Maintain security policies</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p>Weeks or Months of development effort</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p>Requires deep knowledge of standard protocols</p>
              </div>
            </div>

            <div class="comparison-column comparison-column-primary card">
              <h3>Use TokenIDP</h3>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>OAuth2 + OpenID Connect ready</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>Built-in user, rbac &amp; tenant management</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>Built-in token management</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>MFA &amp; security policies</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>Integrate in minutes</p>
              </div>
              <div class="comparison-row comparison-row-compact">
                <p><span class="comparison-check" aria-hidden="true">&#10003;</span>No deep protocol knowledge required</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section id="usecase-integration" class="container">
      <h2>How It Works (High-Level Architecture)</h2>
      <p class="architecture-intro">
        Your application delegates authentication and authorization to the
        Identity Platform, while keeping full control of business logic and
        data. The platform issues secure tokens after login. Your backend
        validates these tokens and enforces access rules based on tenant and
        roles.
      </p>

      <figure class="workflow-figure diagram-placeholder">
        <img
          src="${integrationWorkflowDiagram}"
          alt="Integration workflow showing the application, identity platform, and token-based authorization flow"
        />
      </figure>
    </section>
    
    <section class="container standards-section">
      <h2>Standards-Based Authentication</h2>
      <p class="section-intro">
        TokenIDP is designed around the standards most teams need in production,
        so applications can integrate with clear protocol boundaries instead of
        custom authentication logic.
      </p>
      <div class="standards-layout">
        <div class="card standards-copy">
          <ul class="standards-list">
            <li><strong>OAuth 2.1-aligned architecture</strong><br />Designed for secure, standards-driven authorization without custom implementations.</li>
            <li><strong>OpenID Connect (OIDC) identity layer</strong><br />Enables reliable authentication and user identity verification.</li>
            <li><strong>Authorization Code Flow with PKCE</strong><br />Industry-recommended flow for web and mobile applications, preventing authorization code interception.</li>
            <li><strong>Client Credentials Flow for machine-to-machine access</strong><br />Ideal for backend services, APIs, and microservices communication.</li>
            <li><strong>Device Authorization Flow (Device Flow)</strong><br />Enables secure authentication for devices with limited input capabilities such as TVs and IoT devices.</li>
            <li><strong>Secure Refresh Token handling</strong><br />Supports long-lived sessions with controlled and secure token renewal.</li>
            <li><strong>Token Revocation support</strong><br />Allows clients to invalidate access or refresh tokens to immediately terminate sessions and prevent unauthorized access.</li>
          </ul>
        </div>
        <div class="card standards-diagram-card">
          <h3 class="standards-diagram-title">Device Flow</h3>
          <figure class="standards-diagram-figure">
            <img
              src="${deviceFlowDiagram}"
              alt="Device Flow diagram showing verification on a separate device"
            />
          </figure>
        </div>
      </div>
    </section>

    <section class="container admin-portal-section">
      <h2>Admin Portal</h2>
      <p class="section-intro">
        Operate tenants, applications, users, and policy settings through a
        central management interface built for support teams and platform owners.
      </p>
      <div class="admin-carousel card" data-carousel>
        <div class="admin-carousel-toolbar">
          <h3>Portal Screens</h3>
          <div class="carousel-controls">
            <button class="carousel-control" type="button" data-carousel-prev aria-label="Previous slide">
              Prev
            </button>
            <button class="carousel-control" type="button" data-carousel-next aria-label="Next slide">
              Next
            </button>
          </div>
        </div>
        <div class="admin-carousel-viewport">
${adminSlides.map(renderAdminCarouselSlide).join("\n")}
        </div>
        <div class="carousel-dots">
${adminSlides
  .map(
    (_, index) => `          <button class="carousel-dot${index === 0 ? " is-active" : ""}" type="button" aria-label="Go to slide ${index + 1}" aria-current="${index === 0 ? "true" : "false"}"></button>`,
  )
  .join("\n")}
        </div>
      </div>
    </section>

    <section class="container integrate-section">
      <h2>Integrate in Minutes</h2>
      <p class="section-intro">
        Developers can get started with the existing host integration patterns,
        the React SDK, and discovery-based configuration instead of building
        authentication plumbing from zero.
      </p>
      <div class="integration-layout">
        <div class="card integration-points">
          <ul class="standards-list">
            <li>NuGet package</li>
            <li>React SDK</li>
            <li>OAuth discovery endpoint</li>
          </ul>
        </div>
        <div class="card integration-code-card">
          <pre class="integration-code"><code>services.AddTokenIDPAuthentication();</code></pre>
        </div>
      </div>
    </section>

    `;
}

const useCases = [
  {
    outputPath: ROUTES.usecaseSaas,
    title: "B2B SaaS Applications | TokenIDP",
    bodyClass: "usecase-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "B2B SaaS Applications",
        heroSubtitle:
          "Identity infrastructure for scalable multi-tenant SaaS platforms",
        pageTitle: "Identity Infrastructure for B2B SaaS Platforms",
        intro:
          "Building a B2B SaaS platform means managing users from multiple organizations while ensuring each customer's data and access remain secure and isolated. Authentication, user management, and role-based permissions quickly become complex as your product grows. A dedicated identity platform like TokenIDP simplifies this challenge by providing centralized authentication, secure access control, and tenant-based user management for all your applications. Instead of building identity infrastructure from scratch, SaaS teams can integrate a ready-to-use platform and focus on delivering core product features.",
        features: [
          {
            icon: "multiTenant",
            title: "Multi-Tenant Identity",
            description:
              "TokenIDP keeps each customer organization isolated with its own users, roles, and authentication settings on one secure platform.",
          },
          {
            icon: "centralized",
            title: "Centralized User Management",
            description:
              "Administrators can manage users, roles, and permissions for every SaaS application from one identity control plane.",
          },
          {
            icon: "security",
            title: "Secure Authentication",
            description:
              "Web apps, mobile apps, and APIs can all use OAuth 2 and OpenID Connect for secure, standardized authentication.",
          },
          {
            icon: "rbac",
            title: "Role-Based Access Control",
            description:
              "Define tenant-aware roles and permissions so each user only sees the features and APIs relevant to their responsibilities.",
          },
        ],
        support: {
          kicker: "Delivery Impact",
          title: "Faster Product Development",
          description:
            "Building a secure identity system internally can take months and requires deep security expertise. TokenIDP delivers these capabilities out of the box so product teams can focus on shipping customer-facing features instead of maintaining identity infrastructure.",
        },
      }, helpers),
  },
  {
    outputPath: ROUTES.usecaseCompliance,
    title: "Compliance-Driven Teams | TokenIDP",
    bodyClass: "usecase-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "Compliance-Driven Teams",
        heroSubtitle:
          "Identity controls for regulated and security-sensitive organizations",
        pageTitle: "Identity Platform for Compliance-Driven Organizations",
        intro:
          "Organizations operating in regulated industries must meet strict security and privacy requirements when managing user identities and system access. Authentication systems must ensure that only authorized users can access sensitive data while maintaining clear audit trails of login activity and permission changes. TokenIDP provides a secure, self-hosted identity platform designed to help teams implement strong authentication, centralized access control, and detailed visibility into user activity. By separating identity management from business applications, organizations can maintain consistent security policies while supporting compliance and operational governance.",
        features: [
          {
            icon: "security",
            title: "Secure Authentication",
            description:
              "Support strong authentication with modern login flows and multi-factor authentication to protect sensitive systems.",
          },
          {
            icon: "audit",
            title: "Audit &amp; Activity Logs",
            description:
              "Track login activity, role changes, and security events to maintain visibility across authentication systems.",
          },
          {
            icon: "rbac",
            title: "Role-Based Access Control",
            description:
              "Define roles and permissions to ensure users only access systems and data appropriate to their responsibilities.",
          },
          {
            icon: "selfHosted",
            title: "Self-Hosted Data Control",
            description:
              "Deploy the identity platform inside your infrastructure to maintain full control over authentication data and security policies.",
          },
        ],
        support: {
          kicker: "Why It Fits",
          title: "Built for Regulated Environments",
          description:
            "Compliance-driven teams in healthcare, finance, insurance, government, and privacy-sensitive SaaS environments need secure authentication, clear auditability, least-privilege access control, and internal data ownership. TokenIDP is especially well suited to these organizations because it is self-hosted and keeps identity operations inside the customer's own environment.",
        },
      }, helpers),
  },
  {
    outputPath: ROUTES.usecaseApi,
    title: "API Platform Builders | TokenIDP",
    bodyClass: "usecase-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "API Platform Builders",
        heroSubtitle:
          "Identity infrastructure for token-secured APIs and distributed services",
        pageTitle: "Identity Infrastructure for API Platforms",
        intro:
          "Modern applications rely heavily on APIs to connect services, applications, and external integrations. Securing these APIs while managing authentication for different clients can quickly become complex. TokenIDP provides a centralized identity platform designed for API-driven systems, enabling applications to authenticate users and services securely using standardized token-based access. With support for OAuth2 and OpenID Connect, development teams can protect APIs, manage client applications, and control access permissions across distributed systems.",
        features: [
          {
            icon: "security",
            title: "Secure API Authentication",
            description:
              "Authenticate users and applications with standardized OAuth2 token flows before APIs process protected requests.",
          },
          {
            icon: "centralized",
            title: "Client Application Management",
            description:
              "Register and manage web, mobile, and partner applications with client-specific settings and API access policies.",
          },
          {
            icon: "api",
            title: "Token-Based Authorization",
            description:
              "APIs validate access tokens and embedded claims before allowing requests to protected services and resources.",
          },
          {
            icon: "growth",
            title: "Microservices Ready",
            description:
              "Use one consistent token model across distributed services so microservices can trust and validate requests independently.",
          },
        ],
        support: {
          kicker: "Architecture Fit",
          title: "Built for API-First Systems",
          description:
            "Teams building public APIs, partner integrations, internal developer platforms, and microservices architectures need centralized identity, client registration, and reliable token validation. TokenIDP gives API platform teams one secure control point for authentication and authorization across distributed systems.",
        },
      }, helpers),
  },
  {
    outputPath: ROUTES.usecaseEnterprise,
    title: "Enterprise Architecture | TokenIDP",
    bodyClass: "usecase-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "Enterprise Architecture",
        heroSubtitle:
          "Identity infrastructure for large application ecosystems and internal platforms",
        pageTitle: "Identity Platform for Enterprise Architectures",
        intro:
          "Large organizations often operate multiple applications, services, and internal platforms that require secure and consistent identity management. Without a centralized identity system, authentication and access control can become fragmented across systems. TokenIDP provides a unified identity platform that enables enterprises to manage authentication, authorization, and security policies across all applications while maintaining a scalable and standards-based architecture.",
        features: [
          {
            icon: "centralized",
            title: "Centralized Identity Management",
            description:
              "Manage users, roles, and permissions across multiple enterprise applications from a single platform.",
          },
          {
            icon: "security",
            title: "Consistent Security Policies",
            description:
              "Apply authentication and authorization rules across all enterprise systems.",
          },
          {
            icon: "api",
            title: "Application Integration",
            description:
              "Integrate authentication across web apps, APIs, and enterprise services.",
          },
          {
            icon: "growth",
            title: "Scalable Identity Infrastructure",
            description:
              "Support growing applications, services, and enterprise user bases.",
          },
        ],
        support: {
          kicker: "Enterprise Fit",
          title: "One Identity Layer Across the Organization",
          description:
            "Customer portals, employee portals, partner systems, mobile apps, internal APIs, and legacy applications all need consistent authentication and authorization. TokenIDP gives enterprise architects a central identity layer that reduces duplication, aligns policy enforcement, and scales across diverse application environments.",
        },
      }, helpers),
  },
];

const blogArticles = [
  {
    outputPath: ROUTES.blogMultiTenant,
    title: "Designing Multi-Tenant Identity for B2B SaaS | TokenIDP",
    bodyClass: "blog-article-page",
    activeNav: "blog",
    render: (helpers) =>
      renderBlogArticle(
        {
        category: "Identity Architecture",
        heroTitle: "Designing Multi-Tenant Identity for B2B SaaS",
        dateLabel: "Published March 2026",
        sections: [
          {
            type: "intro",
            body:
              "Building authentication and authorization for multi-tenant SaaS platforms introduces challenges around isolation, permissions, and tenant-specific policies. The identity layer has to preserve strong separation without forcing every product team to rebuild the same controls in every service.",
          },
          {
            title: "Tenant Isolation",
            paragraphs: [
              "Each organization should have its own users, roles, clients, and policy boundaries. A solid platform makes tenant context explicit in both data models and issued tokens so APIs can consistently enforce who belongs to which organization.",
              "Isolation should exist at multiple layers: storage, configuration, administrative workflows, and runtime token validation. Relying on a UI filter alone is not isolation. The tenant identifier needs to become part of the authorization contract.",
            ],
          },
          {
            title: "Client and Environment Modeling",
            paragraphs: [
              "Most B2B platforms eventually support multiple apps per tenant, plus separate staging and production environments. Model clients as first-class entities so redirect URIs, scopes, secrets, and token lifetimes can vary without leaking configuration across tenants.",
              "Keeping tenant metadata separate from platform-wide defaults also makes support and operations safer. Teams can inspect a tenant's configuration without accidentally changing shared behavior for every customer.",
            ],
          },
          {
            title: "RBAC and Permissions",
            paragraphs: [
              "Roles simplify permission management by grouping access rights across services and APIs. Start with tenant-scoped roles that reflect job functions, then map those roles to explicit permissions that backend services can evaluate.",
              "This gives product teams a stable abstraction. The identity platform owns issuance and governance, while APIs continue to enforce fine-grained permissions for sensitive actions.",
            ],
          },
          {
            title: "Operational Guardrails",
            paragraphs: [
              "Multi-tenant identity also needs auditability. Admin invites, role changes, token revocations, and policy updates should be logged with actor, tenant, and timestamp context so security teams can reconstruct what happened when something goes wrong.",
              "The goal is not just login. It is a predictable security boundary that scales as tenants, apps, and teams grow.",
            ],
          },
        ],
        related: [
          {
            title: "OAuth2 Authorization Code Flow Explained",
            description:
              "Understand the most widely used OAuth flow for modern web apps.",
            href: ROUTES.blogOauth2,
          },
          {
            title: "RBAC vs ABAC in Enterprise Applications",
            description:
              "Compare two common access control models for enterprise systems.",
            href: ROUTES.blogRbac,
          },
          {
            title: "Implementing MFA in Identity Platforms",
            description:
              "See how to add stronger authentication without harming usability.",
            href: ROUTES.blogMfa,
          },
        ],
      },
        helpers,
      ),
  },
  {
    outputPath: ROUTES.blogOauth2,
    title: "OAuth2 Authorization Code Flow Explained | TokenIDP",
    bodyClass: "blog-article-page",
    activeNav: "blog",
    render: (helpers) =>
      renderBlogArticle(
        {
        category: "OAuth2",
        heroTitle: "OAuth2 Authorization Code Flow Explained",
        dateLabel: "Published March 2026",
        sections: [
          {
            type: "intro",
            body:
              "Authorization Code with PKCE is the default flow for modern browser and mobile applications because it keeps tokens out of the front channel and gives the authorization server control over client verification.",
          },
          {
            title: "Why PKCE Matters",
            paragraphs: [
              "Public clients cannot safely store a client secret. PKCE solves that gap by binding the authorization request to the token exchange with a one-time verifier. If an authorization code is intercepted, the attacker still cannot redeem it without the original verifier.",
            ],
          },
          {
            title: "Flow Overview",
            paragraphs: [
              "The app redirects the user to the identity platform, the user authenticates, consent is evaluated, and an authorization code is returned to the client's redirect URI. The app then exchanges that code for tokens through a back-channel request.",
              "Backend APIs should validate issuer, audience, expiration, and signature before trusting any access token. The browser never becomes the authority for permission checks.",
            ],
          },
          {
            title: "Operational Considerations",
            paragraphs: [
              "Keep redirect URIs exact, rotate refresh tokens, and log failed exchange attempts. Those details usually matter more than the diagram when teams move from local demos into production environments.",
            ],
          },
        ],
        related: [
          {
            title: "Designing Multi-Tenant Identity for SaaS",
            description:
              "Model tenants, clients, and roles without leaking boundaries.",
            href: ROUTES.blogMultiTenant,
          },
          {
            title: "Secure Token Handling in APIs",
            description:
              "See what should happen after your API receives an access token.",
            href: ROUTES.blogTokens,
          },
          {
            title: "Implementing MFA in Identity Platforms",
            description:
              "Combine strong authentication with modern sign-in flows.",
            href: ROUTES.blogMfa,
          },
        ],
      },
        helpers,
      ),
  },
  {
    outputPath: ROUTES.blogRbac,
    title: "RBAC vs ABAC in Enterprise Applications | TokenIDP",
    bodyClass: "blog-article-page",
    activeNav: "blog",
    render: (helpers) =>
      renderBlogArticle(
        {
        category: "Authorization",
        heroTitle: "RBAC vs ABAC in Enterprise Applications",
        dateLabel: "Published March 2026",
        sections: [
          {
            type: "intro",
            body:
              "Role-based access control is easier to explain, easier to operate, and often enough for most product surfaces. Attribute-based access control is more flexible, but that flexibility introduces policy complexity that teams underestimate.",
          },
          {
            title: "Where RBAC Fits",
            paragraphs: [
              "RBAC works well when access maps cleanly to job functions such as admin, support, analyst, or viewer. It keeps tokens understandable and gives product teams a stable contract for common authorization decisions.",
            ],
          },
          {
            title: "Where ABAC Helps",
            paragraphs: [
              "ABAC becomes useful when decisions depend on context such as geography, data sensitivity, subscription tier, or ownership of a resource. It is especially valuable in enterprise systems with cross-cutting policy rules.",
            ],
          },
          {
            title: "Practical Recommendation",
            paragraphs: [
              "Start with RBAC as the main model, then add attribute checks only for the cases roles cannot represent cleanly. That hybrid approach keeps policy authoring manageable while still supporting advanced enterprise controls.",
            ],
          },
        ],
        related: [
          {
            title: "Designing Multi-Tenant Identity for SaaS",
            description: "Tenant boundaries and role design are closely connected.",
            href: ROUTES.blogMultiTenant,
          },
          {
            title: "Implementing MFA in Identity Platforms",
            description:
              "Security policies usually combine authentication and authorization.",
            href: ROUTES.blogMfa,
          },
          {
            title: "Secure Token Handling in APIs",
            description:
              "Token claims only help if your APIs evaluate them correctly.",
            href: ROUTES.blogTokens,
          },
        ],
      },
        helpers,
      ),
  },
  {
    outputPath: ROUTES.blogMfa,
    title: "Implementing MFA in Identity Platforms | TokenIDP",
    bodyClass: "blog-article-page",
    activeNav: "blog",
    render: (helpers) =>
      renderBlogArticle(
        {
        category: "Authentication",
        heroTitle: "Implementing MFA in Identity Platforms",
        dateLabel: "Published March 2026",
        sections: [
          {
            type: "intro",
            body:
              "MFA is most effective when it is policy-driven rather than universally forced. Identity platforms need enough flexibility to require stronger verification for administrators, risky sign-ins, or sensitive actions without blocking every low-risk workflow.",
          },
          {
            title: "Choose the Right Factors",
            paragraphs: [
              "Time-based one-time passwords and email codes are common starting points. Hardware keys and passkeys can be added for higher assurance environments. The right answer depends on the operational model of the customer, not only on the identity platform's technical capabilities.",
            ],
          },
          {
            title: "Policy Before Prompts",
            paragraphs: [
              "Introduce MFA based on role, tenant policy, device state, or transaction risk. That approach keeps prompts predictable and avoids training users to treat every challenge as background noise.",
            ],
          },
          {
            title: "Recovery Matters",
            paragraphs: [
              "Enrollment, backup methods, and account recovery are part of the security model. If those flows are weak, MFA adds user friction without adding much resilience. Treat recovery and revocation as first-class product features.",
            ],
          },
        ],
        related: [
          {
            title: "OAuth2 Authorization Code Flow Explained",
            description:
              "Step-up authentication has to fit into the login flow cleanly.",
            href: ROUTES.blogOauth2,
          },
          {
            title: "RBAC vs ABAC in Enterprise Applications",
            description:
              "Security policy often combines access control with authentication.",
            href: ROUTES.blogRbac,
          },
          {
            title: "Secure Token Handling in APIs",
            description:
              "Post-authentication controls still matter after MFA succeeds.",
            href: ROUTES.blogTokens,
          },
        ],
      },
        helpers,
      ),
  },
  {
    outputPath: ROUTES.blogTokens,
    title: "Secure Token Handling in APIs | TokenIDP",
    bodyClass: "blog-article-page",
    activeNav: "blog",
    render: (helpers) =>
      renderBlogArticle(
        {
        category: "API Security",
        heroTitle: "Secure Token Handling in APIs",
        dateLabel: "Published March 2026",
        sections: [
          {
            type: "intro",
            body:
              "Token-based systems fail in production when APIs treat tokens as opaque blobs of trust. Validation, storage, propagation, and revocation all need explicit handling rules, especially in multi-service architectures.",
          },
          {
            title: "Validate Every Request",
            paragraphs: [
              "APIs should verify signature, issuer, audience, expiration, and any required tenant or role claims before executing business logic. Those checks belong at the edge of the service, not scattered through handlers.",
            ],
          },
          {
            title: "Reduce Token Exposure",
            paragraphs: [
              "Keep tokens out of logs, browser storage where possible, and debugging traces. If tokens move between services, use short lifetimes and narrow scopes so compromise windows stay small.",
            ],
          },
          {
            title: "Prepare for Revocation",
            paragraphs: [
              "Secret rotation, refresh-token invalidation, and emergency tenant lockout procedures should be part of the operational design. Security incidents rarely wait for the next sprint.",
            ],
          },
        ],
        related: [
          {
            title: "OAuth2 Authorization Code Flow Explained",
            description:
              "Token handling starts with the right grant flow and exchange model.",
            href: ROUTES.blogOauth2,
          },
          {
            title: "Designing Multi-Tenant Identity for SaaS",
            description:
              "Tenant claims only help if every API enforces them correctly.",
            href: ROUTES.blogMultiTenant,
          },
          {
            title: "RBAC vs ABAC in Enterprise Applications",
            description:
              "Authorization depends on trustworthy claims and careful evaluation.",
            href: ROUTES.blogRbac,
          },
        ],
      },
        helpers,
      ),
  },
];

const docsPages = createDocsPages();

export const pages = [
  {
    outputPath: ROUTES.home,
    title: "Enterprise Identity Platform for B2B SaaS",
    bodyClass: "landing-page",
    activeNav: null,
    brandLogo: "tokentresor-tt-lock.svg",
    render: renderLandingPage,
    extraScripts: () => [landingPageScript()],
  },
  ...docsPages,
  {
    outputPath: ROUTES.blogs,
    title: "Blog | TokenIDP",
    bodyClass: "blogs-page",
    activeNav: "blog",
    render: renderBlogsIndex,
  },
  ...blogArticles,
  ...useCases,
  {
    outputPath: ROUTES.contact,
    title: "Contact Us | TokenIDP",
    bodyClass: "blog-article-page contact-page",
    activeNav: "contact",
    render: renderContactPage,
  },
];
