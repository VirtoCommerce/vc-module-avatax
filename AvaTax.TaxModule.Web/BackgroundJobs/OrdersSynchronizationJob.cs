using System;
using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using AvaTax.TaxModule.Web.Models;
using AvaTax.TaxModule.Web.Models.PushNotifications;
using Hangfire;
using Hangfire.Server;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    [CLSCompliant(false)]
    public class OrdersSynchronizationJob
    {
        private readonly IOrdersSynchronizationService _ordersSynchronizationService;
        private readonly IPushNotificationManager _pushNotificationManager;

        public OrdersSynchronizationJob(IOrdersSynchronizationService ordersSynchronizationService, IPushNotificationManager pushNotificationManager)
        {
            _ordersSynchronizationService = ordersSynchronizationService;
            _pushNotificationManager = pushNotificationManager;
        }

        [DisableConcurrentExecution(60 * 60 * 24)]
        public async Task Run(OrdersSynchronizationRequest request, OrdersSynchronizationPushNotification notification, 
            IJobCancellationToken cancellationToken, PerformContext context)
        {
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
                var cancellationTokenWrapper = new JobCancellationTokenWrapper(cancellationToken);
                await _ordersSynchronizationService.SynchronizeOrders(request.OrderIds, ProgressCallback, cancellationTokenWrapper);
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
    }
}