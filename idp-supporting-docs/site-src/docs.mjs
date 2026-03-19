import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const docsRoot = path.resolve(__dirname, "..", "docs-repo");

const SECTION_ORDER = [
  "tutorials",
  "how-to",
  "reference",
  "explanation",
  "admin",
];

const PAGE_ORDER = {
  tutorials: ["index", "getting-started"],
  "how-to": [
    "index",
    "configure-cors",
    "react-sdk",
    "dotnet-api-integration",
    "node-integration",
    "oauth-authorization-code-pkce",
    "oauth-client-credentials",
    "oauth-refresh-token",
    "oauth-device-code",
    "oauth-token-revocation",
    "oauth-token-introspection",
  ],
  reference: [
    "index",
    "supported-specifications",
    "api-scopes-resources",
    "endpoint-authorize",
    "endpoint-token",
    "endpoint-openid-configuration",
    "endpoint-jwks",
    "endpoint-userinfo",
    "endpoint-revoke",
    "endpoint-introspect",
    "endpoint-logout",
  ],
  explanation: [
    "index",
    "overview",
    "security-model",
    "key-management",
    "logs-and-cache",
  ],
  admin: [
    "index",
    "dashboard",
    "applications",
    "tenant-management",
    "user-management",
    "roles-permissions",
    "token-management",
    "mfa-policies",
    "activities",
    "configurations",
    "password-reset",
    "forgot-password",
    "stay-signed-in",
    "external-providers-management",
    "create-free-account",
  ],
};

function toPosix(value) {
  return value.split(path.sep).join("/");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}

function relativePagePath(fromFile, toFile) {
  const targetDir =
    toFile === "index.html" ? "." : path.posix.dirname(toFile);
  const relativeDir =
    path.posix.relative(path.posix.dirname(fromFile), targetDir) || ".";

  return relativeDir === "." ? "./" : `${relativeDir}/`;
}

function relativeAssetPath(fromFile, toFile) {
  return path.posix.relative(path.posix.dirname(fromFile), toFile) || ".";
}

