using System;
using System.Collections.Generic;
using VirtoCommerce.Platform.Core.PushNotifications;

namespace AvaTax.TaxModule.Web.Models.PushNotifications
{
    public class OrdersSynchronizationPushNotification : PushNotification
    {
        public OrdersSynchronizationPushNotification(string creator) 
            : base(creator)
        {
            NotifyType = "AvaTaxOrdersSynchronization";
            Errors = new List<string>();
        }

        public string JobId { get; set; }

        public DateTime? Finished { get; set; }

        public long TotalCount { get; set; }
        public long ProcessedCount { get; set; }
        public long ErrorCount { get; set; }

        public ICollection<string> Errors { get; set; }
    }
}