<template>
  <VcBlade
    :title="sent ? $t('AVALARA.STATUS.TITLE') : $t('AVALARA.STATUS.TITLE_NEW')"
    :subtitle="options?.number ?? (sent ? $t('AVALARA.STATUS.SUBTITLE') : undefined)"
    :toolbar-items="toolbar"
    :loading="loading"
    width="50%"
  >
    <VcContainer>
      <div class="avalara-status">
        <template v-if="status">
          <VcBanner
            v-if="!status.storeUsesAvaTax"
            variant="info"
            icon="lucide-cloud-off"
          >
            {{ $t("AVALARA.STATUS.NOT_USED") }}
          </VcBanner>
          <VcBanner
            v-if="status.hasErrors"
            variant="danger"
            icon="lucide-circle-alert"
          >
            {{ $t("AVALARA.STATUS.ERROR") }}
            <div
              v-for="(e, i) in status.errors ?? []"
              :key="i"
              class="avalara-status__error"
            >
              {{ e }}
            </div>
          </VcBanner>
          <VcCard
            v-if="sent"
            :header="$t('AVALARA.STATUS.TRANSACTION')"
          >
            <div class="avalara-status__card">
              <VcField
                :label="$t('AVALARA.STATUS.LAST_SENT')"
                :model-value="new Date(status.lastSynchronizationDate!).toLocaleString()"
              />
              <div v-if="status.linkToAvaTax">
                <VcLabel>{{ $t("AVALARA.STATUS.LINK") }}</VcLabel>
                <a
                  class="avalara-status__link"
                  :href="status.linkToAvaTax"
                  target="_blank"
                  rel="noopener"
                  >#{{ status.transactionId }}</a
                >
              </div>
            </div>
          </VcCard>
          <VcBanner
            v-else-if="status.storeUsesAvaTax && !status.hasErrors"
            variant="info"
            icon="lucide-cloud-upload"
          >
            {{ $t("AVALARA.STATUS.NOT_SENT") }}
          </VcBanner>
          <VcCard
            v-if="status.rawContent"
            :header="$t('AVALARA.STATUS.RAW')"
            is-collapsable
          >
            <pre class="avalara-status__raw">{{ status.rawContent }}</pre>
          </VcCard>
        </template>
      </div>
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
// The order's AvaTax transaction (legacy: order-synchronization-status blade), and "Send to AvaTax" for this order
import { useAsync, useBlade, useLoading, type IBladeToolbar } from "@vc-shell/framework";
import { VcBanner, VcBlade, VcCard, VcContainer, VcField, VcLabel } from "@vc-shell/framework/ui";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { api, type OrderSyncStatus } from "../api";

defineBlade({
  url: "/avalara-order",
  name: "AvalaraOrderSyncStatus",
  permissions: ["tax:manage"],
});

const { t } = useI18n({ useScope: "global" });
const { param, options, openBlade, exposeToChildren } = useBlade<{ number?: string }>();

const status = ref<OrderSyncStatus>();
const sent = computed(() => !!status.value?.storeUsesAvaTax && !!status.value.lastSynchronizationDate);

const { loading: loadLoading, action: load } = useAsync(async () => {
  status.value = await api.status(param.value!);
});

const { loading: sendLoading, action: send } = useAsync(async () => {
  const notification = await api.synchronize([param.value!]);
  await openBlade({ name: "AvalaraOrdersSyncProgress", options: { notification } });
});

const loading = useLoading(loadLoading, sendLoading);

const toolbar = ref<IBladeToolbar[]>([
  {
    id: "send",
    title: t("AVALARA.TOOLBAR.SEND"),
    icon: "lucide-upload",
    permissions: ["tax:manage"],
    clickHandler: () => send(),
  },
  { id: "refresh", title: t("AVALARA.TOOLBAR.REFRESH"), icon: "lucide-refresh-cw", clickHandler: () => load() },
]);

// the progress blade reloads the status once the send finishes
exposeToChildren({ reload: () => load() });

load();
</script>

<style lang="scss">
.avalara-status {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px 22px 36px;
  &__card {
    display: flex;
    flex-direction: column;
    gap: 16px;
    padding: 16px;
  }
  &__link {
    display: block;
    margin-top: 6px;
    color: var(--color-primary-500);
  }
  &__error {
    margin-top: 6px;
    white-space: pre-wrap;
  }
  &__raw {
    margin: 0;
    padding: 16px;
    max-height: 480px;
    overflow: auto;
    font-family: ui-monospace, monospace;
    font-size: 12px;
    color: var(--color-neutral-700);
  }
}
</style>
