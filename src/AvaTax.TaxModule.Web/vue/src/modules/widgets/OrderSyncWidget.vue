<template>
  <VcWidget
    :icon="state.icon"
    :title="state.title"
    :disabled="!order?.id"
    @click="open"
  />
</template>

<script setup lang="ts">
// The order's AvaTax state on the Orders module's blade (legacy: customerOrderDetailWidgets). The AngularJS widget turned
// LightCoral when the order changed after its last send; here the icon says so instead of a hard-coded color.
import { injectBladeContext, useBlade } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { api, type OrderSyncStatus } from "../api";

const { t } = useI18n({ useScope: "global" });
const ctx = injectBladeContext();
const order = computed(() => ctx.value.item as { id?: string; number?: string; modifiedDate?: string | Date } | undefined);
const { openBlade } = useBlade();

const status = ref<OrderSyncStatus>();
const failed = ref(false);

async function load() {
  status.value = undefined;
  failed.value = false;
  if (!order.value?.id) return;
  try {
    status.value = await api.status(order.value.id);
  } catch {
    failed.value = true;
  }
}
watch(() => order.value?.id, load, { immediate: true });

const outdated = computed(() => {
  const sent = status.value?.lastSynchronizationDate;
  const modified = order.value?.modifiedDate;
  return !!sent && !!modified && new Date(sent).getTime() < new Date(modified).getTime();
});

const state = computed(() => {
  const s = status.value;
  if (failed.value || s?.hasErrors) return { icon: "lucide-cloud-alert", title: t("AVALARA.WIDGETS.ORDER.ERROR") };
  if (!s) return { icon: "lucide-cloud", title: t("AVALARA.WIDGETS.ORDER.CHECKING") };
  if (!s.storeUsesAvaTax) return { icon: "lucide-cloud-off", title: t("AVALARA.WIDGETS.ORDER.NOT_USED") };
  if (!s.lastSynchronizationDate) return { icon: "lucide-cloud-upload", title: t("AVALARA.WIDGETS.ORDER.NOT_SENT") };
  return outdated.value
    ? { icon: "lucide-cloud-alert", title: t("AVALARA.WIDGETS.ORDER.OUTDATED") }
    : { icon: "lucide-cloud-check", title: t("AVALARA.WIDGETS.ORDER.SENT") };
});

const open = () =>
  openBlade({ name: "AvalaraOrderSyncStatus", param: order.value?.id, options: { number: order.value?.number }, onClose: load });
</script>
