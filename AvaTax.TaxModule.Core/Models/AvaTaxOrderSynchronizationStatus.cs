using System;

namespace AvaTax.TaxModule.Core.Models
{
    public class AvaTaxOrderSynchronizationStatus
    {
        public string Id { get; set; }
        public string OrderId { get; set; }

        public DateTime? LastChangeDate { get; set; }
        public DateTime? SynchronizationDate { get; set; }
    }
}