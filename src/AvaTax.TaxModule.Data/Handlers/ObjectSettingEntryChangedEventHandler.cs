using System.Linq;
using System.Threading.Tasks;
using AvaTax.TaxModule.Data.BackgroundJobs;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.Platform.Core.Events;
using VirtoCommerce.Platform.Core.Settings.Events;
using static AvaTax.TaxModule.Core.ModuleConstants.Settings;

namespace AvaTax.TaxModule.Data.Handlers
{
    public class ObjectSettingEntryChangedEventHandler : IEventHandler<ObjectSettingChangedEvent>
    {
        private readonly BackgroundJobsRunner _backgroundJobsRunner;

        public ObjectSettingEntryChangedEventHandler(BackgroundJobsRunner backgroundJobsRunner)
        {
            _backgroundJobsRunner = backgroundJobsRunner;
        }

        public virtual async Task Handle(ObjectSettingChangedEvent message)
        {
            if (message.ChangedEntries.Any(x => (x.EntryState == EntryState.Modified
                                              || x.EntryState == EntryState.Added)
                                  && (x.NewEntry.Name == ScheduledOrdersSynchronization.SynchronizationIsEnabled.Name
                                   || x.NewEntry.Name == ScheduledOrdersSynchronization.SynchronizationCronExpression.Name)))
            {
                await _backgroundJobsRunner.StartStopOrdersSynchronizationJob();
            }
        }
    }
}
