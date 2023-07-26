using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.OrdersModule.Core.Model;
using VirtoCommerce.OrdersModule.Core.Model.Search;
using VirtoCommerce.OrdersModule.Core.Services;
using VirtoCommerce.Platform.Core.ChangeLog;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Model;
using VirtoCommerce.SearchModule.Core.Services;

namespace AvaTax.TaxModule.Data.Services
{
    public class ChangeLogBasedOrdersFeed : IIndexDocumentChangeFeed
    {
        // TECHDEBT: this feed should ideally be implemented in OrdersModule...

        private readonly IChangeLogSearchService _changeLogSearchService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;
        private readonly int _take;
        private int _skip;

        public ChangeLogBasedOrdersFeed(IChangeLogSearchService changeLogSearchService, ICustomerOrderSearchService orderSearchService,
            DateTime? startDate, DateTime? endDate, int batchSize)
        {
            _changeLogSearchService = changeLogSearchService;
            _orderSearchService = orderSearchService;
            _startDate = startDate;
            _endDate = endDate;
            _skip = 0;
            _take = batchSize;
        }

        public long? TotalCount
        {
            get
            {
                if (_startDate == null && _endDate == null)
                {
                    var criteria = new CustomerOrderSearchCriteria { Skip = 0, Take = 0 };
                    var searchResult = _orderSearchService.SearchAsync(criteria).GetAwaiter().GetResult();
                    return searchResult.TotalCount;
                }
                else
                {
                    var customerOrderType = GetCustomerOrderType();

                    var searchResult = _changeLogSearchService.SearchAsync(new ChangeLogSearchCriteria
                    {
                        ObjectType = customerOrderType,
                        StartDate = _startDate,
                        EndDate = _endDate
                    }).GetAwaiter().GetResult();

                    return searchResult.Results.Select(x => x.ObjectId)
                        .Distinct()
                        .Count();
                }
            }
        }

        public Task<IReadOnlyCollection<IndexDocumentChange>> GetNextBatch()
        {
            var nextBatch = PerformGettingOrders(_skip, _take);
            _skip += _take;

            return nextBatch;
        }

        private async Task<IReadOnlyCollection<IndexDocumentChange>> PerformGettingOrders(int skip, int take)
        {
            if (_startDate == null && _endDate == null)
            {
                var searchCriteria = new CustomerOrderSearchCriteria { Skip = skip, Take = take };
                var searchResult = await _orderSearchService.SearchAsync(searchCriteria);
                return searchResult.Results
                                   .Select(x => new IndexDocumentChange
                                   {
                                       ChangeDate = x.ModifiedDate ?? DateTime.MinValue,
                                       DocumentId = x.Id,
                                       ChangeType = IndexDocumentChangeType.Modified
                                   })
                                   .ToArray();
            }
            else
            {
                var customerOrderType = GetCustomerOrderType();

                var searchResult = await _changeLogSearchService.SearchAsync(new ChangeLogSearchCriteria
                {
                    ObjectType = customerOrderType,
                    StartDate = _startDate,
                    EndDate = _endDate
                });

                return searchResult.Results.Select(x => x.ObjectId)
                    .Distinct()
                    .Skip(skip)
                    .Take(take)
                    .Select(x => new IndexDocumentChange
                    {
                        ChangeDate = DateTime.MinValue,
                        DocumentId = x,
                        ChangeType = IndexDocumentChangeType.Modified
                    })
                    .ToArray();
            }
        }

        private string GetCustomerOrderType()
        {
            var concreteOrder = AbstractTypeFactory<CustomerOrder>.TryCreateInstance();
            return concreteOrder.GetType().Name;
        }
    }
}
