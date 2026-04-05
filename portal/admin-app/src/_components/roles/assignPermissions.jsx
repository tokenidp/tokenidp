import React, { useEffect, useMemo, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import useApiClient from "../../_hooks/useApiClient";
import InfoModal from "../common/infoModal";

const MIN_SEARCH_LENGTH = 3;

const buildTree = (permissions) => {
  const nodes = new Map();
  const roots = [];

  permissions.forEach((perm) => {
    nodes.set(perm.id, { ...perm, children: [] });
  });

  nodes.forEach((node) => {
    if (node.parentId) {
      const parent = nodes.get(node.parentId);
      if (parent) {
        parent.children.push(node);
      } else {
        roots.push(node);
      }
    } else {
      roots.push(node);
    }
  });

  const sortNodes = (items) => {
    items.sort((a, b) => (a.sequence || 0) - (b.sequence || 0));
    items.forEach((child) => sortNodes(child.children));
  };

  sortNodes(roots);
  return roots;
};

const filterTree = (node, term) => {
  const haystack = `${node.permissionName} ${node.permissionKey}`.toLowerCase();
  const match = haystack.includes(term);
  const children = node.children
    .map((child) => filterTree(child, term))
    .filter(Boolean);

  if (match || children.length) {
    return { ...node, children };
  }
  return null;
};

const getActionIds = (node) => {
  const ids = [];
  if (node.controlType?.toLowerCase() !== "link") {
    ids.push(node.id);
  }
  node.children.forEach((child) => ids.push(...getActionIds(child)));
  return ids;
};

function AssignPermissions() {
  const { roleId } = useParams();
  const navigate = useNavigate();
  const { get, put } = useApiClient();
  const [permissions, setPermissions] = useState([]);
  const [expanded, setExpanded] = useState(new Set());
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [search, setSearch] = useState("");
  const [infoOpen, setInfoOpen] = useState(false);

  useEffect(() => {
    const load = async () => {
      const response = await get("api/permissions");
      const data = response?.data?.value || response?.data || [];
      const normalized = data.map((item) => ({
        id: item.id ?? item.Id,
        parentId: item.parentId ?? item.ParentId,
        sequence: item.sequence ?? item.Sequence,
        permissionKey: item.permissionKey ?? item.PermissionKey,
        permissionName: item.permissionName ?? item.PermissionName,
        accessUrl: item.accessUrl ?? item.AccessUrl,
        controlType: item.controlType ?? item.ControlType,
      }));
      setPermissions(normalized);
    };
    load();
  }, [get]);

  const tree = useMemo(() => buildTree(permissions), [permissions]);
  const filteredTree = useMemo(() => {
    const trimmedSearch = search.trim();
    if (trimmedSearch.length < MIN_SEARCH_LENGTH) {
      return tree;
    }
    const term = trimmedSearch.toLowerCase();
    return tree.map((node) => filterTree(node, term)).filter(Boolean);
  }, [search, tree]);

  const toggleExpand = (id) => {
    setExpanded((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const togglePermission = (id, checked) => {
    setSelectedIds((prev) => {
      const next = new Set(prev);
      if (checked) {
        next.add(id);
      } else {
        next.delete(id);
      }
      return next;
    });
  };

  const toggleMenu = (node, checked) => {
    const ids = getActionIds(node);
    setSelectedIds((prev) => {
      const next = new Set(prev);
      ids.forEach((id) => {
        if (checked) {
          next.add(id);
        } else {
          next.delete(id);
        }
      });
      return next;
    });
  };

  const isMenuChecked = (node) => {
    const ids = getActionIds(node);
    if (ids.length === 0) {
      return false;
    }
    return ids.every((id) => selectedIds.has(id));
  };

  const handleSave = async () => {
    await put(`api/roles/${roleId}/permissions`, {
      permissionIds: Array.from(selectedIds),
    });
    setInfoOpen(true);
  };

  const renderNode = (node, level = 0) => {
    const hasChildren = node.children.length > 0;
    const isExpanded = expanded.has(node.id);
    const isMenu = hasChildren;

    return (
      <div key={node.id} className="permission-node">
        <div
          className={`d-flex align-items-center gap-2 py-1 permission-indent-${Math.min(
            level,
            6
          )}`}
        >
          {hasChildren && (
            <button
              type="button"
              className="btn btn-link p-0 text-secondary"
              onClick={() => toggleExpand(node.id)}
            >
              <i className={`fa fa-chevron-${isExpanded ? "down" : "right"}`}></i>
            </button>
          )}
          <input
            type="checkbox"
            className="form-check-input mt-0"
            checked={
              isMenu ? isMenuChecked(node) : selectedIds.has(node.id)
            }
            onChange={(event) =>
              isMenu
                ? toggleMenu(node, event.target.checked)
                : togglePermission(node.id, event.target.checked)
            }
          />
          <div>
            <div className="fw-semibold">{node.permissionName}</div>
            <div className="text-muted small">{node.permissionKey}</div>
          </div>
        </div>
        {hasChildren && isExpanded && (
          <div>{node.children.map((child) => renderNode(child, level + 1))}</div>
        )}
      </div>
    );
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">Assign Permissions</h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface">
        <div className="row g-3">
          <div className="col-12 col-lg-8">
            <div className="card">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-center mb-3">
                  <h6 className="card-title mb-0">Permission Tree</h6>
                  <div className="search-box permission-search">
                    <i className="fa fa-search"></i>
                    <input
                      type="text"
                      className="form-control"
                      placeholder="Search permissions (min 3 chars)"
                      value={search}
                      onChange={(event) => setSearch(event.target.value)}
                    />
                  </div>
                </div>
                <div className="permission-tree">
                  {filteredTree.length === 0 ? (
                    <div className="text-muted">No permissions found.</div>
                  ) : (
                    filteredTree.map((node) => renderNode(node))
                  )}
                </div>
              </div>
            </div>
          </div>

          <div className="col-12 col-lg-4">
            <div className="card">
              <div className="card-body">
                <h6 className="card-title">Summary</h6>
                <div className="text-muted mb-3">
                  Selected permissions: {selectedIds.size}
                </div>
                <div className="permission-summary">
                  {Array.from(selectedIds).map((id) => {
                    const perm = permissions.find((p) => p.id === id);
                    return (
                      <div key={id} className="text-muted small">
                        {perm?.permissionName || id}
                      </div>
                    );
                  })}
                  {selectedIds.size === 0 && (
                    <div className="text-muted small">None selected.</div>
                  )}
                </div>
                <div className="d-flex justify-content-end gap-2 mt-4">
                  <button className="btn btn-outline-secondary" type="button" onClick={() => navigate("/roles")}>
                    Cancel
                  </button>
                  <button className="btn btn-primary-solid" type="button" onClick={handleSave}>
                    Save Mapping
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <InfoModal
        open={infoOpen}
        title="Permissions saved"
        message="Role permissions updated successfully."
        onClose={() => setInfoOpen(false)}
      />
    </div>
  );
}

export default AssignPermissions;
