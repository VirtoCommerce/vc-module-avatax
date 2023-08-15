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

        public async Task RunScheduled(IJobCancellationToken cancellationToken, PerformContext context)
        {
            var currentTime = DateTime.UtcNow;

            var lastRunTime = await _settingsManager.GetValueAsync<DateTime?>(ModuleConstants.Settings.ScheduledOrdersSynchronization.LastExecutionDate);

            // NOTE: if lastRunTime is null, the order synchronization job is running first time, and it should process all orders in the database.
            //       To do so, we'll need to pass null for both startTime and endTime.
            var intervalEndTime = lastRunTime == null ? null : (DateTime?)currentTime;
            var ordersFeed = new ChangeLogBasedOrdersFeed(_changeLogService, _orderSearchService, lastRunTime, intervalEndTime, BatchSize);

            await PerformOrderSynchronization(ordersFeed, (AvaTaxOrdersSynchronizationProgress x) => { }, cancellationToken);

            _settingsManager.SetValue(ModuleConstants.Settings.ScheduledOrdersSynchronization.LastExecutionDate.Name, currentTime);
        }

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
                await _pushNotificationManager.SendAsync(notification);
            }
        }

        private Task PerformOrderSynchronization(IIndexDocumentChangeFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback,
            IJobCancellationToken cancellationToken)
        {
            var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);
            return _ordersSynchronizationService.SynchronizeOrdersAsync(ordersFeed, progressCallback, cancellationTokenWrapper);
        }
    }
}
