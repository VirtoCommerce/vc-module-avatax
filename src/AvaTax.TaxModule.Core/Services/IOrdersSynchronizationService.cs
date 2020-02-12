using AvaTax.TaxModule.Core.Models;
using System;
using System.Threading.Tasks;
using VirtoCommerce.Platform.Core.Common;
using VirtoCommerce.SearchModule.Core.Services;

namespace AvaTax.TaxModule.Core.Services
{
    public interface IOrdersSynchronizationService
    {
        Task<AvaTaxOrderSynchronizationStatus> GetOrderSynchronizationStatusAsync(string orderId);
        Task SynchronizeOrdersAsync(IIndexDocumentChangeFeed ordersFeed, Action<AvaTaxOrdersSynchronizationProgress> progressCallback, ICancellationToken cancellationToken);
    }
}