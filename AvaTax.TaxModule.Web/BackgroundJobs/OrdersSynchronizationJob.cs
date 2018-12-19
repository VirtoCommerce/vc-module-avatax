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
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Search;
using VirtoCommerce.Domain.Search.ChangeFeed;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.PushNotifications;
using VirtoCommerce.Platform.Core.Settings;

namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    [CLSCompliant(false)]
    public class OrdersSynchronizationJob
    {
        private const int BatchSize = 50;

        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IChangeLogService _changeLogService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly ISettingsManager _settingsManager;

        public OrdersSynchronizationJob(IOrdersSynchronizationService ordersSynchronizationService, IPushNotificationManager pushNotificationManager, 
            IChangeLogService changeLogService, ICustomerOrderSearchService orderSearchService, ISettingsManager settingsManager)
        {
            _ordersSynchronizationService = ordersSynchronizationService;
            _pushNotificationManager = pushNotificationManager;
            _changeLogService = changeLogService;
            _orderSearchService = orderSearchService;
            _settingsManager = settingsManager;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public async Task RunScheduled(IJobCancellationToken cancellationToken, PerformContext context)
        {
            var currentTime = DateTime.UtcNow;
            var lastRunTime = _settingsManager.GetValue(ModuleConstants.Settings.Synchronization.LastExecutionDate, (DateTime?)null);

            var ordersFeed = new ChangeLogBasedOrdersFeed(_changeLogService, _orderSearchService, lastRunTime, currentTime, BatchSize);

            void ProgressCallback(AvaTaxOrdersSynchronizationProgress progress)
            {
            }

            await PerformOrderSynchronization(ordersFeed, ProgressCallback, cancellationToken);

            _settingsManager.SetValue(ModuleConstants.Settings.Synchronization.LastExecutionDate, currentTime);
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
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

                _pushNotificationManager.Upsert(notification);
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
                _pushNotificationManager.Upsert(notification);
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