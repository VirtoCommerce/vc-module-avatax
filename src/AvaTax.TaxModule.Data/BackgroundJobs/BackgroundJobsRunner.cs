using System.Threading.Tasks;
using Hangfire;
using VirtoCommerce.Platform.Core.Settings;
using static AvaTax.TaxModule.Core.ModuleConstants.Settings;

namespace AvaTax.TaxModule.Data.BackgroundJobs
{
    public class BackgroundJobsRunner
    {
        private readonly ISettingsManager _settingsManager;

        public BackgroundJobsRunner(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
        }

        public async Task StartStopOrdersSynchronizationJob()
        {
            var processJobEnabled = await _settingsManager.GetValueAsync(ScheduledOrdersSynchronization.SynchronizationIsEnabled.Name, (bool)ScheduledOrdersSynchronization.SynchronizationIsEnabled.DefaultValue);
            if (processJobEnabled)
            {
                var cronExpression = _settingsManager.GetValue(ScheduledOrdersSynchronization.SynchronizationCronExpression.Name, (string)ScheduledOrdersSynchronization.SynchronizationCronExpression.DefaultValue);
                RecurringJob.AddOrUpdate<OrdersSynchronizationJob>("SendOrdersToAvaTaxJob", x => x.RunScheduled(JobCancellationToken.Null, null), cronExpression);
            }
            else
            {
                RecurringJob.RemoveIfExists("SendOrdersToAvaTaxJob");
            }
        }
    }
}
