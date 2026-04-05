import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const ApiResourcesContext = createContext();

const initialState = {
  items: [],
  loadingApiResources: false,
  hasLoadedApiResources: false,
  loading: false,
  error: "",
};

const actions = {
  REQUEST_START: "REQUEST_START",
  REQUEST_SUCCESS: "REQUEST_SUCCESS",
  LIST_SUCCESS: "LIST_SUCCESS",
  REQUEST_ERROR: "REQUEST_ERROR",
  CLEAR_ERROR: "CLEAR_ERROR",
};

const reducer = (state, action) => {
  switch (action.type) {
    case actions.REQUEST_START:
      return {
        ...state,
        loadingApiResources: true,
        loading: true,
        error: "",
      };
    case actions.LIST_SUCCESS:
      return {
        ...state,
        loadingApiResources: false,
        hasLoadedApiResources: true,
        loading: false,
        items: action.payload,
        error: "",
      };
    case actions.REQUEST_SUCCESS:
      return { ...state, loadingApiResources: false, loading: false, error: "" };
    case actions.REQUEST_ERROR:
      return {
        ...state,
        loadingApiResources: false,
        hasLoadedApiResources: true,
        loading: false,
        error: action.payload,
      };
    case actions.CLEAR_ERROR:
      return { ...state, error: "" };
    default:
      return state;
  }
};

export const ApiResourcesProvider = ({ children }) => {
  const { get, post, put, deleteRequest } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadApiResources = useCallback(async () => {
    dispatch({ type: actions.REQUEST_START });
    try {
      const response = await get("apiresources");
      const result = normalizeResult(response) || [];
      dispatch({
        type: actions.LIST_SUCCESS,
        payload: Array.isArray(result) ? result : [],
      });
      return Array.isArray(result) ? result : [];
    } catch (error) {
      dispatch({
        type: actions.REQUEST_ERROR,
        payload: error?.message || "Failed to load ApiResources.",
      });
      return null;
    }
  }, [get]);

  const getApiResourceById = useCallback(async (id) => {
    try {
      const response = await get(`apiresources/${id}`);
      dispatch({ type: actions.REQUEST_SUCCESS });
      return normalizeResult(response) || null;
    } catch (error) {
      dispatch({
        type: actions.REQUEST_ERROR,
        payload: error?.message || "Failed to load ApiResource.",
      });
      return null;
    }
  }, [get]);

  const createApiResource = useCallback(async (payload) => {
    dispatch({ type: actions.REQUEST_START });
    try {
      const response = await post("apiresources", payload);
      dispatch({ type: actions.REQUEST_SUCCESS });
      return { ok: true, result: normalizeResult(response) };
    } catch (error) {
      dispatch({
        type: actions.REQUEST_ERROR,
        payload: error?.message || "Failed to create ApiResource.",
      });
      return { ok: false, error };
    }
  }, [post]);

  const updateApiResource = useCallback(async (id, payload) => {
    dispatch({ type: actions.REQUEST_START });
    try {
      await put(`apiresources/${id}`, payload);
      dispatch({ type: actions.REQUEST_SUCCESS });
      return { ok: true };
    } catch (error) {
      dispatch({
        type: actions.REQUEST_ERROR,
        payload: error?.message || "Failed to update ApiResource.",
      });
      return { ok: false, error };
    }
  }, [put]);

  const deleteApiResource = useCallback(async (id) => {
    dispatch({ type: actions.REQUEST_START });
    try {
      await deleteRequest(`apiresources/${id}`);
      dispatch({ type: actions.REQUEST_SUCCESS });
      return { ok: true };
    } catch (error) {
      dispatch({
        type: actions.REQUEST_ERROR,
        payload: error?.message || "Failed to delete ApiResource.",
      });
      return { ok: false, error };
    }
  }, [deleteRequest]);

  return (
    <ApiResourcesContext.Provider
      value={{
        state,
        loadApiResources,
        getApiResourceById,
        createApiResource,
        updateApiResource,
        deleteApiResource,
      }}
    >
      {children}
    </ApiResourcesContext.Provider>
  );
};

export const useApiResources = () => {
  const context = useContext(ApiResourcesContext);
  if (!context) {
    throw new Error("useApiResources must be used within ApiResourcesProvider");
  }
  return context;
};
