import React, { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { useRoles } from "../../_hooks/useRoles";
import useTree from "../../_hooks/useTree";
import InfoModal from "../common/infoModal";

const MIN_SEARCH_LENGTH = 3;

const filterTree = (node, term) => {
  const haystack = `${node.permissionName} ${node.permissionKey}`.toLowerCase();
  const match = haystack.includes(term);
  const children = (node.childrens || [])
    .map((child) => filterTree(child, term))
    .filter(Boolean);

  if (match || children.length) {
    return { ...node, childrens: children };
  }
  return null;
};

const getActionIds = (node) => {
  const ids = [];
  if (node.controlType?.toLowerCase() !== "link") {
    ids.push(node.id);
  }
  (node.childrens || []).forEach((child) => ids.push(...getActionIds(child)));
  return ids;
};

function AddEditRole({ mode }) {
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm({
    defaultValues: {
      name: "",
      description: "",
      isActive: true,
    },
  });
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({ title: "", message: "" });
  const { createRole, updateRole, getRoleById, loadAssignablePermissions } =
    useRoles();
  const { createTree } = useTree();
  const params = useParams();
  const [roleId, setRoleId] = useState(params.roleId || null);
  const [permissions, setPermissions] = useState([]);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [search, setSearch] = useState("");
  const getField = (item, ...keys) =>
    keys.find((key) => item?.[key] !== undefined) !== undefined
      ? item[keys.find((key) => item?.[key] !== undefined)]
      : undefined;

  const onSubmit = async (data) => {
    const rolePermissions = Array.from(selectedIds)
      .map((id) => {
        const permission = permissions.find(
          (item) => getField(item, "id", "Id") === id
        );
        if (!permission) return null;
        return {
          roleId: roleId ? Number(roleId) : 0,
          tenantPermissionId: getField(permission, "id", "Id"),
          permissionKey: getField(permission, "permissionKey", "PermissionKey"),
          isAllowed: true,
        };
      })
      .filter(Boolean);

    const payload = {
      id: roleId ? Number(roleId) : 0,
      roleName: data.name.trim(),
      roleDescription: data.description.trim(),
      isActive: !!data.isActive,
      rolePermissions,
    };

    const response =
      mode === "edit" && roleId
        ? await updateRole(roleId, payload)
        : await createRole(payload);

    const createdRoleId =
      response?.value || response?.result || response?.id || response;

    if (createdRoleId) {
      setRoleId(createdRoleId);
      setInfoContent({
        title: "Role saved",
        message: "Role saved with permissions.",
      });
      setInfoOpen(true);
      return createdRoleId;
    }

    setInfoContent({
      title: mode === "edit" ? "Role updated" : "Role saved",
      message:
        mode === "edit"
          ? "Role updated successfully."
          : "Role saved with permissions.",
    });
    setInfoOpen(true);
    return null;
  };

  useEffect(() => {
    if (mode !== "edit" || !roleId) return;
    const loadRole = async () => {
      const role = await getRoleById(roleId);
      if (!role) return;
      setValue("name", getField(role, "name", "Name") ?? "");
      setValue(
        "description",
        getField(role, "roleDescription", "RoleDescription") ?? ""
      );
      setValue("isActive", getField(role, "isActive", "IsActive") ?? true);
      const rolePermissions =
        getField(role, "rolePermissions", "RolePermissions") || [];
      setSelectedIds(
        new Set(
          rolePermissions
            .filter((p) => p?.isAllowed ?? p?.IsAllowed)
            .map((p) => p?.tenantPermissionId ?? p?.TenantPermissionId)
            .filter((id) => id !== undefined && id !== null)
        )
      );
    };
    loadRole();
  }, [getRoleById, mode, roleId, setValue]);

  useEffect(() => {
    const load = async () => {
      const data = await loadAssignablePermissions();
      setPermissions(Array.isArray(data) ? data : []);
    };
    load();
  }, [loadAssignablePermissions]);

  const tree = useMemo(
    () => createTree(permissions, { allowedControlTypes: "all" }),
    [createTree, permissions]
  );

  const filteredTree = useMemo(() => {
    const trimmedSearch = search.trim();
    if (trimmedSearch.length < MIN_SEARCH_LENGTH) {
      return tree;
    }
    const term = trimmedSearch.toLowerCase();
    return tree.map((node) => filterTree(node, term)).filter(Boolean);
  }, [search, tree]);

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


  const renderPermissionItem = (node, level = 0) => {
    const hasChildren = (node.childrens || []).length > 0;
    const isMenu = hasChildren;
    const isChecked = isMenu ? isMenuChecked(node) : selectedIds.has(node.id);

    const isLeaf = !hasChildren;

    return (
      <div
        key={node.id}
        className={`permission-item ${
          isMenu ? "permission-item-group" : ""
        } permission-level-${Math.min(level, 3)}`}
      >
        <label className="permission-item-label">
          <input
            type="checkbox"
            className="form-check-input permission-checkbox"
            checked={isChecked}
            onChange={(event) =>
              isMenu
                ? toggleMenu(node, event.target.checked)
                : togglePermission(node.id, event.target.checked)
            }
          />
          <span className="permission-item-text">
            <span
              className={`permission-item-title${
                isLeaf ? " permission-item-title-normal" : ""
              }`}
            >
              {node.permissionName}
            </span>
          </span>
        </label>
        {hasChildren && (
          <div className="permission-item-children">
            {(node.childrens || []).map((child) =>
              renderPermissionItem(child, level + 1)
            )}
          </div>
        )}
      </div>
    );
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">
            {mode === "add" ? "Add Role" : "Edit Role"}
          </h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
        <div className="page-actions">
          <Link className="btn btn-outline-primary btn-sm" to="/permissions/new">
            Add Permission
          </Link>
        </div>
      </div>

      <div className="card-surface form-surface">
        <div className="row g-3">
          <div className="col-12">
            <div className="card">
              <div className="card-body">               
                <form onSubmit={handleSubmit(onSubmit)}>
                  <div className="row g-3">
                    <div className="col-12 col-md-6">
                      <label className="form-label">Role Name *</label>
                      <input
                        className={`form-control${
                          errors.name ? " is-invalid" : ""
                        }`}
                        {...register("name", { required: true })}
                        placeholder="Administrator"
                      />
                      {errors.name && (
                        <div className="error-msg">Role name is required.</div>
                      )}
                    </div>
                    <div className="col-12 col-md-6">
                      <label className="form-label">Description</label>
                      <textarea
                        className="form-control"
                        rows="3"
                        placeholder="Role description"
                        {...register("description")}
                      ></textarea>
                    </div>
                    <div className="col-12 col-md-6 d-flex align-items-end ps-md-2">
                      <div className="d-flex align-items-center gap-1 pb-1">
                        <label className="form-label mb-0">Active</label>
                        <div className="form-check form-switch app-switch account-status-switch mb-0">
                          <input
                            className="form-check-input app-switch-input"
                            type="checkbox"
                            {...register("isActive")}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
                </form>
              </div>
            </div>
          </div>
          <div className="col-12">
            <div className="card">
              <div className="card-body">
                <div className="d-flex justify-content-between align-items-center mb-3">
                  <h6 className="card-title mb-0">Assign Permissions</h6>
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
                    <div className="permission-grid">
                      {filteredTree.map((node) => (
                        <div key={node.id} className="permission-card">
                          <div className="permission-card-header">
                            <div className="permission-card-title">
                              <span className="permission-card-title-text">
                                {node.permissionName}
                              </span>
                            </div>
                            <label className="permission-select-all">
                              <input
                                type="checkbox"
                                className="form-check-input permission-checkbox"
                                checked={isMenuChecked(node)}
                                onChange={(event) =>
                                  toggleMenu(node, event.target.checked)
                                }
                              />
                              <span>Select All</span>
                            </label>
                          </div>
                          <div className="permission-card-body">
                            {(node.childrens || []).length === 0 ? (
                              <div className="text-muted small">
                                No sub-permissions found.
                              </div>
                            ) : (
                              (node.childrens || []).map((child) =>
                                renderPermissionItem(child, 0)
                              )
                            )}
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
                <div className="text-muted small mt-3">
                  Selected permissions: {selectedIds.size}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="d-flex justify-content-end gap-2 mt-4">
        <Link className="btn btn-outline-secondary" to="/roles">
          Cancel
        </Link>
        <button
          className="btn btn-primary-solid"
          type="button"
          onClick={handleSubmit(onSubmit)}
        >
          Save
        </button>
      </div>

      <InfoModal
        open={infoOpen}
        title={infoContent.title}
        message={infoContent.message}
        onClose={() => setInfoOpen(false)}
      />
    </div>
  );
}

export default AddEditRole;
