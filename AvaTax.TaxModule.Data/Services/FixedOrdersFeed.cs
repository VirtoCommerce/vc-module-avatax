using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class FixedOrdersFeed : IOrdersFeed
    {
        private readonly string[] _orderIds;
        private readonly ICustomerOrderService _orderService;
        private readonly IStoreService _storeService;

        public FixedOrdersFeed(string[] orderIds, ICustomerOrderService orderService, IStoreService storeService)
        {
            _orderIds = orderIds;
            _orderService = orderService;
            _storeService = storeService;
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
                var orders = _orderService.GetByIds(selectedOrderIds, string.Empty);
                var storeIds = orders.Select(order => order.StoreId).Distinct().ToArray();
                var stores = _storeService.GetByIds(storeIds).ToDictionary(x => x.Id, x => x);

                // NOTE: this feed implementation is used for manual orders synchronization.
                //       That's why we don't filter stores that do not use AvaTax here - this will cause
                //       the orders synchronization service to add an error message about store not using AvaTax,
                //       so that the user could see it and reconfigure the store if needed.

                foreach (var order in orders)
                {
                    var store = stores[order.StoreId];
                    entries.Add(new OrderFeedEntry { CustomerOrder = order, Store = store });
                }
            }

            return entries;
        }
    }
}