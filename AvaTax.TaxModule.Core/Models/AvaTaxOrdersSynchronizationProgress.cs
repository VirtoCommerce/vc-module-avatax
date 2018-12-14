using System.Collections.Generic;

namespace AvaTax.TaxModule.Core.Models
{
    public class AvaTaxOrdersSynchronizationProgress
    {
        public AvaTaxOrdersSynchronizationProgress()
        {
            Errors = new List<string>();
        }

        public string Message { get; set; }

        public long? TotalCount { get; set; }
        public long? ProcessedCount { get; set; }

        public ICollection<string> Errors { get; set; }
    }
}