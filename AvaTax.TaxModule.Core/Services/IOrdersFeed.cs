using AvaTax.TaxModule.Core.Models;
using VirtoCommerce.Domain.Commerce.Model.Search;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IOrdersFeed
    {
        GenericSearchResult<OrderFeedEntry> GetOrders(int skip, int take);
    }
}