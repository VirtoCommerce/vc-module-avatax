using System;

namespace AvaTax.TaxModule.Core.Models
{
    public class AvaTaxOrderSynchronizationStatus
    {
        public bool StoreUsesAvaTax { get; set; }

        public DateTime? LastSynchronizationDate { get; set; }

        public long? TransactionId { get; set; }
        public string LinkToAvaTax { get; set; }
        public string RawContent { get; set; }

        public bool HasErrors { get; set; }
        public string[] Errors { get; set; }
    }
}