import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const TokensContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loadingTokens: false,
  loadingLookups: false,
  hasLoadedTokens: false,
  loading: false,
  error: "",
  tokenTypes: [],
  statuses: [],
  clients: [],
  users: [],
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
  LOOKUPS_START: "LOOKUPS_START",
  LOOKUPS_SUCCESS: "LOOKUPS_SUCCESS",
  LIST_ERROR: "LIST_ERROR",
  LOOKUPS_ERROR: "LOOKUPS_ERROR",
};

const reducer = (state, action) => {
  switch (action.type) {
    case actions.LIST_START:
      return {
        ...state,
        loadingTokens: true,
        loading: true,
        error: "",
      };
    case actions.LIST_SUCCESS:
      return {
        ...state,
        loadingTokens: false,
        hasLoadedTokens: true,
        loading: state.loadingLookups,
        error: "",
        items: action.payload.items,
        totalCount: action.payload.totalCount,
      };
    case actions.LOOKUPS_START:
      return {
        ...state,
        loadingLookups: true,
        loading: true,
        error: "",
      };
    case actions.LOOKUPS_SUCCESS:
      return {
        ...state,
        loadingLookups: false,
        loading: state.loadingTokens,
        tokenTypes: action.payload.tokenTypes,
        statuses: action.payload.statuses,
        clients: action.payload.clients,
        users: action.payload.users,
      };
    case actions.LIST_ERROR:
      return {
        ...state,
        loadingTokens: false,
        hasLoadedTokens: true,
        loading: state.loadingLookups,
        error: action.payload,
      };
    case actions.LOOKUPS_ERROR:
      return {
        ...state,
        loadingLookups: false,
        loading: state.loadingTokens,
        error: action.payload,
      };
    default:
      return state;
  }
};

export const TokensProvider = ({ children }) => {
  const { get, post } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadTokens = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/token/list", search);
        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || result || [];
        const normalizedItems = Array.isArray(items)
          ? items.map((item) => {
              if (!item || typeof item !== "object") {
                return item;
              }
              const idValue =
                item.id ??
                item.Id ??
                item.tokenId ??
                item.TokenId ??
                item.tokenHash ??
                item.TokenHash;
              if (idValue === undefined) {
                return item;
              }
              return item.id === undefined && item.Id === undefined
                ? { ...item, id: idValue }
                : item;
            })
          : items;
        const totalCount =
          result.totalCount ||
          result.TotalCount ||
          (Array.isArray(items) ? items.length : 0);
        dispatch({
          type: actions.LIST_SUCCESS,
          payload: {
            items: normalizedItems,
            totalCount,
          },
        });
        return result;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load tokens.",
        });
        return null;
      }
    },
    [post]
  );

  const loadLookups = useCallback(async () => {
    dispatch({ type: actions.LOOKUPS_START });
    try {
      const response = await get("admin/token/lookups");
      const result = normalizeResult(response) || {};
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: {
          tokenTypes: result.tokenTypes || result.TokenTypes || [],
          statuses: result.statuses || result.Statuses || [],
          clients: result.clients || result.Clients || [],
          users: result.users || result.Users || [],
        },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LOOKUPS_ERROR,
        payload: error?.message || "Failed to load token lookups.",
      });
      return null;
    }
  }, [get]);

  const getTokenById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/token/${id}`);
        return normalizeResult(response) || null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load token details.",
        });
        return null;
      }
    },
    [get]
  );

  const revokeToken = useCallback(
    async (id, reason) => {
      try {
        await post(`admin/token/${id}/revoke`, { reason });
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to revoke token.",
        });
        return { ok: false, error };
      }
    },
    [post]
  );

  const expireToken = useCallback(
    async (id) => {
      try {
        await post(`admin/token/${id}/expire`);
        return { ok: true };
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to expire token.",
        });
        return { ok: false, error };
      }
    },
    [post]
  );

  return (
    <TokensContext.Provider
      value={{
        state,
        loadTokens,
        loadLookups,
        getTokenById,
        revokeToken,
        expireToken,
      }}
    >
      {children}
    </TokensContext.Provider>
  );
};

export const useTokens = () => {
  const context = useContext(TokensContext);
  if (!context) {
    throw new Error("useTokens must be used within TokensProvider");
  }
  return context;
};
