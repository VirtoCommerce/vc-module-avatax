using System.Collections.Generic;
using AvaTax.TaxModule.Core.Models;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IOrdersFeed
    {
        int GetTotalOrdersCount();
        IEnumerable<OrderFeedEntry> GetOrders(int skip, int take);
    }
}