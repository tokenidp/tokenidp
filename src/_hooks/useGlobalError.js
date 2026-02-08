import React, { createContext, useContext, useMemo, useState } from "react";

const GlobalErrorContext = createContext();

export const GlobalErrorProvider = ({ children }) => {
  const [error, setError] = useState(null);

  const value = useMemo(
    () => ({
      error,
      setError,
      clearError: () => setError(null),
    }),
    [error]
  );

  return (
    <GlobalErrorContext.Provider value={value}>
      {children}
    </GlobalErrorContext.Provider>
  );
};

export const useGlobalError = () => {
  const context = useContext(GlobalErrorContext);
  if (!context) {
    throw new Error("useGlobalError must be used within GlobalErrorProvider");
  }
  return context;
};
