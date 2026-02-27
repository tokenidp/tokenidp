import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const UsersContext = createContext();

const initialState = {
  items: [],
  roles: [],
  statuses: [],
  addressTypes: [],
  totalCount: 0,
  loading: false,
  error: "",
};

const actions = {
  LIST_START: "LIST_START",
  LIST_SUCCESS: "LIST_SUCCESS",
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
    case actions.LOOKUPS_SUCCESS:
      return {
        ...state,
        loading: false,
        roles: action.payload.roles,
        statuses: action.payload.statuses,
        addressTypes: action.payload.addressTypes,
      };
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    default:
      return state;
  }
};

export const UsersProvider = ({ children }) => {
  const { get, post, put } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadUsers = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/user/list", search);
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
          payload: error?.message || "Failed to load users.",
        });
        return null;
      }
    },
    [post]
  );

  const loadLookups = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/user/userlookups");
      const result = normalizeResult(response) || {};
      const roles =
        result.roles ||
        result.Roles ||
        result.rolesLookup ||
        result.RolesLookup ||
        [];
      const statuses =
        result.userStatuses ||
        result.UserStatuses ||
        [];
      const addressTypes =
        result.addressTypes ||
        result.AddressTypes ||
        result.addressType ||
        result.AddressType ||
        [];
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: {
          roles: Array.isArray(roles) ? roles : [],
          statuses: Array.isArray(statuses) ? statuses : [],
          addressTypes: Array.isArray(addressTypes) ? addressTypes : [],
        },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load user lookups.",
      });
      return null;
    }
  }, [get]);

  const getUserById = useCallback(
    async (id) => {
      try {
        const response = await get(`admin/user/${id}`);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to load user.",
        });
        return null;
      }
    },
    [get]
  );

  const resolveUserIdByUserName = useCallback(
    async (userName) => {
      if (!userName) {
        return null;
      }

      try {
        const response = await post("admin/user/list", {
          pageNumber: 1,
          pageSize: 50,
          sortColumn: "FullName",
          sortOrder: "asc",
          searchAll: false,
          SearchCriterias: [
            {
              ColumnName: "Search",
              Value: userName,
              ColumnType: 1,
            },
          ],
        });

        const result = normalizeResult(response) || {};
        const items = result.items || result.Items || [];
        const match = (Array.isArray(items) ? items : []).find(
          (item) =>
            String(item?.userName ?? item?.UserName ?? "").toLowerCase() ===
            String(userName).toLowerCase()
        );

        return match?.id ?? match?.Id ?? null;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to resolve user.",
        });
        return null;
      }
    },
    [post]
  );

  const createUser = useCallback(
    async (payload) => {
      try {
        const response = await post("admin/user", payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to create user.",
        });
        return null;
      }
    },
    [post]
  );

  const updateUser = useCallback(
    async (id, payload) => {
      try {
        const response = await put(`admin/user/${id}`, payload);
        return response?.data?.value || response?.data;
      } catch (error) {
        dispatch({
          type: actions.LIST_ERROR,
          payload: error?.message || "Failed to update user.",
        });
        return null;
      }
    },
    [put]
  );

  return (
    <UsersContext.Provider
      value={{
        state,
        loadUsers,
        loadLookups,
        getUserById,
        resolveUserIdByUserName,
        createUser,
        updateUser,
      }}
    >
      {children}
    </UsersContext.Provider>
  );
};

export const useUsers = () => {
  const context = useContext(UsersContext);
  if (!context) {
    throw new Error("useUsers must be used within UsersProvider");
  }
  return context;
};