function getTitle(markdown) {
  const match = markdown.match(/^#\s+(.+)$/m);
  return match ? match[1].trim() : "Untitled";
}

function getSlug(relativePath) {
  const basename = path.posix.basename(relativePath, path.posix.extname(relativePath));
  return basename;
}

function getOutputPath(relativePath) {
  if (relativePath === "index.mdx") {
    return "docs/index.html";
  }

  if (relativePath.endsWith("/index.mdx")) {
    const section = relativePath.slice(0, -"/index.mdx".length);
    return `docs/${section}/index.html`;
  }

  return `docs/${relativePath.replace(/\.mdx?$/, "")}/index.html`;
}

function getNavLabel(item) {
  if (item.relativePath === "index.mdx") {
    return "Overview";
  }

  return item.title;
}

function collectDocFiles() {
  const files = [];

  function walk(currentDir) {
    const entries = fs.readdirSync(currentDir, { withFileTypes: true });

    for (const entry of entries) {
      const absolutePath = path.join(currentDir, entry.name);

      if (entry.isDirectory()) {
        if (entry.name === "images") {
          continue;
        }

        walk(absolutePath);
        continue;
      }

      if (!entry.name.endsWith(".mdx") && !entry.name.endsWith(".md")) {
        continue;
      }

      const relativePath = toPosix(path.relative(docsRoot, absolutePath));
      const markdown = fs.readFileSync(absolutePath, "utf8");
      const title = getTitle(markdown);
      const section = relativePath === "index.mdx" ? "root" : relativePath.split("/")[0];

      files.push({
        absolutePath,
        relativePath,
        outputPath: getOutputPath(relativePath),
        markdown,
        title,
        navLabel: title,
        section,
        slug: getSlug(relativePath),
      });
    }
  }

  walk(docsRoot);

  return files;
}

function sortDocs(items) {
  const sectionRank = new Map(SECTION_ORDER.map((section, index) => [section, index]));

  return [...items].sort((left, right) => {
    if (left.section === "root" || right.section === "root") {
      return left.section === "root" ? -1 : 1;
    }

    const leftSectionRank = sectionRank.get(left.section) ?? Number.MAX_SAFE_INTEGER;
    const rightSectionRank = sectionRank.get(right.section) ?? Number.MAX_SAFE_INTEGER;

    if (leftSectionRank !== rightSectionRank) {
      return leftSectionRank - rightSectionRank;
    }

    const order = PAGE_ORDER[left.section] ?? [];
    const leftIndex = order.indexOf(left.slug);
    const rightIndex = order.indexOf(right.slug);

    if (leftIndex !== -1 || rightIndex !== -1) {
      if (leftIndex === -1) return 1;
      if (rightIndex === -1) return -1;
      if (leftIndex !== rightIndex) return leftIndex - rightIndex;
    }

    return left.navLabel.localeCompare(right.navLabel);
  });
}

function buildDocMap(items) {
  return new Map(items.map((item) => [toPosix(item.absolutePath), item]));
}

function renderInline(text, context) {
  const placeholders = [];

  function stash(html) {
    const key = `\u0000${placeholders.length}\u0000`;
    placeholders.push(html);
    return key;
  }

  let output = text;

  output = output.replace(/`([^`]+)`/g, (_, code) =>
    stash(`<code>${escapeHtml(code)}</code>`),
  );

  output = output.replace(/\[([^\]]+)\]\(([^)]+)\)/g, (_, label, target) => {
    const href = resolveLinkTarget(target, context);
    return stash(`<a href="${escapeHtml(href)}">${escapeHtml(label)}</a>`);
  });

  output = output.replace(/\*\*([^*]+)\*\*/g, (_, strong) =>
    stash(`<strong>${escapeHtml(strong)}</strong>`),
  );

  output = escapeHtml(output);

  return output.replace(/\u0000(\d+)\u0000/g, (_, index) => placeholders[Number(index)]);
}

function resolveLinkTarget(target, context) {
  if (
    target.startsWith("http://") ||
    target.startsWith("https://") ||
    target.startsWith("mailto:") ||
    target.startsWith("#")
  ) {
    return target;
  }

  const cleanTarget = target.split("#")[0];
  const hash = target.includes("#") ? `#${target.split("#").slice(1).join("#")}` : "";
  const resolvedPath = path.resolve(path.dirname(context.sourcePath), cleanTarget);
  const resolvedPosix = toPosix(resolvedPath);

  if (cleanTarget.endsWith(".md") || cleanTarget.endsWith(".mdx")) {
    const targetPage = context.docMap.get(resolvedPosix);

    if (!targetPage) {
      return target;
    }

    return `${relativePagePath(context.outputPath, targetPage.outputPath)}${hash}`;
  }

  if (resolvedPosix.startsWith(toPosix(path.join(docsRoot, "images")))) {
    const assetTarget = `docs/images/${path.posix.basename(resolvedPosix)}`;
    return `${relativeAssetPath(context.outputPath, assetTarget)}${hash}`;
  }

  return target;
}

function parseTable(lines, startIndex, context) {
  const tableLines = [];
  let index = startIndex;

  while (index < lines.length && /^\|/.test(lines[index].trim())) {
    tableLines.push(lines[index].trim());
    index += 1;
  }

  if (tableLines.length < 2) {
    return { html: `<p>${renderInline(tableLines[0] ?? "", context)}</p>`, nextIndex: index };
  }

  const rows = tableLines.map((line) =>
    line
      .split("|")
      .slice(1, -1)
      .map((cell) => cell.trim()),
  );

  const header = rows[0];
  const body = rows.slice(2);
  const html = `
    <div class="docs-table-wrap">
      <table class="docs-table">
        <thead>
          <tr>${header.map((cell) => `<th>${renderInline(cell, context)}</th>`).join("")}</tr>
        </thead>
        <tbody>
${body
  .map(
    (row) => `          <tr>${row.map((cell) => `<td>${renderInline(cell, context)}</td>`).join("")}</tr>`,
  )
  .join("\n")}
        </tbody>
      </table>
    </div>`;

  return { html, nextIndex: index };
}

