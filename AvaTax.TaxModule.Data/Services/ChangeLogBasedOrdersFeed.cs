using System;
using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
using VirtoCommerce.Domain.Commerce.Model.Search;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Store.Services;
using VirtoCommerce.Platform.Core.ChangeLog;

namespace AvaTax.TaxModule.Data.Services
{
    public class ChangeLogBasedOrdersFeed : IOrdersFeed
    {
        private const string CustomerOrderType = "CustomerOrderEntity";

        private readonly IChangeLogService _changeLogService;
        private readonly ICustomerOrderService _orderService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly IStoreService _storeService;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;

        public ChangeLogBasedOrdersFeed(IChangeLogService changeLogService, ICustomerOrderService orderService, ICustomerOrderSearchService orderSearchService,
            IStoreService storeService, DateTime? startDate, DateTime? endDate)
        {
            _changeLogService = changeLogService;
            _orderService = orderService;
            _orderSearchService = orderSearchService;
            _storeService = storeService;
            _startDate = startDate;
            _endDate = endDate;
        }

        public int GetTotalOrdersCount()
        {
            if (_startDate == null || _endDate == null)
            {
                var criteria = new CustomerOrderSearchCriteria { Skip = 0, Take = 0 };
                var searchResult = _orderSearchService.SearchCustomerOrders(criteria);
                return searchResult.TotalCount;
            }
            else
            {
                return _changeLogService.FindChangeHistory(CustomerOrderType, _startDate, _endDate).Count();
            }
        }

        public IEnumerable<OrderFeedEntry> GetOrders(int skip, int take)
        {
            var orders = PerformGettingOrders(skip, take);

            var orderGroups = orders.GroupBy(x => x.StoreId);

            var storeIds = orderGroups.Select(x => x.Key).ToArray();
            var stores = _storeService.GetByIds(storeIds).ToDictionary(x => x.Id, x => x);

            var results = new List<OrderFeedEntry>();
            foreach (var orderGroup in orderGroups)
            {
                var store = stores[orderGroup.Key];

                var avaTaxProvider = store.TaxProviders.FirstOrDefault(x => x.Code == "AvaTaxRateProvider");
                if (avaTaxProvider != null && avaTaxProvider.IsActive)
                {
                    foreach (var order in orderGroup)
                    {
                        var entry = new OrderFeedEntry
                        {
                            CustomerOrder = order,
                            Store = store
                        };
                        results.Add(entry);
                    }
                }
            }

            return results;
        }

        private IEnumerable<CustomerOrder> PerformGettingOrders(int skip, int take)
        {
            if (_startDate == null && _endDate == null)
            {
                var searchCriteria = new CustomerOrderSearchCriteria { Skip = skip, Take = take };
                var searchResult = _orderSearchService.SearchCustomerOrders(searchCriteria);
                return searchResult.Results;
            }
            else
            {
                var logEntries = _changeLogService.FindChangeHistory(CustomerOrderType, _startDate, _endDate).Skip(skip).Take(take).ToArray();
                var orderIds = logEntries.Select(x => x.ObjectId).ToArray();
                var orders = _orderService.GetByIds(orderIds);

                return orders;
            }
        }
    }
}