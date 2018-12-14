using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Store.Model;

namespace AvaTax.TaxModule.Core.Models
{
    public class OrderFeedEntry
    {
        public CustomerOrder CustomerOrder { get; set; }
        public Store Store { get; set; }
    }
}