function parseList(lines, startIndex, context, ordered = false) {
  const items = [];
  let index = startIndex;
  const pattern = ordered ? /^\d+\.\s+(.*)$/ : /^-\s+(.*)$/;

  while (index < lines.length) {
    const match = lines[index].match(pattern);

    if (!match) {
      break;
    }

    items.push(match[1]);
    index += 1;
  }

  const tag = ordered ? "ol" : "ul";
  const html = `<${tag}>\n${items
    .map((item) => `  <li>${renderInline(item, context)}</li>`)
    .join("\n")}\n</${tag}>`;

  return { html, nextIndex: index };
}

function parseCodeBlock(lines, startIndex) {
  const firstLine = lines[startIndex];
  const language = firstLine.slice(3).trim();
  const codeLines = [];
  let index = startIndex + 1;

  while (index < lines.length && !lines[index].startsWith("```")) {
    codeLines.push(lines[index]);
    index += 1;
  }

  return {
    html: `<pre><code${language ? ` class="language-${escapeHtml(language)}"` : ""}>${escapeHtml(
      codeLines.join("\n"),
    )}</code></pre>`,
    nextIndex: Math.min(index + 1, lines.length),
  };
}

function parseBlockquote(lines, startIndex, context) {
  const quoteLines = [];
  let index = startIndex;

  while (index < lines.length) {
    const line = lines[index];

    if (!line.startsWith(">")) {
      break;
    }

    quoteLines.push(line.replace(/^>\s?/, ""));
    index += 1;
  }

  const innerHtml = parseMarkdown(quoteLines.join("\n"), context);
  return {
    html: `<blockquote class="docs-callout">${innerHtml}</blockquote>`,
    nextIndex: index,
  };
}

function parseImage(line, context) {
  const match = line.match(/^!\[([^\]]*)\]\(([^)]+)\)$/);

  if (!match) {
    return null;
  }

  const [, alt, target] = match;
  const resolved = resolveLinkTarget(target, context);

  return `<figure class="docs-figure"><img src="${escapeHtml(resolved)}" alt="${escapeHtml(
    alt,
  )}" /></figure>`;
}

