<template>
  <VcBlade
    :title="job?.title ?? $t('AVALARA.PROGRESS.TITLE')"
    :toolbar-items="toolbar"
    width="50%"
  >
    <VcContainer>
      <JobProgress
        :job="job"
        :finished="finished"
        :percent="percent"
      />
    </VcContainer>
  </VcBlade>
</template>

<script setup lang="ts">
// Progress of "Send to AvaTax" (legacy: orders-synchronization-progress blade), from its push notification
import { useAsync, useBlade, type IBladeToolbar } from "@vc-shell/framework";
import { VcBlade, VcContainer } from "@vc-shell/framework/ui";
import { computed, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { api } from "../api";
import JobProgress from "../components/JobProgress.vue";
import { useJob, type JobNotification } from "../composables/useJob";

// not routable: the job's notification comes only through options, which a page reload doesn't restore
defineBlade({
  url: "/avalara-sync-progress",
  name: "AvalaraOrdersSyncProgress",
  permissions: ["tax:manage"],
  routable: false,
});

const { t } = useI18n({ useScope: "global" });
const { options, callParent } = useBlade<{ notification: JobNotification }>();
const { job, finished, percent, follow, markCancelled } = useJob();

if (options.value?.notification) follow(options.value.notification);

// the order's AvaTax status changes once the send ends
watch(finished, (done) => done && callParent("reload").catch(() => undefined));

const { action: cancel } = useAsync(async () => {
  if (!job.value?.jobId) return;
  await api.cancel(job.value.jobId);
  markCancelled(t("AVALARA.PROGRESS.CANCELLED"));
});

const toolbar = ref<IBladeToolbar[]>([
  {
    id: "cancel",
    title: t("AVALARA.TOOLBAR.CANCEL"),
    icon: "lucide-circle-x",
    permissions: ["tax:manage"],
    disabled: computed(() => finished.value || !job.value?.jobId),
    clickHandler: () => cancel(),
  },
]);
</script>
