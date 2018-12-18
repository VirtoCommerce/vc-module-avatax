using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class FixedOrdersFeed : IOrdersFeed
    {
        private readonly string[] _orderIds;

        public FixedOrdersFeed(string[] orderIds)
        {
            _orderIds = orderIds;
        }

        public int GetTotalOrdersCount()
        {
            return _orderIds.Length;
        }

        public IEnumerable<OrderFeedEntry> GetOrders(int skip, int take)
        {
            var entries = new List<OrderFeedEntry>();
            if (skip < _orderIds.Length && take > 0)
            {
                var selectedOrderIds = _orderIds.Skip(skip).Take(take).ToArray();

                foreach (var orderId in selectedOrderIds)
                {
                    entries.Add(new OrderFeedEntry { CustomerOrderId = orderId });
                }
            }

            return entries;
        }
    }
}