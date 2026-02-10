import { useCallback, useEffect, useMemo } from "react";
import axios from "axios";
import { trackPromise } from "react-promise-tracker";
import { useAuth } from "@tokentresor/idp-react";
import { useGlobalError } from "./useGlobalError";

const defaultBaseURL = process.env.REACT_APP_BASE_URL;

const useApiClient = (options = {}) => {
  const user = useAuth();
  const { setError } = useGlobalError();
  const { baseURL = defaultBaseURL, skipAuth = false, track = true } = options;

  const apiClient = useMemo(() => axios.create({ baseURL }), [baseURL]);

  useEffect(() => {
    const interceptorId = apiClient.interceptors.request.use((config) => {
      const token = user?.accessToken || user?.access_token;
      console.log("accessToken:", token);
      if (!skipAuth && token && !config.headers?.Authorization) {
        config.headers = {
          ...config.headers,
          Authorization: `Bearer ${token}`,
        };
      }
      return config;
    });

    return () => apiClient.interceptors.request.eject(interceptorId);
  }, [apiClient, skipAuth, user?.accessToken]);

  const run = useCallback(
    (promise) => (track ? trackPromise(promise) : promise),
    [track],
  );

  const request = useCallback(
    async (method, endPoint, data, config) => {
      const headers = { ...(config?.headers || {}) };
      const token = user?.accessToken || user?.access_token;
      console.log("accessToken:", token);
      if (!skipAuth && token && !headers.Authorization) {
        headers.Authorization = `Bearer ${token}`;
      }
      try {
        return await run(
          apiClient.request({
            method,
            url: endPoint,
            data,
            ...config,
            headers,
          }),
        );
      } catch (error) {
        const status = error?.response?.status;
        const normalizeMessage = (value) => {
          if (value === null || value === undefined) {
            return null;
          }
          if (typeof value === "string") {
            return value;
          }
          if (Array.isArray(value)) {
            return value.map(normalizeMessage).filter(Boolean).join(", ");
          }
          if (typeof value === "object") {
            if (typeof value.message === "string") {
              return value.message;
            }
            if (value.error) {
              return normalizeMessage(value.error);
            }
            return JSON.stringify(value);
          }
          return String(value);
        };
        const message =
          normalizeMessage(error?.response?.data?.message) ||
          normalizeMessage(error?.response?.data?.error) ||
          normalizeMessage(error?.message) ||
          "Request failed.";
        setError({
          title: status ? `Request failed (${status})` : "Request failed",
          message,
        });
        throw error;
      }
    },
    [apiClient, run, setError, skipAuth, user?.token],
  );

  const get = useCallback(
    (endPoint, config) => request("get", endPoint, undefined, config),
    [request],
  );

  const post = useCallback(
    (endPoint, data, config) => request("post", endPoint, data, config),
    [request],
  );

  const put = useCallback(
    (endPoint, data, config) => request("put", endPoint, data, config),
    [request],
  );

  const patch = useCallback(
    (endPoint, data, config) => request("patch", endPoint, data, config),
    [request],
  );

  const deleteRequest = useCallback(
    (endPoint, config) => request("delete", endPoint, undefined, config),
    [request],
  );

  return { get, post, put, patch, deleteRequest };
};

export default useApiClient;
