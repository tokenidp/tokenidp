import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const RolesContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loading: false,
  error: "",
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
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
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    default:
      return state;
  }
};

export const RolesProvider = ({ children }) => {
  const { get, post, put } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadRoles = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/role/list", search);
        const result = normalizeResult(response) || {};
        dispatch({
          type: actions.LIST_SUCCESS,
          payload: {
            items: result.items || result || [],
            totalCount: result.totalCount || 0,
          },
        });
        return result;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load roles.",
        });
        return null;
      }
    },
    [post]
  );

  const createRole = useCallback(
    async (payload) => {
      try {
        const response = await post("admin/role", payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to create role.",
        });
        return null;
      }
    },
    [post]
  );

  const updateRole = useCallback(
    async (id, payload) => {
      try {
        const response = await put(`admin/role/${id}`, payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to update role.",
        });
        return null;
      }
    },
    [put]
  );

  const getRoleById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/role/${id}`);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load role.",
        });
        return null;
      }
    },
    [get]
  );

  const resolveRoleIdByName = useCallback(
    async (roleName) => {
      if (!roleName) {
        return null;
      }

      try {
        const response = await post("admin/role/list", {
          pageNumber: 1,
          pageSize: 50,
          sortColumn: "RoleName",
          sortOrder: "asc",
          searchAll: true,
          SearchCriterias: [
            {
              ColumnName: "Search",
              Value: roleName,
              ColumnType: 1,
            },
          ],
        });

        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || result || [];
        const list = Array.isArray(items) ? items : [];
        const match = list.find(
          (item) =>
            String(item?.roleName ?? item?.RoleName ?? item?.name ?? item?.Name ?? "")
              .toLowerCase() === String(roleName).toLowerCase()
        );

        return match?.id ?? match?.Id ?? null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to resolve role.",
        });
        return null;
      }
    },
    [post]
  );

  const loadAssignablePermissions = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/permission/assign");
      const result = normalizeResult(response) || [];
      return Array.isArray(result) ? result : [];
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load assignable permissions.",
      });
      return null;
    }
  }, [get]);

  return (
    <RolesContext.Provider
      value={{
        state,
        loadRoles,
        createRole,
        updateRole,
        getRoleById,
        resolveRoleIdByName,
        loadAssignablePermissions,
      }}
    >
      {children}
    </RolesContext.Provider>
  );
};

export const useRoles = () => {
  const context = useContext(RolesContext);
  if (!context) {
    throw new Error("useRoles must be used within RolesProvider");
  }
  return context;
};
