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

function renderUseCasePage(useCase) {
  return `
    <section class="container blog-hero">
      <span class="blog-category">Use Case</span>
      <h1>${useCase.heroTitle}</h1>
      <p class="blog-date">
        ${useCase.heroSubtitle}
      </p>
    </section>

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
    <section class="container blog-header">
      <h1>Engineering Blog</h1>
      <p>
        Insights on OAuth2, identity architecture, security hardening, and SaaS
        engineering patterns teams can apply in production.
      </p>
    </section>

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

const DOC_TOPICS = {
  "getting-started": `
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
  oauth: `
          <h2>OAuth 2.1 Flows</h2>
          <p>
            Use Authorization Code + PKCE for browser and mobile apps. Use Client
            Credentials for service-to-service communication.
          </p>
          <h3>Recommended By Client Type</h3>
          <ul>
            <li>Public apps: Authorization Code with PKCE.</li>
            <li>Backend services: Client Credentials.</li>
            <li>Long sessions: Refresh tokens with rotation.</li>
          </ul>
        `,
  rbac: `
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
  tenants: `
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
  mfa: `
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
  audit: `
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
  api: `
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
};

function renderDocsPage() {
  return `
    <header class="main-header">
      <h1>Documentation</h1>
      <p>
        Browse platform topics from the left navigation. Detailed guidance loads
        on the right.
      </p>
    </header>

    <section class="docs-wrap">
      <aside class="docs-sidebar">
        <h2>Topics</h2>
        <button class="topic-link active" data-topic="getting-started">
          Getting Started
        </button>
        <button class="topic-link" data-topic="oauth">OAuth 2.1 Flows</button>
        <button class="topic-link" data-topic="rbac">Roles and Permissions</button>
        <button class="topic-link" data-topic="tenants">Multi-Tenant Setup</button>
        <button class="topic-link" data-topic="mfa">MFA Policies</button>
        <button class="topic-link" data-topic="audit">Audit Logs</button>
        <button class="topic-link" data-topic="api">API Integration</button>
      </aside>

      <article class="docs-content" id="docs-content"></article>
    </section>`;
}

function docsScript() {
  return `const topicData = ${JSON.stringify(DOC_TOPICS, null, 2)};

const topicButtons = document.querySelectorAll(".topic-link");
const docsContent = document.getElementById("docs-content");

function renderTopic(topicKey) {
  docsContent.innerHTML = topicData[topicKey] || "<h2>Topic not found</h2>";
}

topicButtons.forEach((button) => {
  button.addEventListener("click", () => {
    topicButtons.forEach((item) => item.classList.remove("active"));
    button.classList.add("active");
    renderTopic(button.dataset.topic);
  });
});

renderTopic("getting-started");`;
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

function renderContactPage() {
  return `
    <section class="container blog-hero">
      <span class="blog-category">Contact</span>
      <h1>Talk to the TokenTresor Team</h1>
      <p class="blog-date">
        Reach out about product fit, deployment questions, or a short demo
      </p>
    </section>
${renderContactSection()}`;
}

function renderLandingPage(helpers) {
  const contactHref = helpers.relativePagePath(ROUTES.contact);
  return `
    <header class="main-header">
      <h1>One Identity for Every App and Service</h1>
      <p>
        OAuth2, OpenID Connect, RBAC, and User Management, built from real-world
        experience, simple to configure &amp; deploy, and easy to operate.
      </p>
      <a class="btn" href="${contactHref}">Schedule a Demo</a>
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
      <h2>Why Use a Dedicated Identity Platform?</h2>
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
      "Manage users, login, and access across web apps, APIs, and internal tools from one place.",
  },
  {
    icon: "security",
    title: "Security Standards",
    description:
      "Built-in OAuth 2.1, OpenID Connect, RBAC, MFA, and GDPR by design help meet modern security expectations.",
  },
  {
    icon: "api",
    title: "API Token Access",
    description:
      "Secure access tokens let APIs and services authenticate and authorize every request consistently.",
  },
  {
    icon: "growth",
    title: "Faster Development",
    description:
      "Avoid building authentication from scratch and reduce delivery time for new applications.",
  },
  {
    icon: "audit",
    title: "Operational Visibility",
    description:
      "Monitor authentication activity from one dashboard for faster operations and investigation.",
  },
]
  .map(renderFeatureCard)
  .join("\n")}
      </div>
    </section>

    <section id="usecase-integration" class="container">
      <h2>How It Works (High-Level Architecture)</h2>
      <p class="architecture-intro">
        Your application delegates authentication and authorization to the
        Identity Platform, while keeping full control of business logic and
        data.
      </p>

      <div class="diagram">
        User Browser / Mobile App | | (Login / SSO) v Identity Platform (Auth,
        Tokens, RBAC) | | (Access Token with roles &amp; tenant) v Your API /
        Backend Services | v Business Data &amp; Applications
      </div>

      <p class="mt-16">
        The platform issues secure tokens after login. Your backend validates
        these tokens and enforces access rules based on tenant and roles.
      </p>
    </section>

    <section class="container">
      <h2>Integration Examples</h2>
      <div class="grid">
        <div class="card">
          <h3>Frontend (React)</h3>
          <p>
            Integrate login into your web app with a client SDK. Users
            authenticate via the Identity Platform and return with a secure
            session.
          </p>
          <pre class="diagram">
