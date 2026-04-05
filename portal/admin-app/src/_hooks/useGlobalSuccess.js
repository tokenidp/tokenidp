import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";

const GlobalSuccessContext = createContext();

export const GlobalSuccessProvider = ({ children }) => {
  const [success, setSuccess] = useState(null);
  const clearSuccess = useCallback(() => setSuccess(null), []);

  useEffect(() => {
    if (!success) {
      return undefined;
    }

    const timeoutId = setTimeout(() => {
      setSuccess(null);
    }, 3000);

    return () => clearTimeout(timeoutId);
  }, [success]);

  const value = useMemo(
    () => ({
      success,
      setSuccess,
      clearSuccess,
    }),
    [clearSuccess, success]
  );

  return (
    <GlobalSuccessContext.Provider value={value}>
      {children}
    </GlobalSuccessContext.Provider>
  );
};

export const useGlobalSuccess = () => {
  const context = useContext(GlobalSuccessContext);
  if (!context) {
    throw new Error("useGlobalSuccess must be used within GlobalSuccessProvider");
  }
  return context;
};
