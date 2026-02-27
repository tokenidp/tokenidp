import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const PermissionsContext = createContext();

const initialState = {
  items: [],
  parents: [],
  controlTypes: [],
  totalCount: 0,
  loading: false,
  error: "",
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
  PARENTS_SUCCESS: "PARENTS_SUCCESS",
  LOOKUPS_SUCCESS: "LOOKUPS_SUCCESS",
  LIST_ERROR: "LIST_ERROR",
};

const reducer = (state, action) => {
  switch (action.type) {
    case actions.LIST_START:
      return { ...state, loading: true, error: "" };
    case actions.LIST_SUCCESS:
      return {
        ...state,
        loading: false,
        items: action.payload.items,
        totalCount: action.payload.totalCount,
      };
    case actions.PARENTS_SUCCESS:
      return { ...state, loading: false, parents: action.payload };
    case actions.LOOKUPS_SUCCESS:
      return {
        ...state,
        loading: false,
        parents: action.payload.parents,
        controlTypes: action.payload.controlTypes,
      };
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    default:
      return state;
  }
};

export const PermissionsProvider = ({ children }) => {
  const { get, post, put } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadPermissions = useCallback(async (search) => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await post("admin/permission/list", search);
      const result = normalizeResult(response) || {};
      const items = result.items || result.Items || result || [];
      const totalCount =
        result.totalCount ||
        result.TotalCount ||
        (Array.isArray(items) ? items.length : 0);
      dispatch({
        type: actions.LIST_SUCCESS,
        payload: { items, totalCount },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load permissions.",
      });
      return null;
    }
  }, [post]);

  const loadAssignablePermissions = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/permission/assign");
      const result = normalizeResult(response) || [];
      const items = Array.isArray(result) ? result : [];
      dispatch({
        type: actions.LIST_SUCCESS,
        payload: { items, totalCount: items.length },
      });
      return items;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load assignable permissions.",
      });
      return null;
    }
  }, [get]);

  const loadParents = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/permission/lookups");
      const result = normalizeResult(response) || {};
      const parents =
        result.parentMenus || result.ParentMenus || result.parents || [];
      const controlTypes =
        result.controlTypes || result.ControlTypes || [];
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: { parents, controlTypes },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load permission lookups.",
      });
      return null;
    }
  }, [get]);

  const createPermission = useCallback(
    async (payload) => {
      try {
        const response = await post("admin/permission", payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to create permission.",
        });
        return null;
      }
    },
    [post]
  );

  const updatePermission = useCallback(
    async (id, payload) => {
      try {
        const response = await put(`admin/permission/${id}`, payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to update permission.",
        });
        return null;
      }
    },
    [put]
  );

  const getPermissionById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/permission/${id}`);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load permission.",
        });
        return null;
      }
    },
    [get]
  );

  const resolvePermissionIdByKey = useCallback(
    async (permissionKey) => {
      if (!permissionKey) {
        return null;
      }

      try {
        const response = await post("admin/permission/list", {
          pageNumber: 1,
          pageSize: 50,
          sortColumn: "Sequence",
          sortOrder: "asc",
          searchAll: false,
          SearchCriterias: [
            {
              ColumnName: "PermissionKey",
              Value: permissionKey,
              ColumnType: 1,
            },
          ],
        });

        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || [];
        const list = Array.isArray(items) ? items : [];
        const match = list.find(
          (item) =>
            String(item?.permissionKey ?? item?.PermissionKey ?? "").toLowerCase() ===
            String(permissionKey).toLowerCase()
        );

        return match?.id ?? match?.Id ?? null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to resolve permission.",
        });
        return null;
      }
    },
    [post]
  );

  return (
    <PermissionsContext.Provider
      value={{
        state,
        loadPermissions,
        loadParents,
        loadAssignablePermissions,
        createPermission,
        updatePermission,
        getPermissionById,
        resolvePermissionIdByKey,
      }}
    >
      {children}
    </PermissionsContext.Provider>
  );
};

export const usePermissions = () => {
  const context = useContext(PermissionsContext);
  if (!context) {
    throw new Error("usePermissions must be used within PermissionsProvider");
  }
  return context;
};
