import { SettingClient, useApiClient, type ObjectSettingEntry } from "@vc-shell/framework";

// Same-origin fetch client for api/tax/avatax (cookie auth works: the host is same-origin).
async function http<T>(method: string, url: string, body?: unknown): Promise<T> {
  const res = await fetch(url, {
    method,
    credentials: "same-origin",
    headers: body === undefined ? { Accept: "application/json" } : { Accept: "application/json", "Content-Type": "application/json" },
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await res.text();
  if (!res.ok) {
    let message = text;
    try {
      const json = JSON.parse(text);
      message = typeof json === "string" ? json : (json.message ?? json.title ?? text);
    } catch {
      // not JSON: keep the text
    }
    throw new Error(message || `${res.status} ${res.statusText}`);
  }
  return (text ? JSON.parse(text) : undefined) as T;
}

export interface OrderSyncStatus {
  storeUsesAvaTax?: boolean;
  lastSynchronizationDate?: string | null;
  transactionId?: number | null;
  linkToAvaTax?: string | null;
  rawContent?: string | null;
  hasErrors?: boolean;
  errors?: string[] | null;
}

export const api = {
  // the server adds the account number and license key from its configuration
  ping: (serviceUrl: unknown, companyCode: unknown) => http<unknown>("POST", "/api/tax/avatax/ping", { serviceUrl, companyCode }),
  status: (orderId: string) => http<OrderSyncStatus>("GET", `/api/tax/avatax/orders/${encodeURIComponent(orderId)}/status`),
  synchronize: (orderIds: string[]) => http<Record<string, unknown>>("POST", "/api/tax/avatax/orders/synchronize", { orderIds }),
  cancel: (jobId: string) => http<void>("POST", `/api/tax/avatax/orders/${encodeURIComponent(jobId)}/cancel`),
  globalSettings: () =>
    useApiClient(SettingClient)
      .getApiClient()
      .then((c) => c.getGlobalModuleSettings("VirtoCommerce.AvalaraTax")),
};

export const settingValue = (settings: ObjectSettingEntry[] | undefined, name: string) => {
  const s = settings?.find((x) => x.name === name);
  return s?.value ?? s?.defaultValue ?? null;
};
