import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const SettingsContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loading: false,
  saving: false,
  error: "",
  lastSaveResult: null,
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
  LIST_ERROR: "LIST_ERROR",
  SAVE_START: "SAVE_START",
  SAVE_SUCCESS: "SAVE_SUCCESS",
  SAVE_ERROR: "SAVE_ERROR",
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
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    case actions.SAVE_START:
      return { ...state, saving: true, error: "", lastSaveResult: null };
    case actions.SAVE_SUCCESS:
      return { ...state, saving: false, lastSaveResult: action.payload };
    case actions.SAVE_ERROR:
      return { ...state, saving: false, error: action.payload };
    case actions.CLEAR_ERROR:
      return { ...state, error: "" };
    default:
      return state;
  }
};

export const SettingsProvider = ({ children }) => {
  const { post, put, deleteRequest } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadSettings = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/configuration/list", search);
        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || result || [];
        const totalCount =
          result.totalCount ||
          result.TotalCount ||
          (Array.isArray(items) ? items.length : 0);
        dispatch({
          type: actions.LIST_SUCCESS,
          payload: {
            items,
            totalCount,
          },
        });
        return result;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load configurations.",
        });
        return null;
      }
    },
    [post]
  );

  const bulkSave = useCallback(
    async (items) => {
      dispatch({ type: actions.SAVE_START });
      try {
        const response = await post("admin/configuration/bulk", { Items: items });
        const result = normalizeResult(response) || {};
        dispatch({ type: actions.SAVE_SUCCESS, payload: result });
        return { ok: true, result };
      } catch (error) {
        dispatch({
          type: actions.SAVE_ERROR,
          payload: error?.message || "Failed to save configurations.",
        });
        return { ok: false, error };
      }
    },
    [post]
  );

  const updateConfiguration = useCallback(
    async (id, payload) => {
      dispatch({ type: actions.SAVE_START });
      try {
        await put(`admin/configuration/${id}`, payload);
        dispatch({ type: actions.SAVE_SUCCESS, payload: { id } });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.SAVE_ERROR,
          payload: error?.message || "Failed to update configuration.",
        });
        return { ok: false, error };
      }
    },
    [put]
  );

  const deleteConfiguration = useCallback(
    async (id) => {
      try {
        await deleteRequest(`admin/configuration/${id}`);
        dispatch({ type: actions.CLEAR_ERROR });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to delete configuration.",
        });
        return { ok: false, error };
      }
    },
    [deleteRequest]
  );

  return (
    <SettingsContext.Provider
      value={{
        state,
        loadSettings,
        bulkSave,
        updateConfiguration,
        deleteConfiguration,
      }}
    >
      {children}
    </SettingsContext.Provider>
  );
};

export const useSettings = () => {
  const context = useContext(SettingsContext);
  if (!context) {
    throw new Error("useSettings must be used within SettingsProvider");
  }
  return context;
};
