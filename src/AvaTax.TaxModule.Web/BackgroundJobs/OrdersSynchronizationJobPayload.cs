using AvaTax.TaxModule.Web.Models.PushNotifications;

namespace AvaTax.TaxModule.Web.BackgroundJobs
{
    /// <summary>
    /// Payload of <see cref="OrdersSynchronizationJob"/>. A payload with a <see cref="Notification"/> is a manual run for the
    /// given orders; a payload without one is the scheduled run.
    /// </summary>
    public class OrdersSynchronizationJobPayload
    {
        /// <summary>
        /// Orders to send for a manual run started from the admin UI. Ignored for a scheduled run.
        /// </summary>
        public string[] OrderIds { get; set; } = [];

        /// <summary>
        /// Push notification to report progress through, for a manual run. <c>null</c> for a scheduled run.
        /// </summary>
        public OrdersSynchronizationPushNotification Notification { get; set; }
    }
}
