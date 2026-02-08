import React, { createContext, useContext, useReducer, useEffect } from "react";
import { LOGIN, LOGOUT } from "../_constants/actions";

const initialState = {
  isAuthenticated: false,
  userId: 0,
  tenantId: 0,
  userName: "",
  landingPage: "",
  token: "",
  refreshToken: "",
  error: "",
  permissions: [],
};

const localState = JSON.parse(localStorage.getItem("user"));
const AuthContext = createContext();

const reducer = (state, action) => {
  switch (action.type) {
    case LOGIN:
      localStorage.setItem("user", JSON.stringify(action.payload.user));
      return {
        ...state,
        ...action.payload.user,
      };
    case LOGOUT:
      localStorage.clear();
      return {
        ...state,
        ...initialState,
      };
    default:
      return state;
  }
};

export const AuthProvider = ({ children }) => {
  const [state, dispatch] = useReducer(reducer, localState || initialState);

  // useEffect(() => {
  //   dispatch({
  //     type: LOGIN,
  //     payload: {
  //       user: state,
  //     },
  //   });
  // }, []);

  const data = [state, dispatch];

  return <AuthContext.Provider value={data}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error("useAuth can only be used inside AuthProvider");
  }
  return context;
};
