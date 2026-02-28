import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const TenantsContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loading: false,
  error: "",
  lastCreatedId: null,
  statuses: [],
  externalProviders: [],
  authenticationModes: [],
  themes: [],
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
  LOOKUPS_SUCCESS: "LOOKUPS_SUCCESS",
  LIST_ERROR: "LIST_ERROR",
  CREATE_START: "CREATE_START",
  CREATE_SUCCESS: "CREATE_SUCCESS",
  CREATE_ERROR: "CREATE_ERROR",
  CLEAR_STATUS: "CLEAR_STATUS",
  CLEAR_ERROR: "CLEAR_ERROR",
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
    case actions.LOOKUPS_SUCCESS:
      return {
        ...state,
        loading: false,
        statuses: action.payload.statuses,
        externalProviders: action.payload.externalProviders,
        authenticationModes: action.payload.authenticationModes,
        themes: action.payload.themes,
      };
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    case actions.CREATE_START:
      return { ...state, loading: true, error: "", lastCreatedId: null };
    case actions.CREATE_SUCCESS:
      return { ...state, loading: false, lastCreatedId: action.payload };
    case actions.CREATE_ERROR:
      return { ...state, loading: false, error: action.payload };
    case actions.CLEAR_STATUS:
      return { ...state, error: "", lastCreatedId: null };
    case actions.CLEAR_ERROR:
      return { ...state, error: "" };
    default:
      return state;
  }
};

export const TenantsProvider = ({ children }) => {
  const { get, post, put, deleteRequest } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadTenants = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/tenant/list", search);
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
          payload: error?.message || "Failed to load tenants.",
        });
        return null;
      }
    },
    [post]
  );

  const loadLookups = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/tenant/tenantlookups");
      const result = normalizeResult(response) || {};
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: {
          statuses: result.statuses || result.Statuses || [],
          externalProviders:
            result.externalProviders || result.ExternalProviders || [],
          authenticationModes:
            result.authenticationModes || result.AuthenticationModes || [],
          themes: result.themes || result.Themes || [],
        },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load tenant lookups.",
      });
      return null;
    }
  }, [get]);

  const getTenantById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/tenant/${id}`);
        dispatch({ type: actions.CLEAR_ERROR });
        return normalizeResult(response) || null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load tenant.",
        });
        return null;
      }
    },
    [get]
  );

  const resolveTenantIdByCode = useCallback(
    async (tenantCode) => {
      if (!tenantCode) {
        return null;
      }

      try {
        const response = await post("admin/tenant/list", {
          pageNumber: 1,
          pageSize: 50,
          sortColumn: "TenantName",
          sortOrder: "asc",
          searchAll: false,
          SearchCriterias: [
            {
              ColumnName: "Search",
              Value: tenantCode,
              ColumnType: 1,
            },
          ],
        });

        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || [];
        const match = (Array.isArray(items) ? items : []).find(
          (item) =>
            String(item?.tenantCode ?? item?.TenantCode ?? "").toLowerCase() ===
            String(tenantCode).toLowerCase()
        );

        dispatch({ type: actions.CLEAR_ERROR });
        return match?.id ?? match?.Id ?? null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to resolve tenant.",
        });
        return null;
      }
    },
    [post]
  );

  const createTenant = useCallback(
    async (payload) => {
      dispatch({ type: actions.CREATE_START });
      try {
        const response = await post("admin/tenant", payload);
        const result = normalizeResult(response);
        dispatch({ type: actions.CREATE_SUCCESS, payload: result?.id || null });
        return { ok: true, result };
      } catch (error) {
        dispatch({
          type: actions.CREATE_ERROR,
          payload: error?.message || "Failed to create tenant.",
        });
        return { ok: false, error };
      }
    },
    [post]
  );

  const updateTenant = useCallback(
    async (id, payload) => {
      dispatch({ type: actions.CREATE_START });
      try {
        await put(`admin/tenant/${id}`, payload);
        dispatch({ type: actions.CREATE_SUCCESS, payload: id });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.CREATE_ERROR,
          payload: error?.message || "Failed to update tenant.",
        });
        return { ok: false, error };
      }
    },
    [put]
  );

  const deleteTenant = useCallback(
    async (id) => {
      try {
        await deleteRequest(`admin/tenant/${id}`);
        dispatch({ type: actions.CLEAR_ERROR });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to delete tenant.",
        });
        return { ok: false, error };
      }
    },
    [deleteRequest]
  );

  const clearStatus = useCallback(() => {
    dispatch({ type: actions.CLEAR_STATUS });
  }, []);

  return (
    <TenantsContext.Provider
      value={{
        state,
        loadTenants,
        loadLookups,
        getTenantById,
        resolveTenantIdByCode,
        createTenant,
        updateTenant,
        deleteTenant,
        clearStatus,
      }}
    >
      {children}
    </TenantsContext.Provider>
  );
};

export const useTenants = () => {
  const context = useContext(TenantsContext);
  if (!context) {
    throw new Error("useTenants must be used within TenantsProvider");
  }
  return context;
};
