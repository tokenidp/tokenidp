import React, { createContext, useCallback, useContext, useReducer } from "react";
import useApiClient from "./useApiClient";

const ActivitiesContext = createContext();

const initialState = {
  items: [],
  totalCount: 0,
  loading: false,
  error: "",
  eventTypes: [],
  actorTypes: [],
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
        eventTypes: action.payload.eventTypes,
        actorTypes: action.payload.actorTypes,
      };
    case actions.LIST_ERROR:
      return { ...state, loading: false, error: action.payload };
    default:
      return state;
  }
};

export const ActivitiesProvider = ({ children }) => {
  const { get, post } = useApiClient();
  const [state, dispatch] = useReducer(reducer, initialState);

  const normalizeResult = (response) => response?.data?.value || response?.data;

  const loadActivities = useCallback(
    async (search) => {
      dispatch({ type: actions.LIST_START });
      try {
        const response = await post("admin/activity/list", search);
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
          payload: error?.message || "Failed to load activities.",
        });
        return null;
      }
    },
    [post]
  );

  const loadLookups = useCallback(async () => {
    dispatch({ type: actions.LIST_START });
    try {
      const response = await get("admin/activity/lookups");
      const result = normalizeResult(response) || {};
      dispatch({
        type: actions.LOOKUPS_SUCCESS,
        payload: {
          eventTypes: result.eventTypes || result.EventTypes || [],
          actorTypes: result.actorTypes || result.ActorTypes || [],
        },
      });
      return result;
    } catch (error) {
      dispatch({
        type: actions.LIST_ERROR,
        payload: error?.message || "Failed to load activity lookups.",
      });
      return null;
    }
  }, [get]);

  return (
    <ActivitiesContext.Provider value={{ state, loadActivities, loadLookups }}>
      {children}
    </ActivitiesContext.Provider>
  );
};

export const useActivities = () => {
  const context = useContext(ActivitiesContext);
  if (!context) {
    throw new Error("useActivities must be used within ActivitiesProvider");
  }
  return context;
};
