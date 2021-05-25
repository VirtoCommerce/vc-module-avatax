using System;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Core;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.Models.PushNotifications;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;
using VirtoCommerce.SearchModule.Data.Services;


namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    [CLSCompliant(false)]
    public class OrdersSynchronizationJob
    {
        private const int BatchSize = 50;

        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IChangeLogSearchService _changeLogService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly ISettingsManager _settingsManager;

        public OrdersSynchronizationJob(IOrdersSynchronizationService ordersSynchronizationService, IPushNotificationManager pushNotificationManager,
            IChangeLogSearchService changeLogService, ICustomerOrderSearchService orderSearchService, ISettingsManager settingsManager)
        {
            _ordersSynchronizationService = ordersSynchronizationService;
            _pushNotificationManager = pushNotificationManager;
            _changeLogService = changeLogService;
            _orderSearchService = orderSearchService;
            _settingsManager = settingsManager;
        }

        [DisableConcurrentExecution(10)]
        // "DisableConcurrentExecutionAttribute" prevents to start simultaneous job payloads.
        // Should have short timeout, because this attribute implemented by following manner: newly started job falls into "processing" state immediately.
        // Then it tries to receive job lock during timeout. If the lock received, the job starts payload.
        // When the job is awaiting desired timeout for lock release, it stucks in "processing" anyway. (Therefore, you should not to set long timeouts (like 24*60*60), this will cause a lot of stucked jobs and performance degradation.)
        // Then, if timeout is over and the lock NOT acquired, the job falls into "scheduled" state (this is default fail-retry scenario).
        // Failed job goes to "Failed" state (by default) after retries exhausted.
        public async Task RunScheduled(IJobCancellationToken cancellationToken, PerformContext context)
        {
            var currentTime = DateTime.UtcNow;

            var lastRunTime = _settingsManager.GetValue(ModuleConstants.Settings.ScheduledOrdersSynchronization.LastExecutionDate.Name, (DateTime?)null);

            // NOTE: if lastRunTime is null, the order syncronization job is running first time, and it should process all orders in the database.
            //       To do so, we'll need to pass null for both startTime and endTime.
            var intervalEndTime = lastRunTime == null ? null : (DateTime?)currentTime;
            var ordersFeed = new ChangeLogBasedOrdersFeed(_changeLogService, _orderSearchService, lastRunTime, intervalEndTime, BatchSize);

            void ProgressCallback(AvaTaxOrdersSynchronizationProgress progress)
            {
            }

            await PerformOrderSynchronization(ordersFeed, ProgressCallback, cancellationToken);

            _settingsManager.SetValue(ModuleConstants.Settings.ScheduledOrdersSynchronization.LastExecutionDate.Name, currentTime);
        }

        [DisableConcurrentExecution(10)]
        public async Task RunManually(string[] orderIds, OrdersSynchronizationPushNotification notification,
            IJobCancellationToken cancellationToken, PerformContext context)
        {
            var ordersFeed = new InMemoryIndexDocumentChangeFeed(orderIds, IndexDocumentChangeType.Modified, BatchSize);

            void ProgressCallback(AvaTaxOrdersSynchronizationProgress x)
            {
                notification.Description = x.Message;
                notification.Errors = x.Errors;
                notification.ErrorCount = notification.Errors.Count;
                notification.TotalCount = x.TotalCount ?? 0;
                notification.ProcessedCount = x.ProcessedCount ?? 0;
                notification.JobId = context.BackgroundJob.Id;

                _pushNotificationManager.Send(notification);
            }

            try
            {
                await PerformOrderSynchronization(ordersFeed, ProgressCallback, cancellationToken);
            }
            catch (JobAbortedException)
            {
                //do nothing
            }
            catch (Exception ex)
            {
                notification.ErrorCount++;
                notification.Errors.Add(ex.ToString());
            }
            finally
            {
                notification.Finished = DateTime.UtcNow;
                notification.Description = "Process finished " + (notification.Errors.Any() ? "with errors" : "successfully");
                _pushNotificationManager.Send(notification);
            }
        }

        private async Task PerformOrderSynchronization(IIndexDocumentChangeFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback,
            IJobCancellationToken cancellationToken)
        {
            var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);
            await _ordersSynchronizationService.SynchronizeOrdersAsync(ordersFeed, progressCallback, cancellationTokenWrapper);
        }
    }
}