npm install @yourcompany/identity-sdk

&lt;IdentityProvider clientId="..." domain="https://auth.yourcompany.com"&gt;
  &lt;App /&gt;
&lt;/IdentityProvider&gt;
      </pre>
        </div>
        <div class="card">
          <h3>Backend (API)</h3>
          <p>
            Protect APIs by validating access tokens. Roles and tenant
            identifiers are used to authorize requests.
          </p>
          <pre class="diagram">
GET /api/orders
Authorization: Bearer &lt;access_token&gt;

-&gt; Validate token
-&gt; Check role &amp; tenant
-&gt; Allow or deny access
      </pre>
        </div>
      </div>
    </section>
${renderContactSection()}`;
}

const useCases = [
  {
    outputPath: ROUTES.usecaseSaas,
    title: "B2B SaaS Applications | TokenTresor",
    bodyClass: "blog-article-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "B2B SaaS Applications",
        heroSubtitle:
          "Identity infrastructure for scalable multi-tenant SaaS platforms",
        pageTitle: "Identity Infrastructure for B2B SaaS Platforms",
        intro:
          "Building a B2B SaaS platform means managing users from multiple organizations while ensuring each customer's data and access remain secure and isolated. Authentication, user management, and role-based permissions quickly become complex as your product grows. A dedicated identity platform like TokenTresor simplifies this challenge by providing centralized authentication, secure access control, and tenant-based user management for all your applications. Instead of building identity infrastructure from scratch, SaaS teams can integrate a ready-to-use platform and focus on delivering core product features.",
        features: [
          {
            icon: "multiTenant",
            title: "Multi-Tenant Identity",
            description:
              "TokenTresor keeps each customer organization isolated with its own users, roles, and authentication settings on one secure platform.",
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
            "Building a secure identity system internally can take months and requires deep security expertise. TokenTresor delivers these capabilities out of the box so product teams can focus on shipping customer-facing features instead of maintaining identity infrastructure.",
        },
      }),
  },
  {
    outputPath: ROUTES.usecaseCompliance,
    title: "Compliance-Driven Teams | TokenTresor",
    bodyClass: "blog-article-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "Compliance-Driven Teams",
        heroSubtitle:
          "Identity controls for regulated and security-sensitive organizations",
        pageTitle: "Identity Platform for Compliance-Driven Organizations",
        intro:
          "Organizations operating in regulated industries must meet strict security and privacy requirements when managing user identities and system access. Authentication systems must ensure that only authorized users can access sensitive data while maintaining clear audit trails of login activity and permission changes. TokenTresor provides a secure, self-hosted identity platform designed to help teams implement strong authentication, centralized access control, and detailed visibility into user activity. By separating identity management from business applications, organizations can maintain consistent security policies while supporting compliance and operational governance.",
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
            "Compliance-driven teams in healthcare, finance, insurance, government, and privacy-sensitive SaaS environments need secure authentication, clear auditability, least-privilege access control, and internal data ownership. TokenTresor is especially well suited to these organizations because it is self-hosted and keeps identity operations inside the customer's own environment.",
        },
      }),
  },
  {
    outputPath: ROUTES.usecaseApi,
    title: "API Platform Builders | TokenTresor",
    bodyClass: "blog-article-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "API Platform Builders",
        heroSubtitle:
          "Identity infrastructure for token-secured APIs and distributed services",
        pageTitle: "Identity Infrastructure for API Platforms",
        intro:
          "Modern applications rely heavily on APIs to connect services, applications, and external integrations. Securing these APIs while managing authentication for different clients can quickly become complex. TokenTresor provides a centralized identity platform designed for API-driven systems, enabling applications to authenticate users and services securely using standardized token-based access. With support for OAuth2 and OpenID Connect, development teams can protect APIs, manage client applications, and control access permissions across distributed systems.",
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
            "Teams building public APIs, partner integrations, internal developer platforms, and microservices architectures need centralized identity, client registration, and reliable token validation. TokenTresor gives API platform teams one secure control point for authentication and authorization across distributed systems.",
        },
      }),
  },
  {
    outputPath: ROUTES.usecaseEnterprise,
    title: "Enterprise Architecture | TokenTresor",
    bodyClass: "blog-article-page",
    activeNav: null,
    render: (helpers) =>
      renderUseCasePage({
        heroTitle: "Enterprise Architecture",
        heroSubtitle:
          "Identity infrastructure for large application ecosystems and internal platforms",
        pageTitle: "Identity Platform for Enterprise Architectures",
        intro:
          "Large organizations often operate multiple applications, services, and internal platforms that require secure and consistent identity management. Without a centralized identity system, authentication and access control can become fragmented across systems. TokenTresor provides a unified identity platform that enables enterprises to manage authentication, authorization, and security policies across all applications while maintaining a scalable and standards-based architecture.",
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
            "Customer portals, employee portals, partner systems, mobile apps, internal APIs, and legacy applications all need consistent authentication and authorization. TokenTresor gives enterprise architects a central identity layer that reduces duplication, aligns policy enforcement, and scales across diverse application environments.",
        },
      }),
  },
];

const blogArticles = [
  {
    outputPath: ROUTES.blogMultiTenant,
    title: "Designing Multi-Tenant Identity for B2B SaaS | TokenTresor",
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
    title: "OAuth2 Authorization Code Flow Explained | TokenTresor",
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
    title: "RBAC vs ABAC in Enterprise Applications | TokenTresor",
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
    title: "Implementing MFA in Identity Platforms | TokenTresor",
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
    title: "Secure Token Handling in APIs | TokenTresor",
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

export const pages = [
  {
    outputPath: ROUTES.home,
    title: "Enterprise Identity Platform for B2B SaaS",
    bodyClass: "landing-page",
    activeNav: null,
    brandLogo: "tokentresor-tt-lock.svg",
    render: renderLandingPage,
  },
  {
    outputPath: ROUTES.docs,
    title: "Docs | Enterprise Identity Platform",
    bodyClass: "docs-page",
    activeNav: "docs",
    render: renderDocsPage,
    extraScripts: () => [docsScript()],
  },
  {
    outputPath: ROUTES.blogs,
    title: "Blog | TokenTresor",
    bodyClass: "blogs-page",
    activeNav: "blog",
    render: renderBlogsIndex,
  },
  ...blogArticles,
  ...useCases,
  {
    outputPath: ROUTES.contact,
    title: "Contact Us | TokenTresor",
    bodyClass: "blog-article-page contact-page",
    activeNav: "contact",
    render: renderContactPage,
  },
];
