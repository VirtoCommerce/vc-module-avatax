using System;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Data.Services;
using AvaTax.TaxModule.Web.Models;
using AvaTax.TaxModule.Web.Models.PushNotifications;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    [CLSCompliant(false)]
    public class OrdersSynchronizationJob
    {
        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IPushNotificationManager _pushNotificationManager;
        private readonly IChangeLogService _changeLogService;
        private readonly ICustomerOrderService _orderService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly IStoreService _storeService;

        public OrdersSynchronizationJob(IOrdersSynchronizationService ordersSynchronizationService, IPushNotificationManager pushNotificationManager, 
            IChangeLogService changeLogService, ICustomerOrderService orderService, ICustomerOrderSearchService orderSearchService, IStoreService storeService)
        {
            _ordersSynchronizationService = ordersSynchronizationService;
            _pushNotificationManager = pushNotificationManager;
            _changeLogService = changeLogService;
            _orderService = orderService;
            _orderSearchService = orderSearchService;
            _storeService = storeService;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public async Task RunScheduled(IJobCancellationToken cancellationToken, PerformContext context)
        {
            var currentTime = DateTime.UtcNow;
            var lastRunTime = GetLastRunTime();

            var ordersFeed = new ChangeLogBasedOrdersFeed(_changeLogService, _orderService, _orderSearchService, _storeService, lastRunTime, currentTime);

            void ProgressCallback(AvaTaxOrdersSynchronizationProgress progress)
            {
            }

            await PerformOrderSynchronization(ordersFeed, ProgressCallback, cancellationToken);

            StoreLastRunTime(currentTime);
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public async Task RunManually(string[] orderIds, OrdersSynchronizationPushNotification notification, 
            IJobCancellationToken cancellationToken, PerformContext context)
        {
            var ordersFeed = new FixedOrdersFeed(orderIds, _orderService, _storeService);

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

        private async Task PerformOrderSynchronization(IOrdersFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback,
            IJobCancellationToken cancellationToken)
        {
            var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);
            await _ordersSynchronizationService.SynchronizeOrdersAsync(ordersFeed, progressCallback, cancellationTokenWrapper);
        }

        private DateTime GetLastRunTime()
        {
            throw new NotImplementedException();
        }

        private void StoreLastRunTime(DateTime lastRunTime)
        {
            throw new NotImplementedException();
        }
    }
}