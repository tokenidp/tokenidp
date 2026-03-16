import path from "node:path";

const FOOTER_HTML =
  "&copy; 2026 TokenTresor | Identity &amp; Access Platform for B2B SaaS | Built with security, privacy, and scalability in mind.";

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
        <a class="brand" href="${homeHref}" aria-label="TokenTresor home">
          <img src="${logoHref}" alt="TokenTresor" />
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
    <footer>
      ${FOOTER_HTML}
    </footer>
${scripts}
  </body>
</html>
`;
}
