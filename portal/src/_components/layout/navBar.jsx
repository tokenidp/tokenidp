import { useEffect, useMemo, useState } from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "tokenidp-react";
import useTree from "../../_hooks/useTree";
import logo from "../../_assets/images/TokenIDP.svg";

const SidebarIcon = ({ name }) => {
  const props = {
    width: 18,
    height: 18,
    viewBox: "0 0 24 24",
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 2,
    strokeLinecap: "round",
    strokeLinejoin: "round",
    className: "sidebar-icon",
    "aria-hidden": "true",
  };

  switch (name) {
    case "dashboard":
      return (
        <svg {...props}>
          <rect x="3" y="3" width="7" height="7" rx="1" />
          <rect x="14" y="3" width="7" height="7" rx="1" />
          <rect x="3" y="14" width="7" height="7" rx="1" />
          <rect x="14" y="14" width="7" height="7" rx="1" />
        </svg>
      );
    case "layers":
      return (
        <svg {...props}>
          <path d="M12 3 4 7.5l8 4.5 8-4.5L12 3z" />
          <path d="m4 12 8 4.5 8-4.5" />
          <path d="m4 16.5 8 4.5 8-4.5" />
        </svg>
      );
    case "database":
      return (
        <svg {...props}>
          <ellipse cx="12" cy="6" rx="8" ry="3" />
          <path d="M4 6v6c0 1.657 3.582 3 8 3s8-1.343 8-3V6" />
          <path d="M4 12v6c0 1.657 3.582 3 8 3s8-1.343 8-3v-6" />
        </svg>
      );
    case "building":
      return (
        <svg {...props}>
          <path d="M4 21V5a1 1 0 0 1 1-1h14a1 1 0 0 1 1 1v16" />
          <path d="M9 21V9h6v12" />
          <path d="M8 5h8" />
          <path d="M12 13h.01" />
          <path d="M12 17h.01" />
          <path d="M16 13h.01" />
          <path d="M16 17h.01" />
          <path d="M8 13h.01" />
          <path d="M8 17h.01" />
        </svg>
      );
    case "users":
      return (
        <svg {...props}>
          <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
          <circle cx="9" cy="7" r="4" />
          <path d="M22 21v-2a4 4 0 0 0-3-3.87" />
          <path d="M16 3.13a4 4 0 0 1 0 7.75" />
        </svg>
      );
    case "shield":
      return (
        <svg {...props}>
          <path d="M12 22s8-3.5 8-10V5l-8-3-8 3v7c0 6.5 8 10 8 10z" />
        </svg>
      );
    case "shield-check":
      return (
        <svg {...props}>
          <path d="M12 22s8-3.5 8-10V5l-8-3-8 3v7c0 6.5 8 10 8 10z" />
          <path d="m9 12 2 2 4-4" />
        </svg>
      );
    case "key":
      return (
        <svg {...props}>
          <circle cx="7.5" cy="15.5" r="5.5" />
          <path d="m21 2-9.6 9.6" />
          <path d="m15.5 7.5 3 3L22 7l-3-3" />
        </svg>
      );
    case "activity":
      return (
        <svg {...props}>
          <path d="M3 12h4l3 6 4-12 3 6h4" />
        </svg>
      );
    case "settings":
      return (
        <svg {...props}>
          <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
          <circle cx="12" cy="12" r="3" />
        </svg>
      );
    case "user":
      return (
        <svg {...props}>
          <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2" />
          <circle cx="12" cy="7" r="4" />
        </svg>
      );
    case "help-circle":
      return (
        <svg {...props}>
          <circle cx="12" cy="12" r="10" />
          <path d="M9.09 9a3 3 0 1 1 5.83 1c0 2-3 2.5-3 2.5" />
          <path d="M12 17h.01" />
        </svg>
      );
    case "log-out":
      return (
        <svg {...props}>
          <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
          <path d="M16 17l5-5-5-5" />
          <path d="M21 12H9" />
        </svg>
      );
    default:
      return (
        <svg {...props}>
          <rect x="4" y="4" width="16" height="16" rx="3" />
        </svg>
      );
  }
};

const getSidebarIconName = (item) => {
  const key = String(
    item.permissionKey || item.permissionName || item.url || "",
  ).toLowerCase();
  if (key.includes("dashboard")) return "dashboard";
  if (key.includes("application")) return "layers";
  if (key.includes("api") || key.includes("resource")) return "database";
  if (key.includes("tenant")) return "building";
  if (key.includes("user") && !key.includes("permission")) return "users";
  if (key.includes("role")) return "shield";
  if (key.includes("permission")) return "shield-check";
  if (key.includes("token")) return "key";
  if (key.includes("activity")) return "activity";
  if (key.includes("setting")) return "settings";
  if (key.includes("ciba") || key.includes("backchannel")) return "shield-check";
  if (key.includes("profile")) return "user";
  if (key.includes("help")) return "help-circle";
  if (key.includes("log")) return "log-out";
  return "dashboard";
};

