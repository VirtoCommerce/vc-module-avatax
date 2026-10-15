import * as blades from "./pages";
import * as locales from "./locales";
import { defineAppModule, notification, registerExternalWidget, useExtensionPoint, usePermissions } from "@vc-shell/framework";
import { markRaw } from "vue";
import { api } from "./api";
import type { OrdersListToolbarCommand } from "./toolbar";
import OrderSyncWidget from "./widgets/OrderSyncWidget.vue";
import TestConnectionWidget from "./widgets/TestConnectionWidget.vue";

// The AngularJS module's widgets, all behind "tax:manage" as there
const canManage = () => usePermissions().hasAccess("tax:manage");

// "Test connection with AvaTax" on the Avalara tax provider (legacy: taxProviderDetail)...
registerExternalWidget({
  id: "AvalaraTestConnectionProvider",
  title: "Test connection with AvaTax",
  component: markRaw(TestConnectionWidget),
  targetBlades: ["TaxProviderDetails"],
  isVisible: (blade) => (blade?.options?.method as { typeName?: string } | undefined)?.typeName === "AvaTaxRateProvider" && canManage(),
});
// ...and on the platform settings group "Tax|Avalara" (legacy: settingsDetail)
registerExternalWidget({
  id: "AvalaraTestConnectionSettings",
  title: "Test connection with AvaTax",
  component: markRaw(TestConnectionWidget),
  targetBlades: ["PlatformSettingsGroup"],
  isVisible: (blade) => blade?.param === "Tax|Avalara" && canManage(),
});
// the order's AvaTax state (legacy: customerOrderDetailWidgets)
registerExternalWidget({
  id: "AvalaraOrderSync",
  title: "AvaTax",
  component: markRaw(OrderSyncWidget),
  targetBlades: ["OrdersDetails"],
  isVisible: (blade) => !!blade?.param && canManage(),
});

// "Send to AvaTax" on the orders list's toolbar (legacy: synchronizeOrdersCommand on customerOrderListController, with
// tax:manage, enabled with a selection, no confirmation): sends the selected orders, then follows the job
useExtensionPoint("orders-list-toolbar").add({
  id: "avalara-send-orders",
  priority: 2,
  component: markRaw({ render: () => null }),
  meta: {
    title: "AVALARA.TOOLBAR.SEND",
    icon: "lucide-upload",
    permissions: "tax:manage",
    disabled: (ctx) => !ctx.selection.length,
    onClick: async (ctx) => {
      if (!ctx.selection.length) return; // the toolbar's overflow menu doesn't apply "disabled"
      try {
        const sent = await api.synchronize(ctx.selection.map((x) => x.id!));
        await ctx.openBlade({ name: "AvalaraOrdersSyncProgress", options: { notification: sent } });
      } catch (e) {
        notification.error(String((e as Error).message ?? e), { timeout: 5000 });
      }
    },
  } satisfies OrdersListToolbarCommand,
});

export default defineAppModule({
  blades,
  locales,
  // legacy: the menu/history templates of "AvaTaxOrdersSynchronization" push notifications
  notifications: {
    AvaTaxOrdersSynchronization: { toast: { mode: "progress" } },
  },
});
