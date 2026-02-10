import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const ApplicationsContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loading: false,
  error: "",
  lastCreatedId: null,
  appTypes: [],
  clientTypes: [],
  tokenTypes: [],
  scopes: [],
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
        appTypes: action.payload.appTypes,
        clientTypes: action.payload.clientTypes,
        tokenTypes: action.payload.tokenTypes,
        scopes: action.payload.scopes,
      };
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    case actions.CREATE_START:
      return { ...state, loading: true, error: "", lastCreatedId: null };
    case actions.CREATE_SUCCESS:
      return {
        ...state,
        loading: false,
        lastCreatedId: action.payload,
      };
    case actions.CREATE_ERROR:
      return { ...state, loading: false, error: action.payload };
    case actions.CLEAR_STATUS:
      return { ...state, error: "", lastCreatedId: null };
    default:
      return state;
  }
};

export const ApplicationsProvider = ({ children }) => {
  const { get, post, put, deleteRequest } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadApplications = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/client/list", search);
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
          payload: error?.message || "Failed to load applications.",
        });
        return null;
      }
    },
    [post]
  );

  const loadLookups = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/client/clientlookups");
      const result = normalizeResult(response) || {};
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: {
          appTypes: result.appTypes || result.AppTypes || [],
          clientTypes: result.clientTypes || result.ClientTypes || [],
          tokenTypes: result.tokenTypes || result.TokenTypes || [],
          scopes: result.clientScopes || result.ClientScopes || [],
        },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load application lookups.",
      });
      return null;
    }
  }, [get]);

  const getApplicationById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/client/${id}`);
        return normalizeResult(response) || null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load application.",
        });
        return null;
      }
    },
    [get]
  );

  const createApplication = useCallback(
    async (payload) => {
      dispatch({ type: actions.CREATE_START });
      try {
        const response = await post("admin/client", payload);
        const result = normalizeResult(response);
        dispatch({ type: actions.CREATE_SUCCESS, payload: result?.id || null });
        return { ok: true, result };
      } catch (error) {
        dispatch({
          type: actions.CREATE_ERROR,
          payload: error?.message || "Failed to create application.",
        });
        return { ok: false, error };
      }
    },
    [post]
  );

  const updateApplication = useCallback(
    async (id, payload) => {
      dispatch({ type: actions.CREATE_START });
      try {
        await put(`admin/client/${id}`, payload);
        dispatch({ type: actions.CREATE_SUCCESS, payload: id });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.CREATE_ERROR,
          payload: error?.message || "Failed to update application.",
        });
        return { ok: false, error };
      }
    },
    [put]
  );

  const deleteApplication = useCallback(
    async (id) => {
      try {
        await deleteRequest(`admin/client/${id}`);
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to delete application.",
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
    <ApplicationsContext.Provider
      value={{
        state,
        loadApplications,
        loadLookups,
        getApplicationById,
        createApplication,
        updateApplication,
        deleteApplication,
        clearStatus,
      }}
    >
      {children}
    </ApplicationsContext.Provider>
  );
};

export const useApplications = () => {
  const context = useContext(ApplicationsContext);
  if (!context) {
    throw new Error("useApplications must be used within ApplicationsProvider");
  }
  return context;
};