function parseParagraph(lines, startIndex, context) {
  const buffer = [];
  let index = startIndex;

  while (index < lines.length) {
    const line = lines[index];
    const trimmed = line.trim();

    if (
      trimmed === "" ||
      trimmed.startsWith("#") ||
      trimmed.startsWith(">") ||
      trimmed.startsWith("|") ||
      trimmed.startsWith("```") ||
      /^!\[/.test(trimmed) ||
      /^-\s+/.test(trimmed) ||
      /^\d+\.\s+/.test(trimmed)
    ) {
      break;
    }

    buffer.push(trimmed);
    index += 1;
  }

  return {
    html: `<p>${renderInline(buffer.join(" "), context)}</p>`,
    nextIndex: index,
  };
}

function parseMarkdown(markdown, context) {
  const lines = markdown.replace(/\r\n/g, "\n").split("\n");
  const blocks = [];
  let index = 0;

  while (index < lines.length) {
    const line = lines[index];
    const trimmed = line.trim();

    if (trimmed === "") {
      index += 1;
      continue;
    }

    if (trimmed.startsWith("```")) {
      const result = parseCodeBlock(lines, index);
      blocks.push(result.html);
      index = result.nextIndex;
      continue;
    }

    const headingMatch = trimmed.match(/^(#{1,6})\s+(.+)$/);

    if (headingMatch) {
      const level = headingMatch[1].length;
      blocks.push(`<h${level}>${renderInline(headingMatch[2], context)}</h${level}>`);
      index += 1;
      continue;
    }

    if (trimmed.startsWith(">")) {
      const result = parseBlockquote(lines, index, context);
      blocks.push(result.html);
      index = result.nextIndex;
      continue;
    }

    if (trimmed.startsWith("|")) {
      const result = parseTable(lines, index, context);
      blocks.push(result.html);
      index = result.nextIndex;
      continue;
    }

    if (/^!\[/.test(trimmed)) {
      const imageHtml = parseImage(trimmed, context);

      if (imageHtml) {
        blocks.push(imageHtml);
        index += 1;
        continue;
      }
    }

    if (/^-\s+/.test(trimmed)) {
      const result = parseList(lines, index, context, false);
      blocks.push(result.html);
      index = result.nextIndex;
      continue;
    }

    if (/^\d+\.\s+/.test(trimmed)) {
      const result = parseList(lines, index, context, true);
      blocks.push(result.html);
      index = result.nextIndex;
      continue;
    }

    const result = parseParagraph(lines, index, context);
    blocks.push(result.html);
    index = result.nextIndex;
  }

  return blocks.join("\n");
}

function buildSidebar(items, activeRelativePath) {
  const rootPage = items.find((item) => item.relativePath === "index.mdx");
  const groups = SECTION_ORDER.map((section) => {
    const sectionItems = items.filter((item) => item.section === section);
    const overview = sectionItems.find((item) => item.slug === "index");
    const children = sectionItems.filter((item) => item.slug !== "index");

    return { section, overview, children };
  }).filter((group) => group.overview);

  return (helpers) => `
      <aside class="docs-sidebar">
        <h2>Topics</h2>
        <a
          class="topic-link${rootPage.relativePath === activeRelativePath ? " active" : ""}"
          href="${helpers.relativePagePath(rootPage.outputPath)}"
        >
          ${getNavLabel(rootPage)}
        </a>
${groups
  .map((group) => {
    const hasActive =
      group.overview.relativePath === activeRelativePath ||
      group.children.some((child) => child.relativePath === activeRelativePath);

    return `        <div class="topic-group${hasActive ? " has-active-topic is-open" : ""}" data-topic-group="${group.section}">
          <button
            class="topic-group-toggle"
            type="button"
            aria-expanded="${hasActive ? "true" : "false"}"
          >
            ${group.overview.title}
          </button>
          <div class="topic-submenu"${hasActive ? "" : ' hidden'}>
            <a
              class="topic-link topic-sublink${group.overview.relativePath === activeRelativePath ? " active" : ""}"
              href="${helpers.relativePagePath(group.overview.outputPath)}"
            >
              Overview
            </a>
${group.children
  .map(
    (child) => `            <a
              class="topic-link topic-sublink${child.relativePath === activeRelativePath ? " active" : ""}"
              href="${helpers.relativePagePath(child.outputPath)}"
            >
              ${child.title}
            </a>`,
  )
  .join("\n")}
          </div>
        </div>`;
  })
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

function renderDocPage(item, sidebar, docMap) {
  return (helpers) => {
    const html = parseMarkdown(item.markdown, {
      sourcePath: item.absolutePath,
      outputPath: item.outputPath,
      docMap,
    });

    return `
    <section class="docs-wrap">
${sidebar(helpers)}
      <article class="docs-content docs-markdown">
${html}
      </article>
    </section>`;
  };
}

export function createDocsPages() {
  const collectedItems = sortDocs(collectDocFiles());
  const docMap = buildDocMap(collectedItems);

  return collectedItems.map((item) => {
    const sidebar = buildSidebar(collectedItems, item.relativePath);

    return {
      outputPath: item.outputPath,
      title: `${item.title} | Docs | TokenIDP`,
      bodyClass: "docs-page",
      activeNav: "docs",
      render: renderDocPage(item, sidebar, docMap),
      extraScripts: () => [docsScript()],
    };
  });
}