function NavBar({ onClick, isOpen, onNavigate, onToggleTheme, theme }) {
  const user = useAuth();
  const { createTree } = useTree();
  const [openGroup, setOpenGroup] = useState(null);
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const location = useLocation();
  const navigate = useNavigate();
  const pathname = (location.pathname.replace(/\/+$/, "") || "/").toLowerCase();

  const items = useMemo(() => {
    if (!user?.permissions?.length) return [];

    const normalized = user.permissions.map((value) => {
      const id = value.id || value.Id;

      const parentId = value.parentId || value.ParentId || null;

      const controlType = (
        value.controlType ||
        value.ControlType ||
        ""
      ).toLowerCase();
      const rawUrl = value.url || value.Url;
      const url = rawUrl && rawUrl !== "null" ? rawUrl : "";
      const permissionKey = value.permissionKey || value.PermissionKey || "";

      const resolvedIcon =
        value.icon ||
        value.Icon ||
        (permissionKey.toLowerCase().includes("user.management")
          ? "fa-users-gear"
          : "");

      return {
        ...value,
        id,
        claimId: id,
        parentId,
        controlType,
        url,
        permissionName: value.permissionName || value.PermissionName,
        permissionKey,
        icon: resolvedIcon,
      };
    });

    const tree = createTree(normalized);
    return tree || [];
  }, [createTree, user?.permissions]);

  const normalizeUrl = (url) => {
    if (!url) return "";
    const trimmed = String(url).trim();
    if (!trimmed) return "";
    const withSlash = trimmed.startsWith("/") ? trimmed : `/${trimmed}`;
    return withSlash.replace(/\/+$/, "") || "/";
  };

  const isPathActive = (url) => {
    const normalizedUrl = normalizeUrl(url).toLowerCase();
    if (!normalizedUrl) return false;
    if (normalizedUrl === "/") {
      return pathname === "/";
    }
    return (
      pathname === normalizedUrl || pathname.startsWith(`${normalizedUrl}/`)
    );
  };

  const activeGroupId = useMemo(() => {
    const match = items.find((item) => {
      const controlType = (item.controlType || "").toLowerCase();
      if (controlType !== "navgroup") return false;
      const hasActiveChild =
        Array.isArray(item.childrens) &&
        item.childrens.some((child) => isPathActive(child.url));
      return hasActiveChild || isPathActive(item.url);
    });
    return match ? `sidebar-item-${match.id}` : null;
  }, [items, pathname]);

  useEffect(() => {
    setOpenGroup(activeGroupId);
  }, [activeGroupId]);

  useEffect(() => {
    const handleClickOutside = (e) => {
      if (!e.target.closest(".sidebar-user-profile")) {
        setIsUserMenuOpen(false);
      }
    };
    document.addEventListener("click", handleClickOutside);
    return () => document.removeEventListener("click", handleClickOutside);
  }, []);

  return (
    <aside className={`sidebar ${isOpen ? "active" : ""}`}>
      <div className="brand">
        <img
          src={logo}
          alt="TokenIDP Logo"
          className="brand-logo"
          width={150}
        />
      </div>
      <div className="sidebar-scroll">
        <div className="accordion" id="sidebarAccordion">
          {items.map((item) => {
            const controlType = (item.controlType || "").toLowerCase();
            const hasChildren =
              Array.isArray(item.childrens) && item.childrens.length > 0;
            const url = item.url;
            const normalizedUrl = normalizeUrl(url);
            const label = item.permissionName || "Menu";
            const iconName = getSidebarIconName(item);
            const accordionId = `sidebar-item-${item.id}`;

            const normalizedLabel = label ? label.trim().toLowerCase() : "";
            const showDividerBefore = normalizedLabel === "activities";
            const showDividerAfter = normalizedLabel === "dashboard";

            if (controlType === "navgroup" && hasChildren) {
              const isGroupActive =
                (Array.isArray(item.childrens) &&
                  item.childrens.some((child) => isPathActive(child.url))) ||
                isPathActive(url);
              const isOpen = openGroup === accordionId || isGroupActive;
              return (
                <div
                  className="accordion-item border-0 bg-transparent"
                  key={item.id}
                >
                  {showDividerBefore && <div className="sidebar-divider"></div>}
                  <h2 className="accordion-header" id={`${accordionId}-header`}>
                    <button
                      className={`accordion-button sidebar-accordion-button ${
                        isOpen ? "" : "collapsed"
                      } ${isGroupActive ? "active" : ""}`}
                      type="button"
                      aria-expanded={isOpen}
                      aria-controls={`${accordionId}-collapse`}
                      onClick={() => {
                        setOpenGroup((prev) =>
                          prev === accordionId ? null : accordionId,
                        );
                        onClick?.(item.id, label);
                      }}
                    >
                      <SidebarIcon name={iconName} />
                      {label}
                      <span className="sidebar-chevron">
                        <i
                          className={`fa fa-angle-${isOpen ? "down" : "right"}`}
                        ></i>
                      </span>
                    </button>
                  </h2>
                  <div
                    id={`${accordionId}-collapse`}
                    className={`accordion-collapse collapse ${isOpen ? "show" : ""}`}
                    aria-labelledby={`${accordionId}-header`}
                  >
                    <div className="accordion-body p-0">
                      <ul className="nav flex-column sidebar-subnav ps-4 mt-3">
                        {item.childrens
                          .filter((child) => {
                            const childType = (
                              child.controlType || ""
                            ).toLowerCase();
                            return childType === "navlink" && !!child.url;
                          })
                          .map((child) => (
                            <li key={child.id}>
                              <NavLink
                                to={normalizeUrl(child.url)}
                                className={() =>
                                  `nav-link ${isPathActive(child.url) ? "active" : ""}`
                                }
                                onClick={() => {
                                  onClick?.(child.id, child.permissionName);
                                  onNavigate?.();
                                }}
                              >
                                <SidebarIcon name={getSidebarIconName(child)} />
                                {child.permissionName}
                              </NavLink>
                            </li>
                          ))}
                      </ul>
                    </div>
                  </div>
                  {showDividerAfter && <div className="sidebar-divider"></div>}
                </div>
              );
            }

            if (controlType === "navgroup" && !hasChildren && url) {
              return (
                <div key={item.id}>
                  {showDividerBefore && <div className="sidebar-divider"></div>}
                  <NavLink
                    to={normalizedUrl}
                    className={() =>
                      `nav-link ${isPathActive(url) ? "active" : ""}`
                    }
                    onClick={() => {
                      onClick?.(item.id, label);
                      onNavigate?.();
                    }}
                  >
                    <SidebarIcon name={iconName} />
                    {label}
                  </NavLink>
                  {showDividerAfter && <div className="sidebar-divider"></div>}
                </div>
              );
            }

            if (controlType === "navlink" && url) {
              return (
                <div key={item.id}>
                  {showDividerBefore && <div className="sidebar-divider"></div>}
                  <NavLink
                    to={normalizedUrl}
                    className={() =>
                      `nav-link ${isPathActive(url) ? "active" : ""}`
                    }
                    onClick={() => {
                      onClick?.(item.id, label);
                      onNavigate?.();
                    }}
                  >
                    <SidebarIcon name={iconName} />
                    {label}
                  </NavLink>
                  {showDividerAfter && <div className="sidebar-divider"></div>}
                </div>
              );
            }

            return null;
          })}
        </div>
      </div>

      <div className="sidebar-user-profile">
        <div
          className="user-profile-header"
          onClick={() => setIsUserMenuOpen((prev) => !prev)}
          aria-expanded={isUserMenuOpen}
        >
          <div className="user-avatar">
            {user?.userData?.firstName?.charAt(0)}
            {user?.userData?.lastName?.charAt(0) || ""}
          </div>
          <div className="user-info">
            <div className="user-name">
              {(
                user?.userData?.firstName ||
                user?.firstName ||
                user?.userName ||
                ""
              ).trim()}{" "}
              {(user?.userData?.lastName || user?.lastName || "").trim()}
            </div>
          </div>
        </div>

        <div className={`user-dropdown ${isUserMenuOpen ? "active" : ""}`}>
          <button
            className="user-dropdown-item"
            onClick={() => {
              onToggleTheme?.();
              setIsUserMenuOpen(false);
            }}
          >
            <i className="fa fa-adjust"></i>
            {theme === "dark" ? "Light Mode" : "Dark Mode"}
          </button>
          <button
            className="user-dropdown-item"
            onClick={() => {
              navigate("/profile");
              setIsUserMenuOpen(false);
            }}
          >
            <i className="fa fa-user"></i>
            Profile
          </button>
          <button
            className="user-dropdown-item"
            onClick={() => {
              navigate("/help");
              setIsUserMenuOpen(false);
            }}
          >
            <i className="fa fa-question-circle"></i>
            Help
          </button>
          <button
            className="user-dropdown-item"
            onClick={() => {
              user?.logout?.();
              setIsUserMenuOpen(false);
            }}
          >
            <i className="fa fa-sign-out"></i>
            Log out
          </button>
        </div>
      </div>
    </aside>
  );
}

export default NavBar;
