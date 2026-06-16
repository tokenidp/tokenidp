import { useCallback, useMemo, useState } from "react";
import useApiClient from "./useApiClient";

const readValue = (response) => response?.data?.value ?? response?.data ?? null;

export const useCibaRequests = () => {
  const { get, post } = useApiClient();
  const [state, setState] = useState({
    items: [],
    loading: false,
    hasLoaded: false,
    actingId: null,
    actingAction: "",
  });

  const loadPending = useCallback(async () => {
    setState((prev) => ({
      ...prev,
      loading: true,
    }));

    try {
      const response = await get("admin/backchannel-authentication/requests/pending");
      const items = readValue(response);
      setState((prev) => ({
        ...prev,
        items: Array.isArray(items) ? items : [],
        loading: false,
        hasLoaded: true,
      }));
      return Array.isArray(items) ? items : [];
    } catch (error) {
      setState((prev) => ({
        ...prev,
        loading: false,
        hasLoaded: true,
      }));
      throw error;
    }
  }, [get]);

  const approveRequest = useCallback(
    async (id) => {
      setState((prev) => ({
        ...prev,
        actingId: id,
        actingAction: "approve",
      }));

      try {
        return await post(`admin/backchannel-authentication/requests/${id}/approve`, {});
      } finally {
        setState((prev) => ({
          ...prev,
          actingId: null,
          actingAction: "",
        }));
      }
    },
    [post],
  );

  const denyRequest = useCallback(
    async (id, reason) => {
      setState((prev) => ({
        ...prev,
        actingId: id,
        actingAction: "deny",
      }));

      try {
        return await post(`admin/backchannel-authentication/requests/${id}/deny`, {
          reason: reason?.trim() || null,
        });
      } finally {
        setState((prev) => ({
          ...prev,
          actingId: null,
          actingAction: "",
        }));
      }
    },
    [post],
  );

  const actions = useMemo(
    () => ({
      loadPending,
      approveRequest,
      denyRequest,
    }),
    [approveRequest, denyRequest, loadPending],
  );

  return { state, ...actions };
};

export default useCibaRequests;
