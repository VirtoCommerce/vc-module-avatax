import { computed, getCurrentScope, onScopeDispose, ref } from "vue";
import {
  JobsClient,
  PushNotificationClient,
  useApiClient,
  useNotificationStore,
  usePermissions,
  type PushNotification,
} from "@vc-shell/framework";

export type JobNotification = PushNotification & {
  jobId?: string;
  finished?: Date | string | null;
  totalCount?: number;
  processedCount?: number;
  errorCount?: number;
  errors?: string[];
};

/**
 * Follows a background job through its push notification, the way the AngularJS blades did with
 * "new-notification-event": live updates come from the host's SignalR notification store; until the store has the
 * message, a 2s poll of the notification API covers messages sent before we knew the id and types the store doesn't ingest.
 * The platform job API is polled too (it needs background_jobs:manage; without it a dead job is not detected, as in
 * AngularJS): a job that dies before it reports (state Failed/Deleted) would otherwise leave the notification
 * "running" forever. A job the user cancelled is over once the cancel is accepted (a queued one never reports again).
 */
export function useJob() {
  const store = useNotificationStore();
  const { getApiClient } = useApiClient(PushNotificationClient);
  const { getApiClient: getJobsClient } = useApiClient(JobsClient);
  const started = ref<JobNotification>();
  const polled = ref<JobNotification>();
  const deadState = ref<string>();
  const cancelled = ref<string>();
  const canReadJobs = usePermissions().hasAccess("background_jobs:manage");
  let timer: ReturnType<typeof setTimeout> | undefined;
  let run = 0; // a poll still running for an earlier follow (or after stop) doesn't re-arm

  const live = computed(
    () => store.history.value.find((x) => x.id && x.id === started.value?.id) as JobNotification | undefined,
  );

  // the most advanced snapshot wins: finished first, then the most processed
  const job = computed<JobNotification | undefined>(() => {
    const candidates = [live.value, polled.value, started.value].filter(Boolean) as JobNotification[];
    const best = candidates.find((x) => x.finished) ?? candidates.sort((a, b) => (b.processedCount ?? 0) - (a.processedCount ?? 0))[0];
    if (!best || best.finished) return best;
    if (cancelled.value) return { ...best, finished: new Date(), description: cancelled.value };
    if (!deadState.value) return best;
    const error = `Background job #${best.jobId}: ${deadState.value}`;
    return { ...best, finished: new Date(), description: error, errors: [...(best.errors ?? []), error], errorCount: (best.errorCount ?? 0) + 1 };
  });
  const finished = computed(() => !!job.value?.finished);
  const percent = computed(() =>
    job.value?.totalCount ? Math.min(100, Math.round(((job.value.processedCount ?? 0) / job.value.totalCount) * 100)) : 0,
  );

  function stop() {
    run++;
    clearTimeout(timer);
  }

  // re-armed once a poll settles, so slow responses don't stack up
  async function poll(current: number) {
    if (!started.value?.id || finished.value) return;
    try {
      if (!live.value) {
        const client = await getApiClient();
        const result = await client.searchPushNotification({ ids: [started.value.id], take: 1 });
        if (result.notifyEvents?.[0]) polled.value = result.notifyEvents[0] as JobNotification;
      }
      const jobId = job.value?.jobId;
      if (canReadJobs && jobId && !job.value?.finished) {
        const state = (await (await getJobsClient()).getStatus(jobId)).state;
        if (state === "Failed" || state === "Deleted") deadState.value = state;
      }
    } catch {
      // transient: the next tick retries
    }
    if (current === run && !finished.value) timer = setTimeout(() => poll(current), 2000);
  }

  function follow(notification: JobNotification) {
    stop();
    polled.value = undefined;
    deadState.value = undefined;
    cancelled.value = undefined;
    started.value = notification;
    const current = run;
    timer = setTimeout(() => poll(current), 2000);
  }

  if (getCurrentScope()) onScopeDispose(stop);

  /** Marks the followed job as cancelled by the user (after the cancel request succeeded), with this description. */
  const markCancelled = (description: string) => (cancelled.value = description);

  return { job, finished, percent, follow, markCancelled };
}
