import React, { useEffect, useMemo, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useLocation, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import { useRoles } from "../../_hooks/useRoles";
import useTree from "../../_hooks/useTree";
import { useGlobalSuccess } from "../../_hooks/useGlobalSuccess";

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

const getMenuIds = (node) => {
  const ids = [];
  if ((node.childrens || []).length > 0) {
    ids.push(node.id);
  }
  (node.childrens || []).forEach((child) => ids.push(...getMenuIds(child)));
  return ids;
};

function AddEditRole({ mode }) {
  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm({
      defaultValues: {
        name: "",
        description: "",
        isActive: true,
        isAssignableToNewUsers: false,
      },
  });
  const { setSuccess } = useGlobalSuccess();
  const {
    createRole,
    updateRole,
    getRoleById,
    resolveRoleIdByName,
    loadAssignablePermissions,
  } =
    useRoles();
  const { createTree } = useTree();
  const location = useLocation();
  const params = useParams();
  const roleKey = params.roleKey;
  const decodedRoleKey = decodeURIComponent(roleKey || "");
  const [roleId, setRoleId] = useState(location?.state?.id || null);
  const [permissions, setPermissions] = useState([]);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [search, setSearch] = useState("");
  const [expandedIds, setExpandedIds] = useState(new Set());
  const [isRoleEditable, setIsRoleEditable] = useState(true);
  const isActiveValue = watch("isActive");
  const getField = (item, ...keys) =>
    keys.find((key) => item?.[key] !== undefined) !== undefined
      ? item[keys.find((key) => item?.[key] !== undefined)]
      : undefined;

  const onSubmit = async (data) => {
    const selectedIdSet = new Set(Array.from(selectedIds).map((id) => String(id)));

    const rolePermissions =
      mode === "edit"
        ? permissions
            .map((permission) => {
              const permissionId = getField(permission, "id", "Id");
              const permissionKey = getField(
                permission,
                "permissionKey",
                "PermissionKey"
              );

              if (permissionId === undefined || permissionId === null || !permissionKey) {
                return null;
              }

              return {
                roleId: roleId ? Number(roleId) : 0,
                permissionId,
                permissionKey,
                isAllowed: selectedIdSet.has(String(permissionId)),
              };
            })
            .filter(Boolean)
        : Array.from(selectedIds)
            .map((id) => {
              const permission = permissions.find(
                (item) => String(getField(item, "id", "Id")) === String(id)
              );
              if (!permission) return null;
              return {
                roleId: roleId ? Number(roleId) : 0,
                permissionId: getField(permission, "id", "Id"),
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
        isAssignableToNewUsers:
          !!data.isActive && !!data.isAssignableToNewUsers,
        rolePermissions,
      };

    const response =
      mode === "edit" && roleId
        ? await updateRole(roleId, payload)
        : await createRole(payload);

    if (!response) {
      return null;
    }

    const createdRoleId =
      response?.value || response?.result || response?.id || response;

    if (createdRoleId) {
      setRoleId(createdRoleId);
      setSuccess({
        title: "Role saved",
        message: "Role saved with permissions.",
      });
      return createdRoleId;
    }

    setSuccess({
      title: mode === "edit" ? "Role updated" : "Role saved",
      message:
        mode === "edit"
          ? "Role updated successfully."
          : "Role saved with permissions.",
    });
    return null;
  };

  useEffect(() => {
    if (mode !== "edit") return;
    const loadRole = async () => {
      let resolvedRoleId = roleId;
      if (!resolvedRoleId && decodedRoleKey) {
        resolvedRoleId = await resolveRoleIdByName(decodedRoleKey);
        if (resolvedRoleId) {
          setRoleId(resolvedRoleId);
        }
      }

      if (!resolvedRoleId) return;

      const role = await getRoleById(resolvedRoleId);
      if (!role) return;
      setValue("name", getField(role, "name", "Name") ?? "");
      setValue(
        "description",
        getField(role, "roleDescription", "RoleDescription") ?? ""
      );
      setValue("isActive", getField(role, "isActive", "IsActive") ?? true);
      setValue(
        "isAssignableToNewUsers",
        getField(
          role,
          "isAssignableToNewUsers",
          "IsAssignableToNewUsers"
        ) ?? false
      );
      setIsRoleEditable(getField(role, "isEditable", "IsEditable") ?? true);
      const rolePermissions =
        getField(role, "rolePermissions", "RolePermissions") || [];
      setSelectedIds(
        new Set(
          rolePermissions
            .filter((p) => p?.isAllowed ?? p?.IsAllowed)
            .map((p) =>
              p?.permissionId ??
              p?.PermissionId ??
              p?.tenantPermissionId ??
              p?.TenantPermissionId
            )
            .filter((id) => id !== undefined && id !== null)
        )
      );
    };
    loadRole();
  }, [decodedRoleKey, getRoleById, mode, resolveRoleIdByName, roleId, setValue]);

  useEffect(() => {
    const load = async () => {
      const data = await loadAssignablePermissions();
      setPermissions(Array.isArray(data) ? data : []);
    };
    load();
  }, [loadAssignablePermissions]);

  useEffect(() => {
    if (!isActiveValue) {
      setValue("isAssignableToNewUsers", false, {
        shouldDirty: true,
        shouldValidate: true,
      });
    }
  }, [isActiveValue, setValue]);

  const tree = useMemo(
    () => createTree(permissions, { allowedControlTypes: "all" }),
    [permissions]
  );

  const filteredTree = useMemo(() => {
    const trimmedSearch = search.trim();
    if (trimmedSearch.length < MIN_SEARCH_LENGTH) {
      return tree;
    }
    const term = trimmedSearch.toLowerCase();
    return tree.map((node) => filterTree(node, term)).filter(Boolean);
  }, [search, tree]);

  useEffect(() => {
    setExpandedIds(new Set());
  }, [tree]);

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

  const isMenuIndeterminate = (node) => {
    const ids = getActionIds(node);
    if (ids.length === 0) {
      return false;
    }
    const selectedCount = ids.filter((id) => selectedIds.has(id)).length;
    return selectedCount > 0 && selectedCount < ids.length;
  };

  const toggleExpand = (id) => {
    setExpandedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const expandAll = () => {
    const next = new Set();
    filteredTree.forEach((node) => {
      getMenuIds(node).forEach((id) => next.add(id));
    });
    setExpandedIds(next);
  };

  const collapseAll = () => {
    setExpandedIds(new Set());
  };

  const setAllSelections = (checked) => {
    const ids = filteredTree.flatMap((node) => getActionIds(node));
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

  const renderPermissionItem = (node, level = 0) => {
    const hasChildren = (node.childrens || []).length > 0;
    const isMenu = hasChildren;
    const isChecked = isMenu ? isMenuChecked(node) : selectedIds.has(node.id);
    const isExpanded =
      search.trim().length >= MIN_SEARCH_LENGTH || expandedIds.has(node.id);
    const isIndeterminate = isMenu && isMenuIndeterminate(node);
    return (
      <div
        key={node.id}
        className={`permission-tree-node permission-level-${Math.min(level, 4)}`}
      >
        <div className="permission-tree-row">
          <div className="permission-tree-row-main">
            <span className="permission-tree-indent" style={{ width: level * 18 }} />
            {hasChildren ? (
              <button
                type="button"
                className="permission-tree-toggle"
                onClick={() => toggleExpand(node.id)}
                aria-label={isExpanded ? "Collapse" : "Expand"}
              >
                <i className={`fa fa-chevron-${isExpanded ? "down" : "right"}`}></i>
              </button>
            ) : (
              <span className="permission-tree-toggle-placeholder" />
            )}
            <input
              type="checkbox"
              className="form-check-input permission-checkbox"
              checked={isChecked}
              ref={(input) => {
                if (input) {
                  input.indeterminate = isIndeterminate;
                }
              }}
              onChange={(event) =>
                isMenu
                  ? toggleMenu(node, event.target.checked)
                  : togglePermission(node.id, event.target.checked)
              }
            />
            <span className="permission-tree-text">
              <span className={`permission-tree-title ${isMenu ? "is-menu" : ""}`}>
                {node.permissionName}
              </span>
            </span>
          </div>
        </div>
        {hasChildren && isExpanded && (
          <div className="permission-tree-children">
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
                    <div className="col-12 col-md-6 ps-md-2">
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
                    <div className="col-12 col-md-6 ps-md-2">
                      <div className="d-flex align-items-center gap-1 pb-1">
                          <label
                            className="form-label mb-0"
                            htmlFor="role-is-assignable-to-new-users"
                          >
                            Assignable To New Users
                          </label>
                        <div className="form-check form-switch app-switch account-status-switch mb-0">
                          <input
                            className="form-check-input app-switch-input"
                            type="checkbox"
                            id="role-is-assignable-to-new-users"
                            disabled={!isActiveValue || !isRoleEditable}
                            {...register("isAssignableToNewUsers")}
                          />
                        </div>
                      </div>
                      {!isActiveValue && (
                        <div className="form-text text-muted">
                          Only active roles can be assigned to new users.
                        </div>
                      )}
                      <div className="form-text text-muted">
                        Roles marked here can be assigned automatically during
                        provisioning for external login or self-registration.
                      </div>
                      {!isRoleEditable && (
                        <div className="form-text text-muted">
                          System roles cannot modify this setting.
                        </div>
                      )}
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
                  <div className="d-flex align-items-center gap-2">
                    <button
                      type="button"
                      className="btn btn-outline-secondary btn-sm"
                      onClick={expandAll}
                    >
                      Expand all
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-secondary btn-sm"
                      onClick={collapseAll}
                    >
                      Collapse all
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-primary btn-sm"
                      onClick={() => setAllSelections(true)}
                    >
                      Select all
                    </button>
                    <button
                      type="button"
                      className="btn btn-outline-secondary btn-sm"
                      onClick={() => setAllSelections(false)}
                    >
                      Clear all
                    </button>
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
                </div>
                <div className="permission-tree">
                  {filteredTree.length === 0 ? (
                    <div className="text-muted">No permissions found.</div>
                  ) : (
                    <div className="permission-tree-shell">
                      {filteredTree.map((node) => (
                        renderPermissionItem(node, 0)
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
    </div>
  );
}

export default AddEditRole;
