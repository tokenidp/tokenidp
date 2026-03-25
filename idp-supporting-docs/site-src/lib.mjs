import path from "node:path";

const COMMON_SCRIPT = `document
  .querySelectorAll("a, button, summary, [role='button']")
  .forEach((el) => el.style.setProperty("cursor", "pointer", "important"));`;

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function relativePath(fromFile, toFile) {
  return path.posix.relative(path.posix.dirname(fromFile), toFile) || ".";
}

function relativePagePath(fromFile, toFile) {
  const targetDir =
    toFile === "index.html" ? "." : path.posix.dirname(toFile);
  const relativeDir =
    path.posix.relative(path.posix.dirname(fromFile), targetDir) || ".";

  return relativeDir === "." ? "./" : `${relativeDir}/`;
}

function renderNav(page) {
  const logoFile = page.brandLogo ?? "tokentresor-wordmark-core.svg";
  const homeHref = relativePagePath(page.outputPath, "index.html");
  const docsHref = relativePagePath(page.outputPath, "docs/index.html");
  const blogsHref = relativePagePath(page.outputPath, "blogs/index.html");
  const contactHref = relativePagePath(page.outputPath, "contact/index.html");
  const usecaseSaasHref = relativePagePath(
    page.outputPath,
    "usecases/b2b-saas/index.html",
  );
  const usecaseComplianceHref = relativePagePath(
    page.outputPath,
    "usecases/compliance/index.html",
  );
  const usecaseApiHref = relativePagePath(
    page.outputPath,
    "usecases/api-platform-builders/index.html",
  );
  const enterpriseHref = relativePagePath(
    page.outputPath,
    "usecases/enterprise-architecture/index.html",
  );
  const logoHref = relativePath(page.outputPath, `assets/logos/${logoFile}`);

  const homeClass = page.activeNav === "home" ? ' class="active-link"' : "";
  const docsClass = page.activeNav === "docs" ? ' class="active-link"' : "";
  const blogsClass = page.activeNav === "blog" ? ' class="active-link"' : "";
  const contactClass =
    page.activeNav === "contact" ? ' class="active-link"' : "";

  return `
    <nav class="top-nav">
      <div class="nav-inner">
        <a class="brand" href="${homeHref}" aria-label="TokenIDP home">
          <img src="${logoHref}" alt="TokenIDP" />
        </a>
        <div class="nav-links">
          <a href="${homeHref}"${homeClass}>Home</a>
          <div class="usecases-dropdown">
            <button
              class="usecases-trigger"
              type="button"
              aria-haspopup="true"
              aria-expanded="false"
            >
              Use Cases
            </button>
            <div class="usecases-menu">
              <a href="${usecaseSaasHref}">B2B SaaS Applications</a>
              <a href="${usecaseComplianceHref}">Compliance-Driven Teams</a>
              <a href="${usecaseApiHref}">API Platform Builders</a>
              <a href="${enterpriseHref}">Enterprise Architecture</a>
            </div>
          </div>
          <a href="${docsHref}"${docsClass}>Docs</a>
          <a href="${blogsHref}"${blogsClass}>Blog</a>
          <a href="${contactHref}"${contactClass}>Contact us</a>
        </div>
      </div>
    </nav>`;
}

function renderFooter(page) {
  const getStartedHref = relativePagePath(
    page.outputPath,
    "docs/getting-started/index.html",
  );
  const contactHref = relativePagePath(page.outputPath, "contact/index.html");

  return `
    <footer class="footer-section">
      <div class="footer-wave" aria-hidden="true">
        <svg viewBox="0 0 1440 160" preserveAspectRatio="none">
          <path fill="#00A9FF" d="M0,64C120,140,240,150,360,110C480,70,600,0,720,10C840,20,960,110,1080,130C1200,150,1320,100,1440,40L1440,160L0,160Z"></path>
        </svg>
      </div>
      <div class="footer-content">
        <div class="container">
        <div class="footer-grid">
          <div class="footer-brand">
            <div class="logo">
              <span>TokenIDP</span>
            </div>
            <p>
              TokenIDP helps teams manage authentication, authorization, tenant
              access, and identity operations through a standards-based platform
              built for modern B2B SaaS applications.
            </p>
          </div>
          <div class="footer-cta">
            <h3 class="footer-title">Start Building Secure Applications Today</h3>
            <div class="footer-cta-actions">
              <a class="footer-cta-btn footer-cta-btn-primary" href="${getStartedHref}">Get Started</a>
              <a class="footer-cta-btn footer-cta-btn-secondary" href="${contactHref}">Schedule a Demo</a>
            </div>
          </div>
        </div>
        <div class="footer-bottom">
          &copy; 2026 TokenIDP | Built with security, privacy, and scalability in mind.
        </div>
        </div>
      </div>
    </footer>`;
}

export function renderPage(page) {
  const helpers = {
    escapeHtml,
    relativePath: (target) => relativePath(page.outputPath, target),
    relativePagePath: (target) => relativePagePath(page.outputPath, target),
  };
  const stylesheetHref = helpers.relativePath("assets/styles/site.css");
  const extraScripts = page.extraScripts?.(helpers) ?? [];
  const scripts = [...extraScripts, COMMON_SCRIPT]
    .map((script) => `    <script>\n${script}\n    </script>`)
    .join("\n");

  return `<!doctype html>
<html lang="en">
  <head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>${escapeHtml(page.title)}</title>
    <link rel="stylesheet" href="${stylesheetHref}" />
  </head>
  <body class="${escapeHtml(page.bodyClass)}">
${renderNav(page)}
    <main>
${page.render(helpers)}
    </main>
${renderFooter(page)}
${scripts}
  </body>
</html>
`;
}
