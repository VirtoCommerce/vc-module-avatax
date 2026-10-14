<template>
  <VcWidget
    :icon="icon"
    :title="$t('AVALARA.WIDGETS.TEST_CONNECTION')"
    :disabled="testing"
    @click="test"
  />
</template>

<script setup lang="ts">
// "Test connection with AvaTax" on the Avalara tax provider blade (its unsaved settings, from the blade context) and on
// the platform's "Tax|Avalara" settings group (that blade shares no context: the saved global values are tested, so
// not while the group has unsaved edits, which would be reported as connected or not without being tested).
import { BladeFormKey, injectBladeContext, notification, type ObjectSettingEntry } from "@vc-shell/framework";
import { VcWidget } from "@vc-shell/framework/ui";
import { computed, inject, ref } from "vue";
import { useI18n } from "vue-i18n";
import { api, settingValue } from "../api";

const { t } = useI18n({ useScope: "global" });
const ctx = (() => {
  try {
    return injectBladeContext();
  } catch {
    return undefined;
  }
})();
const form = inject(BladeFormKey, null);

const testing = ref(false);
const result = ref<"ok" | "error">();
const icon = computed(() => {
  if (testing.value) return "lucide-loader-circle";
  if (result.value === "ok") return "lucide-circle-check";
  if (result.value === "error") return "lucide-circle-alert";
  return "lucide-plug-zap";
});

async function test() {
  const provider = ctx?.value.item as { settings?: ObjectSettingEntry[] } | undefined;
  if (!provider && form?.isModified.value) {
    notification.warning(t("AVALARA.WIDGETS.SAVE_FIRST"), { timeout: 5000 });
    return;
  }
  testing.value = true;
  try {
    const settings = provider?.settings ?? (await api.globalSettings());
    await api.ping(settingValue(settings, "Avalara.Credentials.ServiceUrl"), settingValue(settings, "Avalara.Credentials.CompanyCode"));
    result.value = "ok";
    notification.success(t("AVALARA.WIDGETS.CONNECTED"));
  } catch (e) {
    result.value = "error";
    notification.error((e as Error).message, { timeout: 8000 });
  } finally {
    testing.value = false;
  }
}
</script>
