import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";

const GlobalErrorContext = createContext();
const MIN_ERROR_DISPLAY_MS = 5000;

export const GlobalErrorProvider = ({ children }) => {
  const [error, setError] = useState(null);
  const clearError = useCallback(() => setError(null), []);

  useEffect(() => {
    if (!error) {
      return undefined;
    }

    const timeoutId = setTimeout(() => {
      setError(null);
    }, MIN_ERROR_DISPLAY_MS);

    return () => clearTimeout(timeoutId);
  }, [error]);

  const value = useMemo(
    () => ({
      error,
      setError,
      clearError,
    }),
    [clearError, error]
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
