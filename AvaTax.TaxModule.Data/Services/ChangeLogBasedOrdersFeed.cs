using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Order.Model;
using VirtoCommerce.Domain.Order.Services;
using VirtoCommerce.Domain.Search;
using VirtoCommerce.Domain.Search.ChangeFeed;
using VirtoCommerce.Platform.Core.ChangeLog;

namespace AvaTax.TaxModule.Data.Services
{
    public class ChangeLogBasedOrdersFeed : IIndexDocumentChangeFeed
    {
        // TECHDEBT: this feed should ideally be implemented in OrdersModule...

        private const string CustomerOrderType = "CustomerOrderEntity";

        private readonly IChangeLogService _changeLogService;
        private readonly ICustomerOrderSearchService _orderSearchService;
        private readonly DateTime? _startDate;
        private readonly DateTime? _endDate;
        private readonly int _take;
        private int _skip;

        public ChangeLogBasedOrdersFeed(IChangeLogService changeLogService, ICustomerOrderSearchService orderSearchService, 
            DateTime? startDate, DateTime? endDate, int batchSize)
        {
            _changeLogService = changeLogService;
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
                if (_startDate == null || _endDate == null)
                {
                    var criteria = new CustomerOrderSearchCriteria { Skip = 0, Take = 0 };
                    var searchResult = _orderSearchService.SearchCustomerOrders(criteria);
                    return searchResult.TotalCount;
                }
                else
                {
                    return _changeLogService.FindChangeHistory(CustomerOrderType, _startDate, _endDate)
                                            .Select(x => x.ObjectId)
                                            .Distinct()
                                            .Count();
                }
            }
        }

        public Task<IReadOnlyCollection<IndexDocumentChange>> GetNextBatch()
        {
            var nextBatch = PerformGettingOrders(_skip, _take);
            _skip += _take;

            return Task.FromResult(nextBatch);
        }

        private IReadOnlyCollection<IndexDocumentChange> PerformGettingOrders(int skip, int take)
        {
            if (_startDate == null && _endDate == null)
            {
                var searchCriteria = new CustomerOrderSearchCriteria { Skip = skip, Take = take };
                var searchResult = _orderSearchService.SearchCustomerOrders(searchCriteria);
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
                return _changeLogService.FindChangeHistory(CustomerOrderType, _startDate, _endDate)
                                        .Select(x => x.ObjectId)
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
    }
}