using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace AvaTax.TaxModule.Data.Model.PushNotifications
{
    public class OrdersSynchronizationPushNotification : PushNotification
    {
        public OrdersSynchronizationPushNotification(string creator)
            : base(creator)
        {
            NotifyType = "AvaTaxOrdersSynchronization";
            Errors = new List<string>();
        }

        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("finished")]
        public DateTime? Finished { get; set; }

        [JsonProperty("totalCount")]
        public long TotalCount { get; set; }
        [JsonProperty("processedCount")]
        public long ProcessedCount { get; set; }
        [JsonProperty("errorCount")]
        public long ErrorCount { get; set; }

        [JsonProperty("errors")]
        public ICollection<string> Errors { get; set; }
    }
}
