import { useEffect, useMemo, useState } from "react";
import { NavLink, useLocation } from "react-router-dom";
import { useAuth } from "@tokentresor/idp-react";
import useTree from "../../_hooks/useTree";

function NavBar({ onClick, isOpen, onNavigate }) {
  const user = useAuth();
  const { createTree } = useTree();
  const [openGroup, setOpenGroup] = useState(null);
  const location = useLocation();
  const pathname = (location.pathname.replace(/\/+$/, "") || "/").toLowerCase();

  const items = useMemo(() => {
    if (!user?.permissions?.length) return [];

    const normalized = user.permissions.map((value) => {

      const id =
        value.id ||
        value.Id;

      const parentId =
        value.parentId ||
        value.ParentId ||
        null;

      const controlType = (value.controlType || value.ControlType || "").toLowerCase();
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
      pathname === normalizedUrl ||
      pathname.startsWith(`${normalizedUrl}/`)
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

  return (
    <aside className={`sidebar ${isOpen ? "active" : ""}`}>
      <div className="brand">SmartDevCon</div>
      <div className="accordion" id="sidebarAccordion">
        {items.map((item) => {
          const controlType = (item.controlType || "").toLowerCase();
          const hasChildren = Array.isArray(item.childrens) && item.childrens.length > 0;
          const url = item.url;
          const normalizedUrl = normalizeUrl(url);
          const label = item.permissionName || "Menu";
          const iconClass = item.icon ? `fa ${item.icon}` : "fa fa-circle";
          const accordionId = `sidebar-item-${item.id}`;

          const showDivider =
            label && label.trim().toLowerCase() === "activities";

          if (controlType === "navgroup" && hasChildren) {
            const isGroupActive =
              (Array.isArray(item.childrens) &&
                item.childrens.some((child) => isPathActive(child.url))) ||
              isPathActive(url);
            const isOpen = openGroup === accordionId || isGroupActive;
            return (
              <div className="accordion-item border-0 bg-transparent" key={item.id}>
                {showDivider && <div className="sidebar-divider"></div>}
                <h2 className="accordion-header" id={`${accordionId}-header`}>
                  <button
                    className={`accordion-button sidebar-accordion-button ${
                      isOpen ? "" : "collapsed"
                    } ${isGroupActive ? "active" : ""}`}
                    type="button"
                    aria-expanded={isOpen}
                    aria-controls={`${accordionId}-collapse`}
                    onClick={() => {
                      setOpenGroup((prev) => (prev === accordionId ? null : accordionId));
                      onClick?.(item.id, label);
                    }}
                  >
                    <i className={iconClass}></i>
                    {label}
                    <span className="sidebar-chevron">
                      <i className={`fa fa-angle-${isOpen ? "down" : "right"}`}></i>
                    </span>
                  </button>
                </h2>
                <div
                  id={`${accordionId}-collapse`}
                  className={`accordion-collapse collapse ${isOpen ? "show" : ""}`}
                  aria-labelledby={`${accordionId}-header`}
                >
                  <div className="accordion-body p-0">
                    <ul className="nav flex-column sidebar-subnav">
                      {item.childrens
                        .filter((child) => {
                          const childType = (child.controlType || "").toLowerCase();
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
                              <i className={`fa ${child.icon || "fa-circle"}`}></i>
                              {child.permissionName}
                            </NavLink>
                          </li>
                        ))}
                    </ul>
                  </div>
                </div>
              </div>
            );
          }

          if (controlType === "navgroup" && !hasChildren && url) {
            return (
              <div key={item.id}>
                {showDivider && <div className="sidebar-divider"></div>}
                <NavLink
                  to={normalizedUrl}
                  className={() => `nav-link ${isPathActive(url) ? "active" : ""}`}
                  onClick={() => {
                    onClick?.(item.id, label);
                    onNavigate?.();
                  }}
                >
                  <i className={iconClass}></i>
                  {label}
                </NavLink>
              </div>
            );
          }

          if (controlType === "navlink" && url) {
            return (
              <div key={item.id}>
                {showDivider && <div className="sidebar-divider"></div>}
                <NavLink
                  to={normalizedUrl}
                  className={() => `nav-link ${isPathActive(url) ? "active" : ""}`}
                  onClick={() => {
                    onClick?.(item.id, label);
                    onNavigate?.();
                  }}
                >
                  <i className={iconClass}></i>
                  {label}
                </NavLink>
              </div>
            );
          }

          return null;
        })}
      </div>
    </aside>
  );
}

export default NavBar;
