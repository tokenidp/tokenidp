import { useCallback, useEffect, useMemo } from "react";
import axios from "axios";
import { trackPromise } from "react-promise-tracker";
import { useAuth } from "tokenidp-react";
import { useGlobalError } from "./useGlobalError";

const defaultBaseURL = process.env.REACT_APP_BASE_URL;

const useApiClient = (options = {}) => {
  const user = useAuth();
  const { setError, clearError } = useGlobalError();
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
        const response = await run(
          apiClient.request({
            method,
            url: endPoint,
            data,
            ...config,
            headers,
          }),
        );
        clearError();
        return response;
      } catch (error) {
        const status = error?.response?.status;
        const messages = [];
        const seen = new Set();
        const pushMessage = (value) => {
          if (!value) {
            return;
          }
          const text = String(value).trim();
          if (!text || seen.has(text)) {
            return;
          }
          seen.add(text);
          messages.push(text);
        };

        const collectMessages = (value) => {
          if (value === null || value === undefined) {
            return;
          }

          if (typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
            pushMessage(value);
            return;
          }

          if (Array.isArray(value)) {
            value.forEach(collectMessages);
            return;
          }

          if (typeof value !== "object") {
            return;
          }

          if (value.errors && typeof value.errors === "object" && !Array.isArray(value.errors)) {
            Object.values(value.errors).forEach(collectMessages);
          }
          if (value.Errors && typeof value.Errors === "object" && !Array.isArray(value.Errors)) {
            Object.values(value.Errors).forEach(collectMessages);
          }

          collectMessages(value.message);
          collectMessages(value.Message);
          collectMessages(value.error);
          collectMessages(value.Error);
          collectMessages(value.detail);
          collectMessages(value.Detail);
          collectMessages(value.help);
          collectMessages(value.Help);
          collectMessages(value.title);
          collectMessages(value.Title);
          collectMessages(value.errors);
          collectMessages(value.Errors);
          collectMessages(value.value);
          collectMessages(value.Value);
        };

        collectMessages(error?.response?.data);
        if (messages.length === 0) {
          pushMessage(error?.message);
        }
        if (messages.length === 0) {
          pushMessage("Request failed.");
        }

        const message = messages[0] || "Request failed.";
        setError({
          title: status ? `Request failed (${status})` : "Request failed",
          message,
          messages,
        });
        error.message = message;
        error.messages = messages;
        throw error;
      }
    },
    [apiClient, clearError, run, setError, skipAuth, user?.token],
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
