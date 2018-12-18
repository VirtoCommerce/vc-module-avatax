using System;
using System.Collections.Generic;
using System.Linq;
using AvaTax.TaxModule.Core.Models;
using AvaTax.TaxModule.Core.Services;
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
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;

        public ChangeLogBasedOrdersFeed(IChangeLogService changeLogService, ICustomerOrderSearchService orderSearchService, 
            DateTime? startDate, DateTime? endDate)
        {
            _changeLogService = changeLogService;
            _orderSearchService = orderSearchService;
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
            return orders.Select(x => new OrderFeedEntry { CustomerOrderId = x });
        }

        private IEnumerable<string> PerformGettingOrders(int skip, int take)
        {
            if (_startDate == null && _endDate == null)
            {
                var searchCriteria = new CustomerOrderSearchCriteria { Skip = skip, Take = take };
                var searchResult = _orderSearchService.SearchCustomerOrders(searchCriteria);
                return searchResult.Results.Select(x => x.Id);
            }
            else
            {
                var orderIds = _changeLogService.FindChangeHistory(CustomerOrderType, _startDate, _endDate)
                                                .Select(x => x.ObjectId)
                                                .Skip(skip)
                                                .Take(take)
                                                .ToArray();
                return orderIds;
            }
        }
    }
}